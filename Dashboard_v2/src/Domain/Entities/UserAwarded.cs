using Dashboard_v2.Domain.Common;

namespace Dashboard_v2.Domain.Entities;

public class UserAwarded : IAuditableEntity
{
    public int Id { get; set; }

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

    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}