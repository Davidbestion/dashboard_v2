using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.FileStorage;
using Dashboard_v2.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace Dashboard_v2.Infrastructure.Services;

/// <summary>
/// File storage implementation backed by MinIO (S3-compatible). Handles upload, download,
/// URL generation, and deletion of evidence files and certificates.
/// All MinIO communication resides here; Application layer depends only on <see cref="IFileStorageService"/>.
///
/// <para>
/// <b>Detección de MinIO caído:</b> el SDK de MinIO 6.x puede silenciar errores de red y retornar
/// sin excepción aunque el servidor esté caído. Por eso, antes de cada operación se hace un ping
/// HTTP directo al endpoint de salud de MinIO (<c>/minio/health/live</c>) mediante un
/// <see cref="HttpClient"/> dedicado que sí propaga las excepciones de red correctamente.
/// Solo si el ping tiene éxito se invoca el SDK.
/// </para>
/// </summary>
public sealed class MinioFileStorageService : IFileStorageService, IStorageBucketInitialiser
{
    /// <summary>Nombre del <see cref="HttpClient"/> registrado en DI para el health check de MinIO.</summary>
    public const string HealthClientName = "minio-health";

    private readonly IMinioClient _minio;
    private readonly ILogger<MinioFileStorageService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public string BucketName { get; }

    public MinioFileStorageService(
        IMinioClient minio,
        IOptions<MinioOptions> options,
        ILogger<MinioFileStorageService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _minio             = minio;
        _logger            = logger;
        _httpClientFactory = httpClientFactory;
        BucketName         = options.Value.BucketName;
    }

    // ── Public operations ────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task UploadAsync(Stream content, string objectKey, string contentType, long sizeBytes, CancellationToken ct = default)
    {
        await EnsureMinioAvailableAsync(ct);

        var args = new PutObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectKey)
            .WithStreamData(content)
            .WithObjectSize(sizeBytes)
            .WithContentType(contentType);

        try
        {
            await _minio.PutObjectAsync(args, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MinIO: error al subir objeto {Key} al bucket {Bucket}", objectKey, BucketName);
            throw new FileStorageUnavailableException(
                $"No se pudo subir el archivo '{objectKey}'. El servicio de almacenamiento no está disponible.", ex);
        }

        // Verificación secundaria: confirmar que el objeto existe después del upload.
        // El health check garantiza que MinIO está disponible, así que StatObjectAsync es fiable aquí.
        await ConfirmObjectExistsAsync(objectKey, ct);

        _logger.LogInformation("Archivo subido a MinIO: bucket={Bucket} key={Key}", BucketName, objectKey);
    }

    /// <inheritdoc />
    public async Task<Stream> DownloadAsync(string objectKey, CancellationToken ct = default)
    {
        await EnsureMinioAvailableAsync(ct);

        var memStream = new MemoryStream();

        var args = new GetObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectKey)
            .WithCallbackStream(stream => stream.CopyTo(memStream));

        try
        {
            await _minio.GetObjectAsync(args, ct);
            memStream.Position = 0;
            return memStream;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            await memStream.DisposeAsync();
            throw;
        }
        catch (Exception ex)
        {
            await memStream.DisposeAsync();
            _logger.LogError(ex, "MinIO: error al descargar objeto {Key} del bucket {Bucket}", objectKey, BucketName);
            throw new FileStorageUnavailableException(
                $"No se pudo descargar el archivo '{objectKey}'. El servicio de almacenamiento no está disponible.", ex);
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string objectKey, CancellationToken ct = default)
    {
        await EnsureMinioAvailableAsync(ct);

        var removeArgs = new RemoveObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectKey);

        try
        {
            await _minio.RemoveObjectAsync(removeArgs, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MinIO: error al eliminar objeto {Key} del bucket {Bucket}", objectKey, BucketName);
            throw new FileStorageUnavailableException(
                $"No se pudo eliminar el archivo '{objectKey}'. El servicio de almacenamiento no está disponible.", ex);
        }

        // Verificación secundaria: confirmar que el objeto ya no existe.
        // El health check garantiza que MinIO está disponible, así que StatObjectAsync es fiable aquí.
        await ConfirmObjectGoneAsync(objectKey, ct);

        _logger.LogInformation("Archivo eliminado de MinIO: bucket={Bucket} key={Key}", BucketName, objectKey);
    }

    /// <inheritdoc />
    public async Task<string> GetPresignedDownloadUrlAsync(string objectKey, int expirySeconds = 3600, CancellationToken ct = default)
    {
        await EnsureMinioAvailableAsync(ct);

        // PresignedGetObjectAsync calcula la URL localmente sin contactar MinIO,
        // por lo que no detecta si el objeto no existe. StatObjectAsync lo verifica
        // explícitamente (el health check previo garantiza que MinIO es accesible).
        await ConfirmObjectExistsAsync(objectKey, ct);

        var args = new PresignedGetObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectKey)
            .WithExpiry(expirySeconds);

        try
        {
            return await _minio.PresignedGetObjectAsync(args);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MinIO: error al generar URL prefirmada para objeto {Key}", objectKey);
            throw new FileStorageUnavailableException(
                $"No se pudo generar la URL de descarga para '{objectKey}'. El servicio de almacenamiento no está disponible.", ex);
        }
    }

    /// <inheritdoc />
    public async Task EnsureBucketExistsAsync(CancellationToken ct = default)
    {
        try
        {
            var existsArgs = new BucketExistsArgs().WithBucket(BucketName);
            var exists = await _minio.BucketExistsAsync(existsArgs, ct);

            if (!exists)
            {
                var makeArgs = new MakeBucketArgs().WithBucket(BucketName);
                await _minio.MakeBucketAsync(makeArgs, ct);
                _logger.LogInformation("Bucket creado en MinIO: {Bucket}", BucketName);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MinIO: error al verificar o crear el bucket {Bucket}", BucketName);
            throw new FileStorageUnavailableException(
                $"No se pudo verificar o crear el bucket '{BucketName}'. El servicio de almacenamiento no está disponible.", ex);
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Realiza un GET a <c>/minio/health/live</c> usando un <see cref="HttpClient"/> dedicado
    /// que NO pasa por el SDK de MinIO. El SDK 6.x puede silenciar errores de red;
    /// este ping con <see cref="HttpClient"/> crudo propaga correctamente las excepciones de conexión.
    /// </summary>
    private async Task EnsureMinioAvailableAsync(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient(HealthClientName);
        try
        {
            // CancellationTokenSource combinado: respeta el ct del llamador Y aplica timeout propio.
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var response = await client.GetAsync("/minio/health/live", cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                throw new FileStorageUnavailableException(
                    $"MinIO respondió con estado {(int)response.StatusCode} en el endpoint de salud. El servicio no está disponible.");
            }
        }
        catch (FileStorageUnavailableException) { throw; }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MinIO no disponible (health check fallido en {Url})", "/minio/health/live");
            throw new FileStorageUnavailableException(
                "El servicio de almacenamiento (MinIO) no está disponible.", ex);
        }
    }

    /// <summary>
    /// Verifica que <paramref name="objectKey"/> EXISTE en MinIO.
    /// Solo llamar después de <see cref="EnsureMinioAvailableAsync"/>, que garantiza
    /// que MinIO es accesible y que <see cref="IMinioClient.StatObjectAsync"/> es fiable.
    /// </summary>
    private async Task ConfirmObjectExistsAsync(string objectKey, CancellationToken ct)
    {
        var statArgs = new StatObjectArgs().WithBucket(BucketName).WithObject(objectKey);
        try
        {
            await _minio.StatObjectAsync(statArgs, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            throw new FileStorageUnavailableException(
                $"El archivo '{objectKey}' no se encontró en MinIO. " +
                "Es posible que no se haya subido correctamente o que haya sido eliminado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MinIO: error al verificar existencia de {Key}", objectKey);
            throw new FileStorageUnavailableException(
                $"No se pudo verificar la existencia del archivo '{objectKey}'.", ex);
        }
    }

    /// <summary>
    /// Verifica que <paramref name="objectKey"/> YA NO EXISTE en MinIO tras una eliminación.
    /// Solo llamar después de <see cref="EnsureMinioAvailableAsync"/>.
    /// </summary>
    private async Task ConfirmObjectGoneAsync(string objectKey, CancellationToken ct)
    {
        var statArgs = new StatObjectArgs().WithBucket(BucketName).WithObject(objectKey);
        try
        {
            await _minio.StatObjectAsync(statArgs, ct);

            // StatObjectAsync retornó sin excepción: el objeto sigue existiendo.
            throw new FileStorageUnavailableException(
                $"El objeto '{objectKey}' sigue existiendo en MinIO tras la eliminación. La operación no tuvo efecto.");
        }
        catch (FileStorageUnavailableException) { throw; }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            // Esperado: el objeto ya no existe, eliminación confirmada.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MinIO: error al confirmar eliminación de {Key}", objectKey);
            throw new FileStorageUnavailableException(
                $"No se pudo confirmar la eliminación del archivo '{objectKey}'.", ex);
        }
    }
}
