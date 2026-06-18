using Dashboard_v2.Application.Common.Interfaces;

namespace Dashboard_v2.Application.Documents;

/// <summary>
/// Contrato para reportes que producen múltiples archivos empaquetados en un ZIP.
/// A diferencia de <see cref="IDocumentReport"/> (un template → un .xlsx),
/// esta interfaz genera el ZIP completo internamente usando el renderer.
/// </summary>
public interface IZipDocumentReport
{
    /// <summary>
    /// Identificador del reporte. Se usa como segmento en la URL:
    /// GET /api/Documents/{ReportName}
    /// </summary>
    string ReportName { get; }

    /// <summary>
    /// Roles autorizados a generar y descargar este reporte.
    /// </summary>
    IReadOnlyCollection<string> AllowedRoles { get; }

    /// <summary>
    /// Genera el ZIP con todos los archivos Excel y lo devuelve como bytes.
    /// </summary>
    Task<byte[]> GenerateAsync(IDocumentRenderer renderer, CancellationToken ct = default);
}
