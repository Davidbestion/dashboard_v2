namespace Dashboard_v2.Domain.Entities;

public class AuthorPublication
{
    public string AuthorId { get; set; } = default!;
    public Author Author { get; set; } = default!;

    public string PublicationId { get; set; } = default!;
    public Publication Publication { get; set; } = default!;
}
