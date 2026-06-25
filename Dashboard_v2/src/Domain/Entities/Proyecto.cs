using Dashboard_v2.Domain.Common;

namespace Dashboard_v2.Domain.Entities;

/// <summary>
/// Abstract base entity for all research projects. Specialized into ProyectoEnRevision (pre-execution)
/// and ProyectoEnEjecucion (active) subtypes via Table-Per-Type inheritance.
/// </summary>
public abstract class Proyecto : StringAuditableEntity
{
    public string Titulo { get; set; } = default!;

    /// <summary>
    /// FK al usuario que ejerce como jefe del proyecto.
    /// El jefe debe tener el rol <c>Jefe_de_Proyecto</c>.
    /// Un usuario puede dirigir varios proyectos, pero cada proyecto tiene exactamente un jefe.
    /// </summary>
    public string JefeId { get; set; } = default!;

    /// <summary>Navegación al usuario jefe. Cargado explícitamente en los queries.</summary>
    public User JefeUsuario { get; set; } = default!;

    public int NumeroMiembros { get; set; }
    public int CantidadMiembrosUH { get; set; }
    public int CantidadEstudiantes { get; set; }
    public int CantidadEstudiantesContratados { get; set; }
    public bool TributaFormacionDoctoral { get; set; }

    public string ClasificacionId { get; set; } = default!;
    public Clasificacion Clasificacion { get; set; } = default!;

    /// <summary>Usuarios que participan en este proyecto (M:N, tabla ProyectoParticipantes).</summary>
    public ICollection<User> Participantes { get; set; } = new List<User>();

    /// <summary>Publicaciones académicas derivadas de este proyecto (navegación inversa).</summary>
    public ICollection<Publication> PublicacionesDerivadas { get; set; } = new List<Publication>();

    /// <summary>Patentes derivadas de este proyecto (N:M).</summary>
    public ICollection<ProyectoPatente> PatentesDerivadas { get; set; } = new List<ProyectoPatente>();
}
