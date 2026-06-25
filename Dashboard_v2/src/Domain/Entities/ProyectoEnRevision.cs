namespace Dashboard_v2.Domain.Entities;

/// <summary>Research project in proposal/review stage, not yet executing. Will transition to a concrete ProyectoEnEjecucion subtype upon approval.</summary>
public class ProyectoEnRevision : Proyecto
{
    /// <summary>Tipo de proyecto al que se aspira convertir (p.ej. "PE", "PDL").</summary>
    public string Tipo { get; set; } = default!;

    /// <summary>M:N: situación(es) actual(es) del proyecto en revisión.</summary>
    public ICollection<SituacionProyecto> Situaciones { get; set; } = new List<SituacionProyecto>();
}
