using Dashboard_v2.Domain.Common;

namespace Dashboard_v2.Domain.Entities;

public class Registro : IAuditableEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Titulo { get; set; } = default!;
    public string NumeroCertificado { get; set; } = default!;
    public bool EsInformatico { get; set; }

    // País donde se registró (1..1)
    public int CountryId { get; set; }
    public Country Country { get; set; } = default!;

    // Institución que otorga (1..1)
    public string InstitutionId { get; set; } = default!;
    public Institution Institution { get; set; } = default!;

    /// <summary>Archivo de evidencia/certificado adjunto (opcional).</summary>
    public int? EvidenceFileId { get; set; }
    public StoredFile? EvidenceFile { get; set; }

    /// <summary>Autores que son creadores de este registro (N:M).</summary>
    public ICollection<AuthorRegistro> Creadores { get; set; } = new List<AuthorRegistro>();

    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}
