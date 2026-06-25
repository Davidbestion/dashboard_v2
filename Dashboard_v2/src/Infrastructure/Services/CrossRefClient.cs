using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Publications;
using Dashboard_v2.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dashboard_v2.Infrastructure.Services;

/// <summary>
/// HTTP client for the CrossRef API. Fetches publication metadata by DOI with automatic
/// retry/backoff. Normalizes DOIs before lookup to handle common URL and prefix variants.
/// </summary>
public class CrossRefClient : ICrossRefClient
{
    private static readonly Regex DoiPattern = new(@"10\.\d{4,9}/\S+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly HttpClient _http;
    private readonly ILogger<CrossRefClient> _logger;
    private readonly CrossRefOptions _opts;

    public CrossRefClient(HttpClient http, IOptions<CrossRefOptions> options, ILogger<CrossRefClient> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _opts = options?.Value ?? new CrossRefOptions();
    }

    /// <summary>
    /// Fetches metadata for the given DOI from CrossRef. Returns <c>null</c> if not found
    /// or the response cannot be parsed.
    /// </summary>
    public async Task<PublicationCrossRefDto?> GetWorkByDoiAsync(string doi, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(doi))
            return null;

        var normalized = NormalizeDoi(doi);
        try
        {
            var resp = await SendWithRetriesAsync(ct => _http.GetAsync($"works/{Uri.EscapeDataString(normalized)}", ct), ct);
            if (resp is null || !resp.IsSuccessStatusCode)
                return null;

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            if (!doc.RootElement.TryGetProperty("message", out var message))
                return null;

            return ParseMessageToDto(message);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "CrossRef timed out for DOI {Doi}", doi);
            throw new Dashboard_v2.Application.Publications.CrossRefTimeoutException("CrossRef did not respond in time", ex);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching CrossRef work for DOI {Doi}", doi);
            return null;
        }
    }

    public async Task<List<PublicationCrossRefDto>> SearchWorksByTitleAsync(string title, int rows = 5, CancellationToken ct = default)
    {
        var results = new List<PublicationCrossRefDto>();
        if (string.IsNullOrWhiteSpace(title))
            return results;

        try
        {
            var query = $"works?query.title={Uri.EscapeDataString(title)}&rows={rows}";
            var resp = await SendWithRetriesAsync(ct => _http.GetAsync(query, ct), ct);
            if (resp is null || !resp.IsSuccessStatusCode)
                return results;

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            if (!doc.RootElement.TryGetProperty("message", out var message))
                return results;

            if (!message.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
                return results;

            foreach (var item in items.EnumerateArray())
            {
                var dto = ParseMessageToDto(item);
                if (dto != null)
                    results.Add(dto);
            }

            return results;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "CrossRef timed out searching for title {Title}", title);
            throw new Dashboard_v2.Application.Publications.CrossRefTimeoutException("CrossRef did not respond in time", ex);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error searching CrossRef for title {Title}", title);
            return results;
        }
    }

    private async Task<HttpResponseMessage?> SendWithRetriesAsync(Func<CancellationToken, Task<HttpResponseMessage>> operation, CancellationToken ct)
    {
        var attempt = 0;
        while (true)
        {
            attempt++;
            try
            {
                var resp = await operation(ct);
                if (resp.IsSuccessStatusCode)
                    return resp;

                // Retry on 429 or 5xx
                if ((int)resp.StatusCode == 429 || (int)resp.StatusCode >= 500)
                {
                    if (attempt > _opts.MaxRetries)
                        return resp;

                    var delay = ComputeDelayMs(attempt);
                    _logger.LogWarning("CrossRef request returned {Status}. Retrying in {Delay}ms (attempt {Attempt}/{MaxRetries})", (int)resp.StatusCode, delay, attempt, _opts.MaxRetries);
                    try { await Task.Delay(delay, ct); } catch (TaskCanceledException) { return null; }
                    continue;
                }

                // Non-retriable status
                return resp;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                if (attempt > _opts.MaxRetries)
                {
                    _logger.LogWarning(ex, "CrossRef request failed after {Attempt} attempts", attempt);
                    throw;
                }

                var delay = ComputeDelayMs(attempt);
                _logger.LogWarning(ex, "CrossRef request error, retrying in {Delay}ms (attempt {Attempt}/{MaxRetries})", delay, attempt, _opts.MaxRetries);
                try { await Task.Delay(delay, ct); } catch (TaskCanceledException) { return null; }
                continue;
            }
        }
    }

    internal int ComputeDelayMs(int attempt)
    {
        var exp = Math.Pow(2, attempt - 1);
        var baseMs = Math.Max(1, _opts.BaseDelayMs);
        var raw = baseMs * exp;
        var jitter = Random.Shared.NextDouble() * (_opts.JitterFactorMax - _opts.JitterFactorMin) + _opts.JitterFactorMin;
        var delay = (int)Math.Min(60000, raw * jitter);
        return delay;
    }

    internal static string NormalizeDoi(string doi)
    {
        var s = doi.Trim();

        var match = DoiPattern.Match(s);
        if (match.Success)
            s = match.Value;

        s = s.ToLowerInvariant();
        s = System.Text.RegularExpressions.Regex.Replace(s, "^https?://", "");
        s = s.Replace("doi:", "").Replace("dx.doi.org/", "").Replace("doi.org/", "");

        var idx = s.IndexOfAny(['?', '#']);
        if (idx >= 0)
            s = s[..idx];

        return s.Trim().TrimEnd('/').TrimEnd('.', ',', ';', ':');
    }

    internal static PublicationCrossRefDto? ParseMessageToDto(JsonElement message)
    {
        try
        {
            var dto = new PublicationCrossRefDto
            {
                Doi = TryGetString(message, "DOI"),
                Url = TryGetString(message, "URL"),
                Title = TryGetString(message, "title") ?? TryGetStringArrayFirst(message, "title"),
                ContainerTitle = TryGetStringArrayFirst(message, "container-title"),
                Volume = TryGetString(message, "volume"),
                Issue = TryGetString(message, "issue"),
                Page = TryGetString(message, "page"),
                Publisher = TryGetString(message, "publisher"),
                Type = TryGetString(message, "type")
            };

            var issnArr = TryGetStringArray(message, "ISSN");
            if (issnArr != null)
                dto.Issns.AddRange(issnArr);

            var isbnArr = TryGetStringArray(message, "ISBN");
            if (isbnArr != null)
                dto.Isbns.AddRange(isbnArr);

            // Authors
            if (message.TryGetProperty("author", out var authors) && authors.ValueKind == JsonValueKind.Array)
            {
                foreach (var a in authors.EnumerateArray())
                {
                    var given  = TryGetString(a, "given");
                    var family = TryGetString(a, "family");
                    // Formato bibliográfico: "Apellidos, Nombres"
                    string? name;
                    if (string.IsNullOrWhiteSpace(family))
                        name = given;
                    else if (string.IsNullOrWhiteSpace(given))
                        name = family;
                    else
                        name = $"{family}, {given}";
                    if (!string.IsNullOrWhiteSpace(name))
                        dto.Authors.Add(name);
                }
            }

            // Published date
            var published = TryGetDateParts(message, "published-print") ?? TryGetDateParts(message, "published-online") ?? TryGetDateParts(message, "created");
            dto.Published = published;

            return dto;
        }
        catch (Exception ex)
        {
            // CrossRef response could not be parsed; returning null signals no match to caller.
            // ParseMessageToDto is static, so logging deferred to callers that have _logger.
            _ = ex; // suppress unused variable warning
            return null;
        }
    }

    private static string? TryGetString(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v))
            return null;

        return v.ValueKind switch
        {
            JsonValueKind.String => v.GetString(),
            JsonValueKind.Number => v.GetRawText(),
            JsonValueKind.Array when v.GetArrayLength() > 0 && v[0].ValueKind == JsonValueKind.String => v[0].GetString(),
            _ => v.GetRawText()
        };
    }

    private static string? TryGetStringArrayFirst(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v))
            return null;

        if (v.ValueKind == JsonValueKind.Array && v.GetArrayLength() > 0)
        {
            var first = v[0];
            if (first.ValueKind == JsonValueKind.String)
                return first.GetString();
        }

        return null;
    }

    private static List<string>? TryGetStringArray(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v))
            return null;

        if (v.ValueKind != JsonValueKind.Array)
            return null;

        var list = new List<string>();
        foreach (var e in v.EnumerateArray())
        {
            if (e.ValueKind == JsonValueKind.String)
                list.Add(e.GetString()!);
        }

        return list;
    }

    private static string? TryGetDateParts(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v))
            return null;

        if (!v.TryGetProperty("date-parts", out var parts) || parts.ValueKind != JsonValueKind.Array)
            return null;

        var first = parts[0];
        if (first.ValueKind != JsonValueKind.Array)
            return null;

        var items = first.EnumerateArray().Select(x => x.GetRawText().Trim('"')).ToArray();
        if (items.Length == 0)
            return null;

        // Build date string yyyy-MM-dd or shorter
        var date = items[0];
        if (items.Length > 1)
            date += "-" + items[1].PadLeft(2, '0');
        if (items.Length > 2)
            date += "-" + items[2].PadLeft(2, '0');

        return date;
    }
}
