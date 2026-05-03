namespace Dashboard_v2.Domain.Entities;

/// <summary>
/// Especialización de JournalPublication para revistas del grupo 1.
/// PublicationId es a la vez PK y FK hacia JournalPublication.
/// </summary>
public class JournalGroup1Publication
{
    public string PublicationId { get; set; } = default!;
    public string? Cuartil { get; set; }

    public JournalPublication JournalPublication { get; set; } = default!;
}
