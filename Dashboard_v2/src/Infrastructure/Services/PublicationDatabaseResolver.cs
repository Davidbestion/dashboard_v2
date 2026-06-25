using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Publications;
using Microsoft.Extensions.Logging;

namespace Dashboard_v2.Infrastructure.Services;

/// <summary>
/// Resolves the best bibliographic database for a publication by querying all registered
/// <see cref="IPublicationDatabaseProvider"/> implementations in priority order.
/// Returns the first successful match.
/// </summary>
public class PublicationDatabaseResolver : IPublicationDatabaseResolver
{
    private readonly IEnumerable<IPublicationDatabaseProvider> _providers;
    private readonly ILogger<PublicationDatabaseResolver> _logger;

    public PublicationDatabaseResolver(
        IEnumerable<IPublicationDatabaseProvider> providers,
        ILogger<PublicationDatabaseResolver> logger)
    {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Tries each provider in registration order and returns the first match.
    /// Returns <c>null</c> if no provider recognizes the publication.
    /// </summary>
    /// <inheritdoc/>
    public async Task<PublicationDatabaseMatchDto?> ResolveByIssnsAsync(IEnumerable<string> issns, DateOnly? publishedDate = null, CancellationToken ct = default)
    {
        var issnList = issns?
            .Where(i => !string.IsNullOrWhiteSpace(i))
            .Select(PublicationGroupMapper.NormalizeIssn)
            .Distinct()
            .ToList() ?? [];

        if (issnList.Count == 0) return null;

        _logger.LogDebug("Resolving database for ISSNs: {Issns}", string.Join(", ", issnList));

        foreach (var provider in _providers)
        {
            try
            {
                var result = await provider.TryResolveAsync(issnList, publishedDate, ct);
                if (result != null)
                {
                    _logger.LogDebug("Provider {Provider} resolved ISSNs {Issns} → {Database} (group {Group})",
                        provider.ProviderName, string.Join(", ", issnList), result.DatabaseName, result.Group);
                    return result;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Provider {Provider} threw an error for ISSNs {Issns}; continuing to next provider.",
                    provider.ProviderName, string.Join(", ", issnList));
            }
        }

        _logger.LogDebug("No provider could resolve database for ISSNs: {Issns}", string.Join(", ", issnList));
        return null;
    }
}

