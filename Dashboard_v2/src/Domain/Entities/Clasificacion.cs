namespace Dashboard_v2.Domain.Entities;

/// <summary>Classification category for research projects (e.g. Strategic, Routine), managed by administrators.</summary>
public class Clasificacion
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Nombre { get; set; } = default!;

    public ICollection<Proyecto> Proyectos { get; set; } = new List<Proyecto>();
}
