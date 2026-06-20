namespace Dashboard_v2.Domain.Entities;

/// <summary>Proyecto de Apoyo a Programa (PAP). Tipo: N=Nacional, S=Sectorial, T=Territorial.</summary>
public class ProyectoApoyoPrograma : ProyectoEnEjecucion
{
    public override string TipoIdentificador => "PAP";

    /// <summary>M:N: programas a los que apoya el proyecto.</summary>
    public ICollection<Programa> Programas { get; set; } = new List<Programa>();

    public TipoPAP TipoPAP { get; set; }
}
