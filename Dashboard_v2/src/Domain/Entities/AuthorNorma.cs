namespace Dashboard_v2.Domain.Entities;

public class AuthorNorma
{
    public string AuthorId { get; set; } = default!;
    public Author Author { get; set; } = default!;

    public string NormaId { get; set; } = default!;
    public Norma Norma { get; set; } = default!;
}
