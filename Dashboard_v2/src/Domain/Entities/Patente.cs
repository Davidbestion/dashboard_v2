using Dashboard_v2.Domain.Common;

namespace Dashboard_v2.Domain.Entities;

/// <summary>Patent or intellectual property filed by institution researchers.</summary>
public class Patente : StringAuditableEntity
{
    public string Titulo { get; set; } = default!;
    public string NumeroSolicitudConcesion { get; set; } = default!;
    public bool EsNacional { get; set; }

    /// <summary>Autores que son creadores de esta patente (N:M).</summary>
    public ICollection<AuthorPatente> Creadores { get; set; } = new List<AuthorPatente>();

    /// <summary>Proyectos de los que esta patente es resultado (N:M).</summary>
    public ICollection<ProyectoPatente> ProyectosDerivados { get; set; } = new List<ProyectoPatente>();
}
