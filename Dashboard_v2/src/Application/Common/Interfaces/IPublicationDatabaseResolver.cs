using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dashboard_v2.Application.Publications;

namespace Dashboard_v2.Application.Common.Interfaces;

/// <summary>
/// Service responsible for resolving which bibliographic databases a journal
/// (identified by ISSN(s)) is covered by, and mapping that information to the
/// project's local `Group` semantics.
/// </summary>
public interface IPublicationDatabaseResolver
{
    /// <summary>
    /// Given one or more ISSNs, attempt to determine the best matching
    /// bibliographic database and corresponding local group/cuartil.
    /// </summary>
    /// <param name="issns">ISSNs (normalized) to look up.</param>
    Task<PublicationDatabaseMatchDto?> ResolveByIssnsAsync(IEnumerable<string> issns, CancellationToken ct = default);
}
