namespace Dashboard_v2.Domain.Entities;

public class Author
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = default!;

    // Vínculo OPCIONAL con un usuario registrado.
    // Si es null, el autor existe en el sistema pero no tiene cuenta.
    public string? UserId { get; set; }
    public User? User { get; set; }

    // Navegación
    public ICollection<AuthorPublication> AuthorPublications { get; set; } = new List<AuthorPublication>();
}