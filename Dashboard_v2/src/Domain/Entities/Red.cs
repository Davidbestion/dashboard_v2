namespace Dashboard_v2.Domain.Entities;

/// <summary>
/// Representa una red (colaborativa/profesional).
/// El campo <see cref="EsNacional"/> indica si la red es nacional (true) o internacional (false).
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
    /// True si la red es nacional; false si es internacional.
    /// </summary>
    public bool EsNacional { get; set; }

    /// <summary>
    /// Cantidad aproximada de profesores miembros de la red.
    /// </summary>
    public int CantidadProfesores { get; set; }

    /// <summary>
    /// Eventos coordinados por esta red.
    /// </summary>
    public ICollection<Event> Events { get; set; } = new List<Event>();
}
