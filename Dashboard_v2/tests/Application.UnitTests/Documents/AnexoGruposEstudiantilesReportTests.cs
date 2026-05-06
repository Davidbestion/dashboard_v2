using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Documents.Reports;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Documents;

/// <summary>
/// Tests unitarios para <see cref="AnexoGruposEstudiantilesReport"/>.
///
/// Verifican que solo se incluyen los grupos estudiantiles del área del usuario
/// solicitante y que los campos se proyectan correctamente.
/// </summary>
[TestFixture]
public class AnexoGruposEstudiantilesReportTests
{
    // ── helpers ───────────────────────────────────────────────────────────────

    private static ApplicationDbContext BuildDb(string dbName)
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(opts);
    }

    private static AnexoGruposEstudiantilesReport BuildReport(ApplicationDbContext db, string userId)
    {
        var currentUser = new Mock<IUser>();
        currentUser.Setup(u => u.Id).Returns(userId);
        return new AnexoGruposEstudiantilesReport(db, currentUser.Object);
    }

    // ── filtrado por área ─────────────────────────────────────────────────────

    /// <summary>
    /// Solo los grupos cuyo AreaId coincide con el área del usuario deben aparecer.
    /// Los grupos de otras áreas deben excluirse.
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_ReturnsOnlyGroupsFromUserArea()
    {
        await using var db = BuildDb($"{nameof(AnexoGruposEstudiantilesReportTests)}.{nameof(GatherVariablesAsync_ReturnsOnlyGroupsFromUserArea)}");

        var areaA = new Area { Id = "areaA", Nombre = "Área A" };
        var areaB = new Area { Id = "areaB", Nombre = "Área B" };
        var user  = new User { Id = "u1", UserName = "u1", UserLastName1 = "U", Email = "u1@test.cu", AreaId = "areaA" };

        db.Areas.AddRange(areaA, areaB);
        db.Users.Add(user);
        db.GruposEstudiantiles.AddRange(
            new GrupoEstudiantil { Id = "ge1", Nombre = "Grupo Estud A", AreaId = "areaA" },
            new GrupoEstudiantil { Id = "ge2", Nombre = "Grupo Estud B", AreaId = "areaB" }
        );
        await db.SaveChangesAsync();

        var report    = BuildReport(db, user.Id);
        var variables = await report.GatherVariablesAsync(null, CancellationToken.None);

        var rows = (List<AnexoGruposEstudiantilesRowDto>)variables["GruposEstudiantiles"];
        rows.Count.ShouldBe(1);
        rows[0].Nombre.ShouldBe("Grupo Estud A");
    }

    /// <summary>
    /// Cuando el usuario no tiene área asignada, el reporte debe devolver lista vacía.
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_ReturnsEmptyWhenUserHasNoArea()
    {
        await using var db = BuildDb($"{nameof(AnexoGruposEstudiantilesReportTests)}.{nameof(GatherVariablesAsync_ReturnsEmptyWhenUserHasNoArea)}");

        var user  = new User { Id = "u1", UserName = "u1", UserLastName1 = "U", Email = "u1@test.cu", AreaId = null };
        db.Users.Add(user);
        db.GruposEstudiantiles.Add(new GrupoEstudiantil { Id = "ge1", Nombre = "Grupo X", AreaId = "areaX" });
        await db.SaveChangesAsync();

        var report    = BuildReport(db, user.Id);
        var variables = await report.GatherVariablesAsync(null, CancellationToken.None);

        var rows = (List<AnexoGruposEstudiantilesRowDto>)variables["GruposEstudiantiles"];
        rows.ShouldBeEmpty();
    }

    /// <summary>
    /// Los grupos deben aparecer ordenados alfabéticamente por nombre.
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_GroupsAreOrderedByName()
    {
        await using var db = BuildDb($"{nameof(AnexoGruposEstudiantilesReportTests)}.{nameof(GatherVariablesAsync_GroupsAreOrderedByName)}");

        var area = new Area { Id = "area1", Nombre = "Área 1" };
        var user = new User { Id = "u1", UserName = "u1", UserLastName1 = "U", Email = "u1@test.cu", AreaId = "area1" };

        db.Areas.Add(area);
        db.Users.Add(user);
        db.GruposEstudiantiles.AddRange(
            new GrupoEstudiantil { Id = "ge3", Nombre = "Zeta",  AreaId = "area1" },
            new GrupoEstudiantil { Id = "ge1", Nombre = "Alpha", AreaId = "area1" },
            new GrupoEstudiantil { Id = "ge2", Nombre = "Bravo", AreaId = "area1" }
        );
        await db.SaveChangesAsync();

        var report    = BuildReport(db, user.Id);
        var variables = await report.GatherVariablesAsync(null, CancellationToken.None);

        var rows = (List<AnexoGruposEstudiantilesRowDto>)variables["GruposEstudiantiles"];
        rows.Select(r => r.Nombre).ShouldBe(["Alpha", "Bravo", "Zeta"]);
    }

    /// <summary>
    /// El campo AreaTematica debe coincidir con el nombre del área del grupo.
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_AreaTematica_MatchesGroupArea()
    {
        await using var db = BuildDb($"{nameof(AnexoGruposEstudiantilesReportTests)}.{nameof(GatherVariablesAsync_AreaTematica_MatchesGroupArea)}");

        var area = new Area { Id = "area1", Nombre = "Ciencias de la Computación" };
        var user = new User { Id = "u1", UserName = "u1", UserLastName1 = "U", Email = "u1@test.cu", AreaId = "area1" };

        db.Areas.Add(area);
        db.Users.Add(user);
        db.GruposEstudiantiles.Add(new GrupoEstudiantil { Id = "ge1", Nombre = "Grupo IA", AreaId = "area1" });
        await db.SaveChangesAsync();

        var report    = BuildReport(db, user.Id);
        var variables = await report.GatherVariablesAsync(null, CancellationToken.None);

        var rows = (List<AnexoGruposEstudiantilesRowDto>)variables["GruposEstudiantiles"];
        rows[0].AreaTematica.ShouldBe("Ciencias de la Computación");
    }

    /// <summary>
    /// Las líneas de investigación deben aparecer concatenadas y ordenadas en el campo
    /// LineasDeInvestigacion.
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_LineasDeInvestigacion_AreConcatenatedAndOrdered()
    {
        await using var db = BuildDb($"{nameof(AnexoGruposEstudiantilesReportTests)}.{nameof(GatherVariablesAsync_LineasDeInvestigacion_AreConcatenatedAndOrdered)}");

        var area = new Area { Id = "area1", Nombre = "Área 1" };
        var user = new User { Id = "u1", UserName = "u1", UserLastName1 = "U", Email = "u1@test.cu", AreaId = "area1" };
        db.Areas.Add(area);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var linea1 = new LineaDeInvestigacion { Id = "l1", Nombre = "Redes" };
        var linea2 = new LineaDeInvestigacion { Id = "l2", Nombre = "IA" };
        db.LineasDeInvestigacion.AddRange(linea1, linea2);
        await db.SaveChangesAsync();

        var grupo = new GrupoEstudiantil { Id = "ge1", Nombre = "Grupo Test", AreaId = "area1" };
        grupo.LineasDeInvestigacion.Add(linea1);
        grupo.LineasDeInvestigacion.Add(linea2);
        db.GruposEstudiantiles.Add(grupo);
        await db.SaveChangesAsync();

        var report    = BuildReport(db, user.Id);
        var variables = await report.GatherVariablesAsync(null, CancellationToken.None);

        var rows = (List<AnexoGruposEstudiantilesRowDto>)variables["GruposEstudiantiles"];
        // Ordenadas: IA antes que Redes
        rows[0].LineasDeInvestigacion.ShouldBe("IA, Redes");
    }
}
