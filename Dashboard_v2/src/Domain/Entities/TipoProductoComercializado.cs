namespace Dashboard_v2.Domain.Entities;

public class TipoProductoComercializado
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Nombre { get; set; } = default!;

    // Un tipo puede tener 0..* productos
    public ICollection<ProductoComercializado> Productos { get; set; } = new List<ProductoComercializado>();
}
