namespace Dashboard_v2.Domain.Entities;

/// <summary>Proyecto Empresarial (PE).</summary>
public class ProyectoEmpresarial : ProyectoEnEjecucion
{
    public override string TipoIdentificador => "PE";

    /// <summary>M:N: empresas vinculadas al proyecto.</summary>
    public ICollection<Institution> Empresas { get; set; } = new List<Institution>();
}
