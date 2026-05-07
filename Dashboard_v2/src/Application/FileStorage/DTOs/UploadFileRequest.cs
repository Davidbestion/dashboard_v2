namespace Dashboard_v2.Application.FileStorage.DTOs;

/// <summary>
/// Request para subir un archivo al sistema de almacenamiento.
/// El stream del contenido se pasa por separado al método del servicio.
/// </summary>
public sealed class UploadFileRequest
{
    /// <summary>
    /// Nombre original del archivo (ej. "certificado.pdf").
    /// </summary>
    public string FileName { get; init; } = default!;

    /// <summary>
    /// MIME type del archivo. Se valida contra la lista de tipos permitidos.
    /// </summary>
    public string ContentType { get; init; } = default!;

    /// <summary>
    /// Tamaño del archivo en bytes. Debe coincidir con el stream que se pasa al servicio.
    /// </summary>
    public long SizeBytes { get; init; }

    /// <summary>
    /// Prefijo de categoría para organizar objetos en MinIO (ej. "premios", "proyectos").
    /// Si se omite, se usa "general".
    /// </summary>
    public string Category { get; init; } = "general";
}
