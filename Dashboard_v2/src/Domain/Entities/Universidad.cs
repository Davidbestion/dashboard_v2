using Dashboard_v2.Domain.Common;

namespace Dashboard_v2.Domain.Entities;

public class Universidad : IAuditableEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Nombre { get; set; } = default!;

    // Navegación
    public ICollection<Area> Areas { get; set; } = new List<Area>();

    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}
