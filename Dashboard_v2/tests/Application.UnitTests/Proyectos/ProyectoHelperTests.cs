using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
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
/// Tests for <see cref="ProyectoHelper"/> static methods:
/// SetBase, SetEjecucion, GetOwnerFilter, ResolveJefeId, ValidateJefeAsync.
/// </summary>
public class ProyectoHelperTests
{
    private static ApplicationDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(opts);
    }

    private static IUser MockJefe(string id = "jefe-id") =>
        Mock.Of<IUser>(u =>
            u.Id == id &&
            u.Roles == new List<string> { nameof(RolesEnum.Jefe_de_Proyecto) });

    private static IUser MockSuperuser(string id = "superuser-id") =>
        Mock.Of<IUser>(u =>
            u.Id == id &&
            u.Roles == new List<string> { nameof(RolesEnum.Superuser) });

    private static User SeedJefeUser(ApplicationDbContext db, string id = "jefe-user-1")
    {
        var user = new User
        {
            Id = id,
            UserName = "jefe",
            UserLastName1 = "Apellido",
            Email = $"{id}@uh.cu",
            PasswordHash = "hash",
            IsActive = true,
            BirthDate = DateTime.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UserRoles = new List<UserRole>
            {
                new() { UserId = id, Role = RolesEnum.Jefe_de_Proyecto }
            }
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    private static User SeedProfesorUser(ApplicationDbContext db, string id = "profesor-1")
    {
        var user = new User
        {
            Id = id,
            UserName = "profesor",
            UserLastName1 = "Apellido",
            Email = $"{id}@uh.cu",
            PasswordHash = "hash",
            IsActive = true,
            BirthDate = DateTime.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UserRoles = new List<UserRole>
            {
                new() { UserId = id, Role = RolesEnum.Profesor }
            }
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    // ── SetBase ───────────────────────────────────────────────────────────────

    [Test]
    public void SetBase_AssignsAllFields()
    {
        var p = new ProyectoEnRevision();

        ProyectoHelper.SetBase(p,
            titulo: "  Mi Proyecto  ",
            jefeId: "jefe-1",
            numMiembros: 5,
            cantUH: 3,
            cantEst: 2,
            cantEstCont: 1,
            tributaFormacion: true,
            clasificId: "clasif-1");

        p.Titulo.ShouldBe("Mi Proyecto");
        p.JefeId.ShouldBe("jefe-1");
        p.NumeroMiembros.ShouldBe(5);
        p.CantidadMiembrosUH.ShouldBe(3);
        p.CantidadEstudiantes.ShouldBe(2);
        p.CantidadEstudiantesContratados.ShouldBe(1);
        p.TributaFormacionDoctoral.ShouldBeTrue();
        p.ClasificacionId.ShouldBe("clasif-1");
    }

    [Test]
    public void SetBase_TrimsTitleWhitespace()
    {
        var p = new ProyectoEnRevision();
        ProyectoHelper.SetBase(p, "   Espacios   ", "jefe", 1, 1, 0, 0, false, "c1");
        p.Titulo.ShouldBe("Espacios");
    }

    // ── SetEjecucion ──────────────────────────────────────────────────────────

    [Test]
    public void SetEjecucion_AssignsScalarFields()
    {
        var pe = new ProyectoEmpresarial();
        var fecha = new DateOnly(2025, 1, 15);
        var fechaCierre = new DateOnly(2026, 12, 31);

        ProyectoHelper.SetEjecucion(pe,
            fechaInicio: fecha,
            fechaCierre: fechaCierre,
            codigoProyecto: " P-001 ",
            tributaDesarrolloLocal: true);

        pe.FechaInicio.ShouldBe(fecha);
        pe.FechaCierre.ShouldBe(fechaCierre);
        pe.CodigoProyecto.ShouldBe("P-001");
        pe.TributaDesarrolloLocal.ShouldBeTrue();
    }

    [Test]
    public void SetEjecucion_NullFechaCierre_IsPreserved()
    {
        var pe = new ProyectoEmpresarial();
        ProyectoHelper.SetEjecucion(pe, new DateOnly(2025, 1, 1), null, "P-002", false);
        pe.FechaCierre.ShouldBeNull();
    }

    [Test]
    public void SetEjecucion_TrimsCodigoProyecto()
    {
        var pe = new ProyectoEmpresarial();
        ProyectoHelper.SetEjecucion(pe, new DateOnly(2025, 1, 1), null, "  CODE  ", false);
        pe.CodigoProyecto.ShouldBe("CODE");
    }

    // ── GetOwnerFilter ────────────────────────────────────────────────────────

    [Test]
    public void GetOwnerFilter_JefeDeProyecto_ReturnsUserId()
    {
        var filter = ProyectoHelper.GetOwnerFilter(MockJefe("my-id"));
        filter.ShouldBe("my-id");
    }

    [Test]
    public void GetOwnerFilter_Superuser_ReturnsNull()
    {
        var filter = ProyectoHelper.GetOwnerFilter(MockSuperuser());
        filter.ShouldBeNull();
    }

    [Test]
    public void GetOwnerFilter_NullRoles_ReturnsNull()
    {
        var user = Mock.Of<IUser>(u => u.Id == "x" && u.Roles == null);
        ProyectoHelper.GetOwnerFilter(user).ShouldBeNull();
    }

    [Test]
    public void GetOwnerFilter_OtherRole_ReturnsNull()
    {
        var user = Mock.Of<IUser>(u =>
            u.Id == "p1" &&
            u.Roles == new List<string> { nameof(RolesEnum.Profesor) });

        ProyectoHelper.GetOwnerFilter(user).ShouldBeNull();
    }

    // ── ResolveJefeId ─────────────────────────────────────────────────────────

    [Test]
    public void ResolveJefeId_JefeDeProyecto_ReturnsCurrentUserId()
    {
        var jefe = MockJefe("jefe-real");
        var resolved = ProyectoHelper.ResolveJefeId("otro-id", jefe);
        resolved.ShouldBe("jefe-real");
    }

    [Test]
    public void ResolveJefeId_Superuser_ReturnsRequestedJefeId()
    {
        var superuser = MockSuperuser();
        var resolved = ProyectoHelper.ResolveJefeId("jefe-seleccionado", superuser);
        resolved.ShouldBe("jefe-seleccionado");
    }

    [Test]
    public void ResolveJefeId_JefeWithNullId_FallsBackToRequestedId()
    {
        var user = Mock.Of<IUser>(u =>
            u.Id == (string?)null &&
            u.Roles == new List<string> { nameof(RolesEnum.Jefe_de_Proyecto) });

        var resolved = ProyectoHelper.ResolveJefeId("fallback-id", user);
        resolved.ShouldBe("fallback-id");
    }

    // ── ValidateJefeAsync ─────────────────────────────────────────────────────

    [Test]
    public async Task ValidateJefeAsync_EmptyJefeId_ReturnsFailure()
    {
        await using var db = CreateDb();

        var result = await ProyectoHelper.ValidateJefeAsync(db, "", CancellationToken.None);

        result.ShouldNotBeNull();
        result!.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("jefe del proyecto es obligatorio"));
    }

    [Test]
    public async Task ValidateJefeAsync_WhitespaceJefeId_ReturnsFailure()
    {
        await using var db = CreateDb();

        var result = await ProyectoHelper.ValidateJefeAsync(db, "   ", CancellationToken.None);

        result.ShouldNotBeNull();
        result!.Succeeded.ShouldBeFalse();
    }

    [Test]
    public async Task ValidateJefeAsync_NonExistentUser_ReturnsFailure()
    {
        await using var db = CreateDb();

        var result = await ProyectoHelper.ValidateJefeAsync(db, "no-existe", CancellationToken.None);

        result.ShouldNotBeNull();
        result!.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("usuario indicado como jefe no existe"));
    }

    [Test]
    public async Task ValidateJefeAsync_UserWithoutJefeRole_ReturnsFailure()
    {
        await using var db = CreateDb();
        var profesor = SeedProfesorUser(db, "profesor-1");

        var result = await ProyectoHelper.ValidateJefeAsync(db, profesor.Id, CancellationToken.None);

        result.ShouldNotBeNull();
        result!.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("no tiene el rol de Jefe de Proyecto"));
    }

    [Test]
    public async Task ValidateJefeAsync_UserWithJefeRole_ReturnsNull()
    {
        await using var db = CreateDb();
        var jefe = SeedJefeUser(db, "jefe-1");

        var result = await ProyectoHelper.ValidateJefeAsync(db, jefe.Id, CancellationToken.None);

        result.ShouldBeNull("A valid jefe should return null (no validation error)");
    }
}
