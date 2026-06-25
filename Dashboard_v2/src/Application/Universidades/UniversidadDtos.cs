namespace Dashboard_v2.Application.Universidades;

/// <summary>
/// University with its areas.
/// </summary>
public record UniversidadDto
{
    public string Id { get; init; } = default!;
    public string Nombre { get; init; } = default!;
}
