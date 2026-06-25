using Dashboard_v2.Domain.Common;
using Dashboard_v2.Domain.Enums;

namespace Dashboard_v2.Domain.Entities;

/// <summary>
/// Representa una red (colaborativa/profesional).
/// Una red pertenece a un país (relación obligatoria conceptualmente).
/// </summary>
public class Red : IAuditableEntity
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
    /// Tipo de la red: Universitaria, Nacional o Internacional.
    /// </summary>
    public TipoRed Tipo { get; set; } = TipoRed.Universitaria;

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
    /// Identificador del usuario que coordina esta red.
    /// </summary>
    public string? CoordinadorId { get; set; }

    /// <summary>
    /// Usuario coordinador de esta red (N:1 — un usuario puede coordinar muchas redes).
    /// </summary>
    public User? Coordinador { get; set; }

    /// <summary>
    /// Autores que participan en esta red (M:N explícito).
    /// </summary>
    public ICollection<ParticipacionEnRed> Participaciones { get; set; } = new List<ParticipacionEnRed>();

    /// <summary>
    /// Publicaciones generadas por esta red.
    /// </summary>
    public ICollection<Publication> Publications { get; set; } = new List<Publication>();

    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}
