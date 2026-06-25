using Dashboard_v2.Domain.Enums;
using System.Collections.Generic;

namespace Dashboard_v2.Application.Publications;

/// <summary>
/// Request to register a new academic publication. PublishedDate must be ISO 8601 partial ('YYYY', 'YYYY-MM', or 'YYYY-MM-DD').
/// </summary>
public record CreatePublicationRequest
{
    public string Title { get; init; } = default!;
    public string PublicationData { get; init; } = default!;
    public PublicationType PublicationType { get; init; }
    public string? UrlDoi { get; init; }
    /// <summary>Fecha de publicación en formato ISO parcial: "YYYY", "YYYY-MM" o "YYYY-MM-DD". Obligatoria.</summary>
    public string PublishedDate { get; init; } = default!;
    public List<string> AdditionalAuthorIds { get; init; } = [];
    public List<string> AdditionalAuthorNames { get; init; } = [];
    public List<string> AdditionalUserIds { get; init; } = [];
    public int? Index { get; init; }
    public string? DataBase { get; init; }
    public int? Group { get; init; }
    public string? Cuartil { get; init; }
    public string? ProyectoId { get; init; }
    /// <summary>ID de la red que genera esta publicación (opcional). Solo puede ser especificado por un Jefe_de_Redes.</summary>
    public string? RedId { get; init; }
    /// <summary>ID del archivo de evidencia/certificado subido previamente (opcional).</summary>
    public int? EvidenceFileId { get; init; }
    /// <summary>Solo para Superuser: ID del usuario que será el autor de la publicación.</summary>
    public string? TargetUserId { get; init; }
}

/// <summary>
/// Request to update an existing publication's metadata.
/// </summary>
public record UpdatePublicationRequest
{
    public string Id { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string PublicationData { get; init; } = default!;
    public PublicationType PublicationType { get; init; }
    public string? UrlDoi { get; init; }
    /// <summary>Fecha de publicación en formato ISO parcial: "YYYY", "YYYY-MM" o "YYYY-MM-DD". Obligatoria.</summary>
    public string PublishedDate { get; init; } = default!;
    public List<string> AdditionalAuthorIds { get; init; } = [];
    public List<string> AdditionalAuthorNames { get; init; } = [];
    public List<string> AdditionalUserIds { get; init; } = [];
    public int? Index { get; init; }
    public string? DataBase { get; init; }
    public int? Group { get; init; }
    public string? Cuartil { get; init; }
    public string? ProyectoId { get; init; }
    /// <summary>ID de la red que genera esta publicación (opcional). Solo puede ser especificado por un Jefe_de_Redes.</summary>
    public string? RedId { get; init; }
    /// <summary>ID del archivo de evidencia/certificado subido previamente (opcional). Null elimina la evidencia actual.</summary>
    public int? EvidenceFileId { get; init; }
}
