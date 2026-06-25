using Dashboard_v2.Domain.Common;

namespace Dashboard_v2.Domain.Entities;

/// <summary>Records a user's participation in an event. May be extended by Presentation for oral presentations.</summary>
public class ParticipacionEnEvento : BaseAuditableEntity
{
    public string UserId { get; set; } = default!;
    public User User { get; set; } = null!;

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public DateOnly Fecha { get; set; }

}
