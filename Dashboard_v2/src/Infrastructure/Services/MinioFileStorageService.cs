using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace Dashboard_v2.Infrastructure.Services;

/// <summary>
/// Implementación de <see cref="IFileStorageService"/> usando el SDK oficial de MinIO.
/// Toda la lógica de comunicación con MinIO reside aquí; Application solo conoce la interfaz.
/// </summary>
public sealed class MinioFileStorageService : IFileStorageService, IStorageBucketInitialiser
{
    private readonly IMinioClient _minio;
    private readonly ILogger<MinioFileStorageService> _logger;

    public string BucketName { get; }

    public MinioFileStorageService(
        IMinioClient minio,
        IOptions<MinioOptions> options,
        ILogger<MinioFileStorageService> logger)
    {
        _minio = minio;
        _logger = logger;
        BucketName = options.Value.BucketName;
    }

    /// <inheritdoc />
    public async Task UploadAsync(Stream content, string objectKey, string contentType, long sizeBytes, CancellationToken ct = default)
    {
        var args = new PutObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectKey)
            .WithStreamData(content)
            .WithObjectSize(sizeBytes)
            .WithContentType(contentType);

        await _minio.PutObjectAsync(args, ct);
        _logger.LogInformation("Archivo subido a MinIO: bucket={Bucket} key={Key}", BucketName, objectKey);
    }

    /// <inheritdoc />
    public async Task<Stream> DownloadAsync(string objectKey, CancellationToken ct = default)
    {
        var memStream = new MemoryStream();

        var args = new GetObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectKey)
            .WithCallbackStream(stream => stream.CopyTo(memStream));

        await _minio.GetObjectAsync(args, ct);
        memStream.Position = 0;

        return memStream;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string objectKey, CancellationToken ct = default)
    {
        var args = new RemoveObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectKey);

        await _minio.RemoveObjectAsync(args, ct);
        _logger.LogInformation("Archivo eliminado de MinIO: bucket={Bucket} key={Key}", BucketName, objectKey);
    }

    /// <inheritdoc />
    public async Task<string> GetPresignedDownloadUrlAsync(string objectKey, int expirySeconds = 3600, CancellationToken ct = default)
    {
        var args = new PresignedGetObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectKey)
            .WithExpiry(expirySeconds);

        return await _minio.PresignedGetObjectAsync(args);
    }

    /// <inheritdoc />
    public async Task EnsureBucketExistsAsync(CancellationToken ct = default)
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
}
