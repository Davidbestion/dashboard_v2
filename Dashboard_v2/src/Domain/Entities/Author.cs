using Dashboard_v2.Domain.Common;

namespace Dashboard_v2.Domain.Entities;

public class Author : IAuditableEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Apellido(s) del autor. Campo prioritario en el ámbito científico-académico.</summary>
    public string LastName { get; set; } = default!;

    /// <summary>Nombre(s) de pila. Puede ser nulo cuando no se dispone de la información.</summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Nombre completo en formato bibliográfico: "Apellidos, Nombres".
    /// Si no hay nombres de pila se almacena solo el apellido.
    /// Derivado de <see cref="LastName"/> y <see cref="FirstName"/>; no editar directamente.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Clave de búsqueda normalizada (sin tildes, en minúsculas) derivada de <see cref="Name"/>.
    /// Permite búsquedas tolerantes a acentos (ej: "Damian" encuentra "Damián").
    /// No editar directamente; se genera automáticamente en <see cref="Create"/>.
    /// </summary>
    public string SearchKey { get; set; } = default!;

    /// <summary>Clave normalizada de <see cref="LastName"/>. Generada automáticamente.</summary>
    public string LastNameKey { get; set; } = default!;

    /// <summary>Clave normalizada de <see cref="FirstName"/>. Null si FirstName es null.</summary>
    public string? FirstNameKey { get; set; }

    // Vínculo OPCIONAL con un usuario registrado.
    // Si es null, el autor existe en el sistema pero no tiene cuenta.
    public string? UserId { get; set; }
    public User? User { get; set; }

    // Navegación
    public ICollection<AuthorPublication> AuthorPublications { get; set; } = new List<AuthorPublication>();
    public ICollection<AuthorPatente> AuthorPatentes { get; set; } = new List<AuthorPatente>();
    public ICollection<AuthorRegistro> AuthorRegistros { get; set; } = new List<AuthorRegistro>();
    public ICollection<AuthorNorma> AuthorNormas { get; set; } = new List<AuthorNorma>();
    public ICollection<AuthorProductoComercializado> AuthorProductosComercializados { get; set; } = new List<AuthorProductoComercializado>();
    public ICollection<ParticipacionEnRed> ParticipacionesEnRedes { get; set; } = new List<ParticipacionEnRed>();

    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }

    /// <summary>
    /// Crea un autor a partir de apellidos y nombres de pila.
    /// El campo <see cref="Name"/> se genera automáticamente en formato "Apellidos, Nombres".
    /// </summary>
    public static Author Create(string lastName, string? firstName = null)
    {
        var ln = (lastName ?? string.Empty).Trim();
        var fn = firstName?.Trim();
        var display = string.IsNullOrWhiteSpace(fn) ? ln : $"{ln}, {fn}";
        return new Author
        {
            LastName     = ln,
            FirstName    = fn,
            Name         = display,
            SearchKey    = TextNormalizer.Normalize(display),
            LastNameKey  = TextNormalizer.Normalize(ln),
            FirstNameKey = string.IsNullOrWhiteSpace(fn) ? null : TextNormalizer.Normalize(fn),
        };
    }
}