namespace Dashboard_v2.Application.Awards;

public record CreateAwardRequest
{
    public int? AwardId { get; init; }
    public string? NewAwardName { get; init; }
    public int? AwardTypeId { get; init; }
    public DateTime AwardedAt { get; init; }
    /// <summary>ID del archivo de evidencia/certificado subido previamente (opcional).</summary>
    public int? EvidenceFileId { get; init; }
    /// <summary>Solo para Superuser: ID del usuario al que se le asigna el premio.</summary>
    public string? TargetUserId { get; init; }
}

public record UpdateAwardRequest
{
    public int? AwardId { get; init; }
    public string? NewAwardName { get; init; }
    public int? AwardTypeId { get; init; }
    public DateTime AwardedAt { get; init; }
    /// <summary>ID del archivo de evidencia/certificado subido previamente (opcional). Null elimina la evidencia actual.</summary>
    public int? EvidenceFileId { get; init; }
}
