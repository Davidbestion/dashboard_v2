using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.GruposEstudiantiles;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.GruposEstudiantiles;

[TestFixture]
public class GrupoEstudiantilServiceTests
{
    private ApplicationDbContext _db = null!;
    private Mock<IUser> _userMock = null!;
    private GrupoEstudiantilService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(opts);
        _userMock = new Mock<IUser>();
        _sut = new GrupoEstudiantilService(_db, _userMock.Object);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private async Task AddAreaAsync(string id = "area-1")
    {
        _db.Areas.Add(new Area { Id = id, Nombre = "Ciencias" });
        await _db.SaveChangesAsync();
    }

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllAsync_EmptyDb_ReturnsEmptyList()
    {
        var result = await _sut.GetAllAsync();
        result.ShouldBeEmpty();
    }

    [Test]
    public async Task GetAllAsync_WithData_ReturnsGrupoEstudiantilDtos()
    {
        await AddAreaAsync();
        _db.GruposEstudiantiles.Add(new GrupoEstudiantil { Id = "g-1", Nombre = "Grupo Alpha", AreaId = "area-1" });
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync();

        result.Count.ShouldBe(1);
        result[0].Nombre.ShouldBe("Grupo Alpha");
        result[0].AreaId.ShouldBe("area-1");
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsSuccessAndId()
    {
        await AddAreaAsync();

        var (result, id) = await _sut.CreateAsync(
            new CreateGrupoEstudiantilRequest { Nombre = "Grupo B", AreaId = "area-1" });

        result.Succeeded.ShouldBeTrue();
        id.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public async Task CreateAsync_EmptyNombre_ReturnsFailure()
    {
        await AddAreaAsync();
        var (result, _) = await _sut.CreateAsync(
            new CreateGrupoEstudiantilRequest { Nombre = "  ", AreaId = "area-1" });
        result.Succeeded.ShouldBeFalse();
    }

    [Test]
    public async Task CreateAsync_NonExistentArea_ReturnsFailure()
    {
        var (result, _) = await _sut.CreateAsync(
            new CreateGrupoEstudiantilRequest { Nombre = "Grupo B", AreaId = "no-existe" });
        result.Succeeded.ShouldBeFalse();
    }

    [Test]
    public async Task CreateAsync_WithLineasDeInvestigacionIds_AssociatesLineas()
    {
        await AddAreaAsync();
        var linea = new Domain.Entities.LineaDeInvestigacion
        {
            Id = Guid.NewGuid().ToString(),
            Nombre = "Inteligencia Artificial"
        };
        _db.LineasDeInvestigacion.Add(linea);
        await _db.SaveChangesAsync();

        var (result, id) = await _sut.CreateAsync(new CreateGrupoEstudiantilRequest
        {
            Nombre = "Grupo IA",
            AreaId = "area-1",
            LineasDeInvestigacionIds = new List<string> { linea.Id }
        });

        result.Succeeded.ShouldBeTrue();
        var grupo = await _db.GruposEstudiantiles
            .Include(g => g.LineasDeInvestigacion)
            .FirstAsync(g => g.Id == id);
        grupo.LineasDeInvestigacion.Count.ShouldBe(1);
        grupo.LineasDeInvestigacion.First().Id.ShouldBe(linea.Id);
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task UpdateAsync_EmptyNombre_ReturnsFailure()
    {
        var result = await _sut.UpdateAsync("g1",
            new UpdateGrupoEstudiantilRequest { Nombre = "", AreaId = "area-1" });
        result.Succeeded.ShouldBeFalse();
    }

    [Test]
    public async Task UpdateAsync_NonExistentGrupo_ReturnsFailure()
    {
        _userMock.Setup(u => u.Roles).Returns(["Superuser"]);
        var result = await _sut.UpdateAsync("no-existe",
            new UpdateGrupoEstudiantilRequest { Nombre = "Test", AreaId = "area-1" });
        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("no encontrado"));
    }

    [Test]
    public async Task UpdateAsync_WithoutSuperuserRole_ReturnsFailure()
    {
        await AddAreaAsync();
        var (_, id) = await _sut.CreateAsync(
            new CreateGrupoEstudiantilRequest { Nombre = "Grupo B", AreaId = "area-1" });

        _userMock.Setup(u => u.Roles).Returns(["Estudiante"]);

        var result = await _sut.UpdateAsync(id!,
            new UpdateGrupoEstudiantilRequest { Nombre = "Modified", AreaId = "area-1" });

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("permisos"));
    }

    [Test]
    public async Task UpdateAsync_Superuser_UpdatesGrupo()
    {
        await AddAreaAsync();
        var (_, id) = await _sut.CreateAsync(
            new CreateGrupoEstudiantilRequest { Nombre = "Grupo B", AreaId = "area-1" });

        _userMock.Setup(u => u.Roles).Returns(["Superuser"]);

        var result = await _sut.UpdateAsync(id!,
            new UpdateGrupoEstudiantilRequest { Nombre = "Modified", AreaId = "area-1" });

        result.Succeeded.ShouldBeTrue();
        var grupo = await _db.GruposEstudiantiles.FindAsync(id);
        grupo!.Nombre.ShouldBe("Modified");
    }

    [Test]
    public async Task UpdateAsync_NonExistentAreaAfterPermissionCheck_ReturnsFailure()
    {
        await AddAreaAsync();
        var (_, id) = await _sut.CreateAsync(
            new CreateGrupoEstudiantilRequest { Nombre = "Grupo B", AreaId = "area-1" });

        _userMock.Setup(u => u.Roles).Returns(["Superuser"]);

        var result = await _sut.UpdateAsync(id!,
            new UpdateGrupoEstudiantilRequest { Nombre = "Modified", AreaId = "area-inexistente" });

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("área"));
    }

    [Test]
    public async Task UpdateAsync_WithLineasDeInvestigacion_AssociatesLineas()
    {
        await AddAreaAsync();
        var linea = new Domain.Entities.LineaDeInvestigacion
        {
            Id = Guid.NewGuid().ToString(),
            Nombre = "Redes Neuronales"
        };
        _db.LineasDeInvestigacion.Add(linea);
        await _db.SaveChangesAsync();

        var (_, id) = await _sut.CreateAsync(
            new CreateGrupoEstudiantilRequest { Nombre = "Grupo B", AreaId = "area-1" });

        _userMock.Setup(u => u.Roles).Returns(["Superuser"]);

        var result = await _sut.UpdateAsync(id!, new UpdateGrupoEstudiantilRequest
        {
            Nombre = "Grupo B",
            AreaId = "area-1",
            LineasDeInvestigacionIds = new List<string> { linea.Id }
        });

        result.Succeeded.ShouldBeTrue();
        var grupo = await _db.GruposEstudiantiles
            .Include(g => g.LineasDeInvestigacion)
            .FirstAsync(g => g.Id == id);
        grupo.LineasDeInvestigacion.Count.ShouldBe(1);
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteAsync_NonExistentGrupo_ReturnsFailure()
    {
        _userMock.Setup(u => u.Roles).Returns(["Superuser"]);
        var result = await _sut.DeleteAsync("no-existe");
        result.Succeeded.ShouldBeFalse();
    }

    [Test]
    public async Task DeleteAsync_WithoutPermissions_ReturnsFailure()
    {
        await AddAreaAsync();
        var (_, id) = await _sut.CreateAsync(
            new CreateGrupoEstudiantilRequest { Nombre = "Grupo B", AreaId = "area-1" });

        _userMock.Setup(u => u.Roles).Returns(["Estudiante"]);

        var result = await _sut.DeleteAsync(id!);
        result.Succeeded.ShouldBeFalse();
    }

    [Test]
    public async Task DeleteAsync_Superuser_Removes()
    {
        await AddAreaAsync();
        var (_, id) = await _sut.CreateAsync(
            new CreateGrupoEstudiantilRequest { Nombre = "Grupo B", AreaId = "area-1" });

        _userMock.Setup(u => u.Roles).Returns(["Superuser"]);

        var result = await _sut.DeleteAsync(id!);

        result.Succeeded.ShouldBeTrue();
        (await _db.GruposEstudiantiles.CountAsync()).ShouldBe(0);
    }

    // ── GetAreaAsync ─────────────────────────────────────────────────────────

    [Test]
    public async Task GetAreaAsync_EmptyDb_ReturnsEmpty()
    {
        _db.Areas.Add(new Area { Id = "area-1", Nombre = "Ciencias" });
        _db.Users.Add(new User { Id = "vd-1", UserName = "vd", Email = "vd@t.com", UserLastName1 = "VD", AreaId = "area-1" });
        await _db.SaveChangesAsync();
        _userMock.Setup(u => u.Id).Returns("vd-1");

        var result = await _sut.GetAreaAsync();
        result.ShouldBeEmpty();
    }

    [Test]
    public async Task GetAreaAsync_ReturnsOnlyGruposEstudiantilesForVicedecanoArea()
    {
        _db.Areas.AddRange(
            new Area { Id = "area-1", Nombre = "Ciencias" },
            new Area { Id = "area-2", Nombre = "Letras" });
        _db.Users.Add(new User { Id = "vd-1", UserName = "vd", Email = "vd@t.com", UserLastName1 = "VD", AreaId = "area-1" });
        _db.GruposEstudiantiles.AddRange(
            new GrupoEstudiantil { Id = "ge-1", Nombre = "GCE Alpha", AreaId = "area-1" },
            new GrupoEstudiantil { Id = "ge-2", Nombre = "GCE Beta",  AreaId = "area-2" });
        await _db.SaveChangesAsync();
        _userMock.Setup(u => u.Id).Returns("vd-1");

        var result = await _sut.GetAreaAsync();

        result.ShouldHaveSingleItem();
        result[0].Nombre.ShouldBe("GCE Alpha");
    }

    [Test]
    public async Task GetAreaAsync_ExcludesGruposFromOtherAreas()
    {
        _db.Areas.AddRange(
            new Area { Id = "area-1", Nombre = "Ciencias" },
            new Area { Id = "area-2", Nombre = "Letras" });
        _db.Users.Add(new User { Id = "vd-1", UserName = "vd", Email = "vd@t.com", UserLastName1 = "VD", AreaId = "area-1" });
        _db.GruposEstudiantiles.Add(new GrupoEstudiantil { Id = "ge-3", Nombre = "GCE Gamma", AreaId = "area-2" });
        await _db.SaveChangesAsync();
        _userMock.Setup(u => u.Id).Returns("vd-1");

        var result = await _sut.GetAreaAsync();
        result.ShouldBeEmpty();
    }
}
