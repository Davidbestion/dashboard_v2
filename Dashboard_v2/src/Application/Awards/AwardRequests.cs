namespace Dashboard_v2.Application.Awards;

public record CreateAwardRequest
{
    public string AwardName { get; init; } = default!;
    public int AwardTypeId { get; init; }
    public int Year { get; init; }
    public DateTime AwardedAt { get; init; }
}

public record UpdateAwardRequest
{
    public string AwardName { get; init; } = default!;
    public int AwardTypeId { get; init; }
    public int Year { get; init; }
    public DateTime AwardedAt { get; init; }
}
