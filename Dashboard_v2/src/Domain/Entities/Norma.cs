namespace Dashboard_v2.Domain.Entities;

public class Norma
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Titulo { get; set; } = default!;
    public string Tipo { get; set; } = default!;

    // Institución emisora (1..1)
    public string InstitutionId { get; set; } = default!;
    public Institution Institution { get; set; } = default!;

    /// <summary>Autores que son creadores de esta norma (N:M).</summary>
    public ICollection<AuthorNorma> Creadores { get; set; } = new List<AuthorNorma>();
}
