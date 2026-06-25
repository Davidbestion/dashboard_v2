namespace Dashboard_v2.Domain.Entities;

/// <summary>
/// Local Development Project (PDL): research contributing to local community development.
/// Always sets TributaDesarrolloLocal = true.
/// </summary>
public class ProyectoDesarrolloLocal : ProyectoEnEjecucion
{
    public override string TipoIdentificador => "PDL";

    // PDL projects always contribute to local development by definition.

    /// <summary>FK al municipio donde se ejecuta el proyecto (relación 1:1).</summary>
    public int MunicipioId { get; set; }
    /// <summary>Municipality where this local development project is executed.</summary>
    public Municipio Municipio { get; set; } = null!;
}
