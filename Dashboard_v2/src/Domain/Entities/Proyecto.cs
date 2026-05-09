namespace Dashboard_v2.Domain.Entities;

/// <summary>
/// Entidad base abstracta para todos los tipos de proyecto.
/// Especializada en <see cref="ProyectoEnRevision"/> y <see cref="ProyectoEnEjecucion"/>.
/// Mapeada con TPT: la tabla "Proyectos" contiene los campos base comunes;
/// cada subtipo concreto tiene su propia tabla de extensión.
/// </summary>
public abstract class Proyecto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
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

    /// <summary>
    /// FK al Área a la que pertenece el proyecto (obligatoria).
    /// Un proyecto pertenece a exactamente un Área; un Área puede tener 0 o muchos proyectos.
    /// </summary>
    public string AreaId { get; set; } = default!;

    /// <summary>Navegación al Área del proyecto.</summary>
    public Area Area { get; set; } = default!;

    /// <summary>Publicaciones académicas derivadas de este proyecto (navegación inversa).</summary>
    public ICollection<Publication> PublicacionesDerivadas { get; set; } = new List<Publication>();

    /// <summary>Patentes derivadas de este proyecto (N:M).</summary>
    public ICollection<ProyectoPatente> PatentesDerivadas { get; set; } = new List<ProyectoPatente>();
}
