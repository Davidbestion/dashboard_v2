namespace Dashboard_v2.Domain.Common;

/// <summary>
/// Base class for auditable entities with a string primary key.
/// Tracks who created and last modified the entity.
/// </summary>
public abstract class StringAuditableEntity : IAuditableEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}
