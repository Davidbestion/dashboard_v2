using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Publications;
using Microsoft.Extensions.Logging;

namespace Dashboard_v2.Infrastructure.Services.Providers;

/// <summary>
/// Provider that reads one or more CSV files mapping ISSN -> database.
/// Supports two formats:
///   - Simple (comma-delimited): issn,database[,source[,cuartil]]
///   - Scimago (semicolon-delimited): the official SJR export from scimago.org.
///     Detected automatically when the header starts with "Rank;Sourceid;".
///     Maps all journals to "Scopus" with the SJR Best Quartile.
/// ISSNs are normalised to 8 digits (hyphens stripped) so "1234-5678" and "12345678" match.
/// </summary>
internal sealed class LocalCsvPublicationDatabaseProvider : IPublicationDatabaseProvider
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, (string database, string? cuartil, string? source)> _map = new(StringComparer.OrdinalIgnoreCase);

    public string ProviderName => "LocalCsv";

    public LocalCsvPublicationDatabaseProvider(IEnumerable<string> csvPaths, Microsoft.Extensions.Logging.ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        foreach (var p in csvPaths ?? Array.Empty<string>())
        {
            try
            {
                if (!File.Exists(p))
                {
                    _logger.LogDebug("Local mapping CSV not found: {Path}", p);
                    continue;
                }

                using var sr = new StreamReader(p);
                var header = sr.ReadLine();
                if (header == null) continue;

                if (IsScimagoFormat(header))
                    LoadScimagoFile(sr, p);
                else
                    LoadSimpleFile(header, sr, p);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error loading local CSV mapping {Path}", p);
            }
        }
    }

    // ── Scimago format ────────────────────────────────────────────────────────
    // Header: Rank;Sourceid;Title;Type;Issn;Publisher;...;SJR Best Quartile;...
    // Cols:   0    1        2     3    4    5              9
    // ISSN field: quoted, multiple ISSNs separated by ", " e.g. "15424863, 00079235"
    // Quartile field: Q1 / Q2 / Q3 / Q4 / -

    private static bool IsScimagoFormat(string header) =>
        header.StartsWith("Rank;", StringComparison.OrdinalIgnoreCase) ||
        header.Contains(";Issn;", StringComparison.OrdinalIgnoreCase);

    private void LoadScimagoFile(StreamReader sr, string path)
    {
        var fileName = Path.GetFileName(path);
        var loaded = 0;
        string? line;
        while ((line = sr.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = SplitSemicolonLine(line);
            if (cols.Length < 10) continue;

            // col 4 = Issn (may be "12345678, 87654321")
            var issnField = cols[4];
            if (string.IsNullOrWhiteSpace(issnField)) continue;

            // col 9 = SJR Best Quartile (Q1..Q4 or -)
            var quartile = cols[9].Trim();
            if (quartile == "-") quartile = null;
            if (!string.IsNullOrWhiteSpace(quartile) && !quartile.StartsWith('Q'))
                quartile = null;

            foreach (var rawIssn in issnField.Split(','))
            {
                var issn = PublicationGroupMapper.NormalizeIssn(rawIssn.Trim());
                if (string.IsNullOrWhiteSpace(issn) || !IsValidNormalizedIssn(issn)) continue;
                // Only add if not already present (first file wins, 2025 beats 2021)
                _map.TryAdd(issn, ("Scopus", quartile, fileName));
                loaded++;
            }
        }
        _logger.LogInformation("Loaded {Count} ISSN entries from Scimago file {File}", loaded, fileName);
    }

    /// <summary>Splits a semicolon-delimited line, stripping surrounding quotes from each field.</summary>
    private static string[] SplitSemicolonLine(string line)
    {
        var parts = line.Split(';');
        for (var i = 0; i < parts.Length; i++)
        {
            var p = parts[i].Trim();
            if (p.Length >= 2 && p[0] == '"' && p[^1] == '"')
                parts[i] = p[1..^1].Trim();
            else
                parts[i] = p;
        }
        return parts;
    }

    // ── Simple format ─────────────────────────────────────────────────────────
    // issn,database[,source[,cuartil]]   (header row optional, auto-skipped)

    private void LoadSimpleFile(string firstLine, StreamReader sr, string path)
    {
        var fileName = Path.GetFileName(path);
        var loaded = 0;
        var lineNumber = 1;

        void ProcessLine(string line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) return;
            var parts = line.Split(',');
            if (parts.Length < 2) return;
            var issn = PublicationGroupMapper.NormalizeIssn(parts[0]);
            if (string.IsNullOrWhiteSpace(issn) || !IsValidNormalizedIssn(issn)) return;
            var database = parts[1].Trim();
            var source = parts.Length > 2 ? parts[2].Trim() : fileName;
            var cuartil = parts.Length > 3 ? parts[3].Trim() : null;
            _map.TryAdd(issn, (database, string.IsNullOrWhiteSpace(cuartil) ? null : cuartil, source));
            loaded++;
        }

        // Process first line (may be header — skip if not a valid ISSN)
        var firstIssn = PublicationGroupMapper.NormalizeIssn(firstLine.Split(',')[0]);
        if (!string.IsNullOrWhiteSpace(firstIssn) && IsValidNormalizedIssn(firstIssn))
            ProcessLine(firstLine);

        string? line;
        while ((line = sr.ReadLine()) != null)
            ProcessLine(line);

        _logger.LogInformation("Loaded {Count} ISSN entries from simple CSV {File}", loaded, fileName);
    }

    /// <summary>
    /// A normalized ISSN is valid when it is exactly 8 characters: 7 digits followed
    /// by a digit or 'X' (the ISO 3297 check digit).
    /// </summary>
    private static bool IsValidNormalizedIssn(string issn) =>
        issn.Length == 8 &&
        issn[..7].All(char.IsDigit) &&
        (char.IsDigit(issn[7]) || issn[7] == 'X');

    /// <inheritdoc/>
    public Task<PublicationDatabaseMatchDto?> TryResolveAsync(IEnumerable<string> issns, DateOnly? publishedDate = null, CancellationToken ct = default)
    {
        foreach (var issn in issns)
        {
            var key = PublicationGroupMapper.NormalizeIssn(issn);
            if (string.IsNullOrWhiteSpace(key)) continue;
            if (_map.TryGetValue(key, out var v))
            {
                return Task.FromResult<PublicationDatabaseMatchDto?>(new PublicationDatabaseMatchDto
                {
                    DatabaseName = v.database,
                    Cuartil = v.cuartil,
                    Source = v.source,
                    Confidence = 0.95,
                    Group = PublicationGroupMapper.MapToGroup(v.database)
                });
            }
        }

        return Task.FromResult<PublicationDatabaseMatchDto?>(null);
    }
}
