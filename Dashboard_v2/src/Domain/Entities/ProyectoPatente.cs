namespace Dashboard_v2.Domain.Entities;

/// <summary>Junction entity: links a patent to the project it resulted from.</summary>
public class ProyectoPatente
{
    public string ProyectoId { get; set; } = default!;
    public Proyecto Proyecto { get; set; } = default!;

    public string PatenteId { get; set; } = default!;
    public Patente Patente { get; set; } = default!;
}
