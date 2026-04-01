namespace Dashboard_v2.Domain.Entities;

/// <summary>
/// Especialización de Publication para publicaciones en revista (PublicationType = Diario).
/// PublicationId es a la vez PK y FK hacia Publication.
/// </summary>
public class JournalPublication
{
    public string PublicationId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string DataBase { get; set; } = default!;
    public int Group { get; set; }

    public Publication Publication { get; set; } = default!;
    public JournalGroup1Publication? JournalGroup1Publication { get; set; }
}
