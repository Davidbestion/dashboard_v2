namespace Dashboard_v2.Application.Common.Interfaces;

/// <summary>
/// Contrato de inicialización del bucket de almacenamiento de objetos.
/// Solo se invoca durante el arranque de la aplicación para garantizar que
/// el bucket configurado exista antes de procesar solicitudes de archivos.
///
/// Separado de <see cref="IFileStorageService"/> para respetar el Principio de
/// Segregación de Interfaces (ISP): los consumidores en tiempo de ejecución no
/// deben depender de una operación de arranque que nunca usan.
/// </summary>
public interface IStorageBucketInitialiser
{
    /// <summary>
    /// Crea el bucket configurado si no existe. No hace nada si ya existe.
    /// </summary>
    Task EnsureBucketExistsAsync(CancellationToken ct = default);
}
