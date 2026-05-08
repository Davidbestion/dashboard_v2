namespace Dashboard_v2.Domain.Entities;

/// <summary>
/// Tabla de unión entre <see cref="User"/> y <see cref="Registro"/>.
/// Representa la relación N:M "un usuario puede crear varios registros;
/// un registro requiere uno o varios usuarios creadores".
/// </summary>
public class UserRegistro
{
    public string UserId { get; set; } = default!;
    public User User { get; set; } = default!;

    public string RegistroId { get; set; } = default!;
    public Registro Registro { get; set; } = default!;
}
