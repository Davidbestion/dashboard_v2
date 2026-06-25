using Dashboard_v2.Domain.Common;

namespace Dashboard_v2.Domain.Entities;

public class ParticipacionEnEvento : IAuditableEntity
{
    public int Id { get; set; }

    public string UserId { get; set; } = default!;
    public User User { get; set; } = null!;

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public DateOnly Fecha { get; set; }

    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}
