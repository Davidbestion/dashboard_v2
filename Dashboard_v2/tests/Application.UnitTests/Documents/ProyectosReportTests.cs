using Dashboard_v2.Application.Documents.Reports;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Domain.Enums;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Documents.Reports;

[TestFixture]
public class ProyectosReportTests
{
    private ApplicationDbContext _db = null!;
    private ProyectosReport _sut = null!;

    private static readonly string AreaId = "area-1";
    private static readonly string ClasifId = "clasif-1";
    private static readonly string JefeId = "jefe-1";

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _sut = new ProyectosReport(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private async Task SeedBaseAsync()
    {
        _db.Areas.Add(new Area { Id = AreaId, Nombre = "MATCOM", Descripcion = "d", UniversidadId = "uh" });
        _db.Clasificaciones.Add(new Clasificacion { Id = ClasifId, Nombre = "Básica" });
        _db.Users.Add(new User
        {
            Id = JefeId,
            UserName = "juan",
            UserLastName1 = "Pérez",
            Email = "jefe@uh.cu",
            PasswordHash = "x",
            IsActive = true,
            BirthDate = DateTime.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            AreaId = AreaId
        });
        await _db.SaveChangesAsync();
    }

    private T BaseProject<T>(T p) where T : ProyectoEnEjecucion
    {
        p.Titulo = typeof(T).Name;
        p.JefeId = JefeId;
        p.ClasificacionId = ClasifId;
        p.NumeroMiembros = 3;
        p.CantidadMiembrosUH = 2;
        p.FechaInicio = DateOnly.FromDateTime(DateTime.Today);
        p.CodigoProyecto = $"{typeof(T).Name}-001";
        return p;
    }

    [Test]
    public void ReportName_ReturnsExpected()
    {
        _sut.ReportName.ShouldBe("anexo-proyectos");
    }

    [Test]
    public void TemplateName_ReturnsExpected()
    {
        _sut.TemplateName.ShouldBe("AnexoProyectos");
    }

    [Test]
    public async Task GatherVariablesAsync_Empty_ReturnsAllExpectedKeys()
    {
        var result = await _sut.GatherVariablesAsync(null, default);

        result.ShouldContainKey("PE");
        result.ShouldContainKey("PAPN");
        result.ShouldContainKey("PAPS");
        result.ShouldContainKey("PAPT");
        result.ShouldContainKey("PNE");
        result.ShouldContainKey("PDL");
        result.ShouldContainKey("PRCI");
        result.ShouldContainKey("PNAP");
        result.ShouldContainKey("NuevasAplicaciones");
    }

    [Test]
    public async Task GatherVariablesAsync_Empty_AllListsEmpty()
    {
        var result = await _sut.GatherVariablesAsync(null, default);

        foreach (var key in result.Keys)
        {
            var list = result[key] as System.Collections.IList;
            list.ShouldNotBeNull($"Key '{key}' is not a list");
            list.Count.ShouldBe(0, $"Key '{key}' should be empty");
        }
    }

    [Test]
    public async Task GatherVariablesAsync_WithProyectoEmpresarial_PopulatesPERow()
    {
        await SeedBaseAsync();
        var empresa = new Institution { Nombre = "Acme S.A." };
        _db.Institutions.Add(empresa);
        await _db.SaveChangesAsync();

        var proy = BaseProject(new ProyectoEmpresarial());
        proy.Empresas = new List<Institution> { empresa };
        _db.Proyectos.Add(proy);
        await _db.SaveChangesAsync();

        var result = await _sut.GatherVariablesAsync(null, default);

        var pe = (result["PE"] as System.Collections.IList)!;
        pe.Count.ShouldBe(1);
        var row = (ProyectoPERowDto)pe[0]!;
        row.Empresa.ShouldBe("Acme S.A.");
    }

    [Test]
    public async Task GatherVariablesAsync_WithProyectoPAP_Nacional_PopulatesPAPNRow()
    {
        await SeedBaseAsync();
        var prog = new Programa { Nombre = "Prog Nacional" };
        _db.Programas.Add(prog);
        await _db.SaveChangesAsync();

        var proy = BaseProject(new ProyectoApoyoPrograma { TipoPAP = TipoPAP.Nacional });
        proy.Programas = new List<Programa> { prog };
        _db.Proyectos.Add(proy);
        await _db.SaveChangesAsync();

        var result = await _sut.GatherVariablesAsync(null, default);

        var papn = (result["PAPN"] as System.Collections.IList)!;
        papn.Count.ShouldBe(1);
        var row = (ProyectoPAPRowDto)papn[0]!;
        row.NombrePrograma.ShouldBe("Prog Nacional");
    }

    [Test]
    public async Task GatherVariablesAsync_WithProyectoPAP_Sectorial_PopulatesPAPSRow()
    {
        await SeedBaseAsync();
        var prog = new Programa { Nombre = "Prog Sectorial" };
        _db.Programas.Add(prog);
        await _db.SaveChangesAsync();

        var proy = BaseProject(new ProyectoApoyoPrograma { TipoPAP = TipoPAP.Sectorial });
        proy.Programas = new List<Programa> { prog };
        _db.Proyectos.Add(proy);
        await _db.SaveChangesAsync();

        var result = await _sut.GatherVariablesAsync(null, default);

        var paps = (result["PAPS"] as System.Collections.IList)!;
        paps.Count.ShouldBe(1);
    }

    [Test]
    public async Task GatherVariablesAsync_WithProyectoPAP_Territorial_PopulatesPAPTRow()
    {
        await SeedBaseAsync();
        var prog = new Programa { Nombre = "Prog Territorial" };
        _db.Programas.Add(prog);
        await _db.SaveChangesAsync();

        var proy = BaseProject(new ProyectoApoyoPrograma { TipoPAP = TipoPAP.Territorial });
        proy.Programas = new List<Programa> { prog };
        _db.Proyectos.Add(proy);
        await _db.SaveChangesAsync();

        var result = await _sut.GatherVariablesAsync(null, default);

        var papt = (result["PAPT"] as System.Collections.IList)!;
        papt.Count.ShouldBe(1);
    }

    [Test]
    public async Task GatherVariablesAsync_WithProyectoNoEmpresarial_PopulatesPNERow()
    {
        await SeedBaseAsync();
        var entidad = new Institution { Nombre = "Ministerio" };
        _db.Institutions.Add(entidad);
        await _db.SaveChangesAsync();

        var proy = BaseProject(new ProyectoNoEmpresarial());
        proy.Entidades = new List<Institution> { entidad };
        _db.Proyectos.Add(proy);
        await _db.SaveChangesAsync();

        var result = await _sut.GatherVariablesAsync(null, default);

        var pne = (result["PNE"] as System.Collections.IList)!;
        pne.Count.ShouldBe(1);
        var row = (ProyectoPNERowDto)pne[0]!;
        row.EntidadNoEmpresarial.ShouldBe("Ministerio");
    }

    [Test]
    public async Task GatherVariablesAsync_WithProyectoDesarrolloLocal_PopulatesPDLRow()
    {
        await SeedBaseAsync();
        var provincia = new Provincia { Nombre = "La Habana" };
        _db.Provincias.Add(provincia);
        await _db.SaveChangesAsync();
        var municipio = new Municipio { Nombre = "Plaza", ProvinciaId = provincia.Id };
        _db.Municipios.Add(municipio);
        await _db.SaveChangesAsync();

        var proy = BaseProject(new ProyectoDesarrolloLocal { MunicipioId = municipio.Id });
        _db.Proyectos.Add(proy);
        await _db.SaveChangesAsync();

        var result = await _sut.GatherVariablesAsync(null, default);

        var pdl = (result["PDL"] as System.Collections.IList)!;
        pdl.Count.ShouldBe(1);
        var row = (ProyectoPDLRowDto)pdl[0]!;
        row.Municipio.ShouldBe("Plaza");
    }

    [Test]
    public async Task GatherVariablesAsync_WithProyectoColabInternacional_PopulatesPRCIRow()
    {
        await SeedBaseAsync();
        var fuente = new FuenteFinanciacion { Nombre = "EU" };
        _db.FuentesFinanciacion.Add(fuente);
        await _db.SaveChangesAsync();

        var proy = BaseProject(new ProyectoColabInternacional { TerminosReferencia = "TDR" });
        proy.FuentesFinanciacion = new List<FuenteFinanciacion> { fuente };
        _db.Proyectos.Add(proy);
        await _db.SaveChangesAsync();

        var result = await _sut.GatherVariablesAsync(null, default);

        var prci = (result["PRCI"] as System.Collections.IList)!;
        prci.Count.ShouldBe(1);
        var row = (ProyectoPRCIRowDto)prci[0]!;
        row.FuenteFinanciacion.ShouldBe("EU");
    }

    [Test]
    public async Task GatherVariablesAsync_WithProyectoPNAP_PopulatesPNAPRow()
    {
        await SeedBaseAsync();
        var fuente = new FuenteFinanciacion { Nombre = "1M CUP" };
        _db.FuentesFinanciacion.Add(fuente);
        await _db.SaveChangesAsync();

        var proy = BaseProject(new ProyectoPNAP());
        proy.FuentesFinanciacion = new List<FuenteFinanciacion> { fuente };
        _db.Proyectos.Add(proy);
        await _db.SaveChangesAsync();

        var result = await _sut.GatherVariablesAsync(null, default);

        var pnap = (result["PNAP"] as System.Collections.IList)!;
        pnap.Count.ShouldBe(1);
        var row = (ProyectoPNAPRowDto)pnap[0]!;
        row.FinanciamientoUH.ShouldBe("1M CUP");
    }

    [Test]
    public async Task GatherVariablesAsync_WithProyectoEnRevision_PopulatesNuevasAplicacionesRow()
    {
        await SeedBaseAsync();
        _db.Proyectos.Add(new ProyectoEnRevision
        {
            Titulo = "En Revisión Test",
            JefeId = JefeId,
            ClasificacionId = ClasifId,
            NumeroMiembros = 2,
            CantidadMiembrosUH = 1,
            Tipo = "PE"
        });
        await _db.SaveChangesAsync();

        var result = await _sut.GatherVariablesAsync(null, default);

        var nuevas = (result["NuevasAplicaciones"] as System.Collections.IList)!;
        nuevas.Count.ShouldBe(1);
        var row = (ProyectoNuevasAplicacionesRowDto)nuevas[0]!;
        row.TituloProyecto.ShouldBe("En Revisión Test");
    }
}
