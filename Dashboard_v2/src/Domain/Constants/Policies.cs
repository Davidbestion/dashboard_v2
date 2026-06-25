namespace Dashboard_v2.Domain.Constants;

/// <summary>Authorization policy name constants used across the application.</summary>
public abstract class Policies
{
    /// <summary>Policy that grants permission to permanently delete (purge) records.</summary>
    public const string CanPurge = nameof(CanPurge);
}