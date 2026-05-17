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
}
