namespace Dashboard_v2.Domain.Entities;

/// <summary>Proyecto No Empresarial (PNE).</summary>
public class ProyectoNoEmpresarial : ProyectoEnEjecucion
{
    public override string TipoIdentificador => "PNE";
    public string EntidadNoEmpresarial { get; set; } = default!;
}
