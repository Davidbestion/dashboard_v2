using Dashboard_v2.Domain.Common;

namespace Dashboard_v2.Domain.Entities;

/// <summary>Regulation, standard, or technical norm published by institution researchers.</summary>
public class Norma : StringAuditableEntity
{
    public string Titulo { get; set; } = default!;

    public int? TipoNormaId { get; set; }
    public TipoNorma? TipoNorma { get; set; }

    // Institución emisora (1..1)
    public string InstitutionId { get; set; } = default!;
    public Institution Institution { get; set; } = default!;

    /// <summary>Autores que son creadores de esta norma (N:M).</summary>
    public ICollection<AuthorNorma> Creadores { get; set; } = new List<AuthorNorma>();
}
