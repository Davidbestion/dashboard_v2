namespace Dashboard_v2.Domain.Entities;

/// <summary>Oral presentation at an event. Extends ParticipacionEnEvento by adding a presentation title.</summary>
public class Presentation : ParticipacionEnEvento
{
    public string Name { get; set; } = default!;
}
