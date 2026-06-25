namespace Dashboard_v2.Application.Areas;

/// <summary>
/// Request to create a new academic area.
/// </summary>
public record CreateAreaRequest
{
    public string Nombre { get; init; } = default!;
    public string? Descripcion { get; init; }
    public string? UniversidadId { get; init; }
    public IList<string> AreasDelConocimientoIds { get; init; } = [];
}

/// <summary>
/// Request to update an area's name or knowledge domains.
/// </summary>
public record UpdateAreaRequest
{
    public string Nombre { get; init; } = default!;
    public string? Descripcion { get; init; }
    public string? UniversidadId { get; init; }
    public IList<string> AreasDelConocimientoIds { get; init; } = [];
}
