namespace Dashboard_v2.Application.GruposEstudiantiles;

public record GrupoEstudiantilDto
{
    public string Id { get; init; } = default!;
    public string Nombre { get; init; } = default!;
    public string AreaId { get; init; } = default!;
    public string AreaNombre { get; init; } = default!;
    public IReadOnlyList<string> LineasDeInvestigacionIds { get; init; } = [];
}
