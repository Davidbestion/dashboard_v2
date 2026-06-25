namespace Dashboard_v2.Application.Dashboard;

/// <summary>
/// Aggregated research statistics for the Vicedecano dashboard.
/// Contains counters and time series for publications, projects, awards, events, networks, and intellectual property.
/// </summary>
public sealed record VicedecanoDashboardDto
{
    // ── Totales ────────────────────────────────────────────────────────────────
    public int TotalUsuarios       { get; init; }
    public int TotalPremios        { get; init; }
    public int TotalPublicaciones  { get; init; }
    public int TotalProyectos      { get; init; }
    public int TotalEventos        { get; init; }
    public int TotalPonencias      { get; init; }
    public int TotalRedes          { get; init; }
    public int TotalGrupos         { get; init; }
    public int TotalPatentes       { get; init; }
    public int TotalRegistros      { get; init; }
    public int TotalNormas         { get; init; }
    public int TotalProductos      { get; init; }

    // ── Plantilla / Personal ───────────────────────────────────────────────────
    public PlantillaDto Plantilla { get; init; } = new();

    // ── Publicaciones ──────────────────────────────────────────────────────────
    public List<DashboardSerieItemDto> PublicacionesPorGrupo    { get; init; } = [];
    public List<DashboardSerieItemDto> PublicacionesPorAno      { get; init; } = [];
    public List<DashboardSerieItemDto> PublicacionesPorTipo     { get; init; } = [];
    public List<DashboardSerieItemDto> PublicacionesPorProfesor { get; init; } = [];

    // ── Proyectos ──────────────────────────────────────────────────────────────
    public List<DashboardSerieItemDto> ProyectosPorEstado { get; init; } = [];
    public List<DashboardSerieItemDto> ProyectosPorTipo   { get; init; } = [];

    // ── Premios ────────────────────────────────────────────────────────────────
    public List<DashboardSerieItemDto> PremiosPorTipo { get; init; } = [];
    public List<DashboardSerieItemDto> PremiosPorAno  { get; init; } = [];

    // ── Eventos y Ponencias ────────────────────────────────────────────────────
    public List<DashboardSerieItemDto> EventosPorTipo { get; init; } = [];
    public List<DashboardSerieItemDto> EventosPorAno  { get; init; } = [];
    public List<DashboardSerieItemDto> PonenciasPorAno { get; init; } = [];

    // ── Redes ──────────────────────────────────────────────────────────────────
    public List<DashboardSerieItemDto> RedesPorTipo { get; init; } = [];
    public List<RedResumenDto>         RedesDelArea { get; init; } = [];

    // ── Propiedad intelectual ──────────────────────────────────────────────────
    public List<DashboardSerieItemDto> PatentesPorOrigen  { get; init; } = [];
    public List<DashboardSerieItemDto> RegistrosPorTipo   { get; init; } = [];
    public List<DashboardSerieItemDto> NormasPorTipo      { get; init; } = [];
    public List<DashboardSerieItemDto> ProductosPorTipo   { get; init; } = [];
}

// ── Tipos auxiliares ───────────────────────────────────────────────────────────

/// <summary>Single data point in a dashboard chart series: a label and its count.</summary>
public sealed record DashboardSerieItemDto(string Label, int Cantidad);

/// <summary>Academic staff breakdown by scientific, teaching, and research categories.</summary>
public sealed record PlantillaDto
{
    public int TotalDocentes        { get; init; }
    public int TotalInvestigadores  { get; init; }
    public List<DashboardSerieItemDto> PorCategoriaCientifica    { get; init; } = [];
    public List<DashboardSerieItemDto> PorCategoriaDocente       { get; init; } = [];
    public List<DashboardSerieItemDto> PorCategoriaInvestigacion { get; init; } = [];
}

/// <summary>Minimal network entry used in the dashboard network list.</summary>
public sealed record RedResumenDto(string Nombre, string Tipo);
