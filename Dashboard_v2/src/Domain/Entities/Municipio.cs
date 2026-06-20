namespace Dashboard_v2.Domain.Entities;

public class Municipio
{
    public int Id { get; set; }
    public string Nombre { get; set; } = default!;
    public int ProvinciaId { get; set; }
    public Provincia Provincia { get; set; } = null!;
}
