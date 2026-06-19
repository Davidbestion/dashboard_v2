using ClosedXML.Excel;
using Dashboard_v2.Infrastructure.Services.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Publications;

/// <summary>
/// Unit tests for WosExcelPublicationDatabaseProvider.
///
/// Each test creates a temporary directory with one or more .xlsx files that
/// mimic the Clarivate WoS Master Journal List change format, invokes the
/// provider and asserts the expected group/ambiguity result.
///
/// The provider is internal but the test project already references
/// Infrastructure; an [assembly: InternalsVisibleTo] is not needed because
/// the class is sealed-internal — access via its public interface is enough.
/// </summary>
[TestFixture]
public class WosExcelProviderTests
{
    // ── helpers ───────────────────────────────────────────────────────────────

    private static readonly string[] WosHeaders =
        ["Journal title", "ISSN", "eISSN", "Publisher name", "Web of Science Core Collection", "Coverage change", "Notes"];

    /// <summary>Creates a temp directory that is cleaned up after the test.</summary>
    private static string MakeTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "wos_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>
    /// Creates a minimal WoS change Excel file in <paramref name="dir"/>.
    /// <paramref name="fileName"/> must match the provider's naming convention
    /// (e.g. "JOURNAL_CHANGE_YEAR_2023.xlsx").
    /// </summary>
    private static string CreateWosFile(
        string dir,
        string fileName,
        IEnumerable<(string title, string issn, string eissn, string indexes, string change)> rows)
    {
        var path = Path.Combine(dir, fileName);
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");

        // Rows 1-27 are empty (branding) — provider searches for "Journal title"
        // anywhere, so we just put the header on row 1 for simplicity.
        for (int col = 1; col <= WosHeaders.Length; col++)
            ws.Cell(1, col).Value = WosHeaders[col - 1];

        int row = 2;
        foreach (var (title, issn, eissn, indexes, change) in rows)
        {
            ws.Cell(row, 1).Value = title;
            ws.Cell(row, 2).Value = issn;
            ws.Cell(row, 3).Value = eissn;
            ws.Cell(row, 4).Value = "Some Publisher";
            ws.Cell(row, 5).Value = indexes;
            ws.Cell(row, 6).Value = change;
            row++;
        }

        wb.SaveAs(path);
        return path;
    }

    private static WosExcelPublicationDatabaseProvider Build(string? dir) =>
        new(dir, new Microsoft.Extensions.Logging.Abstractions.NullLogger<WosExcelPublicationDatabaseProvider>());

    // ── Group 1: SCIE journal ─────────────────────────────────────────────────

    [Test]
    public async Task ScieJournal_ReturnsGroup1_NoAmbiguity()
    {
        var dir = MakeTempDir();
        try
        {
            CreateWosFile(dir, "JOURNAL_CHANGE_YEAR_2023.xlsx",
            [
                ("Test Journal", "1234-5678", "8765-4321",
                 "Science Citation Index Expanded", "Accepted")
            ]);

            var provider = Build(dir);
            var result = await provider.TryResolveAsync(["1234-5678"]);

            result.ShouldNotBeNull();
            result!.Group.ShouldBe(1);
            result.AmbiguousGroup.ShouldBeFalse();
            result.Cuartil.ShouldBeNull();
            result.Source!.ShouldContain("SCIE");
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    // ── Group 1: SSCI journal ─────────────────────────────────────────────────

    [Test]
    public async Task SsciJournal_ReturnsGroup1()
    {
        var dir = MakeTempDir();
        try
        {
            CreateWosFile(dir, "JOURNAL_CHANGE_YEAR_2023.xlsx",
            [
                ("Social Journal", "2222-3333", "",
                 "Social Sciences Citation Index", "Accepted")
            ]);

            var result = await Build(dir).TryResolveAsync(["2222-3333"]);

            result.ShouldNotBeNull();
            result!.Group.ShouldBe(1);
            result.AmbiguousGroup.ShouldBeFalse();
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    // ── Group 2: ESCI-only journal ────────────────────────────────────────────

    [Test]
    public async Task EsciOnlyJournal_ReturnsGroup2_NoAmbiguity()
    {
        var dir = MakeTempDir();
        try
        {
            CreateWosFile(dir, "JOURNAL_CHANGE_YEAR_2024.xlsx",
            [
                ("Emerging Journal", "4444-5555", "",
                 "Emerging Sources Citation Index", "Accepted")
            ]);

            var result = await Build(dir).TryResolveAsync(["4444-5555"]);

            result.ShouldNotBeNull();
            result!.Group.ShouldBe(2);
            result.AmbiguousGroup.ShouldBeFalse();
            result.Source!.ShouldContain("ESCI");
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    // ── Ambiguous: ESCI in one file, later accepted in SCIE ──────────────────

    [Test]
    public async Task JournalPromotedFromEsciToScie_AmbiguousGroupTrue()
    {
        var dir = MakeTempDir();
        try
        {
            // Year file: journal enters ESCI
            CreateWosFile(dir, "JOURNAL_CHANGE_YEAR_2023.xlsx",
            [
                ("Promoted Journal", "6666-7777", "",
                 "Emerging Sources Citation Index", "Accepted")
            ]);

            // Later year file: journal accepted in SCIE too (still shows ESCI entry
            // without explicit de-listing — real-world scenario)
            CreateWosFile(dir, "JOURNAL_CHANGE_YEAR_2024.xlsx",
            [
                ("Promoted Journal", "6666-7777", "",
                 "Science Citation Index Expanded", "Accepted")
            ]);

            var result = await Build(dir).TryResolveAsync(["6666-7777"]);

            result.ShouldNotBeNull();
            result!.Group.ShouldBe(1);        // main index takes precedence
            result.AmbiguousGroup.ShouldBeTrue();
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    // ── "Moved to SCIE, SSCI, or AHCI" clears ESCI — no ambiguity ────────────

    [Test]
    public async Task MovedToMainIndex_ClearsEsci_NoAmbiguity()
    {
        var dir = MakeTempDir();
        try
        {
            // Year file: journal enters ESCI
            CreateWosFile(dir, "JOURNAL_CHANGE_YEAR_2023.xlsx",
            [
                ("Graduated Journal", "8888-9999", "",
                 "Emerging Sources Citation Index", "Accepted")
            ]);

            // Monthly file: official graduation — the change type explicitly removes ESCI
            CreateWosFile(dir, "JOURNAL_CHANGE_MONTH_2026_01.xlsx",
            [
                ("Graduated Journal", "8888-9999", "",
                 "Science Citation Index Expanded", "Moved to SCIE, SSCI, or AHCI")
            ]);

            var result = await Build(dir).TryResolveAsync(["8888-9999"]);

            result.ShouldNotBeNull();
            result!.Group.ShouldBe(1);
            result.AmbiguousGroup.ShouldBeFalse();   // ESCI was removed by the move
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    // ── De-listing removes the journal entirely ───────────────────────────────

    [Test]
    public async Task DelistedJournal_ReturnsNull()
    {
        var dir = MakeTempDir();
        try
        {
            CreateWosFile(dir, "JOURNAL_CHANGE_YEAR_2023.xlsx",
            [
                ("Delisted Journal", "1111-2222", "",
                 "Science Citation Index Expanded", "Accepted")
            ]);

            CreateWosFile(dir, "JOURNAL_CHANGE_YEAR_2024.xlsx",
            [
                ("Delisted Journal", "1111-2222", "",
                 "Science Citation Index Expanded", "Production De-listing")
            ]);

            var result = await Build(dir).TryResolveAsync(["1111-2222"]);

            result.ShouldBeNull();
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    // ── eISSN lookup ──────────────────────────────────────────────────────────

    [Test]
    public async Task LookupByEissn_Works()
    {
        var dir = MakeTempDir();
        try
        {
            CreateWosFile(dir, "JOURNAL_CHANGE_YEAR_2025.xlsx",
            [
                ("eISSN Journal", "0000-1111", "9999-8888",
                 "Arts & Humanities Citation Index", "Accepted")
            ]);

            // Query by eISSN only
            var result = await Build(dir).TryResolveAsync(["9999-8888"]);

            result.ShouldNotBeNull();
            result!.Group.ShouldBe(1);
            result.Source!.ShouldContain("AHCI");
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    // ── ISSN normalisation (hyphen variants) ─────────────────────────────────

    [Test]
    public async Task IssnNormalisation_HyphenAndNohyphen_BothMatch()
    {
        var dir = MakeTempDir();
        try
        {
            CreateWosFile(dir, "JOURNAL_CHANGE_YEAR_2025.xlsx",
            [
                ("Norm Journal", "1234-5678", "",
                 "Science Citation Index Expanded", "Accepted")
            ]);

            var provider = Build(dir);

            // Query with hyphen
            var r1 = await provider.TryResolveAsync(["1234-5678"]);
            // Query without hyphen
            var r2 = await provider.TryResolveAsync(["12345678"]);

            r1.ShouldNotBeNull();
            r2.ShouldNotBeNull();
            r1!.Group.ShouldBe(r2!.Group);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    // ── Pipe-separated multi-index entry ─────────────────────────────────────

    [Test]
    public async Task PipeSeparatedIndexes_BothParsed()
    {
        var dir = MakeTempDir();
        try
        {
            // Some journals appear in both SCIE and SSCI
            CreateWosFile(dir, "JOURNAL_CHANGE_YEAR_2025.xlsx",
            [
                ("Dual Index Journal", "5555-6666", "",
                 "Science Citation Index Expanded | Social Sciences Citation Index", "Accepted")
            ]);

            var result = await Build(dir).TryResolveAsync(["5555-6666"]);

            result.ShouldNotBeNull();
            result!.Group.ShouldBe(1);
            result.Source!.ShouldContain("SCIE");
            result.Source!.ShouldContain("SSCI");
            result.AmbiguousGroup.ShouldBeFalse();
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    // ── Empty directory / null directory ─────────────────────────────────────

    [Test]
    public async Task NullDirectory_ReturnsNull()
    {
        var provider = Build(null!);
        var result = await provider.TryResolveAsync(["1234-5678"]);
        result.ShouldBeNull();
    }

    [Test]
    public async Task EmptyDirectory_ReturnsNull()
    {
        var dir = MakeTempDir();
        try
        {
            var result = await Build(dir).TryResolveAsync(["1234-5678"]);
            result.ShouldBeNull();
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    // ── Date-based auto-resolution of ambiguous group ────────────────────────

    [Test]
    public async Task AmbiguousJournal_PublishedBeforePromotion_AutoResolvesToGroup2()
    {
        var dir = MakeTempDir();
        try
        {
            CreateWosFile(dir, "JOURNAL_CHANGE_YEAR_2022.xlsx",
            [
                ("Promoted Journal", "1010-2020", "", "Emerging Sources Citation Index", "Accepted")
            ]);
            CreateWosFile(dir, "JOURNAL_CHANGE_YEAR_2024.xlsx",
            [
                ("Promoted Journal", "1010-2020", "", "Science Citation Index Expanded", "Accepted")
            ]);

            // Article published June 2023 — before the 2024 promotion file
            var result = await Build(dir).TryResolveAsync(["1010-2020"], new DateOnly(2023, 6, 1));

            result.ShouldNotBeNull();
            result!.Group.ShouldBe(2);
            result.AmbiguousGroup.ShouldBeFalse();
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Test]
    public async Task AmbiguousJournal_PublishedAfterPromotion_AutoResolvesToGroup1()
    {
        var dir = MakeTempDir();
        try
        {
            CreateWosFile(dir, "JOURNAL_CHANGE_YEAR_2022.xlsx",
            [
                ("Promoted Journal", "1010-2020", "", "Emerging Sources Citation Index", "Accepted")
            ]);
            CreateWosFile(dir, "JOURNAL_CHANGE_YEAR_2024.xlsx",
            [
                ("Promoted Journal", "1010-2020", "", "Science Citation Index Expanded", "Accepted")
            ]);

            // Article published June 2024 — clearly after the promotion (different month)
            var result = await Build(dir).TryResolveAsync(["1010-2020"], new DateOnly(2024, 6, 1));

            result.ShouldNotBeNull();
            result!.Group.ShouldBe(1);
            result.AmbiguousGroup.ShouldBeFalse();
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Test]
    public async Task AmbiguousJournal_PublishedSameMonthAsPromotion_RemainsAmbiguous()
    {
        var dir = MakeTempDir();
        try
        {
            CreateWosFile(dir, "JOURNAL_CHANGE_YEAR_2022.xlsx",
            [
                ("Promoted Journal", "1010-2020", "", "Emerging Sources Citation Index", "Accepted")
            ]);
            CreateWosFile(dir, "JOURNAL_CHANGE_YEAR_2024.xlsx",
            [
                ("Promoted Journal", "1010-2020", "", "Science Citation Index Expanded", "Accepted")
            ]);

            // Annual file → promotion date = 2024-01-01; article also published January 2024
            var result = await Build(dir).TryResolveAsync(["1010-2020"], new DateOnly(2024, 1, 15));

            result.ShouldNotBeNull();
            result!.AmbiguousGroup.ShouldBeTrue();
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Test]
    public async Task AmbiguousJournal_NoPublishedDate_RemainsAmbiguous()
    {
        var dir = MakeTempDir();
        try
        {
            CreateWosFile(dir, "JOURNAL_CHANGE_YEAR_2022.xlsx",
            [
                ("Promoted Journal", "1010-2020", "", "Emerging Sources Citation Index", "Accepted")
            ]);
            CreateWosFile(dir, "JOURNAL_CHANGE_YEAR_2024.xlsx",
            [
                ("Promoted Journal", "1010-2020", "", "Science Citation Index Expanded", "Accepted")
            ]);

            var result = await Build(dir).TryResolveAsync(["1010-2020"]);

            result.ShouldNotBeNull();
            result!.AmbiguousGroup.ShouldBeTrue();
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    // ── Chronological ordering: monthly file overrides year file ─────────────

    [Test]
    public async Task MonthlyFile_ProcessedAfterYearFile_CanDelist()
    {
        var dir = MakeTempDir();
        try
        {
            // Annual file: journal accepted
            CreateWosFile(dir, "JOURNAL_CHANGE_YEAR_2025.xlsx",
            [
                ("Late Removed", "7777-8888", "",
                 "Science Citation Index Expanded", "Accepted")
            ]);

            // Monthly file (2026-02): editorial de-listing
            CreateWosFile(dir, "JOURNAL_CHANGE_MONTH_2026_02.xlsx",
            [
                ("Late Removed", "7777-8888", "",
                 "Science Citation Index Expanded", "Editorial De-listing")
            ]);

            var result = await Build(dir).TryResolveAsync(["7777-8888"]);
            result.ShouldBeNull();
        }
        finally { Directory.Delete(dir, recursive: true); }
    }
}
