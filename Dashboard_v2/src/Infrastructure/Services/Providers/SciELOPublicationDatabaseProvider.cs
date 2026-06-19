using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Publications;
using Microsoft.Extensions.Logging;

namespace Dashboard_v2.Infrastructure.Services.Providers;

/// <summary>
/// Resolves journal database membership by querying the SciELO Article Meta API.
/// A match means the journal is indexed in SciELO (Group 2).
/// API docs: https://articlemeta.scielo.org/api/v1/
/// No authentication required.
/// </summary>
internal sealed class SciELOPublicationDatabaseProvider : IPublicationDatabaseProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<SciELOPublicationDatabaseProvider> _logger;

    public string ProviderName => "SciELO";

    public SciELOPublicationDatabaseProvider(HttpClient http, ILogger<SciELOPublicationDatabaseProvider> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<PublicationDatabaseMatchDto?> TryResolveAsync(IEnumerable<string> issns, DateOnly? publishedDate = null, CancellationToken ct = default)
    {
        var issnList = issns?.Where(i => !string.IsNullOrWhiteSpace(i)).ToList() ?? [];
        if (issnList.Count == 0) return null;

        foreach (var issn in issnList)
        {
            var match = await QuerySciELOAsync(issn, ct);
            if (match != null) return match;
        }

        return null;
    }

    private async Task<PublicationDatabaseMatchDto?> QuerySciELOAsync(string issn, CancellationToken ct)
    {
        try
        {
            // SciELO Article Meta: GET /api/v1/journal/?issn={ISSN}
            // Returns 200 with journal JSON if found, 404 if not indexed.
            var formatted = FormatIssn(issn);
            var url = $"api/v1/journal/?issn={Uri.EscapeDataString(formatted)}";
            var resp = await _http.GetAsync(url, ct);

            if (resp.StatusCode == HttpStatusCode.NotFound) return null;

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogDebug("SciELO API returned {Status} for ISSN {Issn}", (int)resp.StatusCode, issn);
                return null;
            }

            var content = await resp.Content.ReadAsStringAsync(ct);
            // Guard against empty or null-like responses
            var trimmed = content?.Trim() ?? string.Empty;
            if (trimmed.Length == 0 || trimmed == "null" || trimmed == "{}" || trimmed == "[]")
                return null;

            _logger.LogDebug("SciELO match for ISSN {Issn}", issn);

            return new PublicationDatabaseMatchDto
            {
                DatabaseName = "SciELO",
                Group = PublicationGroupMapper.MapToGroup("SciELO"),
                Cuartil = null,
                Source = "SciELO Article Meta API",
                Confidence = 0.90
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error querying SciELO API for ISSN {Issn}", issn);
            return null;
        }
    }

    /// <summary>Inserts a hyphen into a normalized 8-digit ISSN.</summary>
    private static string FormatIssn(string normalized) =>
        normalized.Length == 8 && normalized.All(char.IsDigit)
            ? $"{normalized[..4]}-{normalized[4..]}"
            : normalized;
}
