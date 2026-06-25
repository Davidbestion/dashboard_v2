using System;
using System.Collections.Generic;

namespace Dashboard_v2.Application.Awards;

/// <summary>Minimal user reference for a single award recipient row.</summary>
public record RecipientDto
{
    // Id de la fila UserAwarded
    public int Id { get; init; }
    public string UserId { get; init; } = default!;
    public string UserDisplayName { get; init; } = default!;
    /// <summary>ID del archivo de evidencia/certificado adjunto. Null si no tiene.</summary>
    public int? EvidenceFileId { get; init; }
}

/// <summary>Grouped award granting event: a specific date with all recipients who received the award that day.</summary>
public record GrantingDto
{
    public DateTime AwardedAt { get; init; }
    public List<RecipientDto> Recipients { get; init; } = new();
}

/// <summary>
/// Award type with the list of users who received it, for dashboard display.
/// </summary>
public record AwardWithGrantingsDto
{
    public int AwardId { get; init; }
    public string AwardName { get; init; } = default!;
    public int AwardTypeId { get; init; }
    public string AwardTypeName { get; init; } = default!;
    public List<GrantingDto> Grantings { get; init; } = new();
}
