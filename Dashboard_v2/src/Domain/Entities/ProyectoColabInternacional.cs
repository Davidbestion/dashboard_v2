namespace Dashboard_v2.Domain.Entities;

/// <summary>International Collaboration Project (PRCI): multinational research with international funding.</summary>
public class ProyectoColabInternacional : ProyectoEnEjecucion
{
    public override string TipoIdentificador => "PRCI";

    /// <summary>M:N: fuentes de financiación internacionales del proyecto.</summary>
    public ICollection<FuenteFinanciacion> FuentesFinanciacion { get; set; } = new List<FuenteFinanciacion>();

    public string TerminosReferencia { get; set; } = default!;
}
