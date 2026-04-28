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

    public ICollection<Presentation> Presentations { get; set; } = new List<Presentation>();
}