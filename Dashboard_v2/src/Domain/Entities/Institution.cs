using Dashboard_v2.Domain.Common;

namespace Dashboard_v2.Domain.Entities;

public class Institution : IAuditableEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Nombre { get; set; } = default!;

    // Navegación inversa: una institución puede estar en 0..N eventos
    public ICollection<Event> Events { get; set; } = new List<Event>();
    public ICollection<Registro> Registros { get; set; } = new List<Registro>();
    public ICollection<Norma> Normas { get; set; } = new List<Norma>();
    public ICollection<ProductoComercializado> ProductosComercializados { get; set; } = new List<ProductoComercializado>();

    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}
