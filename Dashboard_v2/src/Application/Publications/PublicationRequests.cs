using Dashboard_v2.Domain.Enums;
using System.Collections.Generic;

namespace Dashboard_v2.Application.Publications;

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
}

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
}
