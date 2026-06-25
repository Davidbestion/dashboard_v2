namespace Dashboard_v2.Domain.Common;

/// <summary>
/// Contract for entities tracked with creation and modification audit metadata.
/// Properties are populated by the infrastructure layer interceptors, not domain code.
/// </summary>
public interface IAuditableEntity
{
    /// <summary>Timestamp of when the entity was first persisted.</summary>
    DateTimeOffset Created { get; set; }
    /// <summary>Identifier of the user who created the entity.</summary>
    string? CreatedBy { get; set; }
    /// <summary>Timestamp of the most recent modification.</summary>
    DateTimeOffset LastModified { get; set; }
    /// <summary>Identifier of the user who last modified the entity.</summary>
    string? LastModifiedBy { get; set; }
}
