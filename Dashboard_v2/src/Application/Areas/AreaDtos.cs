namespace Dashboard_v2.Application.Areas;

/// <summary>
/// Academic area with its associated knowledge domains.
/// </summary>
public record AreaDto
{
    public string Id { get; init; } = default!;
    public string Nombre { get; init; } = default!;
    public string? Descripcion { get; init; }
    public string? UniversidadId { get; init; }
    public string? UniversidadNombre { get; init; }
    public IReadOnlyList<string> AreasDelConocimientoIds { get; init; } = [];
}
