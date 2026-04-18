namespace Dashboard_v2.Domain.Entities;

/// <summary>Proyecto en fase de revisión (no en ejecución aún).</summary>
public class ProyectoEnRevision : Proyecto
{
    /// <summary>Situación actual del proyecto en revisión.</summary>
    public string Situacion { get; set; } = default!;
    /// <summary>Tipo de proyecto en revisión.</summary>
    public string Tipo { get; set; } = default!;
}
