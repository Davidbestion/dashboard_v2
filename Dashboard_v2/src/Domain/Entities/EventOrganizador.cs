namespace Dashboard_v2.Domain.Entities;

public class EventOrganizador
{
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public string UserId { get; set; } = default!;
    public User User { get; set; } = null!;
}
