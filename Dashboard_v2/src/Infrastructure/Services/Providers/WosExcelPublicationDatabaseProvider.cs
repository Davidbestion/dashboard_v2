using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Publications;
using Microsoft.Extensions.Logging;

namespace Dashboard_v2.Infrastructure.Services.Providers;

/// <summary>
/// Provider that reads Clarivate Web of Science Master Journal List change files
/// (.xlsx) to resolve ISSN → WoS index and group.
///
/// Clarivate publishes incremental change files (annual + monthly) at:
///   https://clarivate.com/academia-government/scientific-and-academic-research/research-discovery-and-workflow-solutions/master-journal-list/
///
/// File naming convention expected in the directory:
///   JOURNAL_CHANGE_YEAR_2023.xlsx  (annual changes, processed first)
///   JOURNAL_CHANGE_MONTH_2026_01.xlsx  (monthly changes, processed after)
///
/// All .xlsx files in the configured directory are loaded chronologically at
/// startup so this provider is registered as a Singleton.
///
/// Group assignment:
///   SCIE / SSCI / AHCI  → Group 1  (no cuartil available from this source)
///   ESCI only           → Group 2
///   ESCI + SCIE/SSCI/AHCI → Group 1, AmbiguousGroup = true (journal was promoted;
///                           correct group depends on the publication date vs promotion date)
/// </summary>
internal sealed class WosExcelPublicationDatabaseProvider : IPublicationDatabaseProvider
{
    // Recognized WoS index abbreviations
    private const string SCIE = "SCIE";
    private const string SSCI = "SSCI";
    private const string AHCI = "AHCI";
    private const string ESCI = "ESCI";

    private static readonly Dictionary<string, string> IndexAbbreviations =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Science Citation Index Expanded"]   = SCIE,
            ["Social Sciences Citation Index"]    = SSCI,
            ["Arts & Humanities Citation Index"]  = AHCI,
            ["Arts and Humanities Citation Index"] = AHCI,
            ["Emerging Sources Citation Index"]   = ESCI,
        };

    // Coverage change types that ADD a journal to an index
    private static readonly HashSet<string> AddChanges = new(StringComparer.OrdinalIgnoreCase)
    {
        "Accepted", "Title Change", "Partially Indexed", "Other",
        "Moved to SCIE, SSCI, or AHCI"
    };

    // Coverage change types that REMOVE a journal from an index
    private static readonly HashSet<string> RemoveChanges = new(StringComparer.OrdinalIgnoreCase)
    {
        "Production De-listing", "Editorial De-listing", "Cease"
    };

    // ISSN (8 chars, no hyphen, uppercase) → set of current WoS index abbreviations
    private readonly IReadOnlyDictionary<string, HashSet<string>> _map;

    public string ProviderName => "WosExcel";

    public WosExcelPublicationDatabaseProvider(string? directory, ILogger<WosExcelPublicationDatabaseProvider> logger)
    {
        _map = BuildMap(directory, logger);
        logger.LogInformation("WosExcel: loaded {Count} journal entries from {Dir}", _map.Count, directory ?? "(none)");
    }

    // ── Public interface ──────────────────────────────────────────────────────

    public Task<PublicationDatabaseMatchDto?> TryResolveAsync(
        IEnumerable<string> issns,
        CancellationToken ct = default)
    {
        // Accumulate indexes from any matching ISSN variant
        var indexes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var issn in issns)
        {
            var normalized = NormalizeIssn(issn);
            if (normalized is null) continue;
            if (_map.TryGetValue(normalized, out var found))
                foreach (var idx in found) indexes.Add(idx);
        }

        if (indexes.Count == 0)
            return Task.FromResult<PublicationDatabaseMatchDto?>(null);

        var hasMain = indexes.Any(i => i is SCIE or SSCI or AHCI);
        var hasEsci = indexes.Contains(ESCI);

        // Main index takes precedence; ESCI alone = Group 2
        int? group = (hasMain, hasEsci) switch
        {
            (true, _)      => 1,
            (false, true)  => 2,
            _              => null
        };

        // True when the journal appears in BOTH ESCI and a main index — this can
        // happen because the journal is still in the ESCI change list from an
        // earlier file AND was later added to SCIE/SSCI/AHCI in a newer file
        // without an explicit ESCI de-listing entry.
        var ambiguous = hasMain && hasEsci;

        var indexNames = string.Join(", ", indexes.OrderBy(x => x));

        return Task.FromResult<PublicationDatabaseMatchDto?>(new PublicationDatabaseMatchDto
        {
            DatabaseName   = "Web de la Ciencia",
            Group          = group,
            Cuartil        = null,          // WoS change files don't include JIF quartile
            Source         = $"WoS Excel: {indexNames}",
            Confidence     = 1.0,
            AmbiguousGroup = ambiguous,
        });
    }

    // ── Map construction ──────────────────────────────────────────────────────

    private static IReadOnlyDictionary<string, HashSet<string>> BuildMap(
        string? directory,
        ILogger logger)
    {
        var map = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(directory))
            return map;

        if (!Directory.Exists(directory))
        {
            logger.LogWarning("WoS directory not found: {Dir}", directory);
            return map;
        }

        var files = Directory.GetFiles(directory, "*.xlsx")
            .OrderBy(GetSortKey)   // chronological: annual files first, then monthly
            .ToList();

        if (files.Count == 0)
        {
            logger.LogWarning("WoS directory contains no .xlsx files: {Dir}", directory);
            return map;
        }

        foreach (var file in files)
        {
            try
            {
                ProcessFile(file, map, logger);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "WoS: failed to load file {File}", file);
            }
        }

        return map;
    }

    private static void ProcessFile(
        string path,
        Dictionary<string, HashSet<string>> map,
        ILogger logger)
    {
        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheets.First();

        // Locate the header row by searching for "Journal title" in column 1
        IXLRow? headerRow = null;
        foreach (var row in ws.RowsUsed())
        {
            if (string.Equals(row.Cell(1).GetString().Trim(), "Journal title",
                              StringComparison.OrdinalIgnoreCase))
            {
                headerRow = row;
                break;
            }
        }

        if (headerRow is null)
        {
            logger.LogDebug("WoS: no header row found in {File}", path);
            return;
        }

        // Discover column numbers (1-based) from header
        int issnCol = 0, eissnCol = 0, indexCol = 0, changeCol = 0;
        foreach (var cell in headerRow.CellsUsed())
        {
            var v = cell.GetString().Trim();
            switch (v)
            {
                case "ISSN":                          issnCol   = cell.Address.ColumnNumber; break;
                case "eISSN":                         eissnCol  = cell.Address.ColumnNumber; break;
                case "Web of Science Core Collection": indexCol  = cell.Address.ColumnNumber; break;
                case "Coverage change":               changeCol = cell.Address.ColumnNumber; break;
            }
        }

        int loaded = 0, skipped = 0;
        foreach (var row in ws.RowsUsed().Where(r => r.RowNumber() > headerRow.RowNumber()))
        {
            var changeType = changeCol > 0 ? row.Cell(changeCol).GetString().Trim() : string.Empty;
            var indexRaw   = indexCol  > 0 ? row.Cell(indexCol).GetString().Trim()  : string.Empty;
            var issn  = issnCol  > 0 ? NormalizeIssn(row.Cell(issnCol).GetString())  : null;
            var eissn = eissnCol > 0 ? NormalizeIssn(row.Cell(eissnCol).GetString()) : null;

            var isRemoval = RemoveChanges.Contains(changeType);
            var isAdd     = AddChanges.Contains(changeType);
            var indexes   = ParseIndexes(indexRaw);

            if (!isRemoval && !isAdd)
            {
                skipped++;
                continue;
            }

            foreach (var id in new[] { issn, eissn }
                                .Where(x => !string.IsNullOrWhiteSpace(x))
                                .Cast<string>())
            {
                if (isRemoval)
                {
                    if (map.TryGetValue(id, out var existing))
                    {
                        foreach (var idx in indexes) existing.Remove(idx);
                        if (existing.Count == 0) map.Remove(id);
                    }
                }
                else // isAdd
                {
                    if (indexes.Count == 0) continue;

                    if (!map.ContainsKey(id))
                        map[id] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    // "Moved to SCIE, SSCI, or AHCI": journal graduated from ESCI;
                    // remove ESCI status so it doesn't create false ambiguity.
                    if (string.Equals(changeType, "Moved to SCIE, SSCI, or AHCI",
                                      StringComparison.OrdinalIgnoreCase))
                        map[id].Remove(ESCI);

                    foreach (var idx in indexes) map[id].Add(idx);
                    loaded++;
                }
            }
        }

        logger.LogDebug("WoS: {File} — {Loaded} added, {Skipped} skipped",
                        Path.GetFileName(path), loaded, skipped);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a sort key (year, month) for chronological ordering.
    /// Annual files use month = 0; monthly files use their actual month number.
    /// Unknown naming patterns sort last.
    /// </summary>
    private static (int year, int month) GetSortKey(string path)
    {
        var name  = Path.GetFileNameWithoutExtension(path).ToUpperInvariant();
        var parts = name.Split('_');
        try
        {
            // JOURNAL_CHANGE_YEAR_2025 → parts[3] = "2025"
            if (parts.Length >= 4 && parts[2] == "YEAR" && int.TryParse(parts[3], out int y))
                return (y, 0);
            // JOURNAL_CHANGE_MONTH_2026_01 → parts[3]="2026", parts[4]="01"
            if (parts.Length >= 5 && parts[2] == "MONTH"
                && int.TryParse(parts[3], out int my)
                && int.TryParse(parts[4], out int mm))
                return (my, mm);
        }
        catch { /* fall through */ }

        return (9999, 99);
    }

    /// <summary>
    /// Parses a pipe-separated (with optional newlines) index string into a set
    /// of normalized abbreviations (SCIE, SSCI, AHCI, ESCI).
    /// </summary>
    private static HashSet<string> ParseIndexes(string raw)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(raw)) return result;

        foreach (var part in raw.Split('|'))
        {
            var clean = part.Replace("\n", "").Replace("\r", "").Trim();
            if (IndexAbbreviations.TryGetValue(clean, out var abbr))
                result.Add(abbr);
        }

        return result;
    }

    /// <summary>
    /// Normalizes an ISSN to 8 uppercase digits (hyphens stripped).
    /// Returns null when the input is blank or not a valid 7-8 character ISSN.
    /// Delegates stripping/trimming to <see cref="PublicationGroupMapper.NormalizeIssn"/>
    /// to keep normalization logic in one place.
    /// </summary>
    private static string? NormalizeIssn(string? s)
    {
        var n = PublicationGroupMapper.NormalizeIssn(s);
        return n.Length is 7 or 8 ? n : null;
    }
}
