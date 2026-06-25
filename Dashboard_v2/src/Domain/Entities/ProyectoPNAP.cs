namespace Dashboard_v2.Domain.Entities;

/// <summary>High-Potential Project (PNAP): strategic research with priority national funding.</summary>
public class ProyectoPNAP : ProyectoEnEjecucion
{
    public override string TipoIdentificador => "PNAP";

    /// <summary>M:N: fuentes de financiación de la UH para el proyecto.</summary>
    public ICollection<FuenteFinanciacion> FuentesFinanciacion { get; set; } = new List<FuenteFinanciacion>();
}
