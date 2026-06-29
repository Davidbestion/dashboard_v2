using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.FileStorage;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dashboard_v2.Infrastructure.BackgroundServices;

/// <summary>
/// Servicio en segundo plano que procesa la cola de borrado diferido de archivos en MinIO.
///
/// <para>
/// <b>Por qué existe este servicio:</b> cuando un usuario quita o reemplaza el archivo
/// adjunto de una entidad, la referencia en la BD se actualiza de forma inmediata en la
/// misma transacción (patrón Transactional Outbox). Sin embargo, el objeto en MinIO NO se
/// borra en ese momento, para no acoplar la operación de edición a la disponibilidad del
/// servidor MinIO. En su lugar, se crea un <c>FileDeletionJob</c> que este servicio procesa
/// de forma asíncrona.
/// </para>
///
/// <para>
/// <b>Flujo de procesamiento por iteración:</b>
/// <list type="number">
///   <item>Consulta todos los <c>FileDeletionJob</c> pendientes.</item>
///   <item>Para cada job, calcula el retardo con backoff exponencial:
///   <c>min(RetryDelaySeconds × 2^Attempts, MaxRetryDelaySeconds)</c>.
///   Si aún no ha pasado ese tiempo desde el último intento, lo omite.</item>
///   <item>Si el borrado es exitoso, elimina el registro <c>StoredFile</c>
///   (si aún existe) y el propio job.</item>
///   <item>Si MinIO falla, incrementa <c>Attempts</c> y actualiza <c>LastAttemptAt</c>.
///   El job permanece en la cola para ser reintentado con un retardo mayor.</item>
/// </list>
/// </para>
///
/// <para>
/// <b>Alertas:</b> cuando <c>Attempts</c> alcanza <see cref="FileDeletionOptions.MaxAttempts"/>,
/// se emite un aviso <c>Critical</c> en el log para alertar al operador. El job sigue
/// reintentándose indefinidamente hasta que MinIO vuelva a estar disponible.
/// </para>
///
/// <para>
/// <b>Ciclo de vida:</b> este servicio es un <see cref="BackgroundService"/> (singleton),
/// por lo que usa <see cref="IServiceScopeFactory"/> para crear un scope de DI por iteración
/// y resolver servicios con ciclo de vida Scoped como <see cref="IApplicationDbContext"/>.
/// </para>
///
/// <para>
/// <b>Configuración:</b> ver <see cref="FileDeletionOptions"/> (sección <c>FileDeletion</c>
/// en <c>appsettings.json</c>).
/// </para>
/// </summary>
public sealed class FileDeletionBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FileDeletionBackgroundService> _logger;
    private readonly FileDeletionOptions _options;
    private readonly TimeProvider _timeProvider;

    public FileDeletionBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<FileDeletionBackgroundService> logger,
        IOptions<FileDeletionOptions> options,
        TimeProvider timeProvider)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
        _options      = options.Value;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Bucle principal del servicio. Se ejecuta indefinidamente hasta que el host se detiene.
    /// Cada iteración duerme <see cref="FileDeletionOptions.IntervalSeconds"/> segundos antes
    /// de procesar la cola.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "FileDeletionBackgroundService iniciado. Intervalo: {Interval}s, UmbralAviso: {Max} intentos, " +
            "RetardoBase: {Retry}s, RetardoMáximo: {MaxDelay}s.",
            _options.IntervalSeconds, _options.MaxAttempts,
            _options.RetryDelaySeconds, _options.MaxRetryDelaySeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.IntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await ProcessPendingJobsAsync(stoppingToken);
        }

        _logger.LogInformation("FileDeletionBackgroundService detenido.");
    }

    /// <summary>
    /// Procesa todos los <c>FileDeletionJob</c> pendientes dentro de un scope de DI propio.
    /// Cada job se procesa de forma independiente: un fallo en uno no impide procesar el siguiente.
    /// </summary>
    /// <remarks>
    /// Marcado como <c>internal</c> para permitir su llamada directa desde los tests unitarios
    /// sin tener que iniciar el bucle completo de <see cref="ExecuteAsync"/>.
    /// </remarks>
    internal async Task ProcessPendingJobsAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context     = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        List<FileDeletionJob> jobs;
        try
        {
            // Sin filtro por Attempts: los jobs permanecen en cola indefinidamente hasta
            // que MinIO los procese, usando backoff exponencial para espaciar los reintentos.
            jobs = await context.FileDeletionJobs
                .OrderBy(j => j.ScheduledAt)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al leer FileDeletionJobs de la base de datos.");
            return;
        }

        if (jobs.Count == 0)
            return;

        _logger.LogDebug("FileDeletionBackgroundService: procesando {Count} jobs pendientes.", jobs.Count);

        foreach (var job in jobs)
        {
            if (ct.IsCancellationRequested)
                break;

            // Backoff exponencial: min(base × 2^intentos, techo)
            if (job.LastAttemptAt.HasValue)
            {
                var delaySeconds = Math.Min(
                    _options.RetryDelaySeconds * Math.Pow(2, job.Attempts),
                    _options.MaxRetryDelaySeconds);

                if (job.LastAttemptAt.Value.AddSeconds(delaySeconds) > now)
                    continue;
            }

            await ProcessSingleJobAsync(context, fileStorage, job, ct);
        }
    }

    /// <summary>
    /// Intenta borrar el objeto MinIO correspondiente a un job y actualiza su estado.
    /// </summary>
    private async Task ProcessSingleJobAsync(
        IApplicationDbContext context,
        IFileStorageService fileStorage,
        FileDeletionJob job,
        CancellationToken ct)
    {
        try
        {
            await fileStorage.DeleteAsync(job.ObjectKey, ct);

            _logger.LogInformation(
                "FileDeletionJob {JobId}: objeto '{Key}' del bucket '{Bucket}' eliminado de MinIO.",
                job.Id, job.ObjectKey, job.BucketName);

            if (job.StoredFileId.HasValue)
            {
                var storedFile = await context.StoredFiles.FindAsync(
                    new object[] { job.StoredFileId.Value }, ct);

                if (storedFile is not null)
                    context.StoredFiles.Remove(storedFile);
            }

            context.FileDeletionJobs.Remove(job);
            await context.SaveChangesAsync(ct);
        }
        catch (FileStorageUnavailableException ex)
        {
            var attempt = job.Attempts + 1;
            _logger.LogWarning(
                ex,
                "FileDeletionJob {JobId}: MinIO no disponible al intentar borrar '{Key}' (intento {Attempt}). Se reintentará.",
                job.Id, job.ObjectKey, attempt);

            await IncrementAttemptsAsync(context, job, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "FileDeletionJob {JobId}: error inesperado al procesar '{Key}'. El job se reintentará.",
                job.Id, job.ObjectKey);

            await IncrementAttemptsAsync(context, job, ct);
        }
    }

    /// <summary>
    /// Incrementa el contador de reintentos del job y persiste el cambio.
    /// Si se alcanza <see cref="FileDeletionOptions.MaxAttempts"/>, emite un aviso <c>Critical</c>
    /// para alertar al operador, pero el job permanece en cola para seguir reintentándose.
    /// Aísla el guardado del contador para que un fallo de BD no interrumpa el procesamiento de otros jobs.
    /// </summary>
    private async Task IncrementAttemptsAsync(
        IApplicationDbContext context,
        FileDeletionJob job,
        CancellationToken ct)
    {
        job.Attempts++;
        job.LastAttemptAt = _timeProvider.GetUtcNow().UtcDateTime;

        if (job.Attempts == _options.MaxAttempts)
        {
            var nextDelaySeconds = Math.Min(
                _options.RetryDelaySeconds * Math.Pow(2, job.Attempts),
                _options.MaxRetryDelaySeconds);

            _logger.LogCritical(
                "FileDeletionJob {JobId}: {Max} intentos fallidos para '{Key}' en bucket '{Bucket}'. " +
                "MinIO lleva caído demasiado tiempo. Próximo reintento en {Next:F0}s.",
                job.Id, job.Attempts, job.ObjectKey, job.BucketName, nextDelaySeconds);
        }

        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (Exception saveEx)
        {
            _logger.LogError(saveEx,
                "FileDeletionJob {JobId}: error al persistir el contador de intentos.", job.Id);
        }
    }
}
