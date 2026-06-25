namespace Dashboard_v2.Domain.Entities;

/// <summary>Type/category of regulation or standard (e.g. Decree, Technical Standard, Resolution).</summary>
public class TipoNorma
{
    public int Id { get; set; }
    public string Nombre { get; set; } = default!;
}
