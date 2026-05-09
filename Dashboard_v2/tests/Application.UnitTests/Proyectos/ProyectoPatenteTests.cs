using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Proyectos;

/// <summary>
/// Tests para la relación N:M entre <see cref="Proyecto"/> y <see cref="Patente"/>
/// introducida en AddProyectoPatenteMN.
///
/// Cubre:
///   - Inserción y lectura de la tabla de unión.
///   - Navegación bidireccional Proyecto→Patente y Patente→Proyecto.
///   - Un proyecto puede generar varias patentes.
///   - Una patente puede pertenecer a varios proyectos.
///   - Clave compuesta previene entradas duplicadas.
///   - Cascade delete al eliminar el Proyecto limpia la join table.
///   - Cascade delete al eliminar la Patente limpia la join table.
/// </summary>
[TestFixture]
public class ProyectoPatenteTests
{
    // ── helpers ───────────────────────────────────────────────────────────────

    private static ApplicationDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(opts);
    }

    private static (string areaId, string clasificId, string userId) SeedPrerequisites(ApplicationDbContext db)
    {
        db.Areas.Add(new Area
        {
            Id = "area-1",
            Nombre = "MATCOM",
            Descripcion = "Área de MATCOM",
            UniversidadId = "uh"
        });
        db.Clasificaciones.Add(new Clasificacion { Id = "clasif-1", Nombre = "Básica" });
        db.Users.Add(new User
        {
            Id = "jefe-1",
            UserName = "jefe",
            UserLastName1 = "Jefe",
            Email = "jefe@uh.cu",
            BirthDate = DateTime.UtcNow,
            IsActive = true
        });
        db.SaveChanges();
        return ("area-1", "clasif-1", "jefe-1");
    }

    private static ProyectoEnRevision MakeProyecto(string id, string areaId, string clasificId, string jefeId) =>
        new()
        {
            Id = id,
            Titulo = $"Proyecto {id}",
            JefeId = jefeId,
            AreaId = areaId,
            ClasificacionId = clasificId,
            NumeroMiembros = 2,
            CantidadMiembrosUH = 1,
            CantidadEstudiantes = 0,
            CantidadEstudiantesContratados = 0,
            TributaFormacionDoctoral = false,
            Situacion = "Pendiente",
            Tipo = "en-revision"
        };

    private static Patente MakePatente(string id) => new()
    {
        Id = id,
        Titulo = $"Patente {id}",
        NumeroSolicitudConcesion = $"SOL-{id}",
        EsNacional = true
    };

    // ── tests ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task ProyectoPatente_CanInsertAndRead_JoinRow()
    {
        await using var db = CreateDb();
        var (areaId, clasificId, jefeId) = SeedPrerequisites(db);
        db.Proyectos.Add(MakeProyecto("proy-1", areaId, clasificId, jefeId));
        db.Patentes.Add(MakePatente("pat-1"));
        await db.SaveChangesAsync();

        db.ProyectoPatentes.Add(new ProyectoPatente { ProyectoId = "proy-1", PatenteId = "pat-1" });
        await db.SaveChangesAsync();

        var row = await db.ProyectoPatentes
            .SingleAsync(pp => pp.ProyectoId == "proy-1" && pp.PatenteId == "pat-1");
        row.ProyectoId.ShouldBe("proy-1");
        row.PatenteId.ShouldBe("pat-1");
    }

    [Test]
    public async Task ProyectoPatente_NavigationFromProyecto_ContainsPatente()
    {
        await using var db = CreateDb();
        var (areaId, clasificId, jefeId) = SeedPrerequisites(db);
        db.Proyectos.Add(MakeProyecto("proy-1", areaId, clasificId, jefeId));
        db.Patentes.Add(MakePatente("pat-1"));
        await db.SaveChangesAsync();

        db.ProyectoPatentes.Add(new ProyectoPatente { ProyectoId = "proy-1", PatenteId = "pat-1" });
        await db.SaveChangesAsync();

        var proyecto = await db.Proyectos
            .Include(p => p.PatentesDerivadas)
            .ThenInclude(pp => pp.Patente)
            .SingleAsync(p => p.Id == "proy-1");

        proyecto.PatentesDerivadas.ShouldHaveSingleItem();
        proyecto.PatentesDerivadas.First().Patente.Titulo.ShouldBe("Patente pat-1");
    }

    [Test]
    public async Task ProyectoPatente_NavigationFromPatente_ContainsProyecto()
    {
        await using var db = CreateDb();
        var (areaId, clasificId, jefeId) = SeedPrerequisites(db);
        db.Proyectos.Add(MakeProyecto("proy-1", areaId, clasificId, jefeId));
        db.Patentes.Add(MakePatente("pat-1"));
        await db.SaveChangesAsync();

        db.ProyectoPatentes.Add(new ProyectoPatente { ProyectoId = "proy-1", PatenteId = "pat-1" });
        await db.SaveChangesAsync();

        var patente = await db.Patentes
            .Include(p => p.ProyectosDerivados)
            .ThenInclude(pp => pp.Proyecto)
            .SingleAsync(p => p.Id == "pat-1");

        patente.ProyectosDerivados.ShouldHaveSingleItem();
        patente.ProyectosDerivados.First().Proyecto.Titulo.ShouldBe("Proyecto proy-1");
    }

    [Test]
    public async Task ProyectoPatente_OneProyectoCanHaveMultiplePatentes()
    {
        await using var db = CreateDb();
        var (areaId, clasificId, jefeId) = SeedPrerequisites(db);
        db.Proyectos.Add(MakeProyecto("proy-1", areaId, clasificId, jefeId));
        db.Patentes.Add(MakePatente("pat-1"));
        db.Patentes.Add(MakePatente("pat-2"));
        await db.SaveChangesAsync();

        db.ProyectoPatentes.AddRange(
            new ProyectoPatente { ProyectoId = "proy-1", PatenteId = "pat-1" },
            new ProyectoPatente { ProyectoId = "proy-1", PatenteId = "pat-2" });
        await db.SaveChangesAsync();

        var patentes = await db.ProyectoPatentes
            .Where(pp => pp.ProyectoId == "proy-1").ToListAsync();
        patentes.Count.ShouldBe(2);
    }

    [Test]
    public async Task ProyectoPatente_OnePatenteCanBelongToMultipleProyectos()
    {
        await using var db = CreateDb();
        var (areaId, clasificId, jefeId) = SeedPrerequisites(db);
        db.Proyectos.Add(MakeProyecto("proy-1", areaId, clasificId, jefeId));
        db.Proyectos.Add(MakeProyecto("proy-2", areaId, clasificId, jefeId));
        db.Patentes.Add(MakePatente("pat-1"));
        await db.SaveChangesAsync();

        db.ProyectoPatentes.AddRange(
            new ProyectoPatente { ProyectoId = "proy-1", PatenteId = "pat-1" },
            new ProyectoPatente { ProyectoId = "proy-2", PatenteId = "pat-1" });
        await db.SaveChangesAsync();

        var proyectos = await db.ProyectoPatentes
            .Where(pp => pp.PatenteId == "pat-1").ToListAsync();
        proyectos.Count.ShouldBe(2);
    }

    [Test]
    public async Task ProyectoPatente_DuplicateKey_ThrowsOnAdd()
    {
        await using var db = CreateDb();
        var (areaId, clasificId, jefeId) = SeedPrerequisites(db);
        db.Proyectos.Add(MakeProyecto("proy-1", areaId, clasificId, jefeId));
        db.Patentes.Add(MakePatente("pat-1"));
        await db.SaveChangesAsync();

        db.ProyectoPatentes.Add(new ProyectoPatente { ProyectoId = "proy-1", PatenteId = "pat-1" });
        Should.Throw<InvalidOperationException>(() =>
            db.ProyectoPatentes.Add(new ProyectoPatente { ProyectoId = "proy-1", PatenteId = "pat-1" }));
    }

    [Test]
    public async Task ProyectoPatente_DeleteProyecto_CascadesJoinRow()
    {
        await using var db = CreateDb();
        var (areaId, clasificId, jefeId) = SeedPrerequisites(db);
        db.Proyectos.Add(MakeProyecto("proy-1", areaId, clasificId, jefeId));
        db.Patentes.Add(MakePatente("pat-1"));
        await db.SaveChangesAsync();

        db.ProyectoPatentes.Add(new ProyectoPatente { ProyectoId = "proy-1", PatenteId = "pat-1" });
        await db.SaveChangesAsync();

        db.Proyectos.Remove((await db.Proyectos.FindAsync("proy-1"))!);
        await db.SaveChangesAsync();

        (await db.ProyectoPatentes.ToListAsync()).ShouldBeEmpty();
    }

    [Test]
    public async Task ProyectoPatente_DeletePatente_CascadesJoinRow()
    {
        await using var db = CreateDb();
        var (areaId, clasificId, jefeId) = SeedPrerequisites(db);
        db.Proyectos.Add(MakeProyecto("proy-1", areaId, clasificId, jefeId));
        db.Patentes.Add(MakePatente("pat-1"));
        await db.SaveChangesAsync();

        db.ProyectoPatentes.Add(new ProyectoPatente { ProyectoId = "proy-1", PatenteId = "pat-1" });
        await db.SaveChangesAsync();

        db.Patentes.Remove((await db.Patentes.FindAsync("pat-1"))!);
        await db.SaveChangesAsync();

        (await db.ProyectoPatentes.ToListAsync()).ShouldBeEmpty();
    }

    [Test]
    public async Task ProyectoPatente_DeleteProyecto_DoesNotDeletePatente()
    {
        await using var db = CreateDb();
        var (areaId, clasificId, jefeId) = SeedPrerequisites(db);
        db.Proyectos.Add(MakeProyecto("proy-1", areaId, clasificId, jefeId));
        db.Patentes.Add(MakePatente("pat-1"));
        await db.SaveChangesAsync();

        db.ProyectoPatentes.Add(new ProyectoPatente { ProyectoId = "proy-1", PatenteId = "pat-1" });
        await db.SaveChangesAsync();

        db.Proyectos.Remove((await db.Proyectos.FindAsync("proy-1"))!);
        await db.SaveChangesAsync();

        // La patente sigue existiendo aunque el proyecto fue eliminado
        var patente = await db.Patentes.FindAsync("pat-1");
        patente.ShouldNotBeNull();
    }
}
