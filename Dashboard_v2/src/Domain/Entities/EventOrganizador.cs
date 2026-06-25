namespace Dashboard_v2.Domain.Entities;

/// <summary>Junction entity: users who organize a given event.</summary>
public class EventOrganizador
{
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public string UserId { get; set; } = default!;
    public User User { get; set; } = null!;
}
