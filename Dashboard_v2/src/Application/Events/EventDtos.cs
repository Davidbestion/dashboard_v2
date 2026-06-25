using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.Events;

/// <summary>Minimal country entry for dropdown selection.</summary>
public record CountryDto(int Id, string Name);

/// <summary>Event type nomenclator entry.</summary>
public record EventTypeDto(int Id, string Name);

/// <summary>
/// Event summary for list views.
/// </summary>
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
    public List<string> OrganizadorIds { get; init; } = [];
    /// <summary>ID del archivo de evidencia/certificado adjunto. Null si no tiene.</summary>
    public int? EvidenceFileId { get; init; }
    public DateOnly? FechaInicio { get; init; }
    public DateOnly? FechaFin { get; init; }
}

/// <summary>Academic presentation (ponencia) within an event.</summary>
public record PresentationDto
{
    public int Id { get; init; }
    public string Name { get; init; } = default!;
    public int EventId { get; init; }
    public string EventName { get; init; } = default!;
    public DateOnly Fecha { get; init; }
    public string UserId { get; init; } = default!;
    public LinkedUserSummaryDto? User { get; init; }
}
