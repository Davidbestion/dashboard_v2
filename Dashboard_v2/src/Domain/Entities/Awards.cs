using Dashboard_v2.Domain.Common;

namespace Dashboard_v2.Domain.Entities;

public class Award : IAuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;

    public int AwardTypeId { get; set; }
    public AwardType AwardType { get; set; } = default!;

    public ICollection<UserAwarded> UserAwardeds { get; set; } = new List<UserAwarded>();

    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}