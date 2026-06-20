using Dashboard_v2.Application.Proyectos;
using Dashboard_v2.Domain.Constants;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Domain.Enums;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Proyectos;

/// <summary>
/// Tests for all concrete FluentValidation validators of Proyecto requests.
/// </summary>
public class ProyectoValidatorTests
{
    private static ApplicationDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(opts);
    }

    private static string SeedMinimum(ApplicationDbContext db)
    {
        var clasif = new Clasificacion { Id = "clasif-1", Nombre = "Básica" };
        db.Clasificaciones.Add(clasif);
        db.SaveChanges();
        return clasif.Id;
    }

    // ── ProyectoBaseValidator – shared rules ──────────────────────────────────

    [Test]
    public async Task BaseValidator_EmptyTitulo_FailsWithMessage()
    {
        await using var db = CreateDb();
        var clasificId = SeedMinimum(db);
        var validator = new ProyectoEnRevisionUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(new ProyectoEnRevisionUpsertRequest
        {
            Titulo = "",
            JefeId = "j1",
            ClasificacionId = clasificId,
            Tipo = "PE"
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ProyectoEnRevisionUpsertRequest.Titulo) &&
            e.ErrorMessage.Contains("título es obligatorio"));
    }

    [Test]
    public async Task BaseValidator_EmptyClasificacionId_FailsWithMessage()
    {
        await using var db = CreateDb();
        SeedMinimum(db);
        var validator = new ProyectoEnRevisionUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(new ProyectoEnRevisionUpsertRequest
        {
            Titulo = "Título",
            JefeId = "j1",
            ClasificacionId = "",
            Tipo = "PE"
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ProyectoEnRevisionUpsertRequest.ClasificacionId));
    }

    [Test]
    public async Task BaseValidator_NonExistentClasificacionId_FailsWithMessage()
    {
        await using var db = CreateDb();
        SeedMinimum(db);
        var validator = new ProyectoEnRevisionUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(new ProyectoEnRevisionUpsertRequest
        {
            Titulo = "Título",
            JefeId = "j1",
            ClasificacionId = "no-existe",
            Tipo = "PE"
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ProyectoEnRevisionUpsertRequest.ClasificacionId) &&
            e.ErrorMessage.Contains("clasificación indicada no existe"));
    }

    // ── EnRevision – Tipo rule ────────────────────────────────────────────────

    [Test]
    public async Task EnRevisionValidator_EmptyTipo_FailsWithMessage()
    {
        await using var db = CreateDb();
        var clasificId = SeedMinimum(db);
        var validator = new ProyectoEnRevisionUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(new ProyectoEnRevisionUpsertRequest
        {
            Titulo = "Título",
            JefeId = "j1",
            ClasificacionId = clasificId,
            Tipo = ""
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ProyectoEnRevisionUpsertRequest.Tipo) &&
            e.ErrorMessage.Contains("tipo de proyecto es obligatorio"));
    }

    [Test]
    public async Task EnRevisionValidator_InvalidTipo_FailsWithMessage()
    {
        await using var db = CreateDb();
        var clasificId = SeedMinimum(db);
        var validator = new ProyectoEnRevisionUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(new ProyectoEnRevisionUpsertRequest
        {
            Titulo = "Título",
            JefeId = "j1",
            ClasificacionId = clasificId,
            Tipo = "INVALIDO"
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ProyectoEnRevisionUpsertRequest.Tipo) &&
            e.ErrorMessage.Contains("debe ser uno de:"));
    }

    [TestCase("PE")]
    [TestCase("PAP")]
    [TestCase("PDL")]
    [TestCase("PNE")]
    [TestCase("PRCI")]
    [TestCase("PNAP")]
    public async Task EnRevisionValidator_ValidTipo_NoTipoError(string tipo)
    {
        await using var db = CreateDb();
        var clasificId = SeedMinimum(db);
        var validator = new ProyectoEnRevisionUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(new ProyectoEnRevisionUpsertRequest
        {
            Titulo = "Título",
            JefeId = "j1",
            ClasificacionId = clasificId,
            Tipo = tipo
        });

        result.Errors.ShouldNotContain(e => e.PropertyName == nameof(ProyectoEnRevisionUpsertRequest.Tipo));
    }

    [Test]
    public void TiposProyectoEjecucion_ContainsAllExpectedValues()
    {
        var validos = TiposProyectoEjecucion.Validos;
        validos.ShouldContain("PE");
        validos.ShouldContain("PAP");
        validos.ShouldContain("PDL");
        validos.ShouldContain("PNE");
        validos.ShouldContain("PRCI");
        validos.ShouldContain("PNAP");
        validos.Count.ShouldBe(6);
    }

    // ── Empresarial – EmpresasIds rule ────────────────────────────────────────

    [Test]
    public async Task EmpresarialValidator_EmptyEmpresasIds_FailsWithMessage()
    {
        await using var db = CreateDb();
        var clasificId = SeedMinimum(db);
        var validator = new ProyectoEmpresarialUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(BuildEmpresarial(clasificId, empresasIds: []));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ProyectoEmpresarialUpsertRequest.EmpresasIds) &&
            e.ErrorMessage.Contains("al menos una empresa"));
    }

    [Test]
    public async Task EmpresarialValidator_WithEmpresasIds_NoEmpresasError()
    {
        await using var db = CreateDb();
        var clasificId = SeedMinimum(db);
        var validator = new ProyectoEmpresarialUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(BuildEmpresarial(clasificId, empresasIds: ["inst-1"]));

        result.Errors.ShouldNotContain(e => e.PropertyName == nameof(ProyectoEmpresarialUpsertRequest.EmpresasIds));
    }

    // ── ApoyoPrograma – ProgramasIds rule ─────────────────────────────────────

    [Test]
    public async Task ApoyoProgramaValidator_EmptyProgramasIds_FailsWithMessage()
    {
        await using var db = CreateDb();
        var clasificId = SeedMinimum(db);
        var validator = new ProyectoApoyoProgramaUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(BuildApoyoPrograma(clasificId, programasIds: []));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ProyectoApoyoProgramaUpsertRequest.ProgramasIds) &&
            e.ErrorMessage.Contains("al menos un programa"));
    }

    // ── DesarrolloLocal – MunicipioId rule ────────────────────────────────────

    [Test]
    public async Task DesarrolloLocalValidator_ZeroMunicipioId_FailsWithMessage()
    {
        await using var db = CreateDb();
        var clasificId = SeedMinimum(db);
        var validator = new ProyectoDesarrolloLocalUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(BuildDesarrolloLocal(clasificId, municipioId: 0));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ProyectoDesarrolloLocalUpsertRequest.MunicipioId) &&
            e.ErrorMessage.Contains("municipio es obligatorio"));
    }

    [Test]
    public async Task DesarrolloLocalValidator_ValidMunicipioId_NoMunicipioError()
    {
        await using var db = CreateDb();
        var clasificId = SeedMinimum(db);
        var validator = new ProyectoDesarrolloLocalUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(BuildDesarrolloLocal(clasificId, municipioId: 1));

        result.Errors.ShouldNotContain(e => e.PropertyName == nameof(ProyectoDesarrolloLocalUpsertRequest.MunicipioId));
    }

    // ── NoEmpresarial – EntidadesIds rule ─────────────────────────────────────

    [Test]
    public async Task NoEmpresarialValidator_EmptyEntidadesIds_FailsWithMessage()
    {
        await using var db = CreateDb();
        var clasificId = SeedMinimum(db);
        var validator = new ProyectoNoEmpresarialUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(BuildNoEmpresarial(clasificId, entidadesIds: []));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ProyectoNoEmpresarialUpsertRequest.EntidadesIds) &&
            e.ErrorMessage.Contains("al menos una entidad"));
    }

    // ── ColabInternacional ────────────────────────────────────────────────────

    [Test]
    public async Task ColabInternacionalValidator_EmptyFuentesIds_FailsWithMessage()
    {
        await using var db = CreateDb();
        var clasificId = SeedMinimum(db);
        var validator = new ProyectoColabInternacionalUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(BuildColabInternacional(
            clasificId, fuentesIds: [], terminos: "TDR válidos"));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ProyectoColabInternacionalUpsertRequest.FuentesFinanciacionIds) &&
            e.ErrorMessage.Contains("al menos una fuente de financiación"));
    }

    [Test]
    public async Task ColabInternacionalValidator_EmptyTerminosReferencia_FailsWithMessage()
    {
        await using var db = CreateDb();
        var clasificId = SeedMinimum(db);
        var validator = new ProyectoColabInternacionalUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(BuildColabInternacional(
            clasificId, fuentesIds: [1], terminos: ""));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ProyectoColabInternacionalUpsertRequest.TerminosReferencia) &&
            e.ErrorMessage.Contains("términos de referencia son obligatorios"));
    }

    [Test]
    public async Task ColabInternacionalValidator_BothFieldsProvided_NoSpecificErrors()
    {
        await using var db = CreateDb();
        var clasificId = SeedMinimum(db);
        var validator = new ProyectoColabInternacionalUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(BuildColabInternacional(
            clasificId, fuentesIds: [1], terminos: "TOR 2025"));

        result.Errors.ShouldNotContain(e =>
            e.PropertyName == nameof(ProyectoColabInternacionalUpsertRequest.FuentesFinanciacionIds));
        result.Errors.ShouldNotContain(e =>
            e.PropertyName == nameof(ProyectoColabInternacionalUpsertRequest.TerminosReferencia));
    }

    // ── PNAP – FuentesFinanciacionIds rule ────────────────────────────────────

    [Test]
    public async Task PNAPValidator_EmptyFuentesIds_FailsWithMessage()
    {
        await using var db = CreateDb();
        var clasificId = SeedMinimum(db);
        var validator = new ProyectoPNAPUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(BuildPNAP(clasificId, fuentesIds: []));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ProyectoPNAPUpsertRequest.FuentesFinanciacionIds) &&
            e.ErrorMessage.Contains("al menos una fuente de financiación"));
    }

    // ── Private request builders ──────────────────────────────────────────────

    private static ProyectoEmpresarialUpsertRequest BuildEmpresarial(
        string clasificId, IList<string> empresasIds) => new()
    {
        Titulo = "Título",
        JefeId = "j1",
        ClasificacionId = clasificId,
        FechaInicio = new DateOnly(2025, 1, 1),
        CodigoProyecto = "P-001",
        EmpresasIds = empresasIds
    };

    private static ProyectoApoyoProgramaUpsertRequest BuildApoyoPrograma(
        string clasificId, IList<int> programasIds) => new()
    {
        Titulo = "Título",
        JefeId = "j1",
        ClasificacionId = clasificId,
        FechaInicio = new DateOnly(2025, 1, 1),
        CodigoProyecto = "P-001",
        ProgramasIds = programasIds,
        TipoPAP = TipoPAP.Nacional
    };

    private static ProyectoDesarrolloLocalUpsertRequest BuildDesarrolloLocal(
        string clasificId, int municipioId) => new()
    {
        Titulo = "Título",
        JefeId = "j1",
        ClasificacionId = clasificId,
        FechaInicio = new DateOnly(2025, 1, 1),
        CodigoProyecto = "P-001",
        MunicipioId = municipioId
    };

    private static ProyectoNoEmpresarialUpsertRequest BuildNoEmpresarial(
        string clasificId, IList<string> entidadesIds) => new()
    {
        Titulo = "Título",
        JefeId = "j1",
        ClasificacionId = clasificId,
        FechaInicio = new DateOnly(2025, 1, 1),
        CodigoProyecto = "P-001",
        EntidadesIds = entidadesIds
    };

    private static ProyectoColabInternacionalUpsertRequest BuildColabInternacional(
        string clasificId, IList<int> fuentesIds, string terminos) => new()
    {
        Titulo = "Título",
        JefeId = "j1",
        ClasificacionId = clasificId,
        FechaInicio = new DateOnly(2025, 1, 1),
        CodigoProyecto = "P-001",
        FuentesFinanciacionIds = fuentesIds,
        TerminosReferencia = terminos
    };

    private static ProyectoPNAPUpsertRequest BuildPNAP(
        string clasificId, IList<int> fuentesIds) => new()
    {
        Titulo = "Título",
        JefeId = "j1",
        ClasificacionId = clasificId,
        FechaInicio = new DateOnly(2025, 1, 1),
        CodigoProyecto = "P-001",
        FuentesFinanciacionIds = fuentesIds
    };
}
