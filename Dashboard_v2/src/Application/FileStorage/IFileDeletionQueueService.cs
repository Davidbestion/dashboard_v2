using Dashboard_v2.Domain.Entities;

namespace Dashboard_v2.Application.FileStorage;

/// <summary>
/// Contrato para encolar trabajos de borrado diferido de archivos en MinIO.
///
/// <para>
/// El patrón de uso es el siguiente:
/// <list type="number">
///   <item>Un servicio de aplicación detecta que el <c>EvidenceFileId</c> de una entidad
///   ha cambiado (se ha quitado o reemplazado el archivo).</item>
///   <item>Antes de llamar a <c>SaveChangesAsync</c>, llama a
///   <see cref="EnqueueAsync"/> pasando el <see cref="StoredFile"/> a eliminar.</item>
///   <item>Este método <b>solo añade</b> un <see cref="FileDeletionJob"/> al contexto de EF —
///   NO llama a <c>SaveChangesAsync</c>. Esto garantiza que el job y el cambio de la entidad
///   se persisten en la <b>misma transacción</b>, manteniendo la consistencia.</item>
///   <item>El <see cref="Dashboard_v2.Infrastructure.BackgroundServices.FileDeletionBackgroundService"/>
///   procesa la cola periódicamente e intenta borrar el objeto de MinIO.</item>
/// </list>
/// </para>
///
/// <para>
/// La interfaz vive en la capa Application para que los servicios de aplicación dependan
/// solo de abstracciones (Principio de Inversión de Dependencias). La implementación
/// concreta reside en la capa Infrastructure.
/// </para>
/// </summary>
public interface IFileDeletionQueueService
{
    /// <summary>
    /// Añade un <see cref="FileDeletionJob"/> al contexto de EF Core sin persistirlo.
    /// El llamador es responsable de invocar <c>SaveChangesAsync</c> a continuación.
    /// </summary>
    /// <param name="file">
    /// El <see cref="StoredFile"/> cuyo objeto MinIO se debe borrar.
    /// Sus propiedades <see cref="StoredFile.ObjectKey"/> y <see cref="StoredFile.BucketName"/>
    /// se copian en el job para que el borrado pueda ejecutarse incluso si el
    /// <see cref="StoredFile"/> es eliminado de la BD antes de que el job se procese.
    /// </param>
    /// <param name="ct">Token de cancelación.</param>
    Task EnqueueAsync(StoredFile file, CancellationToken ct = default);
}
