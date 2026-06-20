using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Application.Documents.Reports;

/// <summary>
/// Reporte del anexo de eventos y actividades cientificas.
/// URL generada: GET /api/Documents/anexo-eventos
/// Plantilla:    Infrastructure/Templates/AnexoEventosCientificos.xlsx
///
/// Bloques rellenados automaticamente:
/// - Eventos internacionales.
/// - Eventos nacionales.
/// - Eventos coauspiciados: aquellos en que al menos un organizador pertenece al area del usuario actual.
/// - Conteo base de ponencias por categorias explicitamente modeladas.
/// - Datos detallados de todas las ponencias.
///
/// Bloques que quedan vacios o parcialmente vacios:
/// - Actividades cientificas en la UH.
/// - Las columnas sobre primer autor del area y autores de otras areas.
/// - Toda la seccion de conferencias magistrales.
/// </summary>
public sealed class AnexoEventosReport : IDocumentReport
{
    private const int InternacionalEventTypeId = 0;
    private const int NacionalEventTypeId = 1;

    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public AnexoEventosReport(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public string ReportName => "anexo-eventos";

    public string TemplateName => "AnexoEventosCientificos";

    public IReadOnlyCollection<string> AllowedRoles =>
        [nameof(RolesEnum.Superuser), nameof(RolesEnum.Vicedecano_de_investigacion)];

    public async Task<IReadOnlyDictionary<string, object>> GatherVariablesAsync(IReadOnlyDictionary<string, string>? parameters, CancellationToken ct)
    {
        var userAreaId = await _context.GetUserAreaIdAsync(_currentUser.Id, ct);

        var events = await _context.Events
            .AsNoTracking()
            .Include(e => e.Country)
            .Include(e => e.Institutions)
            .OrderBy(e => e.Name)
            .ToListAsync(ct);

        var presentations = await _context.Presentations
            .AsNoTracking()
            .Include(p => p.Event).ThenInclude(e => e.Country)
            .Include(p => p.User)
            .OrderBy(p => p.Event.Name)
            .ThenBy(p => p.Name)
            .ToListAsync(ct);

        // Eventos coauspiciados = al menos un organizador pertenece al area del usuario actual
        List<Event> eventosCoauspiciados = [];
        if (!string.IsNullOrWhiteSpace(userAreaId))
        {
            eventosCoauspiciados = await _context.Events
                .AsNoTracking()
                .Include(e => e.Country)
                .Include(e => e.Institutions)
                .Where(e => e.Organizadores.Any(o => o.User.AreaId == userAreaId))
                .OrderBy(e => e.Name)
                .ToListAsync(ct);
        }

        var ponenciasInternacionalesExtranjero = presentations.Count(p =>
            p.Event.EventTypeId == InternacionalEventTypeId && !IsHeldInCuba(p.Event));

        var ponenciasInternacionalesCuba = presentations.Count(p =>
            p.Event.EventTypeId == InternacionalEventTypeId && IsHeldInCuba(p.Event));

        var ponenciasNacionalesCuba = presentations.Count(p =>
            p.Event.EventTypeId == NacionalEventTypeId);

        const int ponenciasActividadesUh = 0;

        return new Dictionary<string, object>
        {
            ["EventosInternacionales"] = events
                .Where(e => e.EventTypeId == InternacionalEventTypeId)
                .Select(e => new EventoInternacionalRowDto
                {
                    NombreEventoInternacional = e.Name,
                    PaisSiFueEnElExtranjero = SelectColumnValue(e, heldInCuba: false, GetCountryName(e)),
                    EnCuba = SelectColumnValue(e, heldInCuba: true, GetCountryName(e)),
                })
                .ToList(),
            ["EventosNacionales"] = events
                .Where(e => e.EventTypeId == NacionalEventTypeId)
                .Select(e => new EventoNacionalRowDto
                {
                    NombreEventoNacional = e.Name,
                    InstitucionQueLoOrganizo = BuildInstitutionsSummary(e),
                })
                .ToList(),
            ["EventosCoauspiciados"] = eventosCoauspiciados
                .Select(e => new EventoCoauspiciadoRowDto
                {
                    EventoCoauspiciado = e.Name,
                    InstitucionExternaResponsable = BuildInstitutionsSummary(e),
                    Internacional = e.EventTypeId == InternacionalEventTypeId ? "X" : string.Empty,
                    Nacional = e.EventTypeId == NacionalEventTypeId ? "X" : string.Empty,
                })
                .ToList(),
            ["ActividadesCientificasUH"] = Array.Empty<ActividadCientificaUhRowDto>(),
            ["DatosPonencias"] = presentations
                .Select(p => new DatosPonenciaRowDto
                {
                    NombrePonencia = p.Name,
                    NombreAutores = BuildParticipanteSummary(p),
                    NombreEventoOActividadCientifica = p.Event.Name,
                    PaisDeCelebracion = p.Event.Country.Name,
                })
                .ToList(),
            ["PonenciasInternacionalesExtranjero"] = ponenciasInternacionalesExtranjero,
            ["PonenciasInternacionalesCuba"] = ponenciasInternacionalesCuba,
            ["PonenciasNacionalesCuba"] = ponenciasNacionalesCuba,
            ["PonenciasActividadesUH"] = ponenciasActividadesUh,
            ["PonenciasTotal"] = ponenciasInternacionalesExtranjero
                + ponenciasInternacionalesCuba
                + ponenciasNacionalesCuba
                + ponenciasActividadesUh,
        };
    }

    private static bool IsHeldInCuba(Event eventEntity)
        => string.Equals(eventEntity.Country.Name?.Trim(), "Cuba", StringComparison.OrdinalIgnoreCase);

    private static string SelectColumnValue(Event eventEntity, bool heldInCuba, string value)
        => IsHeldInCuba(eventEntity) == heldInCuba ? value : string.Empty;

    private static string GetCountryName(Event eventEntity)
        => eventEntity.Country.Name?.Trim() ?? string.Empty;

    private static string BuildInstitutionsSummary(Event eventEntity)
        => string.Join(", ", eventEntity.Institutions
            .Where(i => !string.IsNullOrWhiteSpace(i.Nombre))
            .Select(i => i.Nombre.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(i => i));

    private static string BuildParticipanteSummary(Presentation presentation)
    {
        var u = presentation.User;
        if (u is null) return string.Empty;
        var ln2 = string.IsNullOrWhiteSpace(u.UserLastName2) ? "" : $" {u.UserLastName2}";
        return $"{u.UserLastName1}{ln2}, {u.UserName}".Trim();
    }
}

public sealed record EventoInternacionalRowDto
{
    public string NombreEventoInternacional { get; init; } = string.Empty;
    public string PaisSiFueEnElExtranjero { get; init; } = string.Empty;
    public string EnCuba { get; init; } = string.Empty;
}

public sealed record EventoNacionalRowDto
{
    public string NombreEventoNacional { get; init; } = string.Empty;
    public string InstitucionQueLoOrganizo { get; init; } = string.Empty;
}

public sealed record EventoCoauspiciadoRowDto
{
    public string EventoCoauspiciado { get; init; } = string.Empty;
    public string InstitucionExternaResponsable { get; init; } = string.Empty;
    public string Internacional { get; init; } = string.Empty;
    public string Nacional { get; init; } = string.Empty;
}

public sealed record ActividadCientificaUhRowDto
{
    public string ActividadCientifica { get; init; } = string.Empty;
}

public sealed record DatosPonenciaRowDto
{
    public string NombrePonencia { get; init; } = string.Empty;
    public string NombreAutores { get; init; } = string.Empty;
    public string NombreEventoOActividadCientifica { get; init; } = string.Empty;
    public string PaisDeCelebracion { get; init; } = string.Empty;
}
