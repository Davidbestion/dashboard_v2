using Dashboard_v2.Application.Common.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dashboard_v2.Application.Authors;

/// <summary>
/// Application service for author search, linking, and external-name resolution operations.
/// </summary>
public interface IAuthorService
{
    /// <summary>
    /// Links an existing <see cref="Dashboard_v2.Domain.Entities.Author"/> entity to the current user's account.
    /// Returns a failure result if the user or author is already linked.
    /// </summary>
    Task<Result> LinkToUserAsync(string authorId, CancellationToken ct = default);

    /// <summary>
    /// Searches authors by name. Returns an empty list if the query is shorter than 2 characters.
    /// </summary>
    Task<List<AuthorSearchDto>> SearchAsync(string q, CancellationToken ct = default);

    /// <summary>
    /// Searches both Author entities and User profiles for co-author assignment.
    /// Results include a 'Type' field indicating the source ('author' or 'user').
    /// </summary>
    Task<List<CoauthorSearchDto>> SearchCoauthorsAsync(string q, CancellationToken ct = default);

    /// <summary>
    /// Returns candidate Author entities whose name is similar to the current user's name,
    /// for the user to review and optionally claim as their author profile.
    /// </summary>
    Task<PotentialAuthorMatchesDto> GetPotentialAuthorMatchesAsync(CancellationToken ct = default);

    /// <summary>
    /// Para cada nombre externo (proveniente de CrossRef / OpenAIRE) busca si ya existe
    /// un autor coincidente en el sistema. No crea autores nuevos.
    /// </summary>
    Task<List<ExternalAuthorResolutionDto>> ResolveExternalAuthorsAsync(
        List<string> names, CancellationToken ct = default);
}
