namespace Dashboard_v2.Domain.Entities;

/// <summary>Junction entity: links an Author to a Patente (patent) they filed.</summary>
public class AuthorPatente
{
    public string AuthorId { get; set; } = default!;
    public Author Author { get; set; } = default!;

    public string PatenteId { get; set; } = default!;
    public Patente Patente { get; set; } = default!;
}
