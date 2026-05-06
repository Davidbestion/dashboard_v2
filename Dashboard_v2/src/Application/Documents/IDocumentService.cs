using System.Threading;
using System.Threading.Tasks;

namespace Dashboard_v2.Application.Documents;

public interface IDocumentService
{
    Task<byte[]> GenerateAsync(
        string reportName,
        IReadOnlyDictionary<string, string>? parameters = null,
        CancellationToken ct = default);
}
