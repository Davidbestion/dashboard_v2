namespace Dashboard_v2.Domain.Entities;

public class GrupoDeInvestigacion
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Nombre { get; set; } = default!;

    // un Grupo de Investigación pertenece a un Área
    public string AreaId { get; set; } = default!;
    public Area Area { get; set; } = default!;
}
