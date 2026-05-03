using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dashboard_v2.Application.Publications;

namespace Dashboard_v2.Application.Common.Interfaces;

/// <summary>
/// Abstraction for a single source capable of resolving bibliographic database
/// information from a set of ISSNs. Multiple providers are chained by
/// <see cref="IPublicationDatabaseResolver"/>.
/// </summary>
public interface IPublicationDatabaseProvider
{
    /// <summary>Human-readable name of this provider, e.g. "LocalCsv" or "DOAJ".</summary>
    string ProviderName { get; }

    /// <summary>
    /// Try to resolve database/group information from the supplied ISSNs.
    /// Returns null if this provider cannot determine a match.
    /// </summary>
    Task<PublicationDatabaseMatchDto?> TryResolveAsync(IEnumerable<string> issns, CancellationToken ct = default);
}
