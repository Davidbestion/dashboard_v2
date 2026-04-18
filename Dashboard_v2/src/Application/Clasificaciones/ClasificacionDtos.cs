namespace Dashboard_v2.Application.Clasificaciones;

public record ClasificacionDto
{
    public string Id { get; init; } = default!;
    public string Nombre { get; init; } = default!;
}
