namespace Dashboard_v2.Domain.Entities;

/// <summary>
/// Further specialization for Group 1 (highest-impact) journal publications. Stores quartile ranking.
/// </summary>
public class JournalGroup1Publication
{
    public string PublicationId { get; set; } = default!;
    public string? Cuartil { get; set; }

    public JournalPublication JournalPublication { get; set; } = default!;
}
