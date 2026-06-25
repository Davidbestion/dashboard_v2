namespace Dashboard_v2.Application.GruposDeInvestigacion;

/// <summary>
/// Request to create a new research group.
/// </summary>
public record CreateGrupoDeInvestigacionRequest
{
    public string Nombre { get; init; } = default!;
    public string AreaId { get; init; } = default!;
    public IList<string> LineasDeInvestigacionIds { get; init; } = [];
}

/// <summary>Request to update an existing research group's name, area, or research lines.</summary>
public record UpdateGrupoDeInvestigacionRequest
{
    public string Nombre { get; init; } = default!;
    public string AreaId { get; init; } = default!;
    public IList<string> LineasDeInvestigacionIds { get; init; } = [];
}

/// <summary>Request to replace the full member list of a research group.</summary>
public record SetGrupoMiembrosRequest
{
    public IList<string> UsuariosIds { get; init; } = [];
}
