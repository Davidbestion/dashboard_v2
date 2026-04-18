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
    public string Municipio { get; set; } = default!;
}
