namespace Dashboard_v2.Domain.Entities;

/// <summary>Junction entity: links an Author to a Publication with an optional ordering index.</summary>
public class AuthorPublication
{
    public string AuthorId { get; set; } = default!;
    public Author Author { get; set; } = default!;

    public string PublicationId { get; set; } = default!;
    public Publication Publication { get; set; } = default!;
}
