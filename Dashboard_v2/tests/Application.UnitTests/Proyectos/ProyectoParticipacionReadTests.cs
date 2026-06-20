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
/// Tests for <see cref="ProyectoService.GetMisProyectosParticipacionAsync"/>.
/// Verifies that only projects where the current user is a Participante are returned.
/// </summary>
[TestFixture]
public class ProyectoParticipacionReadTests
{
    private static ApplicationDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(opts);
    }

    private static ProyectoService BuildService(ApplicationDbContext db, string currentUserId)
    {
        var userMock = new Mock<IUser>();
        userMock.Setup(u => u.Id).Returns(currentUserId);
        userMock.Setup(u => u.Roles).Returns(new List<string> { nameof(RolesEnum.Profesor) });

        var validationMock = new Mock<IRequestValidationService>();
        validationMock
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new ProyectoService(db, userMock.Object, validationMock.Object);
    }

    private static (string clasificId, string jefeId) SeedBase(ApplicationDbContext db)
    {
        db.Clasificaciones.Add(new Clasificacion { Id = "clasif-1", Nombre = "Básica" });
        db.Users.Add(new User
        {
            Id = "jefe-1",
            UserName = "jefe",
            UserLastName1 = "Perez",
            Email = "jefe@uh.cu",
            PasswordHash = "x",
            IsActive = true,
            BirthDate = DateTime.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UserRoles = new List<UserRole> { new() { UserId = "jefe-1", Role = RolesEnum.Jefe_de_Proyecto } }
        });
        db.SaveChanges();
        return ("clasif-1", "jefe-1");
    }

    private static User SeedParticipante(ApplicationDbContext db, string id)
    {
        var u = new User
        {
            Id = id,
            UserName = $"user{id}",
            UserLastName1 = "Apellido",
            Email = $"{id}@uh.cu",
            PasswordHash = "x",
            IsActive = true,
            BirthDate = DateTime.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.Users.Add(u);
        db.SaveChanges();
        return u;
    }

    // ── GetMisProyectosParticipacionAsync ─────────────────────────────────────

    [Test]
    public async Task GetMisProyectosParticipacionAsync_NoProjects_ReturnsEmpty()
    {
        await using var db = CreateDb();
        var sut = BuildService(db, "user-1");

        var result = await sut.GetMisProyectosParticipacionAsync();

        result.ShouldBeEmpty();
    }

    [Test]
    public async Task GetMisProyectosParticipacionAsync_UserIsParticipant_ReturnsProject()
    {
        await using var db = CreateDb();
        var (clasificId, jefeId) = SeedBase(db);
        var participante = SeedParticipante(db, "prof-1");

        var proyecto = new ProyectoEnRevision
        {
            Id = "proy-1",
            Titulo = "Proyecto Test",
            JefeId = jefeId,
            ClasificacionId = clasificId,
            NumeroMiembros = 2,
            CantidadMiembrosUH = 1,
            Tipo = "PE",
            Participantes = new List<User> { participante },
        };
        db.Proyectos.Add(proyecto);
        await db.SaveChangesAsync();

        var sut = BuildService(db, "prof-1");
        var result = await sut.GetMisProyectosParticipacionAsync();

        result.ShouldHaveSingleItem();
        result[0].Id.ShouldBe("proy-1");
        result[0].Titulo.ShouldBe("Proyecto Test");
    }

    [Test]
    public async Task GetMisProyectosParticipacionAsync_UserIsNotParticipant_ReturnsEmpty()
    {
        await using var db = CreateDb();
        var (clasificId, jefeId) = SeedBase(db);
        var otroUsuario = SeedParticipante(db, "otro-1");

        var proyecto = new ProyectoEnRevision
        {
            Id = "proy-1",
            Titulo = "Proyecto Ajeno",
            JefeId = jefeId,
            ClasificacionId = clasificId,
            NumeroMiembros = 2,
            CantidadMiembrosUH = 1,
            Tipo = "PE",
            Participantes = new List<User> { otroUsuario },
        };
        db.Proyectos.Add(proyecto);
        await db.SaveChangesAsync();

        var sut = BuildService(db, "prof-sin-proyecto");
        var result = await sut.GetMisProyectosParticipacionAsync();

        result.ShouldBeEmpty();
    }

    [Test]
    public async Task GetMisProyectosParticipacionAsync_MultipleProjects_ReturnsOnlyOwn()
    {
        await using var db = CreateDb();
        var (clasificId, jefeId) = SeedBase(db);
        var prof = SeedParticipante(db, "prof-1");
        var otro = SeedParticipante(db, "otro-1");

        db.Proyectos.Add(new ProyectoEnRevision
        {
            Id = "proy-1", Titulo = "Mío", JefeId = jefeId, ClasificacionId = clasificId,
            NumeroMiembros = 1, Tipo = "PE",
            Participantes = new List<User> { prof },
        });
        db.Proyectos.Add(new ProyectoEnRevision
        {
            Id = "proy-2", Titulo = "Ajeno", JefeId = jefeId, ClasificacionId = clasificId,
            NumeroMiembros = 1, Tipo = "PE",
            Participantes = new List<User> { otro },
        });
        await db.SaveChangesAsync();

        var sut = BuildService(db, "prof-1");
        var result = await sut.GetMisProyectosParticipacionAsync();

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe("proy-1");
    }

    [Test]
    public async Task GetMisProyectosParticipacionAsync_ProjectWithNoParticipants_ReturnsEmpty()
    {
        await using var db = CreateDb();
        var (clasificId, jefeId) = SeedBase(db);

        db.Proyectos.Add(new ProyectoEnRevision
        {
            Id = "proy-1", Titulo = "Sin participantes", JefeId = jefeId,
            ClasificacionId = clasificId, NumeroMiembros = 1, Tipo = "PE",
        });
        await db.SaveChangesAsync();

        var sut = BuildService(db, "prof-1");
        var result = await sut.GetMisProyectosParticipacionAsync();

        result.ShouldBeEmpty();
    }

    [Test]
    public async Task GetMisProyectosParticipacionAsync_ParticipantesFieldPopulated()
    {
        await using var db = CreateDb();
        var (clasificId, jefeId) = SeedBase(db);
        var prof = SeedParticipante(db, "prof-1");
        var colega = SeedParticipante(db, "prof-2");

        db.Proyectos.Add(new ProyectoEnRevision
        {
            Id = "proy-1", Titulo = "Proyecto Compartido", JefeId = jefeId,
            ClasificacionId = clasificId, NumeroMiembros = 2, Tipo = "PE",
            Participantes = new List<User> { prof, colega },
        });
        await db.SaveChangesAsync();

        var sut = BuildService(db, "prof-1");
        var result = await sut.GetMisProyectosParticipacionAsync();

        result.ShouldHaveSingleItem();
        result[0].Participantes.Count.ShouldBe(2);
        result[0].Participantes.ShouldContain(p => p.Id == "prof-1");
        result[0].Participantes.ShouldContain(p => p.Id == "prof-2");
    }

    // ── SetParticipantesAsync ─────────────────────────────────────────────────

    private static ProyectoService BuildSuperuserService(ApplicationDbContext db)
    {
        var userMock = new Mock<IUser>();
        userMock.Setup(u => u.Id).Returns("superuser-1");
        userMock.Setup(u => u.Roles).Returns(new List<string> { nameof(RolesEnum.Superuser) });

        var validationMock = new Mock<IRequestValidationService>();
        validationMock
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new ProyectoService(db, userMock.Object, validationMock.Object);
    }

    private static ProyectoService BuildJefeService(ApplicationDbContext db, string jefeId)
    {
        var userMock = new Mock<IUser>();
        userMock.Setup(u => u.Id).Returns(jefeId);
        userMock.Setup(u => u.Roles).Returns(new List<string> { nameof(RolesEnum.Jefe_de_Proyecto) });

        var validationMock = new Mock<IRequestValidationService>();
        validationMock
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new ProyectoService(db, userMock.Object, validationMock.Object);
    }

    [Test]
    public async Task SetParticipantesAsync_ProjectNotFound_ReturnsFailure()
    {
        await using var db = CreateDb();
        var sut = BuildService(db, "jefe-1");

        var result = await sut.SetParticipantesAsync("no-existe", [], default);

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("no encontrado"));
    }

    [Test]
    public async Task SetParticipantesAsync_JefeOwnsProject_AssignsParticipantes()
    {
        await using var db = CreateDb();
        var (clasificId, jefeId) = SeedBase(db);
        var prof = SeedParticipante(db, "prof-1");

        db.Proyectos.Add(new ProyectoEnRevision
        {
            Id = "proy-1", Titulo = "Test", JefeId = jefeId,
            ClasificacionId = clasificId, NumeroMiembros = 1, Tipo = "PE",
        });
        await db.SaveChangesAsync();

        var sut = BuildJefeService(db, jefeId);
        var result = await sut.SetParticipantesAsync("proy-1", ["prof-1"], default);

        result.Succeeded.ShouldBeTrue();
        var proyecto = await db.Proyectos.Include(p => p.Participantes).FirstAsync(p => p.Id == "proy-1");
        proyecto.Participantes.ShouldContain(u => u.Id == "prof-1");
    }

    [Test]
    public async Task SetParticipantesAsync_JefeDoesNotOwnProject_ReturnsFailure()
    {
        await using var db = CreateDb();
        var (clasificId, jefeId) = SeedBase(db);
        db.Proyectos.Add(new ProyectoEnRevision
        {
            Id = "proy-1", Titulo = "Test", JefeId = jefeId,
            ClasificacionId = clasificId, NumeroMiembros = 1, Tipo = "PE",
        });
        await db.SaveChangesAsync();

        var sut = BuildJefeService(db, "otro-jefe");
        var result = await sut.SetParticipantesAsync("proy-1", [], default);

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("permiso"));
    }

    [Test]
    public async Task SetParticipantesAsync_ClearsExistingParticipantes_WhenListIsEmpty()
    {
        await using var db = CreateDb();
        var (clasificId, jefeId) = SeedBase(db);
        var prof = SeedParticipante(db, "prof-1");

        db.Proyectos.Add(new ProyectoEnRevision
        {
            Id = "proy-1", Titulo = "Test", JefeId = jefeId,
            ClasificacionId = clasificId, NumeroMiembros = 1, Tipo = "PE",
            Participantes = new List<User> { prof },
        });
        await db.SaveChangesAsync();

        var sut = BuildJefeService(db, jefeId);
        var result = await sut.SetParticipantesAsync("proy-1", [], default);

        result.Succeeded.ShouldBeTrue();
        var proyecto = await db.Proyectos.Include(p => p.Participantes).FirstAsync(p => p.Id == "proy-1");
        // jefe is always kept even when the list is empty
        proyecto.Participantes.Count.ShouldBe(1);
        proyecto.Participantes.ShouldContain(u => u.Id == jefeId);
    }

    [Test]
    public async Task SetParticipantesAsync_Superuser_CanModifyAnyProject()
    {
        await using var db = CreateDb();
        var (clasificId, jefeId) = SeedBase(db);
        var prof = SeedParticipante(db, "prof-1");

        db.Proyectos.Add(new ProyectoEnRevision
        {
            Id = "proy-1", Titulo = "Test", JefeId = jefeId,
            ClasificacionId = clasificId, NumeroMiembros = 1, Tipo = "PE",
        });
        await db.SaveChangesAsync();

        var sut = BuildSuperuserService(db);
        var result = await sut.SetParticipantesAsync("proy-1", ["prof-1"], default);

        result.Succeeded.ShouldBeTrue();
    }
}
