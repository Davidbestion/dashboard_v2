namespace Dashboard_v2.Domain.Entities;

public class ParticipacionEnRed
{
    public string RedId { get; set; } = default!;
    public Red Red { get; set; } = default!;

    public string AuthorId { get; set; } = default!;
    public Author Author { get; set; } = default!;
}
