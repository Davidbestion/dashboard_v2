using System.Text.Json;
using Dashboard_v2.Infrastructure.Configuration;
using Dashboard_v2.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Dashboard_v2.Application.FunctionalTests.Infrastructure;

[TestFixture]
public class CrossRefClientTests
{
    private CrossRefClient _client = default!;

    [SetUp]
    public void SetUp()
    {
        var opts = Options.Create(new CrossRefOptions
        {
            BaseDelayMs = 500,
            JitterFactorMin = 0.5,
            JitterFactorMax = 1.5,
            MaxRetries = 3
        });
        _client = new CrossRefClient(
            new HttpClient(),
            opts,
            NullLogger<CrossRefClient>.Instance);
    }

    // ── NormalizeDoi ──────────────────────────────────────────────────────────

    [TestCase("10.1234/example", "10.1234/example")]
    [TestCase("https://doi.org/10.1234/example", "10.1234/example")]
    [TestCase("http://doi.org/10.1234/example", "10.1234/example")]
    [TestCase("http://dx.doi.org/10.1234/example", "10.1234/example")]
    [TestCase("doi:10.1234/example", "10.1234/example")]
    [TestCase("  10.1234/EXAMPLE  ", "10.1234/example")]
    [TestCase("10.1234/example?query=1", "10.1234/example")]
    [TestCase("10.1234/example#section", "10.1234/example")]
    [TestCase("10.1234/example.", "10.1234/example")]
    [TestCase("10.1234/example;", "10.1234/example")]
    [TestCase("10.1234/example,", "10.1234/example")]
    [TestCase("https://doi.org/10.5678/journal.pone.0123456", "10.5678/journal.pone.0123456")]
    public void NormalizeDoi_VariousFormats_ReturnsCanonicalForm(string input, string expected)
    {
        var result = CrossRefClient.NormalizeDoi(input);
        result.ShouldBe(expected);
    }

    [Test]
    public void NormalizeDoi_AlreadyNormalized_IsIdempotent()
    {
        const string doi = "10.1234/already-clean";
        CrossRefClient.NormalizeDoi(doi).ShouldBe(doi);
        CrossRefClient.NormalizeDoi(CrossRefClient.NormalizeDoi(doi)).ShouldBe(doi);
    }

    [Test]
    public void NormalizeDoi_UpperCase_ReturnsLowerCase()
    {
        var result = CrossRefClient.NormalizeDoi("10.9999/UPPERCASE-DOI");
        result.ShouldBe("10.9999/uppercase-doi");
    }

    // ── ComputeDelayMs ────────────────────────────────────────────────────────

    [Test]
    public void ComputeDelayMs_Attempt1_IsWithinJitteredRange()
    {
        // baseMs=500, exp=1, raw=500, jitter in [0.5, 1.5] → [250, 750]
        var delay = _client.ComputeDelayMs(1);
        delay.ShouldBeGreaterThanOrEqualTo(250);
        delay.ShouldBeLessThanOrEqualTo(750);
    }

    [Test]
    public void ComputeDelayMs_Attempt2_IsWithinJitteredRange()
    {
        // baseMs=500, exp=2, raw=1000, jitter in [0.5, 1.5] → [500, 1500]
        var delay = _client.ComputeDelayMs(2);
        delay.ShouldBeGreaterThanOrEqualTo(500);
        delay.ShouldBeLessThanOrEqualTo(1500);
    }

    [Test]
    public void ComputeDelayMs_Attempt3_IsWithinJitteredRange()
    {
        // baseMs=500, exp=4, raw=2000, jitter in [0.5, 1.5] → [1000, 3000]
        var delay = _client.ComputeDelayMs(3);
        delay.ShouldBeGreaterThanOrEqualTo(1000);
        delay.ShouldBeLessThanOrEqualTo(3000);
    }

    [Test]
    public void ComputeDelayMs_HighAttempt_IsCappedAt60000()
    {
        var delay = _client.ComputeDelayMs(20);
        delay.ShouldBeLessThanOrEqualTo(60000);
        delay.ShouldBeGreaterThan(0);
    }

    [Test]
    public void ComputeDelayMs_GrowsWithAttempt()
    {
        // En promedio el delay crece — ejecutar 20 muestras para compensar el jitter
        var avg1 = Enumerable.Range(0, 20).Average(_ => _client.ComputeDelayMs(1));
        var avg3 = Enumerable.Range(0, 20).Average(_ => _client.ComputeDelayMs(3));
        avg3.ShouldBeGreaterThan(avg1, "El delay promedio debe crecer con el número de intento");
    }

    // ── ParseMessageToDto ─────────────────────────────────────────────────────

    [Test]
    public void ParseMessageToDto_FullRecord_MapsAllFields()
    {
        const string json = """
            {
                "DOI": "10.1234/test",
                "URL": "https://doi.org/10.1234/test",
                "title": ["Test Article Title"],
                "container-title": ["Journal of Tests"],
                "volume": "12",
                "issue": "3",
                "page": "100-110",
                "publisher": "Test Publisher",
                "type": "journal-article",
                "ISSN": ["1234-5678", "8765-4321"],
                "ISBN": ["978-3-16-148410-0"],
                "author": [
                    { "given": "John", "family": "Doe" },
                    { "family": "Smith" },
                    { "given": "Ana" }
                ],
                "published-print": {
                    "date-parts": [[2023, 6, 15]]
                }
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var dto = CrossRefClient.ParseMessageToDto(doc.RootElement);

        dto.ShouldNotBeNull();
        dto!.Doi.ShouldBe("10.1234/test");
        dto.Url.ShouldBe("https://doi.org/10.1234/test");
        dto.Title.ShouldBe("Test Article Title");
        dto.ContainerTitle.ShouldBe("Journal of Tests");
        dto.Volume.ShouldBe("12");
        dto.Issue.ShouldBe("3");
        dto.Page.ShouldBe("100-110");
        dto.Publisher.ShouldBe("Test Publisher");
        dto.Type.ShouldBe("journal-article");
        dto.Issns.ShouldContain("1234-5678");
        dto.Issns.ShouldContain("8765-4321");
        dto.Isbns.ShouldContain("978-3-16-148410-0");
        dto.Authors.Count.ShouldBe(3);
        dto.Authors[0].ShouldBe("Doe, John");
        dto.Authors[1].ShouldBe("Smith");
        dto.Authors[2].ShouldBe("Ana");
        dto.Published.ShouldBe("2023-06-15");
    }

    [Test]
    public void ParseMessageToDto_MinimalRecord_ReturnsDtoWithNulls()
    {
        const string json = """{ "DOI": "10.9999/minimal", "type": "book" }""";
        using var doc = JsonDocument.Parse(json);
        var dto = CrossRefClient.ParseMessageToDto(doc.RootElement);

        dto.ShouldNotBeNull();
        dto!.Doi.ShouldBe("10.9999/minimal");
        dto.Title.ShouldBeNull();
        dto.Authors.ShouldBeEmpty();
        dto.Issns.ShouldBeEmpty();
        dto.Published.ShouldBeNull();
    }

    [Test]
    public void ParseMessageToDto_EmptyObject_ReturnsDtoWithAllNulls()
    {
        using var doc = JsonDocument.Parse("{}");
        var dto = CrossRefClient.ParseMessageToDto(doc.RootElement);

        dto.ShouldNotBeNull();
        dto!.Doi.ShouldBeNull();
        dto.Authors.ShouldBeEmpty();
        dto.Issns.ShouldBeEmpty();
    }

    [Test]
    public void ParseMessageToDto_DatePrintNotPresent_FallsBackToPublishedOnline()
    {
        const string json = """
            {
                "DOI": "10.1234/online",
                "published-online": { "date-parts": [[2022, 11]] }
            }
            """;
        using var doc = JsonDocument.Parse(json);
        var dto = CrossRefClient.ParseMessageToDto(doc.RootElement);

        dto!.Published.ShouldBe("2022-11");
    }

    [Test]
    public void ParseMessageToDto_NoPrintNorOnline_FallsBackToCreated()
    {
        const string json = """
            {
                "DOI": "10.1234/created",
                "created": { "date-parts": [[2021]] }
            }
            """;
        using var doc = JsonDocument.Parse(json);
        var dto = CrossRefClient.ParseMessageToDto(doc.RootElement);

        dto!.Published.ShouldBe("2021");
    }

    [Test]
    public void ParseMessageToDto_AuthorOnlyFamily_UsesFamily()
    {
        const string json = """
            { "DOI": "10.0000/x", "author": [{ "family": "García" }] }
            """;
        using var doc = JsonDocument.Parse(json);
        var dto = CrossRefClient.ParseMessageToDto(doc.RootElement);

        dto!.Authors.Single().ShouldBe("García");
    }

    [Test]
    public void ParseMessageToDto_AuthorBothNames_UsesLastNameCommaGiven()
    {
        const string json = """
            { "DOI": "10.0000/x", "author": [{ "given": "María", "family": "López" }] }
            """;
        using var doc = JsonDocument.Parse(json);
        var dto = CrossRefClient.ParseMessageToDto(doc.RootElement);

        dto!.Authors.Single().ShouldBe("López, María");
    }
}
