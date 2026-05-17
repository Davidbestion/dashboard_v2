using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.Publications;

/// <summary>Información básica de un autor dentro de una publicación.</summary>
public record AuthorDto
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    /// <summary>Apellido(s) del autor.</summary>
    public string LastName { get; init; } = default!;
    /// <summary>Nombre(s) de pila. Puede ser nulo.</summary>
    public string? FirstName { get; init; }
    /// <summary>null si el autor no tiene cuenta en el sistema.</summary>
    public string? UserId { get; init; }
    /// <summary>
    /// Resumen del usuario vinculado cuando el autor representa a una cuenta real del sistema.
    /// Permite renderizar la misma tarjeta usada para seleccionar usuarios.
    /// </summary>
    public LinkedUserSummaryDto? LinkedUser { get; init; }
}

/// <summary>Datos de indexación para publicaciones de tipo Libro, Monografía, Capítulo o Artículo de Divulgación.</summary>
public record IndexedPublicationDto
{
    public int? Index { get; init; }
}

/// <summary>Datos de revista para publicaciones de tipo Diario (journal).</summary>
public record JournalPublicationDto
{
    public string? DataBase { get; init; }
    public int Group { get; init; }
    /// <summary>Cuartil Scimago (Q1–Q4). Solo presente cuando Group == 1.</summary>
    public string? Cuartil { get; init; }
}

/// <summary>Datos completos de una publicación.</summary>
public record PublicationDto
{
    public string Id { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string PublicationData { get; init; } = default!;
    public string? UrlDoi { get; init; }
    /// <summary>Fecha de publicación: "YYYY", "YYYY-MM" o "YYYY-MM-DD".</summary>
    public string PublishedDate { get; init; } = default!;
    public int PublicationType { get; init; }
    public List<AuthorDto> Authors { get; init; } = [];
    /// <summary>Presente cuando PublicationType != Diario.</summary>
    public IndexedPublicationDto? IndexedPublication { get; init; }
    /// <summary>Presente cuando PublicationType == Diario.</summary>
    public JournalPublicationDto? JournalPublication { get; init; }
    /// <summary>ID del proyecto del que deriva esta publicación. Null si no está vinculada.</summary>
    public string? ProyectoId { get; init; }
    /// <summary>Título del proyecto vinculado cuando la publicación deriva de uno.</summary>
    public string? ProyectoTitulo { get; init; }
    /// <summary>ID de la red que generó esta publicación. Null si no está vinculada a ninguna red.</summary>
    public string? RedId { get; init; }
    /// <summary>Nombre de la red vinculada. Null si no está vinculada a ninguna red.</summary>
    public string? RedNombre { get; init; }
    /// <summary>ID del archivo de evidencia/certificado adjunto. Null si no tiene.</summary>
    public int? EvidenceFileId { get; init; }
}

/// <summary>Tipo (categoría) de publicación disponible en el sistema.</summary>
public record PublicationTypeDto(int Value, string Name);

/// <summary>DTO para candidatos a duplicados encontrados al agregar una publicación.</summary>
public record PublicationDuplicateDto
{
    public string Id { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string? UrlDoi { get; init; }
    /// <summary>Tipo de coincidencia: "doi", "url", "title".</summary>
    public string MatchType { get; init; } = default!;
    /// <summary>Puntuación de similitud (0..1). Para coincidencias exactas será 1.0.</summary>
    public double Score { get; init; }
}
