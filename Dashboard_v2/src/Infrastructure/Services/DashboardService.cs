using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Dashboard;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Infrastructure.Services;

/// <summary>
/// Aggregates research activity statistics for the Vicedecano dashboard. Queries publications,
/// projects, events, awards, patents, networks, and groups filtered by academic area.
/// </summary>
public sealed class DashboardService : IDashboardService
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public DashboardService(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Gathers and aggregates all research activity metrics for the current user's area.
    /// Delegates to private Gather* helper methods for each entity type.
    /// </summary>
    public async Task<VicedecanoDashboardDto> GetVicedecanoDashboardAsync(CancellationToken ct = default)
    {
        var areaId = await _context.GetUserAreaIdAsync(_currentUser.Id, ct) ?? string.Empty;

        var (totalUsuarios, plantilla)                      = await GatherPlantillaAsync(areaId, ct);
        var (premiosTotales, premiosPorTipo, premiosPorAno) = await GatherPremiosAsync(areaId, ct);
        var (pubsTotales, pubsPorGrupo, pubsPorAno, pubsPorTipo, pubsPorProfesor)
                                                            = await GatherPublicacionesAsync(areaId, ct);
        var (proyTotales, proyPorEstado, proyPorTipo)       = await GatherProyectosAsync(areaId, ct);
        var (eventosTotales, eventosPorTipo, eventosPorAno) = await GatherEventosAsync(areaId, ct);
        var (ponencias, ponenciasPorAno)                    = await GatherPonenciasAsync(areaId, ct);
        var (redesTotales, redesPorTipo, redesDelArea)      = await GatherRedesAsync(areaId, ct);
        var grupos                                          = await CountGruposAsync(areaId, ct);
        var (patenteTotal, patentesPorOrigen)               = await GatherPatentesAsync(areaId, ct);
        var (registrosTotal, registrosPorTipo)              = await GatherRegistrosAsync(areaId, ct);
        var (normasTotal, normasPorTipo)                    = await GatherNormasAsync(areaId, ct);
        var (productosTotal, productosPorTipo)              = await GatherProductosAsync(areaId, ct);

        return new VicedecanoDashboardDto
        {
            TotalUsuarios      = totalUsuarios,
            TotalPremios       = premiosTotales,
            TotalPublicaciones = pubsTotales,
            TotalProyectos     = proyTotales,
            TotalEventos       = eventosTotales,
            TotalPonencias     = ponencias,
            TotalRedes         = redesTotales,
            TotalGrupos        = grupos,
            TotalPatentes      = patenteTotal,
            TotalRegistros     = registrosTotal,
            TotalNormas        = normasTotal,
            TotalProductos     = productosTotal,

            Plantilla = plantilla,

            PublicacionesPorGrupo    = pubsPorGrupo,
            PublicacionesPorAno      = pubsPorAno,
            PublicacionesPorTipo     = pubsPorTipo,
            PublicacionesPorProfesor = pubsPorProfesor,

            ProyectosPorEstado = proyPorEstado,
            ProyectosPorTipo   = proyPorTipo,

            PremiosPorTipo = premiosPorTipo,
            PremiosPorAno  = premiosPorAno,

            EventosPorTipo  = eventosPorTipo,
            EventosPorAno   = eventosPorAno,
            PonenciasPorAno = ponenciasPorAno,

            RedesPorTipo = redesPorTipo,
            RedesDelArea = redesDelArea,

            PatentesPorOrigen = patentesPorOrigen,
            RegistrosPorTipo  = registrosPorTipo,
            NormasPorTipo     = normasPorTipo,
            ProductosPorTipo  = productosPorTipo,
        };
    }

    // ── Plantilla / Personal ──────────────────────────────────────────────────

    // Collects active user counts and category breakdowns (scientific, teaching, research) for the area.
    private async Task<(int TotalUsuarios, PlantillaDto Plantilla)> GatherPlantillaAsync(string areaId, CancellationToken ct)
    {
        var users = await _context.Users
            .AsNoTracking()
            .Where(u => u.AreaId == areaId && u.IsActive)
            .Select(u => new
            {
                u.ScientificCategory,
                u.TeachingCategory,
                u.InvestigationCategory,
            })
            .ToListAsync(ct);

        var porCientifica = users
            .GroupBy(u => u.ScientificCategory.ToDisplayString())
            .Select(g => new DashboardSerieItemDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Cantidad)
            .ToList();

        var porDocente = users
            .Where(u => u.TeachingCategory != TeachingCategory.None)
            .GroupBy(u => u.TeachingCategory.ToDisplayString())
            .Select(g => new DashboardSerieItemDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Cantidad)
            .ToList();

        var porInvestigacion = users
            .Where(u => u.InvestigationCategory != InvestigationCategory.None)
            .GroupBy(u => u.InvestigationCategory.ToDisplayString())
            .Select(g => new DashboardSerieItemDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Cantidad)
            .ToList();

        var plantilla = new PlantillaDto
        {
            TotalDocentes        = users.Count(u => u.TeachingCategory != TeachingCategory.None),
            TotalInvestigadores  = users.Count(u => u.InvestigationCategory != InvestigationCategory.None),
            PorCategoriaCientifica    = porCientifica,
            PorCategoriaDocente       = porDocente,
            PorCategoriaInvestigacion = porInvestigacion,
        };

        return (users.Count, plantilla);
    }

    // ── Premios ──────────────────────────────────────────────────────────────

    // Collects awards received by users in the area, grouped by award type and year.
    private async Task<(int Total, List<DashboardSerieItemDto> PorTipo, List<DashboardSerieItemDto> PorAno)>
        GatherPremiosAsync(string areaId, CancellationToken ct)
    {
        var rows = await _context.UserAwardees
            .AsNoTracking()
            .Where(ua => ua.User != null && ua.User.AreaId == areaId)
            .Include(ua => ua.Award).ThenInclude(a => a.AwardType)
            .ToListAsync(ct);

        var porTipo = rows
            .GroupBy(ua => ua.Award.AwardType.Name)
            .Select(g => new DashboardSerieItemDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Cantidad)
            .ToList();

        var porAno = rows
            .GroupBy(ua => ua.AwardedAt.Year.ToString())
            .Select(g => new DashboardSerieItemDto(g.Key, g.Count()))
            .OrderBy(x => x.Label)
            .ToList();

        return (rows.Count, porTipo, porAno);
    }

    // ── Publicaciones ────────────────────────────────────────────────────────

    // Collects publications co-authored by users in the area, grouped by bibliographic group, year, type, and author.
    private async Task<(int Total, List<DashboardSerieItemDto> PorGrupo, List<DashboardSerieItemDto> PorAno,
        List<DashboardSerieItemDto> PorTipo, List<DashboardSerieItemDto> PorProfesor)>
        GatherPublicacionesAsync(string areaId, CancellationToken ct)
    {
        var pubs = await _context.Publications
            .AsNoTracking()
            .Where(p => p.AuthorPublications.Any(ap => ap.Author.UserId != null && ap.Author.User!.AreaId == areaId))
            .Include(p => p.JournalPublication)
            .Include(p => p.IndexedPublication)
            .ToListAsync(ct);

        // PorGrupo
        var porGrupo = new List<DashboardSerieItemDto>();
        for (int g = 1; g <= 4; g++)
        {
            var count = pubs.Count(p => p.JournalPublication?.Group == g);
            if (count > 0) porGrupo.Add(new DashboardSerieItemDto($"G{g}", count));
        }
        var divulgacion = pubs.Count(p => p.PublicationType == PublicationType.Artículo_de_Divulgación);
        if (divulgacion > 0) porGrupo.Add(new DashboardSerieItemDto("Divulgación", divulgacion));

        // PorAno
        var porAno = pubs
            .GroupBy(p => p.PublishedDate.Length >= 4 ? p.PublishedDate[..4] : p.PublishedDate)
            .Select(g => new DashboardSerieItemDto(g.Key, g.Count()))
            .OrderBy(x => x.Label)
            .ToList();

        // PorTipo
        var tipoNames = new Dictionary<PublicationType, string>
        {
            [PublicationType.Artículo_en_Revista_Científica] = "Artículo en Revista",
            [PublicationType.Libro]                          = "Libro",
            [PublicationType.Monografía]                     = "Monografía",
            [PublicationType.Capítulo]                       = "Capítulo de libro",
            [PublicationType.Artículo_de_Divulgación]        = "Divulgación",
        };

        var porTipo = pubs
            .GroupBy(p => tipoNames.TryGetValue(p.PublicationType, out var n) ? n : p.PublicationType.ToString())
            .Select(g => new DashboardSerieItemDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Cantidad)
            .ToList();

        // PorProfesor — nombre del Author vinculado a un User del área
        var authorNames = await _context.AuthorPublications
            .AsNoTracking()
            .Where(ap => ap.Author.UserId != null && ap.Author.User!.AreaId == areaId)
            .Select(ap => ap.Author.Name)
            .ToListAsync(ct);

        var porProfesor = authorNames
            .GroupBy(name => name)
            .Select(g => new DashboardSerieItemDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Cantidad)
            .ToList();

        return (pubs.Count, porGrupo, porAno, porTipo, porProfesor);
    }

    // ── Proyectos ────────────────────────────────────────────────────────────

    // Collects all projects in which area members participate (as leader or collaborator), grouped by type and execution state.
    private async Task<(int Total, List<DashboardSerieItemDto> PorEstado, List<DashboardSerieItemDto> PorTipo)>
        GatherProyectosAsync(string areaId, CancellationToken ct)
    {
        // Cargar todos los proyectos del área para clasificarlos por tipo
        var todos = await _context.Proyectos
            .AsNoTracking()
            .Where(p => p.JefeUsuario.AreaId == areaId || p.Participantes.Any(u => u.AreaId == areaId))
            .ToListAsync(ct);

        var porTipo = todos
            .GroupBy(p => p switch
            {
                ProyectoEmpresarial        => "Empresarial",
                ProyectoNoEmpresarial      => "No Empresarial",
                ProyectoDesarrolloLocal    => "Desarrollo Local",
                ProyectoPNAP               => "PNAP",
                ProyectoApoyoPrograma      => "Apoyo a Programa",
                ProyectoColabInternacional => "Colab. Internacional",
                ProyectoEnRevision         => "En Revisión",
                _                          => "Otro"
            })
            .Select(g => new DashboardSerieItemDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Cantidad)
            .ToList();

        // Cargar estados de ejecución (sólo ProyectoEnEjecucion)
        var enEjecucion = await _context.Proyectos
            .OfType<ProyectoEnEjecucion>()
            .AsNoTracking()
            .Where(p => p.JefeUsuario.AreaId == areaId || p.Participantes.Any(u => u.AreaId == areaId))
            .Include(p => p.EstadosDeEjecucion)
            .ToListAsync(ct);

        var porEstado = enEjecucion
            .SelectMany(p => p.EstadosDeEjecucion.Select(e => e.Nombre))
            .GroupBy(nombre => nombre)
            .Select(g => new DashboardSerieItemDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Cantidad)
            .ToList();

        return (todos.Count, porEstado, porTipo);
    }

    // ── Eventos ──────────────────────────────────────────────────────────────

    // Collects events organized or attended by area users, grouped by event type and year.
    private async Task<(int Total, List<DashboardSerieItemDto> PorTipo, List<DashboardSerieItemDto> PorAno)>
        GatherEventosAsync(string areaId, CancellationToken ct)
    {
        var eventos = await _context.Events
            .AsNoTracking()
            .Where(e =>
                e.Organizadores.Any(o => o.User != null && o.User.AreaId == areaId) ||
                e.Participaciones.Any(p => p.User != null && p.User.AreaId == areaId))
            .Include(e => e.EventType)
            .ToListAsync(ct);

        var porTipo = eventos
            .GroupBy(e => e.EventType?.Name ?? "Sin tipo")
            .Select(g => new DashboardSerieItemDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Cantidad)
            .ToList();

        var porAno = eventos
            .Where(e => e.FechaInicio.HasValue)
            .GroupBy(e => e.FechaInicio!.Value.Year.ToString())
            .Select(g => new DashboardSerieItemDto(g.Key, g.Count()))
            .OrderBy(x => x.Label)
            .ToList();

        return (eventos.Count, porTipo, porAno);
    }

    // ── Ponencias ────────────────────────────────────────────────────────────

    // Collects paper/poster presentations by area users, grouped by year.
    private async Task<(int Total, List<DashboardSerieItemDto> PorAno)> GatherPonenciasAsync(string areaId, CancellationToken ct)
    {
        var fechas = await _context.Presentations
            .AsNoTracking()
            .Where(p => p.User != null && p.User.AreaId == areaId)
            .Select(p => p.Fecha.Year.ToString())
            .ToListAsync(ct);

        var porAno = fechas
            .GroupBy(year => year)
            .Select(g => new DashboardSerieItemDto(g.Key, g.Count()))
            .OrderBy(x => x.Label)
            .ToList();

        return (fechas.Count, porAno);
    }

    // ── Redes ────────────────────────────────────────────────────────────────

    // Collects scientific networks coordinated by or with participation from area users, grouped by network type.
    private async Task<(int Total, List<DashboardSerieItemDto> PorTipo, List<RedResumenDto> Detalle)>
        GatherRedesAsync(string areaId, CancellationToken ct)
    {
        var redes = await _context.Reds
            .AsNoTracking()
            .Where(r =>
                (r.CoordinadorId != null && r.Coordinador!.AreaId == areaId) ||
                r.Participaciones.Any(p => p.Author.User != null && p.Author.User.AreaId == areaId))
            .ToListAsync(ct);

        var tipoNames = new Dictionary<TipoRed, string>
        {
            [TipoRed.Universitaria] = "Universitaria",
            [TipoRed.Nacional]      = "Nacional",
            [TipoRed.Internacional] = "Internacional",
        };

        string TipoLabel(TipoRed t) => tipoNames.TryGetValue(t, out var n) ? n : t.ToString();

        var porTipo = redes
            .GroupBy(r => TipoLabel(r.Tipo))
            .Select(g => new DashboardSerieItemDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Cantidad)
            .ToList();

        var detalle = redes
            .Select(r => new RedResumenDto(r.Nombre, TipoLabel(r.Tipo)))
            .OrderBy(r => r.Tipo)
            .ThenBy(r => r.Nombre)
            .ToList();

        return (redes.Count, porTipo, detalle);
    }

    // ── Grupos de Investigación ───────────────────────────────────────────────

    // Counts research groups directly affiliated with the area.
    private Task<int> CountGruposAsync(string areaId, CancellationToken ct) =>
        _context.GruposDeInvestigacion
            .AsNoTracking()
            .CountAsync(g => g.AreaId == areaId, ct);

    // ── Patentes ─────────────────────────────────────────────────────────────

    // Collects patents by area users, grouped by origin (national vs. foreign).
    private async Task<(int Total, List<DashboardSerieItemDto> PorOrigen)> GatherPatentesAsync(string areaId, CancellationToken ct)
    {
        var flags = await _context.Patentes
            .AsNoTracking()
            .Where(p => p.Creadores.Any(c => c.Author.User != null && c.Author.User.AreaId == areaId))
            .Select(p => p.EsNacional)
            .ToListAsync(ct);

        var porOrigen = new List<DashboardSerieItemDto>
        {
            new("Cuba (nacional)", flags.Count(f => f)),
            new("Extranjero",      flags.Count(f => !f)),
        }.Where(x => x.Cantidad > 0).ToList();

        return (flags.Count, porOrigen);
    }

    // ── Registros ────────────────────────────────────────────────────────────

    // Collects registerable intellectual property (software, other) by area users, grouped by type.
    private async Task<(int Total, List<DashboardSerieItemDto> PorTipo)> GatherRegistrosAsync(string areaId, CancellationToken ct)
    {
        var flags = await _context.Registros
            .AsNoTracking()
            .Where(r => r.Creadores.Any(c => c.Author.User != null && c.Author.User.AreaId == areaId))
            .Select(r => r.EsInformatico)
            .ToListAsync(ct);

        var porTipo = new List<DashboardSerieItemDto>
        {
            new("Software/Informático", flags.Count(f => f)),
            new("Otro",                 flags.Count(f => !f)),
        }.Where(x => x.Cantidad > 0).ToList();

        return (flags.Count, porTipo);
    }

    // ── Normas ───────────────────────────────────────────────────────────────

    // Collects technical standards authored by area users, grouped by standard type.
    private async Task<(int Total, List<DashboardSerieItemDto> PorTipo)> GatherNormasAsync(string areaId, CancellationToken ct)
    {
        var normas = await _context.Normas
            .AsNoTracking()
            .Where(n => n.Creadores.Any(c => c.Author.User != null && c.Author.User.AreaId == areaId))
            .Include(n => n.TipoNorma)
            .ToListAsync(ct);

        var porTipo = normas
            .GroupBy(n => n.TipoNorma?.Nombre ?? "Sin tipo")
            .Select(g => new DashboardSerieItemDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Cantidad)
            .ToList();

        return (normas.Count, porTipo);
    }

    // ── Productos Comercializados ─────────────────────────────────────────────

    // Collects commercialized products and services by area users, grouped by product type.
    private async Task<(int Total, List<DashboardSerieItemDto> PorTipo)> GatherProductosAsync(string areaId, CancellationToken ct)
    {
        var productos = await _context.ProductosComercializados
            .AsNoTracking()
            .Where(p => p.Creadores.Any(c => c.Author.User != null && c.Author.User.AreaId == areaId))
            .Include(p => p.TipoProductoComercializado)
            .ToListAsync(ct);

        var porTipo = productos
            .GroupBy(p => p.TipoProductoComercializado?.Nombre ?? "Sin tipo")
            .Select(g => new DashboardSerieItemDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Cantidad)
            .ToList();

        return (productos.Count, porTipo);
    }
}
