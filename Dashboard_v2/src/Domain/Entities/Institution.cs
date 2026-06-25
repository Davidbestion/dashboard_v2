using Dashboard_v2.Domain.Common;

namespace Dashboard_v2.Domain.Entities;

/// <summary>External organization (company, university, agency) that collaborates in research events or projects.</summary>
public class Institution : StringAuditableEntity
{
    public string Nombre { get; set; } = default!;

    // Navegación inversa: una institución puede estar en 0..N eventos
    public ICollection<Event> Events { get; set; } = new List<Event>();
    public ICollection<Registro> Registros { get; set; } = new List<Registro>();
    public ICollection<Norma> Normas { get; set; } = new List<Norma>();
    public ICollection<ProductoComercializado> ProductosComercializados { get; set; } = new List<ProductoComercializado>();
}
