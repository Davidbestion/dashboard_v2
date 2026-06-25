namespace Dashboard_v2.Domain.Common;

/// <summary>Base class for all domain entities with an integer primary key.</summary>
public abstract class BaseEntity
{
    /// <summary>Integer primary key for this entity.</summary>
    public int Id { get; set; }
}
