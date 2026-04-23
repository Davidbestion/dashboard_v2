using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.Authors;

public record AuthorSearchDto(string Id, string Name);

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
