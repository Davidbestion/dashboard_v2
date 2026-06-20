namespace Dashboard_v2.Application.Dashboard;

public sealed record VicedecanoDashboardDto
{
    // ── Totales ────────────────────────────────────────────────────────────────
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

    // ── Series para gráficos ───────────────────────────────────────────────────
    public List<DashboardSerieItemDto> PremiosPorTipo       { get; init; } = [];
    public List<DashboardSerieItemDto> PublicacionesPorGrupo { get; init; } = [];
    public List<DashboardSerieItemDto> PublicacionesPorAno  { get; init; } = [];
    public List<DashboardSerieItemDto> ProyectosPorEstado   { get; init; } = [];
    public List<DashboardSerieItemDto> RedesPorTipo         { get; init; } = [];
    public List<DashboardSerieItemDto> EventosPorTipo       { get; init; } = [];
    public List<DashboardSerieItemDto> PatentesPorOrigen    { get; init; } = [];
}

public sealed record DashboardSerieItemDto(string Label, int Cantidad);
