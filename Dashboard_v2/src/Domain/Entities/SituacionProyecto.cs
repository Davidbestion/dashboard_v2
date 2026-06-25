namespace Dashboard_v2.Domain.Entities;

/// <summary>Review situation of a proposal-stage project (e.g. Approved, Rejected, Pending Revision).</summary>
public class SituacionProyecto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = default!;
}
