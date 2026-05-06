using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Application.Documents.Reports;

/// <summary>
/// Reporte del anexo de premios.
/// URL generada: GET /api/Documents/anexo-premios
/// Plantilla:    Infrastructure/Templates/AnexoPremios.xlsx
///
/// El cuerpo del documento se genera agrupando por tipo de premio y listando
/// debajo cada premio del tipo con su relación de autores.
/// </summary>
public sealed class AnexoPremiosReport : IDocumentReport
{
    private readonly IApplicationDbContext _context;

    public AnexoPremiosReport(IApplicationDbContext context)
    {
        _context = context;
    }

    public string ReportName => "anexo-premios";

    public string TemplateName => "AnexoPremios";

    public async Task<IReadOnlyDictionary<string, object>> GatherVariablesAsync(IReadOnlyDictionary<string, string>? parameters, CancellationToken ct)
    {
        var awardTypes = await _context.AwardTypes
            .AsNoTracking()
            .Include(t => t.Awards)
                .ThenInclude(a => a.UserAwardeds)
                    .ThenInclude(ua => ua.User)
            .OrderBy(t => t.Id)
            .ToListAsync(ct);

        var tiposPremio = awardTypes
            .Select((type, index) => new AnexoPremiosTipoRowDto
            {
                Numero = index + 1,
                TipoPremio = type.Name,
                Premios = type.Awards
                    .GroupBy(a => NormalizeAwardKey(a.Name))
                    .OrderBy(group => group.Min(a => a.Name))
                    .ThenBy(group => group.Min(a => a.Id))
                    .Select(group =>
                    {
                        var titulo = group.OrderBy(a => a.Id).Select(a => a.Name).First();
                        var autores = BuildAuthorsSummary(group.SelectMany(a => a.UserAwardeds));
                        return new AnexoPremioDetalleRowDto
                        {
                            Titulo = titulo,
                            Autores = autores,
                        };
                    })
                    .Where(det => !string.IsNullOrWhiteSpace(det.Autores))
                    .ToList(),
            })
            .ToList();

        return new Dictionary<string, object>
        {
            ["TiposPremio"] = tiposPremio,
        };
    }

    private static string BuildAuthorsSummary(IEnumerable<UserAwarded> userAwardeds)
    {
        return string.Join(", ", userAwardeds
            .Where(ua => ua.User is not null)
            .Select(ua => BuildUserDisplayName(ua.User))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name));
    }

    private static string BuildUserDisplayName(User user)
    {
        return string.IsNullOrWhiteSpace(user.UserLastName2)
            ? $"{user.UserName} {user.UserLastName1}".Trim()
            : $"{user.UserName} {user.UserLastName1} {user.UserLastName2}".Trim();
    }

    private static string NormalizeAwardKey(string awardName)
        => awardName.Trim().ToUpperInvariant();
}

/// <summary>
/// Grupo de impresión para un tipo de premio.
/// </summary>
public sealed record AnexoPremiosTipoRowDto
{
    public int Numero { get; init; }
    public string TipoPremio { get; init; } = string.Empty;
    public IReadOnlyList<AnexoPremioDetalleRowDto> Premios { get; init; } = [];
}

/// <summary>
/// Fila de premio dentro de un tipo.
/// </summary>
public sealed record AnexoPremioDetalleRowDto
{
    public string Titulo { get; init; } = string.Empty;
    public string Autores { get; init; } = string.Empty;
}
