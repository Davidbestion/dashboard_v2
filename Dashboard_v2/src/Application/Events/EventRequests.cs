namespace Dashboard_v2.Application.Events;

public record CreateCountryRequest(string Name);

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
}

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
}

public record CreatePresentationRequest
{
    public string Name { get; init; } = default!;
    public int EventId { get; init; }
    public DateOnly Fecha { get; init; }
    /// <summary>Solo para Superuser: ID del usuario al que se le asigna la presentación.</summary>
    public string? TargetUserId { get; init; }
}

public record UpdatePresentationRequest
{
    public string Name { get; init; } = default!;
    public int EventId { get; init; }
    public DateOnly Fecha { get; init; }
}
