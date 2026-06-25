namespace Dashboard_v2.Application.LineasDeInvestigacion;

/// <summary>Research line with its associated knowledge domains.</summary>
public record LineaDeInvestigacionDto
{
    public string Id { get; init; } = default!;
    public string Nombre { get; init; } = default!;
    public string? Descripcion { get; init; }
    public IReadOnlyList<string> AreasDelConocimientoIds { get; init; } = [];
    public IReadOnlyList<string> AreasDelConocimientoNombres { get; init; } = [];
}
