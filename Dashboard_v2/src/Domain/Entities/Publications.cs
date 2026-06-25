using Dashboard_v2.Domain.Common;
using Dashboard_v2.Domain.Enums;

namespace Dashboard_v2.Domain.Entities;

/// <summary>Academic publication. May specialize into JournalPublication (journal articles) or IndexedPublication (books/chapters). Tracks authors, evidence files, and links to projects or networks.</summary>
public class Publication : StringAuditableEntity
{
    public string Title { get; set; } = default!;
    /// <summary>Versión normalizada del título (sin diacríticos, sin puntuación, lowercase) para búsqueda rápida.</summary>
    public string? NormalizedTitle { get; set; }
    public string PublicationData { get; set; } = default!;
    /// <summary>URL o DOI que identifica/enlaza la publicación (opcional).</summary>
    public string? UrlDoi { get; set; }
    /// <summary>Versión normalizada de `UrlDoi` para comparaciones (lowercase, sin esquema, doi cleaned).</summary>
    public string? NormalizedUrlDoi { get; set; }

    /// <summary>
    /// Fecha de publicación en formato ISO parcial: "YYYY", "YYYY-MM" o "YYYY-MM-DD".
    /// Obligatoria: una publicación debe tener al menos el año de publicación.
    /// </summary>
    public string PublishedDate { get; set; } = default!;

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

    /// <summary>FK opcional a la red que generó esta publicación. Null si no está vinculada a ninguna red.</summary>
    public string? RedId { get; set; }
    /// <summary>Navegación a la red. Null si la publicación no está vinculada a una red.</summary>
    public Red? Red { get; set; }

    /// <summary>Archivo de evidencia/certificado adjunto (opcional).</summary>
    public int? EvidenceFileId { get; set; }
    public StoredFile? EvidenceFile { get; set; }
}