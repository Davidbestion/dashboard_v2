using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dashboard_v2.Application.Common.Interfaces;

namespace Dashboard_v2.Application.Documents;

public sealed class DocumentService : IDocumentService
{
    private readonly IReadOnlyDictionary<string, IDocumentReport> _reports;
    private readonly IDocumentRenderer _renderer;

    public DocumentService(IEnumerable<IDocumentReport> reports, IDocumentRenderer renderer)
    {
        _reports = reports.ToDictionary(r => r.ReportName, StringComparer.OrdinalIgnoreCase);
        _renderer = renderer;
    }

    public async Task<byte[]> GenerateAsync(
        string reportName,
        IReadOnlyDictionary<string, string>? parameters = null,
        CancellationToken ct = default)
    {
        if (!_reports.TryGetValue(reportName, out var report))
            throw new KeyNotFoundException($"No existe un reporte registrado con el nombre '{reportName}'.");

        var variables = await report.GatherVariablesAsync(parameters, ct);
        return _renderer.Render(report.TemplateName, variables);
    }
}
