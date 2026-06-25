namespace Dashboard_v2.Domain.Entities;

/// <summary>Junction entity: links an Author to a Norma (regulation) they authored.</summary>
public class AuthorNorma
{
    public string AuthorId { get; set; } = default!;
    public Author Author { get; set; } = default!;

    public string NormaId { get; set; } = default!;
    public Norma Norma { get; set; } = default!;
}
