using Dashboard_v2.Domain.Common;

namespace Dashboard_v2.Domain.Entities;

public class LineaDeInvestigacion : IAuditableEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Nombre { get; set; } = default!;
    public string? Descripcion { get; set; }

    // una Línea de Investigación pertenece a 0 o muchas Áreas del Conocimiento (N:N)
    public ICollection<AreaDelConocimiento> AreasDelConocimiento { get; set; } = new List<AreaDelConocimiento>();

    // una Línea de Investigación puede ser estudiada por 0 o muchos Grupos
    public ICollection<GrupoDeInvestigacion> GruposDeInvestigacion { get; set; } = new List<GrupoDeInvestigacion>();

    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}
