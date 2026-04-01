namespace Dashboard_v2.Application.Publications;

/// <summary>Información básica de un autor dentro de una publicación.</summary>
public record AuthorDto
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    /// <summary>null si el autor no tiene cuenta en el sistema.</summary>
    public string? UserId { get; init; }
}

/// <summary>Datos de indexación para publicaciones de tipo Libro, Monografía, Capítulo o Artículo de Divulgación.</summary>
public record IndexedPublicationDto
{
    public string Index { get; init; } = default!;
}

/// <summary>Datos de revista para publicaciones de tipo Diario (journal).</summary>
public record JournalPublicationDto
{
    public string Name { get; init; } = default!;
    public string DataBase { get; init; } = default!;
    public int Group { get; init; }
    /// <summary>Cuartil Scimago (1–4). Solo presente cuando Group == 1.</summary>
    public int? Cuartil { get; init; }
}

/// <summary>Datos completos de una publicación.</summary>
public record PublicationDto
{
    public string Id { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string PublicationData { get; init; } = default!;
    public string? UrlDoi { get; init; }
    public int PublicationType { get; init; }
    public List<AuthorDto> Authors { get; init; } = [];
    /// <summary>Presente cuando PublicationType ∈ {1,2,3,4}.</summary>
    public IndexedPublicationDto? IndexedPublication { get; init; }
    /// <summary>Presente cuando PublicationType == 0 (Diario).</summary>
    public JournalPublicationDto? JournalPublication { get; init; }
}

/// <summary>Tipo (categoría) de publicación disponible en el sistema.</summary>
public record PublicationTypeDto(int Value, string Name);
