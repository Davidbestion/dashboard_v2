namespace Dashboard_v2.Domain.Entities;

public class UserAwarded
{
    public int Id { get; set; }

    // FK explícito para EF Core
    public string UserId { get; set; } = default!;
    public User User { get; set; } = null!;

    public int AwardId { get; set; }
    public Award Award { get; set; } = null!;
    
    /// <summary>Fecha en que se otorgó el premio (para ordenar).</summary>
    public DateTime AwardedAt { get; set; }
}