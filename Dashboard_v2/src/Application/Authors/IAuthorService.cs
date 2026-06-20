using Dashboard_v2.Application.Common.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dashboard_v2.Application.Authors;

public interface IAuthorService
{
    Task<Result> LinkToUserAsync(string authorId, CancellationToken ct = default);
    Task<List<AuthorSearchDto>> SearchAsync(string q, CancellationToken ct = default);
    Task<List<CoauthorSearchDto>> SearchCoauthorsAsync(string q, CancellationToken ct = default);
    Task<PotentialAuthorMatchesDto> GetPotentialAuthorMatchesAsync(CancellationToken ct = default);

    /// <summary>
    /// Para cada nombre externo (proveniente de CrossRef / OpenAIRE) busca si ya existe
    /// un autor coincidente en el sistema. No crea autores nuevos.
    /// </summary>
    Task<List<ExternalAuthorResolutionDto>> ResolveExternalAuthorsAsync(
        List<string> names, CancellationToken ct = default);
}
