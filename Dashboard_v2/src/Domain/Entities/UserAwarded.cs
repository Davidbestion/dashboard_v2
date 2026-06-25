using Dashboard_v2.Domain.Common;

namespace Dashboard_v2.Domain.Entities;

/// <summary>Junction entity: records an award granted to a user on a specific date, with optional evidence.</summary>
public class UserAwarded : BaseAuditableEntity
{
    // FK explícito para EF Core
    public string UserId { get; set; } = default!;
    public User User { get; set; } = null!;

    public int AwardId { get; set; }
    public Award Award { get; set; } = null!;
    
    /// <summary>Fecha en que se otorgó el premio (para ordenar).</summary>
    public DateTime AwardedAt { get; set; }

    /// <summary>Archivo de evidencia/certificado adjunto (opcional).</summary>
    public int? EvidenceFileId { get; set; }
    public StoredFile? EvidenceFile { get; set; }

}