namespace Dashboard_v2.Domain.Entities;

/// <summary>Junction entity: links an Author to a commercialized product they developed.</summary>
public class AuthorProductoComercializado
{
    public string AuthorId { get; set; } = default!;
    public Author Author { get; set; } = default!;

    public string ProductoComercializadoId { get; set; } = default!;
    public ProductoComercializado ProductoComercializado { get; set; } = default!;
}
