using Dashboard_v2.Application.Proyectos;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Proyectos;

/// <summary>
/// Tests for the User ↔ Proyecto M:N participantes relationship introduced
/// when the old Area → Proyecto FK was replaced with a join table.
/// </summary>
public class ProyectoParticipantesTests
{
    private static ApplicationDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(opts);
    }

    private static (string clasificId, string userId) SeedPrerequisites(ApplicationDbContext db)
    {
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
        db.Clasificaciones.Add(clasif);
        db.Users.Add(user);
        db.SaveChanges();
        return (clasif.Id, user.Id);
    }

    private static User SeedUser(ApplicationDbContext db, string id, string nombre = "Participante") =>
        db.Users.Add(new User
        {
            Id = id,
            UserName = nombre,
            UserLastName1 = "Test",
            Email = $"{id}@uh.cu",
            PasswordHash = "hash",
            IsActive = true,
            BirthDate = DateTime.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        }).Entity;

    private static ProyectoEnRevision SeedProyecto(
        ApplicationDbContext db, string clasificId, string userId)
    {
        var p = new ProyectoEnRevision
        {
            Id = Guid.NewGuid().ToString(),
            Titulo = "Proyecto de prueba",
            JefeId = userId,
            NumeroMiembros = 3,
            ClasificacionId = clasificId,
            Tipo = "PE"
        };
        db.Proyectos.Add(p);
        db.SaveChanges();
        return p;
    }

    // ── 1. Entity defaults ─────────────────────────────────────────────────────

    [Test]
    public void ProyectoEntity_Participantes_IsEmptyByDefault()
    {
        var p = new ProyectoEnRevision();
        p.Participantes.ShouldNotBeNull();
        p.Participantes.ShouldBeEmpty();
    }

    [Test]
    public void UserEntity_ProyectosParticipante_IsEmptyByDefault()
    {
        var u = new User();
        u.ProyectosParticipante.ShouldNotBeNull();
        u.ProyectosParticipante.ShouldBeEmpty();
    }

    [Test]
    public void ProyectoEntity_DoesNotHaveAreaIdProperty()
    {
        var prop = typeof(Proyecto).GetProperty("AreaId");
        prop.ShouldBeNull("AreaId was removed from Proyecto when the Area→Proyecto FK was eliminated");
    }

    [Test]
    public void AreaEntity_DoesNotHaveProyectosNavigation()
    {
        var prop = typeof(Area).GetProperty("Proyectos");
        prop.ShouldBeNull("Area.Proyectos navigation was removed — area membership is now implicit");
    }

    // ── 2. EF model – join table ───────────────────────────────────────────────

    [Test]
    public void EfModel_ProyectoParticipantes_JoinTableExists()
    {
        using var db = CreateDb();
        var model = db.Model;

        var joinEntity = model.GetEntityTypes()
            .FirstOrDefault(t => t.GetTableName() == "ProyectoParticipantes");

        joinEntity.ShouldNotBeNull("ProyectoParticipantes join table must be registered in the EF model");
    }

    [Test]
    public void EfModel_ProyectoHasParticipantesNavigation()
    {
        using var db = CreateDb();
        var model = db.Model;

        var proyectoType = model.FindEntityType(typeof(Proyecto));
        proyectoType.ShouldNotBeNull();

        var nav = proyectoType!.GetSkipNavigations()
            .FirstOrDefault(n => n.Name == "Participantes");

        nav.ShouldNotBeNull("Proyecto must have a Participantes skip navigation");
        nav!.IsCollection.ShouldBeTrue();
    }

    [Test]
    public void EfModel_UserHasProyectosParticipanteNavigation()
    {
        using var db = CreateDb();
        var model = db.Model;

        var userType = model.FindEntityType(typeof(User));
        userType.ShouldNotBeNull();

        var nav = userType!.GetSkipNavigations()
            .FirstOrDefault(n => n.Name == "ProyectosParticipante");

        nav.ShouldNotBeNull("User must have a ProyectosParticipante skip navigation");
        nav!.IsCollection.ShouldBeTrue();
    }

    // ── 3. Persistence ────────────────────────────────────────────────────────

    [Test]
    public async Task Proyecto_AddParticipante_PersistedCorrectly()
    {
        await using var db = CreateDb();
        var (clasificId, jefeId) = SeedPrerequisites(db);
        var participante = SeedUser(db, "part-1");
        db.SaveChanges();

        var proyecto = SeedProyecto(db, clasificId, jefeId);

        proyecto.Participantes.Add(participante);
        await db.SaveChangesAsync();

        var loaded = await db.Proyectos.OfType<ProyectoEnRevision>()
            .Include(p => p.Participantes)
            .FirstOrDefaultAsync(p => p.Id == proyecto.Id);

        loaded.ShouldNotBeNull();
        loaded!.Participantes.ShouldHaveSingleItem();
        loaded.Participantes.First().Id.ShouldBe("part-1");
    }

    [Test]
    public async Task Proyecto_RemoveParticipante_PersistedCorrectly()
    {
        await using var db = CreateDb();
        var (clasificId, jefeId) = SeedPrerequisites(db);
        var part1 = SeedUser(db, "part-a");
        var part2 = SeedUser(db, "part-b");
        db.SaveChanges();

        var proyecto = SeedProyecto(db, clasificId, jefeId);
        proyecto.Participantes.Add(part1);
        proyecto.Participantes.Add(part2);
        await db.SaveChangesAsync();

        var loaded = await db.Proyectos.OfType<ProyectoEnRevision>()
            .Include(p => p.Participantes)
            .FirstOrDefaultAsync(p => p.Id == proyecto.Id);
        loaded!.Participantes.Remove(loaded.Participantes.First(u => u.Id == "part-a"));
        await db.SaveChangesAsync();

        var reloaded = await db.Proyectos.OfType<ProyectoEnRevision>()
            .Include(p => p.Participantes)
            .FirstOrDefaultAsync(p => p.Id == proyecto.Id);

        reloaded!.Participantes.ShouldHaveSingleItem();
        reloaded.Participantes.First().Id.ShouldBe("part-b");
    }

    [Test]
    public async Task User_ProyectosParticipante_ReflectsInverseNavigation()
    {
        await using var db = CreateDb();
        var (clasificId, jefeId) = SeedPrerequisites(db);
        var part = SeedUser(db, "part-inv");
        db.SaveChanges();

        var proyecto = SeedProyecto(db, clasificId, jefeId);
        proyecto.Participantes.Add(part);
        await db.SaveChangesAsync();

        var loadedUser = await db.Users
            .Include(u => u.ProyectosParticipante)
            .FirstOrDefaultAsync(u => u.Id == "part-inv");

        loadedUser.ShouldNotBeNull();
        loadedUser!.ProyectosParticipante.ShouldHaveSingleItem();
        loadedUser.ProyectosParticipante.First().Id.ShouldBe(proyecto.Id);
    }

    // ── 4. GetAllAsync projects Participantes ─────────────────────────────────

    [Test]
    public async Task GetAllAsync_ProjectionIncludesParticipantes()
    {
        await using var db = CreateDb();
        var (clasificId, userId) = SeedPrerequisites(db);
        var part = SeedUser(db, "part-all");
        db.SaveChanges();

        var proyecto = SeedProyecto(db, clasificId, userId);
        proyecto.Participantes.Add(part);
        await db.SaveChangesAsync();

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
                Participantes = p.Participantes.Select(u => new UserRefDto(
                    u.Id,
                    u.UserName + " " + u.UserLastName1,
                    u.Email)).ToList(),
                Tipo = "en-revision"
            })
            .ToListAsync();

        result.ShouldHaveSingleItem();
        result[0].Participantes.ShouldHaveSingleItem();
        result[0].Participantes[0].Id.ShouldBe("part-all");
    }

    // ── 5. GetProyectoAsync loads Participantes navigation ────────────────────

    [Test]
    public async Task GetProyectoAsync_ParticipantesNavigationIsLoaded()
    {
        await using var db = CreateDb();
        var (clasificId, userId) = SeedPrerequisites(db);
        var part = SeedUser(db, "part-load");
        db.SaveChanges();

        var proyecto = SeedProyecto(db, clasificId, userId);
        proyecto.Participantes.Add(part);
        await db.SaveChangesAsync();

        var loaded = await db.Proyectos.OfType<ProyectoEnRevision>()
            .Include(x => x.Clasificacion)
            .Include(x => x.Participantes)
            .Include(x => x.JefeUsuario)
            .Include(x => x.PublicacionesDerivadas)
            .FirstOrDefaultAsync(x => x.Id == proyecto.Id);

        loaded.ShouldNotBeNull();
        loaded!.Participantes.ShouldHaveSingleItem();
        loaded.Participantes.First().Id.ShouldBe("part-load");
    }
}
