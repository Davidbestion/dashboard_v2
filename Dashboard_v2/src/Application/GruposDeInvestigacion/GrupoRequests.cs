namespace Dashboard_v2.Application.GruposDeInvestigacion;

public record CreateGrupoDeInvestigacionRequest
{
    public string Nombre { get; init; } = default!;
    public string AreaId { get; init; } = default!;
    public IList<string> LineasDeInvestigacionIds { get; init; } = [];
}

public record UpdateGrupoDeInvestigacionRequest
{
    public string Nombre { get; init; } = default!;
    public string AreaId { get; init; } = default!;
    public IList<string> LineasDeInvestigacionIds { get; init; } = [];
}

public record SetGrupoMiembrosRequest
{
    public IList<string> UsuariosIds { get; init; } = [];
}
