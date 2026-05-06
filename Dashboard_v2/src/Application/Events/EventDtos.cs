using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.Events;

public record CountryDto(int Id, string Name);

public record EventTypeDto(int Id, string Name);

public record EventDto
{
    public int Id { get; init; }
    public string Name { get; init; } = default!;
    public int CountryId { get; init; }
    public string CountryName { get; init; } = default!;
    public int EventTypeId { get; init; }
    public string EventTypeName { get; init; } = default!;
    public List<string> Institutions { get; init; } = [];
    public int PresentationCount { get; init; }
    public string? RedId { get; init; }
    public string? RedName { get; init; }
    public List<string> AreaIdsPatrocinadoras { get; init; } = [];
}

public record PresentationDto
{
    public int Id { get; init; }
    public string Name { get; init; } = default!;
    public int EventId { get; init; }
    public string EventName { get; init; } = default!;
    public List<PresentationAuthorDto> Authors { get; init; } = [];
}

/// <summary>
/// Autor de una presentación. Si el autor está vinculado a un usuario del sistema,
/// la propiedad <see cref="LinkedUser"/> expone la información necesaria para renderizar su tarjeta.
/// </summary>
public sealed record PresentationAuthorDto
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? UserId { get; init; }
    public LinkedUserSummaryDto? LinkedUser { get; init; }
}
