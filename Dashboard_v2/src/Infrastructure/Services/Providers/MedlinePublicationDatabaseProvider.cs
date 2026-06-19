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
/// Resolves journal database membership by querying the NLM E-utilities API.
/// A match means the journal is currently indexed in MEDLINE (Group 2).
/// NLM E-utilities docs: https://www.ncbi.nlm.nih.gov/books/NBK25499/
/// No API key required; rate limit is 3 req/s without key (well within our usage).
/// </summary>
internal sealed class MedlinePublicationDatabaseProvider : IPublicationDatabaseProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<MedlinePublicationDatabaseProvider> _logger;

    public string ProviderName => "MEDLINE";

    public MedlinePublicationDatabaseProvider(HttpClient http, ILogger<MedlinePublicationDatabaseProvider> logger)
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
            var match = await QueryMedlineAsync(issn, ct);
            if (match != null) return match;
        }

        return null;
    }

    private async Task<PublicationDatabaseMatchDto?> QueryMedlineAsync(string issn, CancellationToken ct)
    {
        try
        {
            // NLM esearch: db=nlmcatalog, filter to MEDLINE subset, term = ISSN[ISSN]
            // "medline[subset]" restricts to journals currently indexed in MEDLINE.
            var formatted = FormatIssn(issn);
            var url = $"esearch.fcgi?db=nlmcatalog&term={Uri.EscapeDataString(formatted)}[ISSN]+AND+medline[subset]&retmode=json&retmax=1";
            var resp = await _http.GetAsync(url, ct);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogDebug("NLM API returned {Status} for ISSN {Issn}", (int)resp.StatusCode, issn);
                return null;
            }

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            // Response: { "esearchresult": { "count": "N", ... } }
            if (!doc.RootElement.TryGetProperty("esearchresult", out var result)) return null;
            if (!result.TryGetProperty("count", out var countEl)) return null;
            if (!int.TryParse(countEl.GetString(), out var count) || count == 0) return null;

            _logger.LogDebug("MEDLINE match for ISSN {Issn}", issn);

            return new PublicationDatabaseMatchDto
            {
                DatabaseName = "MEDLINE",
                Group = PublicationGroupMapper.MapToGroup("MEDLINE"),
                Cuartil = null,
                Source = "NLM E-utilities API",
                Confidence = 0.90
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error querying NLM API for ISSN {Issn}", issn);
            return null;
        }
    }

    /// <summary>Inserts a hyphen into a normalized 8-digit ISSN (required by NLM search).</summary>
    private static string FormatIssn(string normalized) =>
        normalized.Length == 8 && normalized.All(char.IsDigit)
            ? $"{normalized[..4]}-{normalized[4..]}"
            : normalized;
}
