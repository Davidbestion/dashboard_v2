namespace Dashboard_v2.Application.AreasDelConocimiento;

/// <summary>Knowledge domain with its associated research lines.</summary>
public record AreaDelConocimientoDto
{
    public string Id { get; init; } = default!;
    public string Nombre { get; init; } = default!;
    public string? Descripcion { get; init; }
    public IReadOnlyList<string> LineasDeInvestigacionIds { get; init; } = [];
}
