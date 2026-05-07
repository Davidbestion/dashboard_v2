namespace Dashboard_v2.Infrastructure.Configuration;

/// <summary>
/// Opciones de configuración para la conexión con MinIO.
/// Se vincula a la sección "MinIO" de <c>appsettings.json</c>.
/// </summary>
public sealed class MinioOptions
{
    public const string SectionName = "MinIO";

    /// <summary>
    /// Endpoint del servidor MinIO (sin esquema), ej. "localhost:9000".
    /// </summary>
    public string Endpoint { get; set; } = default!;

    /// <summary>
    /// Access key (equivalente al usuario).
    /// </summary>
    public string AccessKey { get; set; } = default!;

    /// <summary>
    /// Secret key (equivalente a la contraseña).
    /// </summary>
    public string SecretKey { get; set; } = default!;

    /// <summary>
    /// Nombre del bucket donde se almacenan los documentos.
    /// </summary>
    public string BucketName { get; set; } = "dashboard-documents";

    /// <summary>
    /// Indica si la conexión debe usar SSL/TLS.
    /// En desarrollo local con Docker normalmente es <c>false</c>.
    /// </summary>
    public bool UseSSL { get; set; } = false;
}
