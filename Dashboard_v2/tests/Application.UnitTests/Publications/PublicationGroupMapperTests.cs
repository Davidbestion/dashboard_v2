using Dashboard_v2.Application.Publications;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Publications;

[TestFixture]
public class PublicationGroupMapperTests
{
    // ─── MapToGroup ───────────────────────────────────────────────────────────

    [Test]
    public void MapToGroup_Null_Returns4()
    {
        PublicationGroupMapper.MapToGroup(null).ShouldBe(4);
    }

    [Test]
    public void MapToGroup_Empty_Returns4()
    {
        PublicationGroupMapper.MapToGroup("").ShouldBe(4);
    }

    [Test]
    public void MapToGroup_Whitespace_Returns4()
    {
        PublicationGroupMapper.MapToGroup("   ").ShouldBe(4);
    }

    [TestCase("Scopus")]
    [TestCase("Scimago")]
    [TestCase("Web of Science")]
    [TestCase("SCIE")]
    [TestCase("SSCI")]
    [TestCase("AHCI")]
    public void MapToGroup_Group1Database_Returns1(string db)
    {
        PublicationGroupMapper.MapToGroup(db).ShouldBe(1);
    }

    [TestCase("MEDLINE")]
    [TestCase("Emerging Sources")]
    [TestCase("BIOSIS")]
    [TestCase("Compendex")]
    [TestCase("CAB Abstracts")]
    [TestCase("INSPEC")]
    public void MapToGroup_Group2Database_Returns2(string db)
    {
        PublicationGroupMapper.MapToGroup(db).ShouldBe(2);
    }

    [TestCase("DOAJ")]
    [TestCase("LILACS")]
    [TestCase("Redalyc")]
    [TestCase("Latindex")]
    [TestCase("ICYT")]
    [TestCase("Periodica")]
    [TestCase("CLASE")]
    public void MapToGroup_Group3Database_Returns3(string db)
    {
        PublicationGroupMapper.MapToGroup(db).ShouldBe(3);
    }

    [TestCase("Some Local DB")]
    [TestCase("Institutional Repository")]
    [TestCase("Custom")]
    public void MapToGroup_UnknownDatabase_Returns4(string db)
    {
        PublicationGroupMapper.MapToGroup(db).ShouldBe(4);
    }

    // ─── NormalizeIssn ────────────────────────────────────────────────────────

    [Test]
    public void NormalizeIssn_Null_ReturnsEmpty()
    {
        PublicationGroupMapper.NormalizeIssn(null).ShouldBe(string.Empty);
    }

    [Test]
    public void NormalizeIssn_Empty_ReturnsEmpty()
    {
        PublicationGroupMapper.NormalizeIssn("").ShouldBe(string.Empty);
    }

    [Test]
    public void NormalizeIssn_WithHyphen_RemovesHyphen()
    {
        PublicationGroupMapper.NormalizeIssn("1234-5678").ShouldBe("12345678");
    }

    [Test]
    public void NormalizeIssn_Lowercase_Uppercases()
    {
        PublicationGroupMapper.NormalizeIssn("  abcd1234  ").ShouldBe("ABCD1234");
    }
}
