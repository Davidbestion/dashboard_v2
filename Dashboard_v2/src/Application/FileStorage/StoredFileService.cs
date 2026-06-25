using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.FileStorage.DTOs;
using Dashboard_v2.Domain.Entities;

namespace Dashboard_v2.Application.FileStorage;

/// <summary>
/// Implementación de <see cref="IStoredFileService"/>.
/// Coordina la persistencia de metadata en PostgreSQL (via <see cref="IApplicationDbContext"/>)
/// y el almacenamiento del binario en MinIO (via <see cref="IFileStorageService"/>).
/// </summary>
public sealed class StoredFileService : IStoredFileService
{
    private const int DefaultUrlExpirySeconds = 3600;

    private readonly IApplicationDbContext _context;
    private readonly IFileStorageService _storage;
    private readonly IUser _currentUser;
    private readonly IRequestValidationService _validationService;

    public StoredFileService(
        IApplicationDbContext context,
        IFileStorageService storage,
        IUser currentUser,
        IRequestValidationService validationService)
    {
        _context = context;
        _storage = storage;
        _currentUser = currentUser;
        _validationService = validationService;
    }

    /// <summary>
    /// Uploads a file to object storage under the given category subfolder. Returns metadata including the assigned object key.
    /// </summary>
    public async Task<StoredFileDto> UploadAsync(UploadFileRequest request, Stream content, CancellationToken ct = default)
    {
        await _validationService.ValidateAndThrowAsync(request, ct);

        var objectKey = BuildObjectKey(request.Category, request.FileName);

        await _storage.UploadAsync(content, objectKey, request.ContentType, request.SizeBytes, ct);

        var file = new StoredFile
        {
            FileName     = request.FileName,
            ObjectKey    = objectKey,
            BucketName   = _storage.BucketName,
            ContentType  = request.ContentType,
            SizeBytes    = request.SizeBytes,
            UploadedById = _currentUser.Id!,
        };

        _context.StoredFiles.Add(file);
        await _context.SaveChangesAsync(ct);

        return ToDto(file);
    }

    public async Task<(Stream Content, string ContentType, string FileName)> DownloadAsync(int fileId, CancellationToken ct = default)
    {
        var file = await GetFileOrThrowAsync(fileId, ct);
        var stream = await _storage.DownloadAsync(file.ObjectKey, ct);
        return (stream, file.ContentType, file.FileName);
    }

    /// <summary>
    /// Generates a pre-signed download URL valid for the specified duration (default <see cref="DefaultUrlExpirySeconds"/> seconds).
    /// </summary>
    public async Task<string> GetDownloadUrlAsync(int fileId, int expirySeconds = DefaultUrlExpirySeconds, CancellationToken ct = default)
    {
        var file = await GetFileOrThrowAsync(fileId, ct);
        return await _storage.GetPresignedDownloadUrlAsync(file.ObjectKey, expirySeconds, ct);
    }

    public async Task DeleteAsync(int fileId, CancellationToken ct = default)
    {
        var file = await GetFileOrThrowAsync(fileId, ct);

        await _storage.DeleteAsync(file.ObjectKey, ct);

        _context.StoredFiles.Remove(file);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<StoredFileDto> ReplaceAsync(int fileId, UploadFileRequest request, Stream content, CancellationToken ct = default)
    {
        await _validationService.ValidateAndThrowAsync(request, ct);

        var file = await GetFileOrThrowAsync(fileId, ct);

        // Eliminar el objeto anterior de MinIO antes de subir el nuevo
        await _storage.DeleteAsync(file.ObjectKey, ct);

        var newObjectKey = BuildObjectKey(request.Category, request.FileName);
        await _storage.UploadAsync(content, newObjectKey, request.ContentType, request.SizeBytes, ct);

        file.FileName    = request.FileName;
        file.ObjectKey   = newObjectKey;
        file.ContentType = request.ContentType;
        file.SizeBytes   = request.SizeBytes;

        await _context.SaveChangesAsync(ct);

        return ToDto(file);
    }

    public async Task<IReadOnlyList<StoredFileDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.StoredFiles
            .AsNoTracking()
            .Where(f => f.UploadedById == _currentUser.Id)
            .OrderByDescending(f => f.Created)
            .Select(f => ToDto(f))
            .ToListAsync(ct);
    }

    public async Task<StoredFileDto> GetByIdAsync(int fileId, CancellationToken ct = default)
    {
        var file = await GetFileOrThrowAsync(fileId, ct);
        return ToDto(file);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<StoredFile> GetFileOrThrowAsync(int fileId, CancellationToken ct)
    {
        var file = await _context.StoredFiles
            .FirstOrDefaultAsync(f => f.Id == fileId, ct);

        if (file is null)
            throw new NotFoundException(nameof(fileId), $"No se encontró un archivo con Id = {fileId}.");

        return file;
    }

    /// <summary>
    /// Construye una clave de objeto única para MinIO:  {category}/{guid}{ext}
    /// El GUID garantiza que no haya colisiones aunque dos archivos tengan el mismo nombre.
    /// </summary>
    private static string BuildObjectKey(string category, string fileName)
    {
        var ext = Path.GetExtension(fileName);
        return $"{category.ToLowerInvariant()}/{Guid.NewGuid()}{ext}";
    }

    private static StoredFileDto ToDto(StoredFile f) => new()
    {
        Id           = f.Id,
        FileName     = f.FileName,
        ContentType  = f.ContentType,
        SizeBytes    = f.SizeBytes,
        UploadedById = f.UploadedById,
        Created      = f.Created,
    };
}
