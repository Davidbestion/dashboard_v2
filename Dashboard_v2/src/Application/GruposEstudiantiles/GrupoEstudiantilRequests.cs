namespace Dashboard_v2.Application.GruposEstudiantiles;

public record CreateGrupoEstudiantilRequest
{
    public string Nombre { get; init; } = default!;
    public string AreaId { get; init; } = default!;
    public IList<string> LineasDeInvestigacionIds { get; init; } = [];
}

public record UpdateGrupoEstudiantilRequest
{
    public string Nombre { get; init; } = default!;
    public string AreaId { get; init; } = default!;
    public IList<string> LineasDeInvestigacionIds { get; init; } = [];
}
