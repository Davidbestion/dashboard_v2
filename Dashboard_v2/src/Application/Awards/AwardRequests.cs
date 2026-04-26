namespace Dashboard_v2.Application.Awards;

public record CreateAwardRequest
{
    public int? AwardId { get; init; }
    public string? NewAwardName { get; init; }
    public int? AwardTypeId { get; init; }
    public DateTime AwardedAt { get; init; }
}

public record UpdateAwardRequest
{
    public int? AwardId { get; init; }
    public string? NewAwardName { get; init; }
    public int? AwardTypeId { get; init; }
    public DateTime AwardedAt { get; init; }
}
