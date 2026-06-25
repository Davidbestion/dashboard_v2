using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Application.Documents.Reports;

/// <summary>
/// Reporte del anexo de premios.
/// URL generada: GET /api/Documents/anexo-premios
/// Plantilla:    Infrastructure/Templates/AnexoPremios.xlsx
///
/// El cuerpo del documento se genera agrupando por tipo de premio y listando
/// debajo cada premio del tipo con su relación de autores.
/// Solo se incluyen premios en los que al menos un premiado pertenece
/// al área del usuario que solicita el reporte.
/// </summary>
public sealed class AnexoPremiosReport : IDocumentReport
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public AnexoPremiosReport(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public string ReportName => "anexo-premios";

    public string TemplateName => "AnexoPremios";

    public IReadOnlyCollection<string> AllowedRoles =>
        [nameof(RolesEnum.Superuser), nameof(RolesEnum.Vicedecano_de_investigacion)];

    public async Task<IReadOnlyDictionary<string, object>> GatherVariablesAsync(IReadOnlyDictionary<string, string>? parameters, CancellationToken ct)
    {
        var requestingAreaId = await _context.GetUserAreaIdAsync(_currentUser.Id, ct);

        var awardTypes = await _context.AwardTypes
            .AsNoTracking()
            .Include(t => t.Awards)
                .ThenInclude(a => a.UserAwardees)
                    .ThenInclude(ua => ua.User)
            .OrderBy(t => t.Id)
            .ToListAsync(ct);

        var tiposPremio = awardTypes
            .Select((type, index) => new AnexoPremiosTipoRowDto
            {
                Numero = index + 1,
                TipoPremio = type.Name,
                Premios = type.Awards
                    .Where(a => requestingAreaId == null || a.UserAwardees.Any(ua => ua.User?.AreaId == requestingAreaId))
                    .GroupBy(a => NormalizeAwardKey(a.Name))
                    .OrderBy(group => group.Min(a => a.Name))
                    .ThenBy(group => group.Min(a => a.Id))
                    .Select(group =>
                    {
                        var titulo = group.OrderBy(a => a.Id).Select(a => a.Name).First();
                        var autores = BuildAuthorsSummary(group.SelectMany(a => a.UserAwardees), requestingAreaId);
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

    private static string BuildAuthorsSummary(IEnumerable<UserAwarded> userAwardeds, string? areaId)
    {
        return string.Join(", ", userAwardeds
            .Where(ua => ua.User is not null && (areaId == null || ua.User.AreaId == areaId))
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
