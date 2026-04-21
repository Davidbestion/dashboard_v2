namespace Dashboard_v2.Application.LineasDeInvestigacion;

public record CreateLineaDeInvestigacionRequest
{
    public string Nombre { get; init; } = default!;
    public string? Descripcion { get; init; }
    public IList<string> AreasDelConocimientoIds { get; init; } = [];
}

public record UpdateLineaDeInvestigacionRequest
{
    public string Nombre { get; init; } = default!;
    public string? Descripcion { get; init; }
    public IList<string> AreasDelConocimientoIds { get; init; } = [];
}
