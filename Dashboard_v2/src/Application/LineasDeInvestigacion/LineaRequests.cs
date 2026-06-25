namespace Dashboard_v2.Application.LineasDeInvestigacion;

/// <summary>Request to create a new research line.</summary>
public record CreateLineaDeInvestigacionRequest
{
    public string Nombre { get; init; } = default!;
    public string? Descripcion { get; init; }
    public IList<string> AreasDelConocimientoIds { get; init; } = [];
}

/// <summary>Request to update an existing research line's name, description, or associated knowledge domains.</summary>
public record UpdateLineaDeInvestigacionRequest
{
    public string Nombre { get; init; } = default!;
    public string? Descripcion { get; init; }
    public IList<string> AreasDelConocimientoIds { get; init; } = [];
}
