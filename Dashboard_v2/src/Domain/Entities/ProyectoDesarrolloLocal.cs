namespace Dashboard_v2.Domain.Entities;

/// <summary>
/// Proyecto de Desarrollo Local (PDL).
/// Por definición, <see cref="ProyectoEnEjecucion.TributaDesarrolloLocal"/> es
/// siempre <c>true</c> para este tipo: los comandos Create y Update lo fijan
/// explícitamente y nunca lo exponen como campo editable.
/// </summary>
public class ProyectoDesarrolloLocal : ProyectoEnEjecucion
{
    public override string TipoIdentificador => "PDL";

    /// <summary>FK al municipio donde se ejecuta el proyecto (relación 1:1).</summary>
    public int MunicipioId { get; set; }
    public Municipio Municipio { get; set; } = null!;
}
