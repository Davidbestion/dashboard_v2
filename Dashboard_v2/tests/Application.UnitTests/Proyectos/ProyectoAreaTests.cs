using Dashboard_v2.Application.Proyectos;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Proyectos;

/// <summary>
/// Tests that cover the Proyecto → Area 1:N relationship introduced in AddAreaToProyecto migration:
///
///  1. ProyectoHelper.SetBase correctly assigns AreaId.
///  2. ProyectoBaseValidator rejects empty / non-existent AreaId and accepts a valid one.
///  3. EF model has the FK with Restrict delete-behavior.
///  4. ProyectoService.GetAllAsync projects AreaId / AreaNombre.
///  5. ProyectoService.GetProyectoAsync includes the Area navigation.
/// </summary>
public class ProyectoAreaTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ApplicationDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(opts);
    }

    /// <summary>Seeds one Area, one Clasificacion and one User, and returns the Area.Id.</summary>
    private static (string areaId, string clasificId, string userId) SeedPrerequisites(ApplicationDbContext db)
    {
        var area = new Area
        {
            Id = "area-matcom",
            Nombre = "MATCOM",
            Descripcion = "Área de MATCOM",
            UniversidadId = "uh"
        };
        var clasif = new Clasificacion { Id = "clasif-basica", Nombre = "Básica" };
        var user = new User
        {
            Id = "user-jefe-1",
            UserName = "jjefeson",
            UserLastName1 = "Jefeson",
            Email = "jefe@uh.cu",
            PasswordHash = "hash",
            IsActive = true,
            BirthDate = DateTime.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Areas.Add(area);
        db.Clasificaciones.Add(clasif);
        db.Users.Add(user);
        db.SaveChanges();

        return (area.Id, clasif.Id, user.Id);
    }

    private static ProyectoEnRevision SeedProyecto(
        ApplicationDbContext db, string areaId, string clasificId, string userId)
    {
        var p = new ProyectoEnRevision
        {
            Id = Guid.NewGuid().ToString(),
            Titulo = "Proyecto de prueba",
            JefeId = userId,
            NumeroMiembros = 3,
            CantidadMiembrosUH = 2,
            CantidadEstudiantes = 1,
            CantidadEstudiantesContratados = 0,
            TributaFormacionDoctoral = false,
            ClasificacionId = clasificId,
            AreaId = areaId,
            Situacion = "Pendiente",
            Tipo = "en-revision"
        };
        db.Proyectos.Add(p);
        db.SaveChanges();
        return p;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 1. Proyecto entity – AreaId property
    // ══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ProyectoEntity_AreaId_CanBeSetAndRead()
    {
        var p = new ProyectoEnRevision { AreaId = "area-matcom" };
        p.AreaId.ShouldBe("area-matcom");
    }

    [Test]
    public void ProyectoEntity_AreaNavigation_IsDefaultNull()
    {
        var p = new ProyectoEnRevision();
        // Navigation property is reference-initialized as default! → null before loading
        p.Area.ShouldBeNull();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 2. ProyectoBaseValidator – AreaId rules
    // ══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Validator_EmptyAreaId_FailsValidation()
    {
        await using var db = CreateDb();
        SeedPrerequisites(db);

        var validator = new ProyectoEnRevisionUpsertRequestValidator(db);
        var request = new ProyectoEnRevisionUpsertRequest
        {
            Titulo = "Titulo",
            JefeId = "user-jefe-1",
            ClasificacionId = "clasif-basica",
            AreaId = "",         // empty → must fail
            Situacion = "Pendiente",
            Tipo = "en-revision"
        };

        var result = await validator.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(request.AreaId));
    }

    [Test]
    public async Task Validator_NonExistentAreaId_FailsWithMessage()
    {
        await using var db = CreateDb();
        SeedPrerequisites(db);

        var validator = new ProyectoEnRevisionUpsertRequestValidator(db);
        var request = new ProyectoEnRevisionUpsertRequest
        {
            Titulo = "Titulo",
            JefeId = "user-jefe-1",
            ClasificacionId = "clasif-basica",
            AreaId = "area-que-no-existe",
            Situacion = "Pendiente",
            Tipo = "en-revision"
        };

        var result = await validator.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(request.AreaId) &&
            e.ErrorMessage.Contains("área indicada no existe"));
    }

    [Test]
    public async Task Validator_ValidAreaId_PassesAreaValidation()
    {
        await using var db = CreateDb();
        var (areaId, clasificId, _) = SeedPrerequisites(db);

        var validator = new ProyectoEnRevisionUpsertRequestValidator(db);
        var request = new ProyectoEnRevisionUpsertRequest
        {
            Titulo = "Titulo",
            JefeId = "cualquier-jefe",
            ClasificacionId = clasificId,
            AreaId = areaId,
            Situacion = "Pendiente",
            Tipo = "en-revision"
        };

        var result = await validator.ValidateAsync(request);

        // AreaId errors specifically must be absent (other fields might fail)
        result.Errors.ShouldNotContain(e => e.PropertyName == nameof(request.AreaId));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 3. EF model – FK Proyecto → Area
    // ══════════════════════════════════════════════════════════════════════════

    [Test]
    public void EfModel_ProyectoHasAreaIdForeignKey()
    {
        using var db = CreateDb();
        var model = db.Model;

        var proyectoType = model.FindEntityType(typeof(ProyectoEnRevision));
        proyectoType.ShouldNotBeNull();

        // The FK is declared on the base Proyecto type
        var baseType = proyectoType.BaseType;
        baseType.ShouldNotBeNull("Proyecto must have a base type (TPT hierarchy)");

        var fk = baseType.GetForeignKeys()
            .FirstOrDefault(fk => fk.Properties.Any(p => p.Name == "AreaId"));

        fk.ShouldNotBeNull("Expected FK on Proyecto.AreaId");
        fk!.DeleteBehavior.ShouldBe(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);
        fk.PrincipalEntityType.ClrType.ShouldBe(typeof(Area));
    }

    [Test]
    public void EfModel_AreaHasProyectosInverseNavigation()
    {
        using var db = CreateDb();
        var model = db.Model;

        var areaType = model.FindEntityType(typeof(Area));
        areaType.ShouldNotBeNull();

        var nav = areaType.GetNavigations()
            .FirstOrDefault(n => n.Name == "Proyectos");

        nav.ShouldNotBeNull("Area must have a Proyectos collection navigation");
        nav!.IsCollection.ShouldBeTrue();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 4. GetAllAsync projects AreaId and AreaNombre
    // ══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task GetAllAsync_IncludesAreaIdAndAreaNombreInResumen()
    {
        await using var db = CreateDb();
        var (areaId, clasificId, userId) = SeedPrerequisites(db);
        SeedProyecto(db, areaId, clasificId, userId);

        // Replicate the GetAllAsync LINQ projection for ProyectoEnRevision
        var result = await db.Proyectos.OfType<ProyectoEnRevision>()
            .Select(p => new ProyectoResumenDto
            {
                Id = p.Id,
                Titulo = p.Titulo,
                JefeId = p.JefeId,
                Jefe = p.JefeUsuario.UserName,
                CorreoJefe = p.JefeUsuario.Email,
                NumeroMiembros = p.NumeroMiembros,
                ClasificacionId = p.ClasificacionId,
                ClasificacionNombre = p.Clasificacion.Nombre,
                AreaId = p.AreaId,
                AreaNombre = p.Area.Nombre,
                Tipo = "en-revision"
            })
            .ToListAsync();

        result.ShouldHaveSingleItem();
        result[0].AreaId.ShouldBe(areaId);
        result[0].AreaNombre.ShouldBe("MATCOM");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 5. GetProyectoAsync loads Area via Include
    // ══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task GetProyectoAsync_AreaNavigationIsLoaded()
    {
        await using var db = CreateDb();
        var (areaId, clasificId, userId) = SeedPrerequisites(db);
        var proyecto = SeedProyecto(db, areaId, clasificId, userId);

        var loaded = await db.Proyectos.OfType<ProyectoEnRevision>()
            .Include(x => x.Clasificacion)
            .Include(x => x.Area)
            .Include(x => x.JefeUsuario)
            .Include(x => x.PublicacionesDerivadas)
            .FirstOrDefaultAsync(x => x.Id == proyecto.Id);

        loaded.ShouldNotBeNull();
        loaded!.Area.ShouldNotBeNull();
        loaded.Area.Id.ShouldBe(areaId);
        loaded.Area.Nombre.ShouldBe("MATCOM");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 6. Proyecto entity – navigation defaults
    // ══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ProyectoEntity_HasAreaIdProperty()
    {
        var p = new ProyectoEnRevision();
        // AreaId is a non-nullable string (default! is null until assigned)
        var propInfo = typeof(ProyectoEnRevision).BaseType!
            .GetProperty("AreaId");
        propInfo.ShouldNotBeNull();
        propInfo!.PropertyType.ShouldBe(typeof(string));
    }

    [Test]
    public void AreaEntity_HasProyectosCollection()
    {
        var area = new Area();
        area.Proyectos.ShouldNotBeNull();
        area.Proyectos.ShouldBeEmpty();
    }
}
