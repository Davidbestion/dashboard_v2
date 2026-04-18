using Dashboard_v2.Domain.Enums;

namespace Dashboard_v2.Domain.Entities;

public class Publication
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = default!;
    public string PublicationData { get; set; } = default!;
    /// <summary>URL o DOI que identifica/enlaza la publicación (opcional).</summary>
    public string? UrlDoi { get; set; }

    public PublicationType PublicationType { get; set; }

    // Navegación: autores de esta publicación
    public ICollection<AuthorPublication> AuthorPublications { get; set; } = new List<AuthorPublication>();

    // Especializaciones (solo una estará presente según PublicationType)
    public IndexedPublication? IndexedPublication { get; set; }
    public JournalPublication? JournalPublication { get; set; }

    /// <summary>FK opcional al proyecto del que deriva esta publicación. Null si no está vinculada a ninguno.</summary>
    public string? ProyectoId { get; set; }
    /// <summary>Navegación al proyecto. Null si la publicación no está vinculada a un proyecto.</summary>
    public Proyecto? Proyecto { get; set; }
}