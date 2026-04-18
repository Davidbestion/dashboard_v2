namespace Dashboard_v2.Domain.Entities;

/// <summary>Proyecto de Colaboración Internacional (PRCI).</summary>
public class ProyectoColabInternacional : ProyectoEnEjecucion
{
    public override string TipoIdentificador => "PRCI";
    public string FuenteFinanciacion { get; set; } = default!;
    public string TerminosReferencia { get; set; } = default!;
}
