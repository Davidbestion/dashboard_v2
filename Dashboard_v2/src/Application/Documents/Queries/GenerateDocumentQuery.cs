using Dashboard_v2.Application.Common.Interfaces;

namespace Dashboard_v2.Application.Documents.Queries;

/// <summary>
/// Query genérico para generar cualquier documento Excel registrado en el sistema.
/// El <paramref name="ReportName"/> se resuelve contra los <see cref="IDocumentReport"/>
/// registrados en el contenedor de dependencias.
/// </summary>
public record GenerateDocumentQuery(string ReportName) : IRequest<byte[]>;

public class GenerateDocumentQueryHandler : IRequestHandler<GenerateDocumentQuery, byte[]>
{
    private readonly IReadOnlyDictionary<string, IDocumentReport> _reports;
    private readonly IDocumentRenderer _renderer;

    public GenerateDocumentQueryHandler(
        IEnumerable<IDocumentReport> reports,
        IDocumentRenderer renderer)
    {
        _reports = reports.ToDictionary(r => r.ReportName, StringComparer.OrdinalIgnoreCase);
        _renderer = renderer;
    }

    public async Task<byte[]> Handle(GenerateDocumentQuery request, CancellationToken ct)
    {
        Guard.Against.NullOrWhiteSpace(request.ReportName);

        if (!_reports.TryGetValue(request.ReportName, out var report))
            throw new KeyNotFoundException(
                $"No existe un reporte registrado con el nombre '{request.ReportName}'. " +
                $"Nombres disponibles: {string.Join(", ", _reports.Keys)}");

        var variables = await report.GatherVariablesAsync(ct);
        return _renderer.Render(report.TemplateName, variables);
    }
}
