using Dashboard_v2.Application.Publications;
using Dashboard_v2.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Publications;

public class CrossRefToPublicationMapperTests
{
    [Test]
    public void BuildPublicationDataBuildsExpectedSummary()
    {
        var dto = new PublicationCrossRefDto
        {
            ContainerTitle = "Journal of Testing",
            Issns = ["1234-5678"],
            Isbns = ["978-1-2345-6789-0"],
            Volume = "42",
            Issue = "7",
            Page = "100-120",
            Publisher = "Test Publisher",
            Published = "2026-04",
            Authors = ["Ada Lovelace", "Alan Turing"],
        };

        var result = CrossRefToPublicationMapper.BuildPublicationData(dto);

        result.ShouldContain("Container: Journal of Testing");
        result.ShouldContain("ISSN: 1234-5678");
        result.ShouldContain("ISBN: 978-1-2345-6789-0");
        result.ShouldContain("Volume: 42");
        result.ShouldContain("Issue: 7");
        result.ShouldContain("Pages: 100-120");
        result.ShouldContain("Publisher: Test Publisher");
        result.ShouldContain("Published: 2026-04");
        result.ShouldContain("Authors: Ada Lovelace; Alan Turing");
    }

    [TestCase("journal-article", PublicationType.Diario)]
    [TestCase("book", PublicationType.Libro)]
    [TestCase("monograph", PublicationType.Monografía)]
    [TestCase("book-chapter", PublicationType.Capítulo)]
    public void MapCrossRefTypeToPublicationTypeMapsKnownValues(string crossRefType, PublicationType expected)
    {
        var result = CrossRefToPublicationMapper.MapCrossRefTypeToPublicationType(crossRefType);

        result.ShouldBe(expected);
    }

    [Test]
    public void MapCrossRefTypeToPublicationTypeReturnsNullForUnknownType()
    {
        var result = CrossRefToPublicationMapper.MapCrossRefTypeToPublicationType("posted-content");

        result.ShouldBeNull();
    }
}
