namespace Dashboard_v2.Application.GruposDeInvestigacion;

public record GrupoDeInvestigacionDto
{
    public string Id { get; init; } = default!;
    public string Nombre { get; init; } = default!;
    public string AreaId { get; init; } = default!;
    public string AreaNombre { get; init; } = default!;
    public IReadOnlyList<string> LineasDeInvestigacionIds { get; init; } = [];
    public IReadOnlyList<string> UsuariosIds { get; init; } = [];
    public string? CreadorId { get; init; }
}
