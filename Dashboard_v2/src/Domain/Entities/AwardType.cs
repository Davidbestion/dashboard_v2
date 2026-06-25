namespace Dashboard_v2.Domain.Entities;

/// <summary>Category of academic award (reference data). Allows dynamic addition of new award types without code changes.</summary>
public class AwardType
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }
    /// <summary>Display name of the award type.</summary>
    public string Name { get; set; } = default!;

    /// <summary>Awards belonging to this type.</summary>
    public ICollection<Award> Awards { get; set; } = new List<Award>();
}
