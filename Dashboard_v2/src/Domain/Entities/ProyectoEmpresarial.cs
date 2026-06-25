namespace Dashboard_v2.Domain.Entities;

/// <summary>Business Project (PE): research involving private sector enterprises.</summary>
public class ProyectoEmpresarial : ProyectoEnEjecucion
{
    public override string TipoIdentificador => "PE";

    /// <summary>M:N: empresas vinculadas al proyecto.</summary>
    public ICollection<Institution> Empresas { get; set; } = new List<Institution>();
}
