namespace Dashboard_v2.Domain.Entities;

/// <summary>Category of commercialized product (e.g. Software, Hardware, Service).</summary>
public class TipoProductoComercializado
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Nombre { get; set; } = default!;

    // Un tipo puede tener 0..* productos
    public ICollection<ProductoComercializado> Productos { get; set; } = new List<ProductoComercializado>();
}
