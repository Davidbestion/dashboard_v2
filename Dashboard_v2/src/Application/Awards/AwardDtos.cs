using Dashboard_v2.Domain.Entities;

namespace Dashboard_v2.Application.Awards;

/// <summary>Premio recibido por el usuario actual (fila de UserAwarded + detalles del premio).</summary>
public record AwardDto
{
    /// <summary>Id de la fila UserAwarded (se usa para PUT/DELETE).</summary>
    public int Id { get; init; }
    public int AwardId { get; init; }
    public string AwardName { get; init; } = default!;
    public int AwardTypeId { get; init; }
    public string AwardTypeName { get; init; } = default!;
    public DateTime AwardedAt { get; init; }
    // Usuario que recibió el premio
    public string UserId { get; init; } = default!;
    public string UserDisplayName { get; init; } = default!;
}

/// <summary>Elemento del catálogo de premios reutilizable por los formularios.</summary>
public record AwardCatalogDto
{
    public int Id { get; init; }
    public string AwardName { get; init; } = default!;
    public int AwardTypeId { get; init; }
    public string AwardTypeName { get; init; } = default!;
}
