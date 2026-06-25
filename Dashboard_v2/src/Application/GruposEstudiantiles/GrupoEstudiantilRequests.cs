namespace Dashboard_v2.Application.GruposEstudiantiles;

/// <summary>Request to create a new student research group.</summary>
public record CreateGrupoEstudiantilRequest
{
    public string Nombre { get; init; } = default!;
    public string AreaId { get; init; } = default!;
    public IList<string> LineasDeInvestigacionIds { get; init; } = [];
}

/// <summary>Request to update an existing student research group.</summary>
public record UpdateGrupoEstudiantilRequest
{
    public string Nombre { get; init; } = default!;
    public string AreaId { get; init; } = default!;
    public IList<string> LineasDeInvestigacionIds { get; init; } = [];
}
