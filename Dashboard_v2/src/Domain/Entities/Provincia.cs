namespace Dashboard_v2.Domain.Entities;

public class Provincia
{
    public int Id { get; set; }
    public string Nombre { get; set; } = default!;
    public ICollection<Municipio> Municipios { get; set; } = new List<Municipio>();
}
