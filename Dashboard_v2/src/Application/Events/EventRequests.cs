namespace Dashboard_v2.Application.Events;

/// <summary>Request to add a new country to the catalog.</summary>
public record CreateCountryRequest(string Name);

/// <summary>Request to create a new academic event.</summary>
public record CreateEventRequest
{
    public string Name { get; init; } = default!;
    public int CountryId { get; init; }
    public int EventType { get; init; }
    public List<string> Institutions { get; init; } = [];
    public string? RedId { get; init; }
    public List<string> OrganizadorIds { get; init; } = [];
    /// <summary>ID del archivo de evidencia/certificado subido previamente (opcional).</summary>
    public int? EvidenceFileId { get; init; }
    public DateOnly? FechaInicio { get; init; }
    public DateOnly? FechaFin { get; init; }
}

/// <summary>Request to update an existing event's metadata.</summary>
public record UpdateEventRequest
{
    public string Name { get; init; } = default!;
    public int CountryId { get; init; }
    public int EventType { get; init; }
    public List<string> Institutions { get; init; } = [];
    public string? RedId { get; init; }
    public List<string> OrganizadorIds { get; init; } = [];
    /// <summary>ID del archivo de evidencia/certificado subido previamente (opcional). Null elimina la evidencia actual.</summary>
    public int? EvidenceFileId { get; init; }
    public DateOnly? FechaInicio { get; init; }
    public DateOnly? FechaFin { get; init; }
}

/// <summary>Request to register a new academic presentation (ponencia) within an event.</summary>
public record CreatePresentationRequest
{
    public string Name { get; init; } = default!;
    public int EventId { get; init; }
    public DateOnly Fecha { get; init; }
    /// <summary>Solo para Superuser: ID del usuario al que se le asigna la presentación.</summary>
    public string? TargetUserId { get; init; }
}

/// <summary>Request to update an existing presentation's metadata.</summary>
public record UpdatePresentationRequest
{
    public string Name { get; init; } = default!;
    public int EventId { get; init; }
    public DateOnly Fecha { get; init; }
}
