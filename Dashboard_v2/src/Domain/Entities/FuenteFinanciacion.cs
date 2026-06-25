namespace Dashboard_v2.Domain.Entities;

/// <summary>Funding source or sponsoring agency for research projects.</summary>
public class FuenteFinanciacion
{
    public int Id { get; set; }
    public string Nombre { get; set; } = default!;
}
