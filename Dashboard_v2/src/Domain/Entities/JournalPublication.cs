namespace Dashboard_v2.Domain.Entities;

/// <summary>
/// Especialización para publicaciones de tipo Diario (revista/journal).
/// PublicationId es a la vez PK y FK hacia Publication.
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
