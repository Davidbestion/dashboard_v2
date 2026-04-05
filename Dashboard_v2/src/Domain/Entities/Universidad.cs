namespace Dashboard_v2.Domain.Entities;

public class Universidad
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Nombre { get; set; } = default!;

    // Navegación
    public ICollection<Area> Areas { get; set; } = new List<Area>();
}
