using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Dashboard;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Infrastructure.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public DashboardService(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<VicedecanoDashboardDto> GetVicedecanoDashboardAsync(CancellationToken ct = default)
    {
        var areaId = await _context.GetUserAreaIdAsync(_currentUser.Id, ct) ?? string.Empty;

        var (premiosTotales, premiosPorTipo)         = await GatherPremiosAsync(areaId, ct);
        var (pubsTotales, pubsPorGrupo, pubsPorAno)  = await GatherPublicacionesAsync(areaId, ct);
        var (proyTotales, proyPorEstado)             = await GatherProyectosAsync(areaId, ct);
        var (eventosTotales, eventosPorTipo)         = await GatherEventosAsync(areaId, ct);
        var (redesTotales, redesPorTipo)             = await GatherRedesAsync(areaId, ct);
        var ponencias   = await GatherPonenciasAsync(areaId, ct);
        var grupos      = await CountGruposAsync(areaId, ct);
        var (patenteTotal, patentesPorOrigen) = await GatherPatentesAsync(areaId, ct);
        var registros   = await CountAsync(_context.Registros,  r => r.Creadores.Any(c => c.Author.User != null && c.Author.User.AreaId == areaId), ct);
        var normas      = await CountAsync(_context.Normas,     n => n.Creadores.Any(c => c.Author.User != null && c.Author.User.AreaId == areaId), ct);
        var productos   = await CountAsync(_context.ProductosComercializados, p => p.Creadores.Any(c => c.Author.User != null && c.Author.User.AreaId == areaId), ct);

        return new VicedecanoDashboardDto
        {
            TotalPremios       = premiosTotales,
            TotalPublicaciones = pubsTotales,
            TotalProyectos     = proyTotales,
            TotalEventos       = eventosTotales,
            TotalPonencias     = ponencias,
            TotalRedes         = redesTotales,
            TotalGrupos        = grupos,
            TotalPatentes      = patenteTotal,
            TotalRegistros     = registros,
            TotalNormas        = normas,
            TotalProductos     = productos,

            PremiosPorTipo        = premiosPorTipo,
            PublicacionesPorGrupo = pubsPorGrupo,
            PublicacionesPorAno   = pubsPorAno,
            ProyectosPorEstado    = proyPorEstado,
            RedesPorTipo          = redesPorTipo,
            EventosPorTipo        = eventosPorTipo,
            PatentesPorOrigen     = patentesPorOrigen,
        };
    }

    // ── Premios ──────────────────────────────────────────────────────────────

    private async Task<(int Total, List<DashboardSerieItemDto> PorTipo)> GatherPremiosAsync(string areaId, CancellationToken ct)
    {
        var rows = await _context.UserAwardeds
            .AsNoTracking()
            .Where(ua => ua.User != null && ua.User.AreaId == areaId)
            .Include(ua => ua.Award).ThenInclude(a => a.AwardType)
            .ToListAsync(ct);

        var porTipo = rows
            .GroupBy(ua => ua.Award.AwardType.Name)
            .Select(g => new DashboardSerieItemDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Cantidad)
            .ToList();

        return (rows.Count, porTipo);
    }

    // ── Publicaciones ────────────────────────────────────────────────────────

    private async Task<(int Total, List<DashboardSerieItemDto> PorGrupo, List<DashboardSerieItemDto> PorAno)>
        GatherPublicacionesAsync(string areaId, CancellationToken ct)
    {
        var pubs = await _context.Publications
            .AsNoTracking()
            .Where(p => p.AuthorPublications.Any(ap => ap.Author.UserId != null && ap.Author.User!.AreaId == areaId))
            .Include(p => p.JournalPublication)
            .Include(p => p.IndexedPublication)
            .ToListAsync(ct);

        var porGrupo = new List<DashboardSerieItemDto>();
        for (int g = 1; g <= 4; g++)
        {
            var count = pubs.Count(p => p.JournalPublication?.Group == g);
            if (count > 0) porGrupo.Add(new DashboardSerieItemDto($"G{g}", count));
        }
        var divulgacion = pubs.Count(p => p.PublicationType == PublicationType.Artículo_de_Divulgación);
        if (divulgacion > 0) porGrupo.Add(new DashboardSerieItemDto("Divulgación", divulgacion));

        var porAno = pubs
            .GroupBy(p => p.PublishedDate.Length >= 4 ? p.PublishedDate[..4] : p.PublishedDate)
            .Select(g => new DashboardSerieItemDto(g.Key, g.Count()))
            .OrderBy(x => x.Label)
            .ToList();

        return (pubs.Count, porGrupo, porAno);
    }

    // ── Proyectos ────────────────────────────────────────────────────────────

    private async Task<(int Total, List<DashboardSerieItemDto> PorEstado)> GatherProyectosAsync(string areaId, CancellationToken ct)
    {
        var total = await _context.Proyectos
            .AsNoTracking()
            .CountAsync(p => p.JefeUsuario.AreaId == areaId || p.Participantes.Any(u => u.AreaId == areaId), ct);

        // EstadosDeEjecucion only exists on ProyectoEnEjecucion subtypes
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

        return (total, porEstado);
    }

    // ── Eventos ──────────────────────────────────────────────────────────────

    private async Task<(int Total, List<DashboardSerieItemDto> PorTipo)> GatherEventosAsync(string areaId, CancellationToken ct)
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

        return (eventos.Count, porTipo);
    }

    // ── Ponencias ────────────────────────────────────────────────────────────

    private Task<int> GatherPonenciasAsync(string areaId, CancellationToken ct) =>
        _context.Presentations
            .AsNoTracking()
            .CountAsync(p => p.User != null && p.User.AreaId == areaId, ct);

    // ── Redes ────────────────────────────────────────────────────────────────

    private async Task<(int Total, List<DashboardSerieItemDto> PorTipo)> GatherRedesAsync(string areaId, CancellationToken ct)
    {
        var redes = await _context.Reds
            .AsNoTracking()
            .Where(r =>
                (r.CoordinadorId != null && r.Coordinador!.AreaId == areaId) ||
                r.Participaciones.Any(p => p.Author.User != null && p.Author.User.AreaId == areaId))
            .ToListAsync(ct);

        var tipoNames = new Dictionary<TipoRed, string>
        {
            [TipoRed.Universitaria]    = "Universitaria",
            [TipoRed.Nacional]         = "Nacional",
            [TipoRed.Internacional]    = "Internacional",
        };

        var porTipo = redes
            .GroupBy(r => tipoNames.TryGetValue(r.Tipo, out var n) ? n : r.Tipo.ToString())
            .Select(g => new DashboardSerieItemDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Cantidad)
            .ToList();

        return (redes.Count, porTipo);
    }

    // ── Grupos de Investigación ───────────────────────────────────────────────

    private Task<int> CountGruposAsync(string areaId, CancellationToken ct) =>
        _context.GruposDeInvestigacion
            .AsNoTracking()
            .CountAsync(g => g.AreaId == areaId, ct);

    // ── Patentes ─────────────────────────────────────────────────────────────

    private async Task<(int Total, List<DashboardSerieItemDto> PorOrigen)> GatherPatentesAsync(string areaId, CancellationToken ct)
    {
        var flags = await _context.Patentes
            .AsNoTracking()
            .Where(p => p.Creadores.Any(c => c.Author.User != null && c.Author.User.AreaId == areaId))
            .Select(p => p.EsNacional)
            .ToListAsync(ct);

        var porOrigen = new List<DashboardSerieItemDto>
        {
            new("Cuba (nacional)",     flags.Count(f => f)),
            new("Extranjero",          flags.Count(f => !f)),
        }.Where(x => x.Cantidad > 0).ToList();

        return (flags.Count, porOrigen);
    }

    // ── Genérico ─────────────────────────────────────────────────────────────

    private static Task<int> CountAsync<T>(
        IQueryable<T> set,
        System.Linq.Expressions.Expression<Func<T, bool>> predicate,
        CancellationToken ct) where T : class =>
        set.AsNoTracking().CountAsync(predicate, ct);
}
