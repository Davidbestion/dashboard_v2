namespace Dashboard_v2.Domain.Entities;

/// <summary>
/// Clase base abstracta para proyectos en ejecución.
/// Especializada en PE, PAP, PDL, PNE, PRCI y PNAP.
/// </summary>
public abstract class ProyectoEnEjecucion : Proyecto
{
    public DateOnly FechaInicio { get; set; }
    public DateOnly? FechaCierre { get; set; }
    public string CodigoProyecto { get; set; } = default!;

    /// <summary>
    /// Identificador estable del tipo de proyecto en ejecución (p.ej. "PE", "PDL").
    /// Usado por <see cref="ProyectoEnRevision.Tipo"/> para indicar en qué tipo se convertirá.
    /// </summary>
    public abstract string TipoIdentificador { get; }

    /// <summary>
    /// Indica si el proyecto tributa al desarrollo local.
    /// Para <see cref="ProyectoDesarrolloLocal"/> (PDL) este valor es siempre <c>true</c>.
    /// </summary>
    public bool TributaDesarrolloLocal { get; set; }

    // M:N: estado(s) de ejecución del proyecto
    public ICollection<EstadoProyecto> EstadosDeEjecucion { get; set; } = new List<EstadoProyecto>();

    // M:N: entidades ejecutoras (principales y participantes, separadas semánticamente)
    public ICollection<Institution> EntidadesEjecutorasPrincipales { get; set; } = new List<Institution>();
    public ICollection<Institution> EntidadesEjecutorasParticipantes { get; set; } = new List<Institution>();

    // M:N: sectores y ejes estratégicos a los que contribuye
    public ICollection<SectorEstrategico> SectoresEstrategicos { get; set; } = new List<SectorEstrategico>();
    public ICollection<EjeEstrategico> EjesEstrategicos { get; set; } = new List<EjeEstrategico>();
}
