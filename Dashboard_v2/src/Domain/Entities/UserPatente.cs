namespace Dashboard_v2.Domain.Entities;

/// <summary>
/// Tabla de unión entre <see cref="User"/> y <see cref="Patente"/>.
/// Representa la relación N:M "un usuario puede crear varias patentes;
/// una patente requiere uno o varios usuarios creadores".
/// </summary>
public class UserPatente
{
    public string UserId { get; set; } = default!;
    public User User { get; set; } = default!;

    public string PatenteId { get; set; } = default!;
    public Patente Patente { get; set; } = default!;
}
