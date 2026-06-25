using Dashboard_v2.Domain.Common;

namespace Dashboard_v2.Domain.Entities;

/// <summary>
/// Metadata for a file stored in MinIO object storage. Used to attach evidence or certificates to entities.
/// The actual file bytes live in MinIO; this entity only tracks its key and URL.
/// </summary>
public class StoredFile : BaseAuditableEntity
{
    /// <summary>
    /// Nombre original del archivo tal como lo subió el usuario (ej. "certificado.pdf").
    /// Solo para mostrar; no se usa como clave en MinIO.
    /// </summary>
    public string FileName { get; set; } = default!;

    /// <summary>
    /// Clave única del objeto en MinIO (ej. "proyectos/a1b2c3d4.pdf").
    /// Combina un prefijo de categoría y un GUID para evitar colisiones.
    /// </summary>
    public string ObjectKey { get; set; } = default!;

    /// <summary>
    /// Bucket de MinIO donde reside el archivo.
    /// </summary>
    public string BucketName { get; set; } = default!;

    /// <summary>
    /// MIME type del archivo (ej. "application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document").
    /// </summary>
    public string ContentType { get; set; } = default!;

    /// <summary>
    /// Tamaño del archivo en bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Id del usuario que subió el archivo.
    /// </summary>
    public string UploadedById { get; set; } = default!;

    // Navigation property
    public User UploadedBy { get; set; } = default!;
}
