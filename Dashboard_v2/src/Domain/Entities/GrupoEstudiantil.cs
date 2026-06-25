using Dashboard_v2.Domain.Common;

namespace Dashboard_v2.Domain.Entities;

public class GrupoEstudiantil : IAuditableEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Nombre { get; set; } = default!;

    // pertenece a un Área
    public string AreaId { get; set; } = default!;
    public Area Area { get; set; } = default!;

    // estudia 0 o muchas Líneas de Investigación
    public ICollection<LineaDeInvestigacion> LineasDeInvestigacion { get; set; } = new List<LineaDeInvestigacion>();

    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}
