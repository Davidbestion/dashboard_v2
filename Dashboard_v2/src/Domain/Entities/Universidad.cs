using Dashboard_v2.Domain.Common;

namespace Dashboard_v2.Domain.Entities;

/// <summary>University entity. Groups academic areas and research groups.</summary>
public class Universidad : StringAuditableEntity
{
    public string Nombre { get; set; } = default!;

    // Navegación
    public ICollection<Area> Areas { get; set; } = new List<Area>();
}
