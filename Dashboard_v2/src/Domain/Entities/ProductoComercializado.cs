using Dashboard_v2.Domain.Common;

namespace Dashboard_v2.Domain.Entities;

/// <summary>Commercialized product or technology-transfer outcome resulting from research.</summary>
public class ProductoComercializado : StringAuditableEntity
{
    public string Titulo { get; set; } = default!;

    // Tipo de producto (1..1)
    public string TipoProductoComercializadoId { get; set; } = default!;
    public TipoProductoComercializado TipoProductoComercializado { get; set; } = default!;

    // Institución relacionada (0..*) -> (1..1) en Institución
    public string InstitutionId { get; set; } = default!;
    public Institution Institution { get; set; } = default!;

    /// <summary>Autores que son creadores de este producto (N:M).</summary>
    public ICollection<AuthorProductoComercializado> Creadores { get; set; } = new List<AuthorProductoComercializado>();
}
