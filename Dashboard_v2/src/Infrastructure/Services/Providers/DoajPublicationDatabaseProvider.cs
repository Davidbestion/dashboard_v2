using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Publications;
using Microsoft.Extensions.Logging;

namespace Dashboard_v2.Infrastructure.Services.Providers;

/// <summary>
/// Resolves journal database membership by querying the free DOAJ REST API.
/// A match means the journal is open-access and indexed in DOAJ (Group 3).
/// DOAJ API docs: https://doaj.org/api/docs
/// </summary>
internal sealed class DoajPublicationDatabaseProvider : IPublicationDatabaseProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<DoajPublicationDatabaseProvider> _logger;

    public string ProviderName => "DOAJ";

    public DoajPublicationDatabaseProvider(HttpClient http, ILogger<DoajPublicationDatabaseProvider> logger)
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
            var match = await QueryDoajAsync(issn, ct);
            if (match != null) return match;
        }

        return null;
    }

    private async Task<PublicationDatabaseMatchDto?> QueryDoajAsync(string issn, CancellationToken ct)
    {
        try
        {
            // DOAJ Search API v3: GET /api/v3/search/journals/{query}?pageSize=1
            // The query is a path segment, not a query parameter.
            // Use the hyphenated form (e.g. "2739-039X") — DOAJ indexes ISSNs with the hyphen.
            var url = $"search/journals/issn%3A{Uri.EscapeDataString(FormatIssn(issn))}?pageSize=1";
            var resp = await _http.GetAsync(url, ct);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogDebug("DOAJ API returned {Status} for ISSN {Issn}", (int)resp.StatusCode, issn);
                return null;
            }

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            if (!doc.RootElement.TryGetProperty("total", out var totalEl) || totalEl.GetInt32() == 0)
                return null;

            // Journal found in DOAJ — extract title if available for logging
            var title = TryGetJournalTitle(doc.RootElement);
            _logger.LogDebug("DOAJ match for ISSN {Issn}: {Title}", issn, title ?? "(unknown)");

            return new PublicationDatabaseMatchDto
            {
                DatabaseName = "DOAJ",
                Group = PublicationGroupMapper.MapToGroup("DOAJ"),
                Cuartil = null, // DOAJ does not publish quartile info
                Source = "DOAJ API",
                Confidence = 0.90
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
    }

    /// <summary>Inserts a hyphen into a normalized 8-char ISSN, e.g. "2739039X" → "2739-039X".</summary>
    private static string FormatIssn(string normalized) =>
        normalized.Length == 8
            ? $"{normalized[..4]}-{normalized[4..]}"
            : normalized;

    private static string? TryGetJournalTitle(JsonElement root)
    {
        try
        {
            if (root.TryGetProperty("results", out var results) &&
                results.ValueKind == JsonValueKind.Array &&
                results.GetArrayLength() > 0 &&
                results[0].TryGetProperty("bibjson", out var bib) &&
                bib.TryGetProperty("title", out var titleEl))
            {
                return titleEl.GetString();
            }
        }
        catch { /* best-effort */ }
        return null;
    }
}
