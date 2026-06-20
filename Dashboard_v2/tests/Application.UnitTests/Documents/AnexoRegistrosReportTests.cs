using Dashboard_v2.Application.Documents.Reports;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Documents.Reports;

[TestFixture]
public class AnexoRegistrosReportTests
{
    private ApplicationDbContext _db = null!;
    private AnexoRegistrosReport _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _sut = new AnexoRegistrosReport(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public void ReportName_ReturnsExpected()
    {
        _sut.ReportName.ShouldBe("anexo-registros");
    }

    [Test]
    public void TemplateName_ReturnsExpected()
    {
        _sut.TemplateName.ShouldBe("AnexoRegistros");
    }

    [Test]
    public async Task GatherVariablesAsync_Empty_ReturnsAllExpectedKeys()
    {
        var result = await _sut.GatherVariablesAsync(null, default);

        result.ShouldContainKey("Patentes");
        result.ShouldContainKey("RegistrosInformaticos");
        result.ShouldContainKey("RegistrosNoInformaticos");
        result.ShouldContainKey("Normas");
        result.ShouldContainKey("ProductosTipos");
    }

    [Test]
    public async Task GatherVariablesAsync_Empty_AllListsAreEmpty()
    {
        var result = await _sut.GatherVariablesAsync(null, default);

        (result["Patentes"] as List<AnexoRegistrosPatenteRowDto>)!.ShouldBeEmpty();
        (result["RegistrosInformaticos"] as List<AnexoRegistroRowDto>)!.ShouldBeEmpty();
        (result["RegistrosNoInformaticos"] as List<AnexoRegistroRowDto>)!.ShouldBeEmpty();
        (result["Normas"] as List<AnexoNormaRowDto>)!.ShouldBeEmpty();
        (result["ProductosTipos"] as List<AnexoProductoTipoRowDto>)!.ShouldBeEmpty();
    }

    [Test]
    public async Task GatherVariablesAsync_WithPatente_PopulatesPatenteRow()
    {
        _db.Patentes.Add(new Patente
        {
            Titulo = "Patente de Prueba",
            NumeroSolicitudConcesion = "P-2024-001",
            EsNacional = true,
        });
        await _db.SaveChangesAsync();

        var result = await _sut.GatherVariablesAsync(null, default);

        var patentes = (result["Patentes"] as List<AnexoRegistrosPatenteRowDto>)!;
        patentes.Count.ShouldBe(1);
        patentes[0].Titulo.ShouldBe("Patente de Prueba");
        patentes[0].NumeroSolicitudConcesion.ShouldBe("P-2024-001");
        patentes[0].EsNacional.ShouldBeTrue();
    }

    [Test]
    public async Task GatherVariablesAsync_WithRegistro_PopulatesRow()
    {
        var institution = new Institution { Id = "inst-1", Nombre = "IDICT" };
        var country = new Country { Id = 1, Name = "Cuba" };
        _db.Institutions.Add(institution);
        _db.Countries.Add(country);
        await _db.SaveChangesAsync();

        _db.Registros.Add(new Registro
        {
            Titulo = "Registro de Prueba",
            NumeroCertificado = "R-001",
            EsInformatico = false,
            InstitutionId = institution.Id,
            CountryId = country.Id
        });
        await _db.SaveChangesAsync();

        var result = await _sut.GatherVariablesAsync(null, default);

        var noInf = (result["RegistrosNoInformaticos"] as List<AnexoRegistroRowDto>)!;
        noInf.Count.ShouldBe(1);
        noInf[0].Titulo.ShouldBe("Registro de Prueba");
        noInf[0].NumeroCertificado.ShouldBe("R-001");
        (result["RegistrosInformaticos"] as List<AnexoRegistroRowDto>)!.ShouldBeEmpty();
    }

    [Test]
    public async Task GatherVariablesAsync_WithRegistroInformatico_PopulatesInformaticoList()
    {
        var institution = new Institution { Id = "inst-2", Nombre = "UH" };
        var country = new Country { Id = 2, Name = "Cuba" };
        _db.Institutions.Add(institution);
        _db.Countries.Add(country);
        await _db.SaveChangesAsync();

        _db.Registros.Add(new Registro
        {
            Titulo = "Software Educativo",
            NumeroCertificado = "S-001",
            EsInformatico = true,
            InstitutionId = institution.Id,
            CountryId = country.Id
        });
        await _db.SaveChangesAsync();

        var result = await _sut.GatherVariablesAsync(null, default);

        var inf = (result["RegistrosInformaticos"] as List<AnexoRegistroRowDto>)!;
        inf.Count.ShouldBe(1);
        inf[0].EsInformatico.ShouldBeTrue();
    }

    [Test]
    public async Task GatherVariablesAsync_WithNorma_PopulatesNormaRow()
    {
        var institution = new Institution { Id = "inst-3", Nombre = "CITMA" };
        _db.Institutions.Add(institution);
        var tipoNorma = new TipoNorma { Nombre = "ISO" };
        _db.TiposNorma.Add(tipoNorma);
        await _db.SaveChangesAsync();

        _db.Normas.Add(new Norma
        {
            Titulo = "Norma ISO-001",
            TipoNormaId = tipoNorma.Id,
            InstitutionId = institution.Id
        });
        await _db.SaveChangesAsync();

        var result = await _sut.GatherVariablesAsync(null, default);

        var normas = (result["Normas"] as List<AnexoNormaRowDto>)!;
        normas.Count.ShouldBe(1);
        normas[0].Titulo.ShouldBe("Norma ISO-001");
        normas[0].Tipo.ShouldBe("ISO");
    }

    [Test]
    public async Task GatherVariablesAsync_WithProducto_PopulatesProductoTipoRow()
    {
        var institution = new Institution { Id = "inst-4", Nombre = "Empresa TI" };
        var tipo = new TipoProductoComercializado { Id = "tipo-1", Nombre = "Software" };
        _db.Institutions.Add(institution);
        _db.TipoProductosComercializados.Add(tipo);
        await _db.SaveChangesAsync();

        _db.ProductosComercializados.Add(new ProductoComercializado
        {
            Titulo = "App Móvil",
            TipoProductoComercializadoId = tipo.Id,
            InstitutionId = institution.Id
        });
        await _db.SaveChangesAsync();

        var result = await _sut.GatherVariablesAsync(null, default);

        var tipos = (result["ProductosTipos"] as List<AnexoProductoTipoRowDto>)!;
        tipos.Count.ShouldBe(1);
        tipos[0].TipoProductoComercializadoNombre.ShouldBe("Software");
        tipos[0].Productos.Count.ShouldBe(1);
        tipos[0].Productos[0].Titulo.ShouldBe("App Móvil");
    }
}
