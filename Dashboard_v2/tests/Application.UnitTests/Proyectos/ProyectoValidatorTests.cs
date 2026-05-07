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
/// Each test class covers:
///   - The inherited base rules (Titulo, ClasificacionId, AreaId) through one representative test.
///   - The specific rule(s) unique to that concrete validator.
/// </summary>
public class ProyectoValidatorTests
{
    // ── DB helper ─────────────────────────────────────────────────────────────

    private static ApplicationDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(opts);
    }

    /// <summary>Seeds the minimum data needed for a valid request.</summary>
    private static (string areaId, string clasificId) SeedMinimum(ApplicationDbContext db)
    {
        var area = new Area { Id = "area-1", Nombre = "MATCOM", Descripcion = "d", UniversidadId = "uh" };
        var clasif = new Clasificacion { Id = "clasif-1", Nombre = "Básica" };
        db.Areas.Add(area);
        db.Clasificaciones.Add(clasif);
        db.SaveChanges();
        return (area.Id, clasif.Id);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ProyectoBaseValidator – shared rules (tested via EnRevision validator)
    // ══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task BaseValidator_EmptyTitulo_FailsWithMessage()
    {
        await using var db = CreateDb();
        var (areaId, clasificId) = SeedMinimum(db);
        var validator = new ProyectoEnRevisionUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(new ProyectoEnRevisionUpsertRequest
        {
            Titulo = "",
            JefeId = "j1",
            ClasificacionId = clasificId,
            AreaId = areaId,
            Situacion = "Pendiente",
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
        var (areaId, _) = SeedMinimum(db);
        var validator = new ProyectoEnRevisionUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(new ProyectoEnRevisionUpsertRequest
        {
            Titulo = "Título",
            JefeId = "j1",
            ClasificacionId = "",
            AreaId = areaId,
            Situacion = "Pendiente",
            Tipo = "PE"
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ProyectoEnRevisionUpsertRequest.ClasificacionId));
    }

    [Test]
    public async Task BaseValidator_NonExistentClasificacionId_FailsWithMessage()
    {
        await using var db = CreateDb();
        var (areaId, _) = SeedMinimum(db);
        var validator = new ProyectoEnRevisionUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(new ProyectoEnRevisionUpsertRequest
        {
            Titulo = "Título",
            JefeId = "j1",
            ClasificacionId = "no-existe",
            AreaId = areaId,
            Situacion = "Pendiente",
            Tipo = "PE"
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ProyectoEnRevisionUpsertRequest.ClasificacionId) &&
            e.ErrorMessage.Contains("clasificación indicada no existe"));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ProyectoEnRevisionUpsertRequestValidator – Tipo rule
    // ══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task EnRevisionValidator_EmptyTipo_FailsWithMessage()
    {
        await using var db = CreateDb();
        var (areaId, clasificId) = SeedMinimum(db);
        var validator = new ProyectoEnRevisionUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(new ProyectoEnRevisionUpsertRequest
        {
            Titulo = "Título",
            JefeId = "j1",
            ClasificacionId = clasificId,
            AreaId = areaId,
            Situacion = "Pendiente",
            Tipo = ""    // empty
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
        var (areaId, clasificId) = SeedMinimum(db);
        var validator = new ProyectoEnRevisionUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(new ProyectoEnRevisionUpsertRequest
        {
            Titulo = "Título",
            JefeId = "j1",
            ClasificacionId = clasificId,
            AreaId = areaId,
            Situacion = "Pendiente",
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
        var (areaId, clasificId) = SeedMinimum(db);
        var validator = new ProyectoEnRevisionUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(new ProyectoEnRevisionUpsertRequest
        {
            Titulo = "Título",
            JefeId = "j1",
            ClasificacionId = clasificId,
            AreaId = areaId,
            Situacion = "Pendiente",
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

    // ══════════════════════════════════════════════════════════════════════════
    // ProyectoEmpresarialUpsertRequestValidator – Empresa rule
    // ══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task EmpresarialValidator_EmptyEmpresa_FailsWithMessage()
    {
        await using var db = CreateDb();
        var (areaId, clasificId) = SeedMinimum(db);
        var validator = new ProyectoEmpresarialUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(BuildEmpresarial(areaId, clasificId, empresa: ""));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ProyectoEmpresarialUpsertRequest.Empresa) &&
            e.ErrorMessage.Contains("nombre de la empresa es obligatorio"));
    }

    [Test]
    public async Task EmpresarialValidator_ValidEmpresa_NoEmpresaError()
    {
        await using var db = CreateDb();
        var (areaId, clasificId) = SeedMinimum(db);
        var validator = new ProyectoEmpresarialUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(BuildEmpresarial(areaId, clasificId, empresa: "MATCOM SRL"));

        result.Errors.ShouldNotContain(e => e.PropertyName == nameof(ProyectoEmpresarialUpsertRequest.Empresa));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ProyectoApoyoProgramaUpsertRequestValidator – NombrePrograma rule
    // ══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task ApoyoProgramaValidator_EmptyNombrePrograma_FailsWithMessage()
    {
        await using var db = CreateDb();
        var (areaId, clasificId) = SeedMinimum(db);
        var validator = new ProyectoApoyoProgramaUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(BuildApoyoPrograma(areaId, clasificId, nombrePrograma: ""));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ProyectoApoyoProgramaUpsertRequest.NombrePrograma) &&
            e.ErrorMessage.Contains("nombre del programa es obligatorio"));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ProyectoDesarrolloLocalUpsertRequestValidator – Municipio rule
    // ══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task DesarrolloLocalValidator_EmptyMunicipio_FailsWithMessage()
    {
        await using var db = CreateDb();
        var (areaId, clasificId) = SeedMinimum(db);
        var validator = new ProyectoDesarrolloLocalUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(BuildDesarrolloLocal(areaId, clasificId, municipio: ""));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ProyectoDesarrolloLocalUpsertRequest.Municipio) &&
            e.ErrorMessage.Contains("municipio es obligatorio"));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ProyectoNoEmpresarialUpsertRequestValidator – EntidadNoEmpresarial rule
    // ══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task NoEmpresarialValidator_EmptyEntidad_FailsWithMessage()
    {
        await using var db = CreateDb();
        var (areaId, clasificId) = SeedMinimum(db);
        var validator = new ProyectoNoEmpresarialUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(BuildNoEmpresarial(areaId, clasificId, entidad: ""));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ProyectoNoEmpresarialUpsertRequest.EntidadNoEmpresarial) &&
            e.ErrorMessage.Contains("entidad no empresarial es obligatoria"));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ProyectoColabInternacionalUpsertRequestValidator
    // ══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task ColabInternacionalValidator_EmptyFuenteFinanciacion_FailsWithMessage()
    {
        await using var db = CreateDb();
        var (areaId, clasificId) = SeedMinimum(db);
        var validator = new ProyectoColabInternacionalUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(BuildColabInternacional(
            areaId, clasificId, fuente: "", terminos: "términos válidos"));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ProyectoColabInternacionalUpsertRequest.FuenteFinanciacion) &&
            e.ErrorMessage.Contains("fuente de financiación es obligatoria"));
    }

    [Test]
    public async Task ColabInternacionalValidator_EmptyTerminosReferencia_FailsWithMessage()
    {
        await using var db = CreateDb();
        var (areaId, clasificId) = SeedMinimum(db);
        var validator = new ProyectoColabInternacionalUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(BuildColabInternacional(
            areaId, clasificId, fuente: "UE", terminos: ""));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ProyectoColabInternacionalUpsertRequest.TerminosReferencia) &&
            e.ErrorMessage.Contains("términos de referencia son obligatorios"));
    }

    [Test]
    public async Task ColabInternacionalValidator_BothFieldsProvided_NoSpecificErrors()
    {
        await using var db = CreateDb();
        var (areaId, clasificId) = SeedMinimum(db);
        var validator = new ProyectoColabInternacionalUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(BuildColabInternacional(
            areaId, clasificId, fuente: "UE", terminos: "TOR 2025"));

        result.Errors.ShouldNotContain(e =>
            e.PropertyName == nameof(ProyectoColabInternacionalUpsertRequest.FuenteFinanciacion));
        result.Errors.ShouldNotContain(e =>
            e.PropertyName == nameof(ProyectoColabInternacionalUpsertRequest.TerminosReferencia));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ProyectoPNAPUpsertRequestValidator – FinanciamientoUH rule
    // ══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task PNAPValidator_EmptyFinanciamientoUH_FailsWithMessage()
    {
        await using var db = CreateDb();
        var (areaId, clasificId) = SeedMinimum(db);
        var validator = new ProyectoPNAPUpsertRequestValidator(db);

        var result = await validator.ValidateAsync(BuildPNAP(areaId, clasificId, financiamiento: ""));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ProyectoPNAPUpsertRequest.FinanciamientoUH) &&
            e.ErrorMessage.Contains("financiamiento UH es obligatorio"));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Private request builders
    // ══════════════════════════════════════════════════════════════════════════

    private static ProyectoEnEjecucionUpsertRequestBase BaseEjecucion(string areaId, string clasificId) =>
        // returned as base to be used by derived builder helpers
        new ProyectoEmpresarialUpsertRequest
        {
            Titulo = "Título",
            JefeId = "j1",
            ClasificacionId = clasificId,
            AreaId = areaId,
            FechaInicio = new DateOnly(2025, 1, 1),
            EstadoDeEjecucion = "En curso",
            CodigoProyecto = "P-001",
            EntidadEjecutoraPrincipal = "UH",
            Empresa = "X"
        };

    private static ProyectoEmpresarialUpsertRequest BuildEmpresarial(
        string areaId, string clasificId, string empresa) => new()
    {
        Titulo = "Título",
        JefeId = "j1",
        ClasificacionId = clasificId,
        AreaId = areaId,
        FechaInicio = new DateOnly(2025, 1, 1),
        EstadoDeEjecucion = "En curso",
        CodigoProyecto = "P-001",
        EntidadEjecutoraPrincipal = "UH",
        Empresa = empresa
    };

    private static ProyectoApoyoProgramaUpsertRequest BuildApoyoPrograma(
        string areaId, string clasificId, string nombrePrograma) => new()
    {
        Titulo = "Título",
        JefeId = "j1",
        ClasificacionId = clasificId,
        AreaId = areaId,
        FechaInicio = new DateOnly(2025, 1, 1),
        EstadoDeEjecucion = "En curso",
        CodigoProyecto = "P-001",
        EntidadEjecutoraPrincipal = "UH",
        NombrePrograma = nombrePrograma,
        TipoPAP = TipoPAP.Nacional
    };

    private static ProyectoDesarrolloLocalUpsertRequest BuildDesarrolloLocal(
        string areaId, string clasificId, string municipio) => new()
    {
        Titulo = "Título",
        JefeId = "j1",
        ClasificacionId = clasificId,
        AreaId = areaId,
        FechaInicio = new DateOnly(2025, 1, 1),
        EstadoDeEjecucion = "En curso",
        CodigoProyecto = "P-001",
        EntidadEjecutoraPrincipal = "UH",
        Municipio = municipio
    };

    private static ProyectoNoEmpresarialUpsertRequest BuildNoEmpresarial(
        string areaId, string clasificId, string entidad) => new()
    {
        Titulo = "Título",
        JefeId = "j1",
        ClasificacionId = clasificId,
        AreaId = areaId,
        FechaInicio = new DateOnly(2025, 1, 1),
        EstadoDeEjecucion = "En curso",
        CodigoProyecto = "P-001",
        EntidadEjecutoraPrincipal = "UH",
        EntidadNoEmpresarial = entidad
    };

    private static ProyectoColabInternacionalUpsertRequest BuildColabInternacional(
        string areaId, string clasificId, string fuente, string terminos) => new()
    {
        Titulo = "Título",
        JefeId = "j1",
        ClasificacionId = clasificId,
        AreaId = areaId,
        FechaInicio = new DateOnly(2025, 1, 1),
        EstadoDeEjecucion = "En curso",
        CodigoProyecto = "P-001",
        EntidadEjecutoraPrincipal = "UH",
        FuenteFinanciacion = fuente,
        TerminosReferencia = terminos
    };

    private static ProyectoPNAPUpsertRequest BuildPNAP(
        string areaId, string clasificId, string financiamiento) => new()
    {
        Titulo = "Título",
        JefeId = "j1",
        ClasificacionId = clasificId,
        AreaId = areaId,
        FechaInicio = new DateOnly(2025, 1, 1),
        EstadoDeEjecucion = "En curso",
        CodigoProyecto = "P-001",
        EntidadEjecutoraPrincipal = "UH",
        FinanciamientoUH = financiamiento
    };
}
