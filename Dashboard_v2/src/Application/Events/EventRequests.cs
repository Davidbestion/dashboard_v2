namespace Dashboard_v2.Application.Events;

public record CreateCountryRequest(string Name);

public record CreateEventRequest
{
    public string Name { get; init; } = default!;
    public int CountryId { get; init; }
    public int EventType { get; init; }
    public List<string> Institutions { get; init; } = [];
}

public record UpdateEventRequest
{
    public string Name { get; init; } = default!;
    public int CountryId { get; init; }
    public int EventType { get; init; }
    public List<string> Institutions { get; init; } = [];
}

public record CreatePresentationRequest
{
    public string Name { get; init; } = default!;
    public int EventId { get; init; }
    public List<string> CoauthorIds { get; init; } = [];
    public List<string> CoauthorUserIds { get; init; } = [];
    public List<string> CoauthorNames { get; init; } = [];
}

public record UpdatePresentationRequest
{
    public string Name { get; init; } = default!;
    public int EventId { get; init; }
    public List<string> CoauthorIds { get; init; } = [];
    public List<string> CoauthorUserIds { get; init; } = [];
    public List<string> CoauthorNames { get; init; } = [];
}
