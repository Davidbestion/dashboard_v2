namespace Dashboard_v2.Domain.Entities;

public class Event
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;

    /// <summary>Instituciones organizadoras.</summary>
    public ICollection<Institution> Institutions { get; set; } = new List<Institution>();

    // País donde se desarrolló el evento (1 país por evento según el modelo)
    public int CountryId { get; set; }
    public Country Country { get; set; } = null!;

    public int EventTypeId { get; set; }
    public EventType EventType { get; set; } = default!;

    /// <summary>
    /// Red coordinadora (opcional). Una red puede coordinar varios eventos;
    /// un evento puede no tener una red coordinadora o tener una sola.
    /// </summary>
    public string? RedId { get; set; }
    public Red? Red { get; set; }

    /// <summary>Usuarios que organizan este evento (0..*).</summary>
    public ICollection<EventOrganizador> Organizadores { get; set; } = new List<EventOrganizador>();

    /// <summary>Participaciones de usuarios en este evento (incluye ponencias).</summary>
    public ICollection<ParticipacionEnEvento> Participaciones { get; set; } = new List<ParticipacionEnEvento>();

    /// <summary>Archivo de evidencia/certificado adjunto (opcional).</summary>
    public int? EvidenceFileId { get; set; }
    public StoredFile? EvidenceFile { get; set; }
}