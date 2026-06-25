namespace Dashboard_v2.Domain.Entities;

/// <summary>Non-Business Project (PNE): research involving public or non-profit entities.</summary>
public class ProyectoNoEmpresarial : ProyectoEnEjecucion
{
    public override string TipoIdentificador => "PNE";

    /// <summary>M:N: entidades no empresariales vinculadas al proyecto.</summary>
    public ICollection<Institution> Entidades { get; set; } = new List<Institution>();
}
