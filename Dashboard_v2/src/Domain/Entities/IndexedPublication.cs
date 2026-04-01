namespace Dashboard_v2.Domain.Entities;

/// <summary>
/// Especialización de Publication para tipos indexados (Libro, Monografía, Capítulo, Artículo de Divulgación).
/// PublicationId es a la vez PK y FK hacia Publication.
/// </summary>
public class IndexedPublication
{
    public string PublicationId { get; set; } = default!;
    public string Index { get; set; } = default!;

    public Publication Publication { get; set; } = default!;
}
