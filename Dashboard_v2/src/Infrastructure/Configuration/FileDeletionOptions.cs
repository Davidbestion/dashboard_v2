namespace Dashboard_v2.Infrastructure.Configuration;

/// <summary>
/// Ajustes de comportamiento del <see cref="Dashboard_v2.Infrastructure.BackgroundServices.FileDeletionBackgroundService"/>.
/// Se leen de la sección <c>FileDeletion</c> de <c>appsettings.json</c>.
///
/// <para>Ejemplo de configuración mínima (valores por defecto si la sección no existe):</para>
/// <code>
/// "FileDeletion": {
///   "IntervalSeconds": 60,
///   "MaxAttempts": 5,
///   "RetryDelaySeconds": 30,
///   "MaxRetryDelaySeconds": 21600
/// }
/// </code>
/// </summary>
public sealed class FileDeletionOptions
{
    /// <summary>Nombre de la sección en <c>appsettings.json</c>.</summary>
    public const string SectionName = "FileDeletion";

    /// <summary>
    /// Intervalo (en segundos) entre ejecuciones del background service.
    /// Valor por defecto: 60 segundos.
    /// </summary>
    public int IntervalSeconds { get; init; } = 60;

    /// <summary>
    /// Número de intentos fallidos a partir del cual se emite un aviso <c>Critical</c> en el log.
    /// El job sigue reintentándose indefinidamente con backoff exponencial hasta que MinIO
    /// vuelva a estar disponible.
    /// Valor por defecto: 5.
    /// </summary>
    public int MaxAttempts { get; init; } = 5;

    /// <summary>
    /// Retardo base (en segundos) para el backoff exponencial entre reintentos.
    /// El retardo real de cada intento es: <c>min(RetryDelaySeconds × 2^Attempts, MaxRetryDelaySeconds)</c>.
    /// Valor por defecto: 30 segundos.
    /// </summary>
    public int RetryDelaySeconds { get; init; } = 30;

    /// <summary>
    /// Techo del retardo exponencial (en segundos). Una vez alcanzado este valor,
    /// el job se reintenta cada <c>MaxRetryDelaySeconds</c> indefinidamente.
    /// Valor por defecto: 21 600 segundos (6 horas).
    /// </summary>
    public int MaxRetryDelaySeconds { get; init; } = 21_600;
}
