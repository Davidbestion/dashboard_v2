namespace Dashboard_v2.Domain.Entities;

/// <summary>
/// Tabla de unión entre <see cref="User"/> y <see cref="Norma"/>.
/// Representa la relación N:M "un usuario puede crear varias normas;
/// una norma requiere uno o varios usuarios creadores".
/// </summary>
public class UserNorma
{
    public string UserId { get; set; } = default!;
    public User User { get; set; } = default!;

    public string NormaId { get; set; } = default!;
    public Norma Norma { get; set; } = default!;
}
