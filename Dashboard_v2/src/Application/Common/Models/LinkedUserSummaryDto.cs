namespace Dashboard_v2.Application.Common.Models;

/// <summary>
/// Resumen enriquecido de un usuario pensado para componentes visuales de selección.
/// Expone nombre, correo, categorías académicas y adscripción institucional
/// para evitar llamadas adicionales desde el frontend al construir tarjetas.
/// </summary>
public sealed record LinkedUserSummaryDto
{
    public string Id { get; init; } = default!;
    public string UserName { get; init; } = default!;
    public string UserLastName1 { get; init; } = default!;
    public string? UserLastName2 { get; init; }
    public string Email { get; init; } = default!;
    public bool IsTrained { get; init; }
    public int ScientificCategory { get; init; }
    public int TeachingCategory { get; init; }
    public int InvestigationCategory { get; init; }
    public string? AreaId { get; init; }
    public string? AreaNombre { get; init; }
    public string? UniversidadId { get; init; }
    public string? UniversidadNombre { get; init; }
}
