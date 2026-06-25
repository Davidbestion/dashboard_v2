namespace Dashboard_v2.Domain.Entities;

/// <summary>Junction entity: institutions that host or participate in a given event.</summary>
public class EventInstitution
{
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    public string InstitutionId { get; set; } = default!;
    public Institution Institution { get; set; } = null!;
}
