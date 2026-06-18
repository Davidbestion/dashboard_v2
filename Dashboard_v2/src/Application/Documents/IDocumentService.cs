using System.Threading;
using System.Threading.Tasks;

namespace Dashboard_v2.Application.Documents;

public interface IDocumentService
{
    Task<byte[]> GenerateAsync(
        string reportName,
        IReadOnlyDictionary<string, string>? parameters = null,
        CancellationToken ct = default);

    /// <summary>
    /// Indica si el reporte con el nombre dado produce un ZIP en lugar de un .xlsx individual.
    /// </summary>
    bool IsZipReport(string reportName);

    /// <summary>
    /// Roles autorizados a generar el reporte indicado, o <c>null</c> si no existe
    /// ningún reporte registrado con ese nombre.
    /// </summary>
    IReadOnlyCollection<string>? GetAllowedRoles(string reportName);
}
