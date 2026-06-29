using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Documents.Reports;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Documents.Reports;

[TestFixture]
public class AnexoPremiosReportTests
{
    private ApplicationDbContext _db = null!;
    private Mock<IUser> _userMock = null!;
    private AnexoPremiosReport _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _userMock = new Mock<IUser>();
        _userMock.Setup(u => u.Id).Returns((string?)null);
        _sut = new AnexoPremiosReport(_db, _userMock.Object);
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
        _db.UserAwardees.Add(new UserAwarded { UserId = "u1", AwardId = 1, AwardedAt = DateTime.Today });
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
        _db.UserAwardees.Add(new UserAwarded { UserId = "u1", AwardId = 1, AwardedAt = DateTime.Today });
        _db.UserAwardees.Add(new UserAwarded { UserId = "u1", AwardId = 2, AwardedAt = DateTime.Today });
        await _db.SaveChangesAsync();

        var result = await _sut.GatherVariablesAsync(null, default);
        var tipos = result["TiposPremio"] as List<AnexoPremiosTipoRowDto>;
        tipos![0].Premios.Count.ShouldBe(1); // deduplicated
    }

    [Test]
    public async Task GatherVariablesAsync_WithAreaFilter_IncludesAwardFromUserArea()
    {
        _db.Users.Add(new User { Id = "req-a1", AreaId = "area-a", Email = "r@t.cu", UserName = "req", UserLastName1 = "R" });
        _db.Users.Add(new User { Id = "member-a1", AreaId = "area-a", Email = "m@t.cu", UserName = "mem", UserLastName1 = "M" });
        _db.AwardTypes.Add(new AwardType { Id = 50, Name = "Nacional" });
        _db.Awards.Add(new Award { Id = 50, Name = "Premio del Área", AwardTypeId = 50 });
        _db.UserAwardees.Add(new UserAwarded { UserId = "member-a1", AwardId = 50, AwardedAt = DateTime.Today });
        await _db.SaveChangesAsync();

        _userMock.Setup(u => u.Id).Returns("req-a1");

        var result = await _sut.GatherVariablesAsync(null, default);
        var tipos = result["TiposPremio"] as List<AnexoPremiosTipoRowDto>;
        tipos.ShouldNotBeNull();
        tipos![0].Premios.Count.ShouldBe(1);
        tipos[0].Premios[0].Titulo.ShouldBe("Premio del Área");
    }

    [Test]
    public async Task GatherVariablesAsync_WithAreaFilter_ExcludesAwardFromOtherArea()
    {
        _db.Users.Add(new User { Id = "req-a2", AreaId = "area-a", Email = "r2@t.cu", UserName = "req2", UserLastName1 = "R2" });
        _db.Users.Add(new User { Id = "member-b1", AreaId = "area-b", Email = "mb@t.cu", UserName = "mb", UserLastName1 = "MB" });
        _db.AwardTypes.Add(new AwardType { Id = 60, Name = "Internacional" });
        _db.Awards.Add(new Award { Id = 60, Name = "Premio Otra Área", AwardTypeId = 60 });
        _db.UserAwardees.Add(new UserAwarded { UserId = "member-b1", AwardId = 60, AwardedAt = DateTime.Today });
        await _db.SaveChangesAsync();

        _userMock.Setup(u => u.Id).Returns("req-a2");

        var result = await _sut.GatherVariablesAsync(null, default);
        var tipos = result["TiposPremio"] as List<AnexoPremiosTipoRowDto>;
        tipos.ShouldNotBeNull();
        tipos![0].Premios.ShouldBeEmpty();
    }
}
