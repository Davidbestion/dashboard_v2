namespace Dashboard_v2.Domain.Entities;

/// <summary>
/// One-to-one specialization of Publication for journal articles.
/// Tracks the bibliographic database and impact group/quartile.
/// </summary>
public class JournalPublication
{
    public string PublicationId { get; set; } = default!;
    public int? BaseDeDatosId { get; set; }
    public int Group { get; set; }

    public Publication Publication { get; set; } = default!;
    public BaseDeDatosPublicacion? BaseDeDatos { get; set; }
    public JournalGroup1Publication? JournalGroup1Publication { get; set; }
}
