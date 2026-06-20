using Dashboard_v2.Application.FileStorage.DTOs;

namespace Dashboard_v2.Application.FileStorage;

/// <summary>
/// Casos de uso de gestión de archivos almacenados.
/// Orquesta la persistencia de metadata en PostgreSQL con el almacenamiento binario en MinIO.
/// </summary>
public interface IStoredFileService
{
    /// <summary>
    /// Sube un nuevo archivo. Persiste la metadata en BD y el binario en MinIO.
    /// </summary>
    /// <param name="request">Metadata del archivo a subir.</param>
    /// <param name="content">Stream con el contenido binario del archivo.</param>
    /// <returns>DTO con la metadata del archivo creado, incluyendo su <c>Id</c> de BD.</returns>
    Task<StoredFileDto> UploadAsync(UploadFileRequest request, Stream content, CancellationToken ct = default);

    /// <summary>
    /// Descarga el binario de un archivo junto con su metadata.
    /// </summary>
    /// <returns>Tupla con el stream de contenido, MIME type y nombre original del archivo.</returns>
    Task<(Stream Content, string ContentType, string FileName)> DownloadAsync(int fileId, CancellationToken ct = default);

    /// <summary>
    /// Genera una URL presignada de MinIO para descarga directa desde el cliente.
    /// La URL expira según la configuración (por defecto 1 hora).
    /// </summary>
    Task<string> GetDownloadUrlAsync(int fileId, int expirySeconds = 3600, CancellationToken ct = default);

    /// <summary>
    /// Elimina el archivo de MinIO y su registro en base de datos.
    /// </summary>
    Task DeleteAsync(int fileId, CancellationToken ct = default);

    /// <summary>
    /// Reemplaza el binario de un archivo existente por uno nuevo.
    /// Elimina el objeto anterior de MinIO y sube el nuevo.
    /// La metadata (nombre, tipo, tamaño) se actualiza en BD.
    /// </summary>
    Task<StoredFileDto> ReplaceAsync(int fileId, UploadFileRequest request, Stream content, CancellationToken ct = default);

    /// <summary>
    /// Retorna la metadata de todos los archivos del usuario autenticado.
    /// </summary>
    Task<IReadOnlyList<StoredFileDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Retorna la metadata de un archivo por su Id. Lanza <see cref="KeyNotFoundException"/> si no existe.
    /// </summary>
    Task<StoredFileDto> GetByIdAsync(int fileId, CancellationToken ct = default);
}
