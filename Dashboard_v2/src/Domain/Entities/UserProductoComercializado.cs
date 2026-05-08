namespace Dashboard_v2.Domain.Entities;

/// <summary>
/// Tabla de unión entre <see cref="User"/> y <see cref="ProductoComercializado"/>.
/// Representa la relación N:M "un usuario puede crear varios productos comercializados;
/// un producto requiere uno o varios usuarios creadores".
/// </summary>
public class UserProductoComercializado
{
    public string UserId { get; set; } = default!;
    public User User { get; set; } = default!;

    public string ProductoComercializadoId { get; set; } = default!;
    public ProductoComercializado ProductoComercializado { get; set; } = default!;
}
