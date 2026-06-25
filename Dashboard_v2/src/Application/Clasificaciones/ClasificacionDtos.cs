namespace Dashboard_v2.Application.Clasificaciones;

/// <summary>Project classification nomenclator entry (Id and name).</summary>
public record ClasificacionDto
{
    public string Id { get; init; } = default!;
    public string Nombre { get; init; } = default!;
}
