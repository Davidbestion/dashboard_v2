namespace Dashboard_v2.Domain.Entities;

/// <summary>Program Support Project (PAP): research that supports a national, sectorial, or territorial program.</summary>
public class ProyectoApoyoPrograma : ProyectoEnEjecucion
{
    public override string TipoIdentificador => "PAP";

    /// <summary>M:N: programas a los que apoya el proyecto.</summary>
    public ICollection<Programa> Programas { get; set; } = new List<Programa>();

    public TipoPAP TipoPAP { get; set; }
}
