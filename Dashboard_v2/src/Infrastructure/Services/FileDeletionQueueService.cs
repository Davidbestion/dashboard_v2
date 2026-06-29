using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.FileStorage;
using Dashboard_v2.Domain.Entities;

namespace Dashboard_v2.Infrastructure.Services;

/// <summary>
/// Implementación de <see cref="IFileDeletionQueueService"/> que inserta un
/// <see cref="FileDeletionJob"/> en el contexto de EF Core sin llamar a <c>SaveChangesAsync</c>.
///
/// <para>
/// El servicio de aplicación (p.ej. <c>AwardService</c>) es responsable de llamar a
/// <c>SaveChangesAsync</c> después de encolar el job. De esta forma el job y el cambio
/// en la entidad se guardan en la <b>misma transacción</b>, garantizando consistencia:
/// <list type="bullet">
///   <item>Si el guardado de la entidad falla, el job tampoco queda en la BD.</item>
///   <item>Si el job se guarda, la entidad también — MinIO recibirá eventualmente la orden de borrado.</item>
/// </list>
/// </para>
///
/// <para>Ciclo de vida: <b>Scoped</b> (al igual que <see cref="IApplicationDbContext"/>).</para>
/// </summary>
public sealed class FileDeletionQueueService : IFileDeletionQueueService
{
    private readonly IApplicationDbContext _context;
    private readonly TimeProvider _timeProvider;

    public FileDeletionQueueService(IApplicationDbContext context, TimeProvider timeProvider)
    {
        _context      = context;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc/>
    public Task EnqueueAsync(StoredFile file, CancellationToken ct = default)
    {
        // Crear el job copiando ObjectKey y BucketName para que el background service
        // pueda borrar el objeto en MinIO aunque el StoredFile sea eliminado antes.
        var job = new FileDeletionJob
        {
            StoredFileId = file.Id,
            ObjectKey    = file.ObjectKey,
            BucketName   = file.BucketName,
            ScheduledAt  = _timeProvider.GetUtcNow().UtcDateTime,
        };

        // Solo añadimos al contexto; el llamador llama a SaveChangesAsync.
        _context.FileDeletionJobs.Add(job);

        return Task.CompletedTask;
    }
}
