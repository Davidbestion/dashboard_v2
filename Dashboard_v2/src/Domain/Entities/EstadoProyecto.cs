namespace Dashboard_v2.Domain.Entities;

/// <summary>Execution state of an active project (e.g. Active, Completed, Suspended).</summary>
public class EstadoProyecto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = default!;
}
