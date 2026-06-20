namespace Dashboard_v2.Application.Common.Interfaces;

/// <summary>
/// Abstracción de bajo nivel para el almacenamiento de objetos binarios.
/// Application no depende del SDK de MinIO; solo conoce este contrato.
/// La implementación concreta (<c>MinioFileStorageService</c>) vive en Infrastructure.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Nombre del bucket configurado. Expuesto para que Application pueda
    /// almacenar la referencia de bucket en la metadata del archivo.
    /// </summary>
    string BucketName { get; }
    /// <summary>
    /// Sube un objeto al bucket configurado.
    /// </summary>
    /// <param name="content">Stream con el contenido del archivo.</param>
    /// <param name="objectKey">Clave única del objeto dentro del bucket (ej. "proyectos/guid.pdf").</param>
    /// <param name="contentType">MIME type del archivo.</param>
    /// <param name="sizeBytes">Tamaño exacto en bytes. Requerido por el SDK de MinIO.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task UploadAsync(Stream content, string objectKey, string contentType, long sizeBytes, CancellationToken ct = default);

    /// <summary>
    /// Descarga el contenido de un objeto como Stream.
    /// El caller es responsable de disponer el stream devuelto.
    /// </summary>
    Task<Stream> DownloadAsync(string objectKey, CancellationToken ct = default);

    /// <summary>
    /// Elimina un objeto del bucket. No lanza excepción si el objeto no existe.
    /// </summary>
    Task DeleteAsync(string objectKey, CancellationToken ct = default);

    /// <summary>
    /// Genera una URL presignada de descarga temporal.
    /// Permite al frontend descargar directamente desde MinIO sin pasar por la API.
    /// </summary>
    /// <param name="objectKey">Clave del objeto en el bucket.</param>
    /// <param name="expirySeconds">Segundos de validez de la URL.</param>
    Task<string> GetPresignedDownloadUrlAsync(string objectKey, int expirySeconds = 3600, CancellationToken ct = default);
}
