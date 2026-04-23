namespace Dashboard_v2.Domain.Entities;

/// <summary>
/// Entidad principal del sistema. Representa a un usuario registrado.
/// Almacena su perfil completo (nombre, email, contraseña hasheada, fechas y categorías académicas)
/// y sus referencias de navegación hacia los roles asignados, recursos que posee y área institucional.
/// </summary>
public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserName { get; set; } = default!;
    /// <summary>Primer apellido (obligatorio).</summary>
    public string UserLastName1 { get; set; } = default!;
    /// <summary>Segundo apellido (opcional).</summary>
    public string? UserLastName2 { get; set; }
    public string Email { get; set; } = default!;
    public string? PasswordHash { get; set; }
    public DateTime BirthDate { get; set; }
    //bool de si es adiestrado (no posee categoria cientifica ni docente) o no
    public bool IsTrained { get; set; } = false;

    public ScientificCategory ScientificCategory { get; set; } = ScientificCategory.None;
    public TeachingCategory TeachingCategory { get; set; } = TeachingCategory.None;
    public InvestigationCategory InvestigationCategory { get; set; } = InvestigationCategory.None;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navegación
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<Resource> OwnedResources { get; set; } = new List<Resource>();

    // Si el usuario es también un autor académico registrado, este es su perfil.
    public Author? AuthorProfile { get; set; }

    // Grupos de investigación a los que pertenece este usuario
    public ICollection<GrupoDeInvestigacion> GruposDeInvestigacion { get; set; } = new List<GrupoDeInvestigacion>();

    /// <summary>Proyectos de los que este usuario es jefe. Solo aplicable a usuarios con rol Jefe_de_Proyecto.</summary>
    public ICollection<Proyecto> ProyectosComoJefe { get; set; } = new List<Proyecto>();

    /// <summary>
    /// Identificador del área institucional del usuario.
    /// Puede permanecer nulo temporalmente mientras el usuario completa su primer inicio de sesión
    /// y selecciona el área a la que pertenece.
    /// </summary>
    public string? AreaId { get; set; }

    /// <summary>
    /// Área a la que pertenece el usuario.
    /// La navegación se materializa cuando <see cref="AreaId"/> ya fue definido.
    /// </summary>
    public Area? Area { get; set; }
}
