namespace Dashboard_v2.Application.AreasDelConocimiento;

public record CreateAreaDelConocimientoRequest
{
    public string Nombre { get; init; } = default!;
    public string? Descripcion { get; init; }
    public IList<string> LineasDeInvestigacionIds { get; init; } = new List<string>();
}

public record UpdateAreaDelConocimientoRequest
{
    public string Nombre { get; init; } = default!;
    public string? Descripcion { get; init; }
    public IList<string> LineasDeInvestigacionIds { get; init; } = new List<string>();
}
