namespace Dashboard_v2.Application.AreasDelConocimiento;

/// <summary>Request to create a new knowledge domain.</summary>
public record CreateAreaDelConocimientoRequest
{
    public string Nombre { get; init; } = default!;
    public string? Descripcion { get; init; }
    public IList<string> LineasDeInvestigacionIds { get; init; } = new List<string>();
}

/// <summary>Request to update an existing knowledge domain.</summary>
public record UpdateAreaDelConocimientoRequest
{
    public string Nombre { get; init; } = default!;
    public string? Descripcion { get; init; }
    public IList<string> LineasDeInvestigacionIds { get; init; } = new List<string>();
}
