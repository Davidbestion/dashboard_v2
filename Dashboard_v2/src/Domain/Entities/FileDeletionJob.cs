namespace Dashboard_v2.Domain.Entities;

/// <summary>
/// Representa un trabajo pendiente de borrado de archivo en MinIO.
///
/// <para>
/// Cuando el usuario quita o reemplaza el archivo adjunto de una entidad (Publicación,
/// Evento, Premio, Registro), el servicio de aplicación crea un registro de este tipo
/// en la <b>misma transacción</b> de base de datos que actualiza la entidad. Así se
/// garantiza que, si el guardado de la entidad falla y se revierte, tampoco queda el
/// job encolado, y viceversa — consistencia total en PostgreSQL.
/// </para>
///
/// <para>
/// Un <see cref="Dashboard_v2.Infrastructure.BackgroundServices.FileDeletionBackgroundService"/>
/// procesa esta tabla periódicamente: intenta borrar el objeto de MinIO y, si lo consigue,
/// elimina también el <see cref="StoredFile"/> de la BD y este registro. Si MinIO no está
/// disponible, incrementa <see cref="Attempts"/> y lo reintenta en la siguiente ejecución.
/// </para>
///
/// <para>
/// <b>Por qué almacenamos ObjectKey y BucketName aquí:</b> si el registro <see cref="StoredFile"/>
/// es eliminado por otra vía antes de que el background service procese este job, aún
/// podemos borrar el objeto en MinIO usando los datos copiados en el momento de encolar.
/// </para>
/// </summary>
public class FileDeletionJob
{
    /// <summary>Clave primaria auto-generada.</summary>
    public int Id { get; set; }

    /// <summary>
    /// Id del <see cref="StoredFile"/> pendiente de borrar. Nullable: la FK está configurada
    /// con <c>ON DELETE SET NULL</c>, de modo que si el <see cref="StoredFile"/> es eliminado
    /// por otra vía, el job permanece en la cola (sin FK) para que el background service
    /// siga pudiendo borrar el objeto en MinIO usando <see cref="ObjectKey"/>.
    /// </summary>
    public int? StoredFileId { get; set; }

    /// <summary>
    /// Clave del objeto en MinIO (ej. "proyectos/a1b2c3d4.pdf").
    /// Copiada del <see cref="StoredFile"/> al momento de encolar.
    /// </summary>
    public string ObjectKey { get; set; } = default!;

    /// <summary>
    /// Bucket de MinIO donde reside el archivo. Copiado del <see cref="StoredFile"/>
    /// al momento de encolar.
    /// </summary>
    public string BucketName { get; set; } = default!;

    /// <summary>Momento (UTC) en que se creó el job.</summary>
    public DateTime ScheduledAt { get; set; }

    /// <summary>
    /// Número de intentos de borrado fallidos realizados hasta ahora.
    /// El background service reintenta indefinidamente con backoff exponencial;
    /// <c>MaxAttempts</c> (en <c>FileDeletionOptions</c>) es solo el umbral para emitir
    /// un aviso <c>Critical</c> en el log.
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>Momento (UTC) del último intento fallido. <c>null</c> si aún no se ha intentado.</summary>
    public DateTime? LastAttemptAt { get; set; }
}
