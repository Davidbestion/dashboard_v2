using Dashboard_v2.Application.Documents.Reports;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Domain.Enums;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Documents.Reports;

[TestFixture]
public class AnexoRedesNacInterReportTests
{
    private ApplicationDbContext _db = null!;
    private AnexoRedesNacInterReport _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _sut = new AnexoRedesNacInterReport(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public void ReportName_ReturnsExpected()
    {
        _sut.ReportName.ShouldBe("anexo-redes-nac-inter");
    }

    [Test]
    public void TemplateName_ReturnsExpected()
    {
        _sut.TemplateName.ShouldBe("AnexoRedesNacInter");
    }

    [Test]
    public async Task GatherVariablesAsync_Empty_ReturnsBothKeys()
    {
        var result = await _sut.GatherVariablesAsync(null, default);
        result.ShouldContainKey("RedesNacionales");
        result.ShouldContainKey("RedesInternacionales");
        (result["RedesNacionales"] as List<AnexoRedNacionalRowDto>)!.ShouldBeEmpty();
        (result["RedesInternacionales"] as List<AnexoRedInternacionalRowDto>)!.ShouldBeEmpty();
    }

    [Test]
    public async Task GatherVariablesAsync_WithNacionalRed_AppearsInNacionales()
    {
        _db.Reds.Add(new Red { Nombre = "Red Nacional A", Tipo = TipoRed.Nacional, CantidadProfesores = 5 });
        await _db.SaveChangesAsync();

        var result = await _sut.GatherVariablesAsync(null, default);
        var nac = result["RedesNacionales"] as List<AnexoRedNacionalRowDto>;
        nac.ShouldHaveSingleItem();
        nac![0].Nombre.ShouldBe("Red Nacional A");
        nac[0].CantidadProfesores.ShouldBe(5);
    }

    [Test]
    public async Task GatherVariablesAsync_WithInternacionalRed_AppearsInInternacionales()
    {
        _db.Countries.Add(new Country { Id = 1, Name = "Cuba" });
        _db.Reds.Add(new Red { Nombre = "Red Inter A", Tipo = TipoRed.Internacional, CountryId = 1, CantidadProfesores = 3 });
        await _db.SaveChangesAsync();

        var result = await _sut.GatherVariablesAsync(null, default);
        var inter = result["RedesInternacionales"] as List<AnexoRedInternacionalRowDto>;
        inter.ShouldHaveSingleItem();
        inter![0].Nombre.ShouldBe("Red Inter A");
        inter[0].Pais.ShouldBe("Cuba");
    }

    [Test]
    public async Task GatherVariablesAsync_UniversitariaRed_IsFiltered()
    {
        _db.Reds.Add(new Red { Nombre = "Red Univ", Tipo = TipoRed.Universitaria });
        await _db.SaveChangesAsync();

        var result = await _sut.GatherVariablesAsync(null, default);
        (result["RedesNacionales"] as List<AnexoRedNacionalRowDto>)!.ShouldBeEmpty();
        (result["RedesInternacionales"] as List<AnexoRedInternacionalRowDto>)!.ShouldBeEmpty();
    }
}
