namespace Dashboard_v2.Domain.Entities;

/// <summary>Type/category of event (e.g. Conference, Workshop, Seminar), reference data.</summary>
public class EventType
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;

    public ICollection<Event> Events { get; set; } = new List<Event>();
}
