namespace Dashboard_v2.Domain.Entities;

/// <summary>Bibliographic index or database name (e.g. Scopus, Web of Science) where publications are indexed.</summary>
public class BaseDeDatosPublicacion
{
    public int Id { get; set; }
    public string Nombre { get; set; } = default!;
}
