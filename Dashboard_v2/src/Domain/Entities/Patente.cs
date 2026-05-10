namespace Dashboard_v2.Domain.Entities;

public class Patente
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Titulo { get; set; } = default!;
    public string NumeroSolicitudConcesion { get; set; } = default!;
    public bool EsNacional { get; set; }

    /// <summary>Autores que son creadores de esta patente (N:M).</summary>
    public ICollection<AuthorPatente> Creadores { get; set; } = new List<AuthorPatente>();

    /// <summary>Proyectos de los que esta patente es resultado (N:M).</summary>
    public ICollection<ProyectoPatente> ProyectosDerivados { get; set; } = new List<ProyectoPatente>();
}
