using Dashboard_v2.Application.Documents.Reports;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Documents.Reports;

[TestFixture]
public class AnexoPremiosReportTests
{
    private ApplicationDbContext _db = null!;
    private AnexoPremiosReport _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _sut = new AnexoPremiosReport(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public void ReportName_ReturnsExpected()
    {
        _sut.ReportName.ShouldBe("anexo-premios");
    }

    [Test]
    public void TemplateName_ReturnsExpected()
    {
        _sut.TemplateName.ShouldBe("AnexoPremios");
    }

    [Test]
    public async Task GatherVariablesAsync_Empty_ReturnsTiposPremioKey()
    {
        var result = await _sut.GatherVariablesAsync(null, default);
        result.ShouldContainKey("TiposPremio");
    }

    [Test]
    public async Task GatherVariablesAsync_WithAwardTypes_ReturnsTiposOrdered()
    {
        _db.AwardTypes.Add(new AwardType { Id = 1, Name = "Nacional" });
        _db.AwardTypes.Add(new AwardType { Id = 2, Name = "Internacional" });
        await _db.SaveChangesAsync();

        var result = await _sut.GatherVariablesAsync(null, default);
        var tipos = result["TiposPremio"] as List<AnexoPremiosTipoRowDto>;
        tipos.ShouldNotBeNull();
        tipos.Count.ShouldBe(2);
        tipos[0].TipoPremio.ShouldBe("Nacional");
        tipos[1].TipoPremio.ShouldBe("Internacional");
    }

    [Test]
    public async Task GatherVariablesAsync_WithAwardAndUsers_BuildsAuthorsSummary()
    {
        _db.Users.Add(new User { Id = "u1", UserName = "alice", Email = "a@a.com", UserLastName1 = "Smith" });
        _db.AwardTypes.Add(new AwardType { Id = 1, Name = "Nacional" });
        _db.Awards.Add(new Award { Id = 1, Name = "Premio X", AwardTypeId = 1 });
        _db.UserAwardeds.Add(new UserAwarded { UserId = "u1", AwardId = 1, AwardedAt = DateTime.Today });
        await _db.SaveChangesAsync();

        var result = await _sut.GatherVariablesAsync(null, default);
        var tipos = result["TiposPremio"] as List<AnexoPremiosTipoRowDto>;
        tipos.ShouldNotBeNull();
        var premio = tipos[0].Premios.ShouldHaveSingleItem();
        premio.Titulo.ShouldBe("Premio X");
        premio.Autores.ShouldContain("alice");
    }

    [Test]
    public async Task GatherVariablesAsync_DuplicateAwardNames_Deduplicates()
    {
        _db.Users.Add(new User { Id = "u1", UserName = "alice", Email = "a@a.com", UserLastName1 = "Smith" });
        _db.AwardTypes.Add(new AwardType { Id = 1, Name = "Nacional" });
        _db.Awards.Add(new Award { Id = 1, Name = "Premio X", AwardTypeId = 1 });
        _db.Awards.Add(new Award { Id = 2, Name = "Premio X", AwardTypeId = 1 });
        _db.UserAwardeds.Add(new UserAwarded { UserId = "u1", AwardId = 1, AwardedAt = DateTime.Today });
        _db.UserAwardeds.Add(new UserAwarded { UserId = "u1", AwardId = 2, AwardedAt = DateTime.Today });
        await _db.SaveChangesAsync();

        var result = await _sut.GatherVariablesAsync(null, default);
        var tipos = result["TiposPremio"] as List<AnexoPremiosTipoRowDto>;
        tipos![0].Premios.Count.ShouldBe(1); // deduplicated
    }
}
