using Dashboard_v2.Domain.Common;

namespace Dashboard_v2.Domain.Entities;

public class Patente : IAuditableEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Titulo { get; set; } = default!;
    public string NumeroSolicitudConcesion { get; set; } = default!;
    public bool EsNacional { get; set; }

    /// <summary>Autores que son creadores de esta patente (N:M).</summary>
    public ICollection<AuthorPatente> Creadores { get; set; } = new List<AuthorPatente>();

    /// <summary>Proyectos de los que esta patente es resultado (N:M).</summary>
    public ICollection<ProyectoPatente> ProyectosDerivados { get; set; } = new List<ProyectoPatente>();

    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}
