using Dashboard_v2.Domain.Common;

namespace Dashboard_v2.Domain.Entities;

/// <summary>Type of academic award or recognition (reference data, managed by administrators).</summary>
public class Award : BaseAuditableEntity
{
    /// <summary>Display name of the award.</summary>
    public string Name { get; set; } = default!;

    /// <summary>FK to the award's category type.</summary>
    public int AwardTypeId { get; set; }
    /// <summary>Navigation to the award type.</summary>
    public AwardType AwardType { get; set; } = default!;

    /// <summary>Users who have received this award.</summary>
    public ICollection<UserAwarded> UserAwardees { get; set; } = new List<UserAwarded>();

}