using Dashboard_v2.Domain.Common;

namespace Dashboard_v2.Domain.Entities;

public class ProductoComercializado : IAuditableEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Titulo { get; set; } = default!;

    // Tipo de producto (1..1)
    public string TipoProductoComercializadoId { get; set; } = default!;
    public TipoProductoComercializado TipoProductoComercializado { get; set; } = default!;

    // Institución relacionada (0..*) -> (1..1) en Institución
    public string InstitutionId { get; set; } = default!;
    public Institution Institution { get; set; } = default!;

    /// <summary>Autores que son creadores de este producto (N:M).</summary>
    public ICollection<AuthorProductoComercializado> Creadores { get; set; } = new List<AuthorProductoComercializado>();

    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}
