using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.Authors;

public record AuthorSearchDto(string Id, string Name, string LastName, string? FirstName);

/// <summary>
/// Resultado de búsqueda para seleccionar coautores.
/// El identificador representa el valor que el cliente debe reenviar al backend:
/// puede ser un <c>Author.Id</c> o un <c>User.Id</c> según <see cref="Type"/>.
/// Cuando existe una cuenta vinculada, <see cref="LinkedUser"/> permite renderizar la tarjeta completa.
/// </summary>
public sealed record CoauthorSearchDto
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Type { get; init; } = default!;
    public LinkedUserSummaryDto? LinkedUser { get; init; }
}

public record PotentialAuthorMatchDto(string Id, string Name);

public record PotentialAuthorMatchesDto(
	IReadOnlyList<PotentialAuthorMatchDto> ExactMatches,
	IReadOnlyList<PotentialAuthorMatchDto> FuzzyMatches);

// ── External-author resolution ─────────────────────────────────────────────

/// <summary>
/// Coincidencia encontrada en el sistema para un nombre externo (CrossRef / OpenAIRE).
/// </summary>
public sealed record ExternalAuthorMatchDto
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string? FirstName { get; init; }
    /// <summary>Snapshot del usuario vinculado, cuando existe.</summary>
    public LinkedUserSummaryDto? LinkedUser { get; init; }
}

/// <summary>
/// Resultado de la resolución de un único nombre externo.
/// <see cref="Match"/> es <c>null</c> cuando no hay ningún autor en el sistema
/// que coincida con <see cref="ExternalName"/>.
/// </summary>
public sealed record ExternalAuthorResolutionDto
{
    public string ExternalName { get; init; } = default!;
    public ExternalAuthorMatchDto? Match { get; init; }
}

