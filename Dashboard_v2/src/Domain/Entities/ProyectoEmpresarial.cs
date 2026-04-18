namespace Dashboard_v2.Domain.Entities;

/// <summary>Proyecto Empresarial (PE).</summary>
public class ProyectoEmpresarial : ProyectoEnEjecucion
{
    public override string TipoIdentificador => "PE";
    public string Empresa { get; set; } = default!;
}
