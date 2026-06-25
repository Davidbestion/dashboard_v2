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

namespace Dashboard_v2.Infrastructure.Services;

/// <summary>
/// HTTP client for the OpenAIRE research graph API. Fetches open-access publication metadata.
/// OpenAIRE aggregates from CrossRef, SciELO, PubMed, Zenodo, institutional repositories,
/// and many more — making it the best fallback when CrossRef lacks coverage.
///
/// API: https://api.openaire.eu/graph/v1/researchProducts
/// Response format: JSON (default, no explicit parameter needed)
/// Rate limit: unauthenticated ~30 req/min — well within our single-query usage.
///
/// Graph API JSON structure (flat, clean):
/// {
///   "header": { "numFound": N, "page": 1, "pageSize": M },
///   "results": [
///     {
///       "mainTitle": "...",
///       "publicationDate": "YYYY-MM-DD",
///       "publisher": "...",
///       "authors": [ { "fullName": "...", "name": "...", "surname": "..." } ],
///       "container": { "name": "journal", "issnOnline": "...", "issnPrinted": "...",
///                      "vol": "...", "iss": "...", "sp": "...", "ep": "..." },
///       "pid": [ { "scheme": "doi", "value": "10.xxx/xxx" } ]
///     }
///   ]
/// }
/// </summary>
public sealed class OpenAireClient : IOpenAireClient
{
    private readonly HttpClient _http;
    private readonly ILogger<OpenAireClient> _logger;

    public OpenAireClient(HttpClient http, ILogger<OpenAireClient> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── Public interface ─────────────────────────────────────────────────────

    public async Task<PublicationCrossRefDto?> GetWorkByDoiAsync(string doi, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(doi)) return null;

        var normalized = NormalizeDoi(doi);
        try
        {
            // The Graph API uses `pid` for identifier search (scheme-agnostic)
            var url = $"?pid={Uri.EscapeDataString(normalized)}&type=publication";
            var results = await FetchResultsAsync(url, ct);
            return results.FirstOrDefault();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAIRE: error fetching DOI {Doi}", doi);
            return null;
        }
    }

    public async Task<List<PublicationCrossRefDto>> SearchWorksByTitleAsync(string title, int rows = 5, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(title)) return [];

        try
        {
            // `mainTitle` does word-based matching against the title field.
            var url = $"?mainTitle={Uri.EscapeDataString(title)}&type=publication&pageSize={rows}";
            return await FetchResultsAsync(url, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAIRE: error searching title '{Title}'", title);
            return [];
        }
    }

    // ── HTTP + parsing ────────────────────────────────────────────────────────

    private async Task<List<PublicationCrossRefDto>> FetchResultsAsync(string relativeUrl, CancellationToken ct)
    {
        var resp = await _http.GetAsync(relativeUrl, ct);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogDebug("OpenAIRE returned {Status} for {Url}", (int)resp.StatusCode, relativeUrl);
            return [];
        }

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;

        if (!root.TryGetProperty("results", out var resultsEl) || resultsEl.ValueKind != JsonValueKind.Array)
            return [];

        var dtos = new List<PublicationCrossRefDto>();
        foreach (var item in resultsEl.EnumerateArray())
        {
            var dto = ParseResult(item);
            if (dto != null) dtos.Add(dto);
        }
        return dtos;
    }

    private PublicationCrossRefDto? ParseResult(JsonElement item)
    {
        try
        {
            var dto = new PublicationCrossRefDto();

            // Title
            dto.Title = GetString(item, "mainTitle");
            if (string.IsNullOrWhiteSpace(dto.Title)) return null;

            // Publication date
            dto.Published = GetString(item, "publicationDate");

            // Publisher
            dto.Publisher = GetString(item, "publisher");

            // DOI from pids array (Graph API uses "pids", plural)
            if (item.TryGetProperty("pids", out var pids) && pids.ValueKind == JsonValueKind.Array)
            {
                foreach (var pid in pids.EnumerateArray())
                {
                    if (string.Equals(GetString(pid, "scheme"), "doi", StringComparison.OrdinalIgnoreCase))
                    {
                        dto.Doi = GetString(pid, "value");
                        break;
                    }
                }
            }

            // Fallback: originalIds often contains the bare DOI as the first entry (starts with "10.")
            if (string.IsNullOrWhiteSpace(dto.Doi)
                && item.TryGetProperty("originalIds", out var origIds)
                && origIds.ValueKind == JsonValueKind.Array)
            {
                foreach (var oid in origIds.EnumerateArray())
                {
                    var raw = oid.ValueKind == JsonValueKind.String ? oid.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(raw) && raw.StartsWith("10.", StringComparison.Ordinal))
                    {
                        dto.Doi = raw;
                        break;
                    }
                }
            }

            // Authors — preferir campos estructurados (surname/name) cuando estén disponibles.
            // Formato bibliográfico de salida: "Apellidos, Nombres".
            if (item.TryGetProperty("authors", out var authors) && authors.ValueKind == JsonValueKind.Array)
            {
                foreach (var a in authors.EnumerateArray())
                {
                    var surname   = GetString(a, "surname");
                    var givenName = GetString(a, "name");
                    string? authorName;
                    if (!string.IsNullOrWhiteSpace(surname))
                    {
                        authorName = string.IsNullOrWhiteSpace(givenName)
                            ? surname
                            : $"{surname}, {givenName}";
                    }
                    else
                    {
                        // Fallback: fullName ya puede venir en formato "Apellido, Nombre" de algunas fuentes.
                        authorName = GetString(a, "fullName");
                    }
                    if (!string.IsNullOrWhiteSpace(authorName))
                        dto.Authors.Add(authorName);
                }
            }

            // Journal container (issnOnline, issnPrinted, name, vol, iss, pages)
            if (item.TryGetProperty("container", out var container) && container.ValueKind == JsonValueKind.Object)
            {
                dto.ContainerTitle = GetString(container, "name");

                var eissn = GetString(container, "issnOnline");
                var issn  = GetString(container, "issnPrinted");
                if (!string.IsNullOrWhiteSpace(eissn)) dto.Issns.Add(eissn);
                if (!string.IsNullOrWhiteSpace(issn) && issn != eissn) dto.Issns.Add(issn);

                dto.Volume = GetString(container, "vol");
                dto.Issue  = GetString(container, "iss");

                var sp = GetString(container, "sp");
                var ep = GetString(container, "ep");
                if (!string.IsNullOrWhiteSpace(sp))
                    dto.Page = string.IsNullOrWhiteSpace(ep) ? sp : $"{sp}-{ep}";
            }

            dto.Type = "journal-article";

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAIRE: failed to parse a result item");
            return null;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string? GetString(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v)) return null;
        return v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    }

    private static string NormalizeDoi(string doi)
    {
        var s = doi.Trim();
        s = System.Text.RegularExpressions.Regex.Replace(s, @"^https?://", "");
        s = s.Replace("doi.org/", "").Replace("dx.doi.org/", "").Replace("doi:", "");
        var idx = s.IndexOfAny(['?', '#']);
        if (idx >= 0) s = s[..idx];
        return s.Trim().TrimEnd('/');
    }
}

