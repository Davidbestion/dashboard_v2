using System;
using System.Collections.Generic;

namespace Dashboard_v2.Application.Awards;

public record RecipientDto
{
    // Id de la fila UserAwarded
    public int Id { get; init; }
    public string UserId { get; init; } = default!;
    public string UserDisplayName { get; init; } = default!;
}

public record GrantingDto
{
    public DateTime AwardedAt { get; init; }
    public List<RecipientDto> Recipients { get; init; } = new();
}

public record AwardWithGrantingsDto
{
    public int AwardId { get; init; }
    public string AwardName { get; init; } = default!;
    public int AwardTypeId { get; init; }
    public string AwardTypeName { get; init; } = default!;
    public List<GrantingDto> Grantings { get; init; } = new();
}
