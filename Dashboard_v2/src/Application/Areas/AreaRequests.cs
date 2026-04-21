namespace Dashboard_v2.Application.Areas;

public record CreateAreaRequest
{
    public string Nombre { get; init; } = default!;
    public string? Descripcion { get; init; }
    public string? UniversidadId { get; init; }
    public IList<string> AreasDelConocimientoIds { get; init; } = [];
}

public record UpdateAreaRequest
{
    public string Nombre { get; init; } = default!;
    public string? Descripcion { get; init; }
    public string? UniversidadId { get; init; }
    public IList<string> AreasDelConocimientoIds { get; init; } = [];
}
