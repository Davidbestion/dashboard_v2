namespace Dashboard_v2.Domain.Entities;

public class Clasificacion
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Nombre { get; set; } = default!;

    public ICollection<Proyecto> Proyectos { get; set; } = new List<Proyecto>();
}
