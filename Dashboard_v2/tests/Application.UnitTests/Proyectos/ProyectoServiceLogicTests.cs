using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Proyectos;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Application.UnitTests.Proyectos;

/// <summary>
/// Tests for <see cref="ProyectoService"/> business logic:
///   - ResolveAreaIdAsync: Jefe uses own area, Superuser uses request area, Jefe without area fails.
///   - Owner-filter enforcement on Delete and Update.
///   - LinkPublicacionAsync / UnlinkPublicacionAsync edge-cases.
/// </summary>
public class ProyectoServiceLogicTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ApplicationDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(opts);
    }

    /// <summary>
    /// Creates a ProyectoService with the supplied IUser mock and a no-op validation service.
    /// </summary>
    private static ProyectoService BuildService(ApplicationDbContext db, IUser user)
    {
        var validationMock = new Mock<IRequestValidationService>();
        validationMock
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new ProyectoService(db, user, validationMock.Object);
    }

    private static IUser MakeJefe(string id, string? areaId = null)
    {
        var mock = new Mock<IUser>();
        mock.Setup(u => u.Id).Returns(id);
        mock.Setup(u => u.Roles).Returns(new List<string> { nameof(RolesEnum.Jefe_de_Proyecto) });
        return mock.Object;
    }

    private static IUser MakeSuperuser(string id = "super-1")
    {
        var mock = new Mock<IUser>();
        mock.Setup(u => u.Id).Returns(id);
        mock.Setup(u => u.Roles).Returns(new List<string> { nameof(RolesEnum.Superuser) });
        return mock.Object;
    }

    /// <summary>Seeds Area, Clasificacion, and a Jefe user. Returns their IDs.</summary>
    private static (string areaId, string area2Id, string clasificId, string jefeId) SeedBase(
        ApplicationDbContext db)
    {
        var area1 = new Area { Id = "area-1", Nombre = "MATCOM", Descripcion = "d", UniversidadId = "uh" };
        var area2 = new Area { Id = "area-2", Nombre = "FIS", Descripcion = "d", UniversidadId = "uh" };
        var clasif = new Clasificacion { Id = "clasif-1", Nombre = "Básica" };
        var jefe = new User
        {
            Id = "jefe-1",
            UserName = "jefe",
            UserLastName1 = "Apellido",
            Email = "jefe@uh.cu",
            PasswordHash = "hash",
            IsActive = true,
            BirthDate = DateTime.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            AreaId = area1.Id,
            UserRoles = new List<UserRole>
            {
                new() { UserId = "jefe-1", Role = RolesEnum.Jefe_de_Proyecto }
            }
        };

        db.Areas.AddRange(area1, area2);
        db.Clasificaciones.Add(clasif);
        db.Users.Add(jefe);
        db.SaveChanges();

        return (area1.Id, area2.Id, clasif.Id, jefe.Id);
    }

    private static ProyectoEnRevisionUpsertRequest BuildRevisionRequest(
        string clasificId, string areaId, string jefeId = "jefe-1") => new()
    {
        Titulo = "Proyecto Test",
        JefeId = jefeId,
        ClasificacionId = clasificId,
        AreaId = areaId,
        NumeroMiembros = 3,
        CantidadMiembrosUH = 2,
        Situacion = "Pendiente",
        Tipo = "PE"
    };

    // ══════════════════════════════════════════════════════════════════════════
    // ResolveAreaIdAsync – Superuser uses the request AreaId
    // ══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Create_Superuser_UsesRequestAreaId()
    {
        await using var db = CreateDb();
        var (area1Id, area2Id, clasificId, jefeId) = SeedBase(db);

        var superuser = MakeSuperuser();
        var service = BuildService(db, superuser);

        // Superuser explicitly selects area2
        var request = BuildRevisionRequest(clasificId, areaId: area2Id, jefeId: jefeId);
        var (result, id) = await service.CreateEnRevisionAsync(request);

        result.Succeeded.ShouldBeTrue();
        var created = await db.Proyectos.OfType<ProyectoEnRevision>()
            .FirstOrDefaultAsync(p => p.Id == id);
        created.ShouldNotBeNull();
        created!.AreaId.ShouldBe(area2Id);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ResolveAreaIdAsync – Jefe always gets their own area (ignoring request)
    // ══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Create_JefeDeProyecto_IgnoresRequestAreaId_UsesOwnArea()
    {
        await using var db = CreateDb();
        var (area1Id, area2Id, clasificId, jefeId) = SeedBase(db);

        var jefe = MakeJefe(jefeId);   // jefe-1 has AreaId = area1Id
        var service = BuildService(db, jefe);

        // Jefe tries to set area2 in the request — should be silently overridden
        var request = BuildRevisionRequest(clasificId, areaId: area2Id, jefeId: jefeId);
        var (result, id) = await service.CreateEnRevisionAsync(request);

        result.Succeeded.ShouldBeTrue();
        var created = await db.Proyectos.OfType<ProyectoEnRevision>()
            .FirstOrDefaultAsync(p => p.Id == id);
        created.ShouldNotBeNull();
        created!.AreaId.ShouldBe(area1Id,
            "Jefe's project must always belong to the jefe's own area, not the requested one");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ResolveAreaIdAsync – Jefe without assigned area gets a failure
    // ══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Create_JefeWithoutArea_ReturnsFailure()
    {
        await using var db = CreateDb();
        var (_, _, clasificId, _) = SeedBase(db);

        // Seed a separate jefe user with NO area
        var jefeNoArea = new User
        {
            Id = "jefe-sin-area",
            UserName = "jefe2",
            UserLastName1 = "B",
            Email = "jefe2@uh.cu",
            PasswordHash = "x",
            IsActive = true,
            BirthDate = DateTime.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            AreaId = null,   // <-- no area
            UserRoles = new List<UserRole>
            {
                new() { UserId = "jefe-sin-area", Role = RolesEnum.Jefe_de_Proyecto }
            }
        };
        db.Users.Add(jefeNoArea);
        await db.SaveChangesAsync();

        var jefe = MakeJefe("jefe-sin-area");
        var service = BuildService(db, jefe);

        var request = BuildRevisionRequest(clasificId, areaId: "area-1", jefeId: "jefe-sin-area");
        var (result, id) = await service.CreateEnRevisionAsync(request);

        result.Succeeded.ShouldBeFalse();
        id.ShouldBeNull();
        result.Errors.ShouldContain(e => e.Contains("no tiene un área asignada"));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Owner filter – Jefe cannot delete another jefe's project
    // ══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Delete_JefeTriesToDeleteOtherJefesProject_ReturnsForbidden()
    {
        await using var db = CreateDb();
        var (area1Id, _, clasificId, jefeId) = SeedBase(db);

        // Seed a second jefe
        var jefe2 = new User
        {
            Id = "jefe-2",
            UserName = "jefe2",
            UserLastName1 = "B",
            Email = "jefe2@uh.cu",
            PasswordHash = "x",
            IsActive = true,
            BirthDate = DateTime.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            AreaId = area1Id,
            UserRoles = new List<UserRole>
            {
                new() { UserId = "jefe-2", Role = RolesEnum.Jefe_de_Proyecto }
            }
        };
        db.Users.Add(jefe2);

        // Create a project belonging to jefe-1
        var proyecto = new ProyectoEnRevision
        {
            Id = "proj-a",
            Titulo = "Proyecto A",
            JefeId = jefeId,          // owned by jefe-1
            AreaId = area1Id,
            ClasificacionId = clasificId,
            NumeroMiembros = 1,
            Situacion = "Pendiente",
            Tipo = "PE"
        };
        db.Proyectos.Add(proyecto);
        await db.SaveChangesAsync();

        // jefe-2 tries to delete jefe-1's project
        var service = BuildService(db, MakeJefe("jefe-2"));
        var result = await service.DeleteAsync("proj-a");

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("permiso para eliminar"));
    }

    [Test]
    public async Task Delete_JefeDeletesOwnProject_Succeeds()
    {
        await using var db = CreateDb();
        var (area1Id, _, clasificId, jefeId) = SeedBase(db);

        var proyecto = new ProyectoEnRevision
        {
            Id = "proj-b",
            Titulo = "Proyecto B",
            JefeId = jefeId,
            AreaId = area1Id,
            ClasificacionId = clasificId,
            NumeroMiembros = 1,
            Situacion = "Pendiente",
            Tipo = "PE"
        };
        db.Proyectos.Add(proyecto);
        await db.SaveChangesAsync();

        var service = BuildService(db, MakeJefe(jefeId));
        var result = await service.DeleteAsync("proj-b");

        result.Succeeded.ShouldBeTrue();
        db.Proyectos.Find("proj-b").ShouldBeNull();
    }

    [Test]
    public async Task Delete_Superuser_CanDeleteAnyProject()
    {
        await using var db = CreateDb();
        var (area1Id, _, clasificId, jefeId) = SeedBase(db);

        var proyecto = new ProyectoEnRevision
        {
            Id = "proj-c",
            Titulo = "Proyecto C",
            JefeId = jefeId,
            AreaId = area1Id,
            ClasificacionId = clasificId,
            NumeroMiembros = 1,
            Situacion = "Pendiente",
            Tipo = "PE"
        };
        db.Proyectos.Add(proyecto);
        await db.SaveChangesAsync();

        var service = BuildService(db, MakeSuperuser("super-1"));
        var result = await service.DeleteAsync("proj-c");

        result.Succeeded.ShouldBeTrue();
    }

    [Test]
    public async Task Delete_NonExistentProject_ReturnsFailure()
    {
        await using var db = CreateDb();
        var service = BuildService(db, MakeSuperuser());

        var result = await service.DeleteAsync("no-existe");

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("no encontrado"));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // LinkPublicacionAsync / UnlinkPublicacionAsync
    // ══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task LinkPublicacion_PublicationNotFound_ReturnsFailure()
    {
        await using var db = CreateDb();
        var service = BuildService(db, MakeSuperuser());

        var result = await service.LinkPublicacionAsync("proj-1", "pub-no-existe");

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("no encontrada"));
    }

    [Test]
    public async Task LinkPublicacion_AlreadyLinkedToDifferentProject_ReturnsFailure()
    {
        await using var db = CreateDb();
        var pub = new Publication { Id = "pub-1", Title = "Pub", PublicationData = "data", PublishedDate = "2024", ProyectoId = "otro-proyecto" };
        db.Publications.Add(pub);
        await db.SaveChangesAsync();

        var service = BuildService(db, MakeSuperuser());
        var result = await service.LinkPublicacionAsync("mi-proyecto", "pub-1");

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("ya está vinculada a otro proyecto"));
    }

    [Test]
    public async Task LinkPublicacion_AvailablePublication_Succeeds()
    {
        await using var db = CreateDb();
        var pub = new Publication { Id = "pub-2", Title = "Pub disponible", PublicationData = "data", PublishedDate = "2024", ProyectoId = null };
        db.Publications.Add(pub);
        await db.SaveChangesAsync();

        var service = BuildService(db, MakeSuperuser());
        var result = await service.LinkPublicacionAsync("proj-99", "pub-2");

        result.Succeeded.ShouldBeTrue();
        var updated = await db.Publications.FindAsync("pub-2");
        updated!.ProyectoId.ShouldBe("proj-99");
    }

    [Test]
    public async Task LinkPublicacion_AlreadyLinkedToSameProject_Succeeds()
    {
        // Re-linking to the same project is idempotent (no error)
        await using var db = CreateDb();
        var pub = new Publication { Id = "pub-3", Title = "Pub", PublicationData = "data", PublishedDate = "2024", ProyectoId = "proj-99" };
        db.Publications.Add(pub);
        await db.SaveChangesAsync();

        var service = BuildService(db, MakeSuperuser());
        var result = await service.LinkPublicacionAsync("proj-99", "pub-3");

        result.Succeeded.ShouldBeTrue();
    }

    [Test]
    public async Task UnlinkPublicacion_NotFoundForProject_ReturnsFailure()
    {
        await using var db = CreateDb();
        // pub exists but belongs to a different project
        var pub = new Publication { Id = "pub-4", Title = "Pub", PublicationData = "data", PublishedDate = "2024", ProyectoId = "otro" };
        db.Publications.Add(pub);
        await db.SaveChangesAsync();

        var service = BuildService(db, MakeSuperuser());
        var result = await service.UnlinkPublicacionAsync("mi-proyecto", "pub-4");

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("no encontrada"));
    }

    [Test]
    public async Task UnlinkPublicacion_Succeeds()
    {
        await using var db = CreateDb();
        var pub = new Publication { Id = "pub-5", Title = "Pub", PublicationData = "data", PublishedDate = "2024", ProyectoId = "proj-99" };
        db.Publications.Add(pub);
        await db.SaveChangesAsync();

        var service = BuildService(db, MakeSuperuser());
        var result = await service.UnlinkPublicacionAsync("proj-99", "pub-5");

        result.Succeeded.ShouldBeTrue();
        var updated = await db.Publications.FindAsync("pub-5");
        updated!.ProyectoId.ShouldBeNull();
    }
}
