namespace Dashboard_v2.Domain.Common;

/// <summary>Base class for auditable entities with an integer primary key. Tracks who created and last modified the entity.</summary>
public abstract class BaseAuditableEntity : BaseEntity, IAuditableEntity
{
    /// <inheritdoc/>
    public DateTimeOffset Created { get; set; }

    /// <inheritdoc/>
    public string? CreatedBy { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset LastModified { get; set; }

    /// <inheritdoc/>
    public string? LastModifiedBy { get; set; }
}
