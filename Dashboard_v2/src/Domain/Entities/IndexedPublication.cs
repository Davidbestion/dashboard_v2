namespace Dashboard_v2.Domain.Entities;

/// <summary>
/// One-to-one specialization of Publication for indexed/book-type publications
/// (PublicationId serves as both PK and FK to Publication).
/// </summary>
public class IndexedPublication
{
    public string PublicationId { get; set; } = default!;
    public int? Index { get; set; }

    public Publication Publication { get; set; } = default!;
}
