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
/// Tests unitarios para <see cref="AnexoGruposReport"/>.
///
/// Verifican que solo se incluyen los grupos de investigación del área del usuario
/// solicitante y que los conteos de categorías se calculan correctamente.
/// </summary>
[TestFixture]
public class AnexoGruposReportTests
{
    // ── helpers ───────────────────────────────────────────────────────────────

    private static ApplicationDbContext BuildDb(string dbName)
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(opts);
    }

    private static AnexoGruposReport BuildReport(ApplicationDbContext db, string userId)
    {
        var currentUser = new Mock<IUser>();
        currentUser.Setup(u => u.Id).Returns(userId);
        return new AnexoGruposReport(db, currentUser.Object);
    }

    // ── filtrado por área ─────────────────────────────────────────────────────

    /// <summary>
    /// Solo los grupos cuyo AreaId coincide con el área del usuario deben aparecer
    /// en el reporte. Los grupos de otras áreas deben excluirse.
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_ReturnsOnlyGroupsFromUserArea()
    {
        await using var db = BuildDb($"{nameof(AnexoGruposReportTests)}.{nameof(GatherVariablesAsync_ReturnsOnlyGroupsFromUserArea)}");

        var areaA = new Area { Id = "areaA", Nombre = "Área A" };
        var areaB = new Area { Id = "areaB", Nombre = "Área B" };
        var user  = new User { Id = "u1", UserName = "u1", UserLastName1 = "U", Email = "u1@test.cu", AreaId = "areaA" };

        var grupoA = new GrupoDeInvestigacion { Id = "g1", Nombre = "Grupo A", AreaId = "areaA" };
        var grupoB = new GrupoDeInvestigacion { Id = "g2", Nombre = "Grupo B", AreaId = "areaB" };

        db.Areas.AddRange(areaA, areaB);
        db.Users.Add(user);
        db.GruposDeInvestigacion.AddRange(grupoA, grupoB);
        await db.SaveChangesAsync();

        var report    = BuildReport(db, user.Id);
        var variables = await report.GatherVariablesAsync(null, CancellationToken.None);

        var rows = (List<AnexoGruposRowDto>)variables["Grupos"];
        rows.Count.ShouldBe(1);
        rows[0].Nombre.ShouldBe("Grupo A");
    }

    /// <summary>
    /// Cuando el usuario no tiene área asignada (AreaId == null) el reporte
    /// debe devolver una lista vacía.
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_ReturnsEmptyWhenUserHasNoArea()
    {
        await using var db = BuildDb($"{nameof(AnexoGruposReportTests)}.{nameof(GatherVariablesAsync_ReturnsEmptyWhenUserHasNoArea)}");

        var user  = new User { Id = "u1", UserName = "u1", UserLastName1 = "U", Email = "u1@test.cu", AreaId = null };
        var grupo = new GrupoDeInvestigacion { Id = "g1", Nombre = "Grupo X", AreaId = "areaX" };

        db.Users.Add(user);
        db.GruposDeInvestigacion.Add(grupo);
        await db.SaveChangesAsync();

        var report    = BuildReport(db, user.Id);
        var variables = await report.GatherVariablesAsync(null, CancellationToken.None);

        var rows = (List<AnexoGruposRowDto>)variables["Grupos"];
        rows.ShouldBeEmpty();
    }

    /// <summary>
    /// Los grupos deben aparecer ordenados alfabéticamente por nombre.
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_GroupsAreOrderedByName()
    {
        await using var db = BuildDb($"{nameof(AnexoGruposReportTests)}.{nameof(GatherVariablesAsync_GroupsAreOrderedByName)}");

        var area = new Area { Id = "area1", Nombre = "Área 1" };
        var user = new User { Id = "u1", UserName = "u1", UserLastName1 = "U", Email = "u1@test.cu", AreaId = "area1" };

        db.Areas.Add(area);
        db.Users.Add(user);
        db.GruposDeInvestigacion.AddRange(
            new GrupoDeInvestigacion { Id = "g3", Nombre = "Zeta",  AreaId = "area1" },
            new GrupoDeInvestigacion { Id = "g1", Nombre = "Alpha", AreaId = "area1" },
            new GrupoDeInvestigacion { Id = "g2", Nombre = "Bravo", AreaId = "area1" }
        );
        await db.SaveChangesAsync();

        var report    = BuildReport(db, user.Id);
        var variables = await report.GatherVariablesAsync(null, CancellationToken.None);

        var rows = (List<AnexoGruposRowDto>)variables["Grupos"];
        rows.Select(r => r.Nombre).ShouldBe(["Alpha", "Bravo", "Zeta"]);
    }

    /// <summary>
    /// Verifica que el conteo de integrantes por categoría científica y docente
    /// se calcula correctamente para un grupo con miembros conocidos.
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_MemberCategoryCounts_AreCorrect()
    {
        await using var db = BuildDb($"{nameof(AnexoGruposReportTests)}.{nameof(GatherVariablesAsync_MemberCategoryCounts_AreCorrect)}");

        var area  = new Area { Id = "area1", Nombre = "Área 1" };
        var owner = new User { Id = "owner", UserName = "owner", UserLastName1 = "O", Email = "owner@test.cu", AreaId = "area1" };

        var doctor    = new User { Id = "u2", UserName = "u2", UserLastName1 = "A", Email = "u2@test.cu", ScientificCategory = ScientificCategory.Doctor,     TeachingCategory = TeachingCategory.Titular   };
        var master     = new User { Id = "u3", UserName = "u3", UserLastName1 = "B", Email = "u3@test.cu", ScientificCategory = ScientificCategory.Master,     TeachingCategory = TeachingCategory.Auxiliar  };
        var licenciado = new User { Id = "u4", UserName = "u4", UserLastName1 = "C", Email = "u4@test.cu", ScientificCategory = ScientificCategory.Licenciado, TeachingCategory = TeachingCategory.Asistente };

        db.Areas.Add(area);
        db.Users.AddRange(owner, doctor, master, licenciado);
        await db.SaveChangesAsync();

        var grupo = new GrupoDeInvestigacion { Id = "g1", Nombre = "Grupo 1", AreaId = "area1" };
        grupo.Usuarios.Add(doctor);
        grupo.Usuarios.Add(master);
        grupo.Usuarios.Add(licenciado);
        db.GruposDeInvestigacion.Add(grupo);
        await db.SaveChangesAsync();

        var report    = BuildReport(db, owner.Id);
        var variables = await report.GatherVariablesAsync(null, CancellationToken.None);

        var rows = (List<AnexoGruposRowDto>)variables["Grupos"];
        rows.Count.ShouldBe(1);
        var row = rows[0];
        row.TotalIntegrantes.ShouldBe(3);
        row.CantDoctores.ShouldBe(1);
        row.CantMasters.ShouldBe(1);
        row.CantLicenciados.ShouldBe(1);
        row.CantPT.ShouldBe(1);
        row.CantPAUX.ShouldBe(1);
        row.CantPASIST.ShouldBe(1);
    }

    /// <summary>
    /// Las líneas de investigación del grupo deben aparecer en el campo
    /// LineasDeInvestigacion como una cadena separada por comas.
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_LineasDeInvestigacion_AreConcatenated()
    {
        await using var db = BuildDb($"{nameof(AnexoGruposReportTests)}.{nameof(GatherVariablesAsync_LineasDeInvestigacion_AreConcatenated)}");

        var area = new Area { Id = "area1", Nombre = "Área 1" };
        var user = new User { Id = "u1", UserName = "u1", UserLastName1 = "U", Email = "u1@test.cu", AreaId = "area1" };
        db.Areas.Add(area);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var linea1 = new LineaDeInvestigacion { Id = "l1", Nombre = "IA" };
        var linea2 = new LineaDeInvestigacion { Id = "l2", Nombre = "Redes" };
        db.LineasDeInvestigacion.AddRange(linea1, linea2);
        await db.SaveChangesAsync();

        var grupo = new GrupoDeInvestigacion { Id = "g1", Nombre = "Grupo 1", AreaId = "area1" };
        grupo.LineasDeInvestigacion.Add(linea1);
        grupo.LineasDeInvestigacion.Add(linea2);
        db.GruposDeInvestigacion.Add(grupo);
        await db.SaveChangesAsync();

        var report    = BuildReport(db, user.Id);
        var variables = await report.GatherVariablesAsync(null, CancellationToken.None);

        var rows = (List<AnexoGruposRowDto>)variables["Grupos"];
        rows[0].LineasDeInvestigacion.ShouldContain("IA");
        rows[0].LineasDeInvestigacion.ShouldContain("Redes");
    }
}
