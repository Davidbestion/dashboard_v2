using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dashboard_v2.Application.Publications;

namespace Dashboard_v2.Application.Common.Interfaces;

/// <summary>
/// Client for querying the OpenAIRE Research Graph API to retrieve publication metadata.
/// OpenAIRE aggregates from CrossRef, SciELO, PubMed, institutional repositories, and more.
/// API docs: https://api.openaire.eu/search/publications
/// </summary>
public interface IOpenAireClient
{
    /// <summary>
    /// Fetches a single publication by its DOI.
    /// Returns null if the DOI is not found in OpenAIRE.
    /// </summary>
    Task<PublicationCrossRefDto?> GetWorkByDoiAsync(string doi, CancellationToken ct = default);

    /// <summary>
    /// Searches for publications matching the given title.
    /// </summary>
    Task<List<PublicationCrossRefDto>> SearchWorksByTitleAsync(string title, int rows = 5, CancellationToken ct = default);
}
