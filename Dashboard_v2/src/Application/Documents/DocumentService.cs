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
    private readonly IReadOnlyDictionary<string, IZipDocumentReport> _zipReports;
    private readonly IDocumentRenderer _renderer;

    public DocumentService(
        IEnumerable<IDocumentReport> reports,
        IEnumerable<IZipDocumentReport> zipReports,
        IDocumentRenderer renderer)
    {
        _reports = reports.ToDictionary(r => r.ReportName, StringComparer.OrdinalIgnoreCase);
        _zipReports = zipReports.ToDictionary(r => r.ReportName, StringComparer.OrdinalIgnoreCase);
        _renderer = renderer;
    }

    public bool IsZipReport(string reportName) =>
        _zipReports.ContainsKey(reportName);

    public IReadOnlyCollection<string>? GetAllowedRoles(string reportName)
    {
        if (_zipReports.TryGetValue(reportName, out var zipReport))
            return zipReport.AllowedRoles;
        if (_reports.TryGetValue(reportName, out var report))
            return report.AllowedRoles;
        return null;
    }

    public async Task<byte[]> GenerateAsync(
        string reportName,
        IReadOnlyDictionary<string, string>? parameters = null,
        CancellationToken ct = default)
    {
        if (_zipReports.TryGetValue(reportName, out var zipReport))
            return await zipReport.GenerateAsync(_renderer, ct);

        if (!_reports.TryGetValue(reportName, out var report))
            throw new KeyNotFoundException($"No existe un reporte registrado con el nombre '{reportName}'.");

        var variables = await report.GatherVariablesAsync(parameters, ct);
        return _renderer.Render(report.TemplateName, variables);
    }
}
