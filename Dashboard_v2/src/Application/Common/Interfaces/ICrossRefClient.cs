using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dashboard_v2.Application.Publications;

namespace Dashboard_v2.Application.Common.Interfaces;

/// <summary>
/// Client for querying the CrossRef REST API to retrieve publication metadata from DOI or title.
/// </summary>
public interface ICrossRefClient
{
    /// <summary>
    /// Fetches a single publication by its DOI. Returns null if not found.
    /// </summary>
    Task<PublicationCrossRefDto?> GetWorkByDoiAsync(string doi, CancellationToken ct = default);

    /// <summary>
    /// Searches CrossRef for publications matching the given title.
    /// </summary>
    /// <param name="title">Title to search for.</param>
    /// <param name="rows">Maximum number of results to return.</param>
    Task<List<PublicationCrossRefDto>> SearchWorksByTitleAsync(string title, int rows = 5, CancellationToken ct = default);
}
