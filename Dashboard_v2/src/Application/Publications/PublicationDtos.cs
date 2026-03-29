namespace Dashboard_v2.Application.Publications;

/// <summary>Información básica de un autor dentro de una publicación.</summary>
public record AuthorDto
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    /// <summary>null si el autor no tiene cuenta en el sistema.</summary>
    public string? UserId { get; init; }
}

/// <summary>Datos completos de una publicación con su tipo y lista de autores.</summary>
public record PublicationDto
{
    public string Id { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string PublicationData { get; init; } = default!;
    public string? UrlDoi { get; init; }
    public PublicationTypeDto PublicationType { get; init; } = default!;
    public List<AuthorDto> Authors { get; init; } = [];
}

/// <summary>Tipo (categoría) de publicación disponible en el sistema.</summary>
public record PublicationTypeDto
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
}
