namespace Dashboard_v2.Domain.Entities;

/// <summary>
/// Representa una red (colaborativa/profesional).
/// Una red pertenece a un país (relación obligatoria conceptualmente).
/// </summary>
public class Red
{
    /// <summary>
    /// Identificador único de la red (GUID como cadena).
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Nombre de la red.
    /// </summary>
    public string Nombre { get; set; } = default!;

    /// <summary>
    /// Identificador del país al que pertenece la red.
    /// Puede ser null temporalmente hasta que se asigne en migraciones.
    /// </summary>
    public int? CountryId { get; set; }

    /// <summary>
    /// Navegación al país de la red.
    /// </summary>
    public Country? Country { get; set; }

    /// <summary>
    /// Cantidad aproximada de profesores miembros de la red.
    /// </summary>
    public int CantidadProfesores { get; set; }

    /// <summary>
    /// Eventos coordinados por esta red.
    /// </summary>
    public ICollection<Event> Events { get; set; } = new List<Event>();

    /// <summary>
    /// Usuarios que participan en esta red (miembros).
    /// Relación N:N con User.
    /// </summary>
    public ICollection<User> Usuarios { get; set; } = new List<User>();

    /// <summary>
    /// Áreas que coordinan esta red (relación agregada RedCoordinada)
    /// </summary>
    public ICollection<RedCoordinada> RedesCoordinadas { get; set; } = new List<RedCoordinada>();
}
