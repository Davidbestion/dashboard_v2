using Dashboard_v2.Domain.Common;

namespace Dashboard_v2.Domain.Entities;

/// <summary>Academic area (department/faculty) within a university, grouping research groups and users.</summary>
public class Area : StringAuditableEntity
{
    public string Nombre { get; set; } = default!;
    public string? Descripcion { get; set; }

    // un Área pertenece a 0 o 1 Universidad
    public string? UniversidadId { get; set; }
    public Universidad? Universidad { get; set; }

    // un Área posee 0 o muchos Grupos de Investigación
    public ICollection<GrupoDeInvestigacion> GruposDeInvestigacion { get; set; } = new List<GrupoDeInvestigacion>();

    // un Área puede tener 0 o muchos Usuarios
    public ICollection<User> Users { get; set; } = new List<User>();

    // Investiga sobre: un Área investiga 1 o muchas Áreas del Conocimiento
    public ICollection<AreaDelConocimiento> AreasDelConocimiento { get; set; } = new List<AreaDelConocimiento>();

}
