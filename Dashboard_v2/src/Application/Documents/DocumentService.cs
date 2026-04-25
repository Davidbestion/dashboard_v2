using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dashboard_v2.Application.Common.Interfaces;

namespace Dashboard_v2.Application.Documents;

public sealed class DocumentService : IDocumentService
{
    // TODO(david): El flujo 'anexo-eventos' pasa hoy por ClosedXML.Report y la plantilla
    // compuesta de una sola hoja termina rompiendo formato. Opciones para arreglarlo:
    // 1. Desviar 'anexo-eventos' a una ruta de render manual.
    // 2. Mantener la plantilla pero con relleno en coordenadas fijas.
    // 3. Rediseñar el documento en varias hojas.
    // 4. Simplificar la plantilla para que no dependa de merges ni corrimiento de filas.
    private readonly IReadOnlyDictionary<string, IDocumentReport> _reports;
    private readonly IDocumentRenderer _renderer;

    public DocumentService(IEnumerable<IDocumentReport> reports, IDocumentRenderer renderer)
    {
        _reports = reports.ToDictionary(r => r.ReportName, StringComparer.OrdinalIgnoreCase);
        _renderer = renderer;
    }

    public async Task<byte[]> GenerateAsync(string reportName, CancellationToken ct = default)
    {
        if (!_reports.TryGetValue(reportName, out var report))
            throw new KeyNotFoundException($"No existe un reporte registrado con el nombre '{reportName}'.");

        var variables = await report.GatherVariablesAsync(ct);
        return _renderer.Render(report.TemplateName, variables);
    }
}
