using Dashboard_v2.Domain.Common;

namespace Dashboard_v2.Domain.Entities;

/// <summary>Student scientific group attached to a research group or area.</summary>
public class GrupoEstudiantil : StringAuditableEntity
{
    public string Nombre { get; set; } = default!;

    // pertenece a un Área
    public string AreaId { get; set; } = default!;
    public Area Area { get; set; } = default!;

    // estudia 0 o muchas Líneas de Investigación
    public ICollection<LineaDeInvestigacion> LineasDeInvestigacion { get; set; } = new List<LineaDeInvestigacion>();
}
