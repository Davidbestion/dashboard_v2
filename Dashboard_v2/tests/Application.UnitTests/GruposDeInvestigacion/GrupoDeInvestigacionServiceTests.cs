using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.GruposDeInvestigacion;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.GruposDeInvestigacion;

[TestFixture]
public class GrupoDeInvestigacionServiceTests
{
    private ApplicationDbContext _db = null!;
    private Mock<IUser> _userMock = null!;
    private GrupoDeInvestigacionService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(opts);
        _userMock = new Mock<IUser>();
        _sut = new GrupoDeInvestigacionService(_db, _userMock.Object);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private async Task<Area> AddAreaAsync(string id = "area-1", string nombre = "Ciencias")
    {
        var area = new Area { Id = id, Nombre = nombre };
        _db.Areas.Add(area);
        await _db.SaveChangesAsync();
        return area;
    }

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllAsync_EmptyDb_ReturnsEmptyList()
    {
        var result = await _sut.GetAllAsync();
        result.ShouldBeEmpty();
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsSuccessAndId()
    {
        await AddAreaAsync();
        _userMock.Setup(u => u.Id).Returns("creator-1");

        var (result, id) = await _sut.CreateAsync(
            new CreateGrupoDeInvestigacionRequest { Nombre = "Grupo A", AreaId = "area-1" });

        result.Succeeded.ShouldBeTrue();
        id.ShouldNotBeNullOrEmpty();
        var grupo = await _db.GruposDeInvestigacion.FindAsync(id);
        grupo!.CreadorId.ShouldBe("creator-1");
    }

    [Test]
    public async Task CreateAsync_EmptyNombre_ReturnsFailure()
    {
        await AddAreaAsync();
        var (result, id) = await _sut.CreateAsync(
            new CreateGrupoDeInvestigacionRequest { Nombre = " ", AreaId = "area-1" });

        result.Succeeded.ShouldBeFalse();
        id.ShouldBeNull();
    }

    [Test]
    public async Task CreateAsync_NonExistentArea_ReturnsFailure()
    {
        var (result, id) = await _sut.CreateAsync(
            new CreateGrupoDeInvestigacionRequest { Nombre = "Grupo A", AreaId = "no-existe" });

        result.Succeeded.ShouldBeFalse();
    }

    [Test]
    public async Task CreateAsync_WithLineasDeInvestigacionIds_AssociatesLineas()
    {
        await AddAreaAsync();
        _userMock.Setup(u => u.Id).Returns("creator-1");

        var linea = new Domain.Entities.LineaDeInvestigacion
        {
            Id = Guid.NewGuid().ToString(),
            Nombre = "Sistemas Distribuidos"
        };
        _db.LineasDeInvestigacion.Add(linea);
        await _db.SaveChangesAsync();

        var (result, id) = await _sut.CreateAsync(new CreateGrupoDeInvestigacionRequest
        {
            Nombre = "Grupo Distribuidos",
            AreaId = "area-1",
            LineasDeInvestigacionIds = new List<string> { linea.Id }
        });

        result.Succeeded.ShouldBeTrue();
        var grupo = await _db.GruposDeInvestigacion
            .Include(g => g.LineasDeInvestigacion)
            .FirstAsync(g => g.Id == id);
        grupo.LineasDeInvestigacion.Count.ShouldBe(1);
        grupo.LineasDeInvestigacion.First().Id.ShouldBe(linea.Id);
    }

    [Test]
    public async Task CreateAsync_WithMultipleLineas_AssociatesAll()
    {
        await AddAreaAsync();
        _userMock.Setup(u => u.Id).Returns("creator-1");

        var linea1 = new Domain.Entities.LineaDeInvestigacion { Id = Guid.NewGuid().ToString(), Nombre = "L1" };
        var linea2 = new Domain.Entities.LineaDeInvestigacion { Id = Guid.NewGuid().ToString(), Nombre = "L2" };
        _db.LineasDeInvestigacion.AddRange(linea1, linea2);
        await _db.SaveChangesAsync();

        var (result, id) = await _sut.CreateAsync(new CreateGrupoDeInvestigacionRequest
        {
            Nombre = "Grupo Multi",
            AreaId = "area-1",
            LineasDeInvestigacionIds = new List<string> { linea1.Id, linea2.Id }
        });

        result.Succeeded.ShouldBeTrue();
        var grupo = await _db.GruposDeInvestigacion
            .Include(g => g.LineasDeInvestigacion)
            .FirstAsync(g => g.Id == id);
        grupo.LineasDeInvestigacion.Count.ShouldBe(2);
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task UpdateAsync_EmptyNombre_ReturnsFailure()
    {
        var result = await _sut.UpdateAsync("g1",
            new UpdateGrupoDeInvestigacionRequest { Nombre = "", AreaId = "area-1" });
        result.Succeeded.ShouldBeFalse();
    }

    [Test]
    public async Task UpdateAsync_NonExistentGrupo_ReturnsFailure()
    {
        _userMock.Setup(u => u.Roles).Returns(["Superuser"]);
        var result = await _sut.UpdateAsync("no-existe",
            new UpdateGrupoDeInvestigacionRequest { Nombre = "Test", AreaId = "area-1" });
        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("no encontrado"));
    }

    [Test]
    public async Task UpdateAsync_WithoutPermissions_ReturnsFailure()
    {
        await AddAreaAsync();
        var (_, id) = await _sut.CreateAsync(
            new CreateGrupoDeInvestigacionRequest { Nombre = "Grupo A", AreaId = "area-1" });

        _userMock.Setup(u => u.Roles).Returns(["Investigador"]);

        var result = await _sut.UpdateAsync(id!,
            new UpdateGrupoDeInvestigacionRequest { Nombre = "Modified", AreaId = "area-1" });

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("permisos"));
    }

    [Test]
    public async Task UpdateAsync_Superuser_UpdatesGrupo()
    {
        await AddAreaAsync();
        _userMock.Setup(u => u.Id).Returns("creator-1");
        var (_, id) = await _sut.CreateAsync(
            new CreateGrupoDeInvestigacionRequest { Nombre = "Grupo A", AreaId = "area-1" });

        _userMock.Setup(u => u.Roles).Returns(["Superuser"]);

        var result = await _sut.UpdateAsync(id!,
            new UpdateGrupoDeInvestigacionRequest { Nombre = "Modified", AreaId = "area-1" });

        result.Succeeded.ShouldBeTrue();
        var grupo = await _db.GruposDeInvestigacion.FindAsync(id);
        grupo!.Nombre.ShouldBe("Modified");
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
        _userMock.Setup(u => u.Id).Returns("creator-1");
        var (_, id) = await _sut.CreateAsync(
            new CreateGrupoDeInvestigacionRequest { Nombre = "Grupo A", AreaId = "area-1" });

        _userMock.Setup(u => u.Roles).Returns(["Investigador"]);

        var result = await _sut.DeleteAsync(id!);
        result.Succeeded.ShouldBeFalse();
    }

    [Test]
    public async Task DeleteAsync_Superuser_Removes()
    {
        await AddAreaAsync();
        _userMock.Setup(u => u.Id).Returns("creator-1");
        var (_, id) = await _sut.CreateAsync(
            new CreateGrupoDeInvestigacionRequest { Nombre = "Grupo A", AreaId = "area-1" });

        _userMock.Setup(u => u.Roles).Returns(["Superuser"]);

        var result = await _sut.DeleteAsync(id!);

        result.Succeeded.ShouldBeTrue();
        (await _db.GruposDeInvestigacion.CountAsync()).ShouldBe(0);
    }

    [Test]
    public async Task DeleteAsync_JefeDeGrupo_Removes()
    {
        await AddAreaAsync();
        _userMock.Setup(u => u.Id).Returns("creator-1");
        var (_, id) = await _sut.CreateAsync(
            new CreateGrupoDeInvestigacionRequest { Nombre = "Grupo A", AreaId = "area-1" });

        _userMock.Setup(u => u.Roles).Returns(new List<string> { "Jefe_de_Grupo_de_investigacion" });

        var result = await _sut.DeleteAsync(id!);

        result.Succeeded.ShouldBeTrue();
    }

    // ── SetMiembrosAsync ─────────────────────────────────────────────────────

    [Test]
    public async Task SetMiembrosAsync_NonExistentGrupo_ReturnsFailure()
    {
        _userMock.Setup(u => u.Roles).Returns(["Superuser"]);
        var result = await _sut.SetMiembrosAsync("no-existe", new SetGrupoMiembrosRequest { UsuariosIds = [] });
        result.Succeeded.ShouldBeFalse();
    }

    [Test]
    public async Task SetMiembrosAsync_Superuser_SetsMembers()
    {
        await AddAreaAsync();
        _userMock.Setup(u => u.Id).Returns("creator-1");
        var (_, id) = await _sut.CreateAsync(
            new CreateGrupoDeInvestigacionRequest { Nombre = "Grupo A", AreaId = "area-1" });

        _db.Users.Add(new User
        {
            Id = "user-1", UserName = "alice", Email = "a@a.com",
            UserLastName1 = "A"
        });
        await _db.SaveChangesAsync();

        _userMock.Setup(u => u.Roles).Returns(["Superuser"]);

        var result = await _sut.SetMiembrosAsync(id!, new SetGrupoMiembrosRequest { UsuariosIds = ["user-1"] });

        result.Succeeded.ShouldBeTrue();
        var grupo = await _db.GruposDeInvestigacion
            .Include(g => g.Usuarios)
            .FirstAsync(g => g.Id == id);
        grupo.Usuarios.Count.ShouldBe(1);
    }

    [Test]
    public async Task SetMiembrosAsync_WithoutPermissions_ReturnsFailure()
    {
        await AddAreaAsync();
        _userMock.Setup(u => u.Id).Returns("creator-1");
        _userMock.Setup(u => u.Roles).Returns(new List<string>());
        var (_, id) = await _sut.CreateAsync(
            new CreateGrupoDeInvestigacionRequest { Nombre = "Grupo A", AreaId = "area-1" });

        // Reset roles to empty after creation
        _userMock.Setup(u => u.Roles).Returns(new List<string>());

        var result = await _sut.SetMiembrosAsync(id!, new SetGrupoMiembrosRequest { UsuariosIds = [] });
        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("permisos"));
    }

    // ── GetMineAsync ─────────────────────────────────────────────────────────

    [Test]
    public async Task GetMineAsync_Empty_ReturnsEmptyList()
    {
        _userMock.Setup(u => u.Id).Returns("user-x");
        var result = await _sut.GetMineAsync();
        result.ShouldBeEmpty();
    }

    [Test]
    public async Task GetMineAsync_ReturnsOnlyUserGroups()
    {
        var area = await AddAreaAsync();

        // Seed users
        var user1 = new User { Id = "user-mine", UserName = "U1", UserLastName1 = "L1", Email = "u1@x.com" };
        var user2 = new User { Id = "user-other", UserName = "U2", UserLastName1 = "L2", Email = "u2@x.com" };
        _db.Users.AddRange(user1, user2);

        // Seed groups with Usuarios nav property pre-populated
        var miGrupo = new GrupoDeInvestigacion
        {
            Nombre = "Mi Grupo",
            AreaId = area.Id,
            CreadorId = user1.Id,
            Usuarios = new List<User> { user1 }
        };
        var otroGrupo = new GrupoDeInvestigacion
        {
            Nombre = "Otro Grupo",
            AreaId = area.Id,
            CreadorId = user2.Id,
            Usuarios = new List<User> { user2 }
        };
        _db.GruposDeInvestigacion.AddRange(miGrupo, otroGrupo);
        await _db.SaveChangesAsync();

        _userMock.Setup(u => u.Id).Returns("user-mine");
        var result = await _sut.GetMineAsync();

        result.Count.ShouldBe(1);
        result[0].Nombre.ShouldBe("Mi Grupo");
    }

    // ── UpdateAsync extra branches ────────────────────────────────────────────

    [Test]
    public async Task UpdateAsync_NonExistentArea_ReturnsFailure()
    {
        await AddAreaAsync();
        _userMock.Setup(u => u.Id).Returns("creator-1");
        _userMock.Setup(u => u.Roles).Returns(new List<string> { "Superuser" });
        var (_, id) = await _sut.CreateAsync(
            new CreateGrupoDeInvestigacionRequest { Nombre = "Grupo A", AreaId = "area-1" });

        var result = await _sut.UpdateAsync(id!, new UpdateGrupoDeInvestigacionRequest
        {
            Nombre = "Actualizado",
            AreaId = "no-existe",
            LineasDeInvestigacionIds = new List<string>()
        });

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("área"));
    }

    [Test]
    public async Task UpdateAsync_JefeDeGrupo_UpdatesGrupo()
    {
        await AddAreaAsync();
        await AddAreaAsync("area-2", "FIS");
        _userMock.Setup(u => u.Id).Returns("creator-1");
        _userMock.Setup(u => u.Roles).Returns(new List<string> { "Jefe_de_Grupo_de_investigacion" });
        var (_, id) = await _sut.CreateAsync(
            new CreateGrupoDeInvestigacionRequest { Nombre = "Grupo A", AreaId = "area-1" });

        var result = await _sut.UpdateAsync(id!, new UpdateGrupoDeInvestigacionRequest
        {
            Nombre = "Grupo Actualizado",
            AreaId = "area-2",
            LineasDeInvestigacionIds = new List<string>()
        });

        result.Succeeded.ShouldBeTrue();
        var grupo = await _db.GruposDeInvestigacion.FindAsync(id);
        grupo!.Nombre.ShouldBe("Grupo Actualizado");
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
    public async Task GetAreaAsync_ReturnsOnlyGruposForVicedecanoArea()
    {
        _db.Areas.AddRange(
            new Area { Id = "area-1", Nombre = "Ciencias" },
            new Area { Id = "area-2", Nombre = "Letras" });
        _db.Users.Add(new User { Id = "vd-1", UserName = "vd", Email = "vd@t.com", UserLastName1 = "VD", AreaId = "area-1" });
        _db.GruposDeInvestigacion.AddRange(
            new GrupoDeInvestigacion { Id = "g-1", Nombre = "Grupo A", AreaId = "area-1" },
            new GrupoDeInvestigacion { Id = "g-2", Nombre = "Grupo B", AreaId = "area-2" });
        await _db.SaveChangesAsync();
        _userMock.Setup(u => u.Id).Returns("vd-1");

        var result = await _sut.GetAreaAsync();

        result.ShouldHaveSingleItem();
        result[0].Nombre.ShouldBe("Grupo A");
    }

    [Test]
    public async Task GetAreaAsync_ExcludesGruposFromOtherAreas()
    {
        _db.Areas.AddRange(
            new Area { Id = "area-1", Nombre = "Ciencias" },
            new Area { Id = "area-2", Nombre = "Letras" });
        _db.Users.Add(new User { Id = "vd-1", UserName = "vd", Email = "vd@t.com", UserLastName1 = "VD", AreaId = "area-1" });
        _db.GruposDeInvestigacion.Add(new GrupoDeInvestigacion { Id = "g-3", Nombre = "Grupo C", AreaId = "area-2" });
        await _db.SaveChangesAsync();
        _userMock.Setup(u => u.Id).Returns("vd-1");

        var result = await _sut.GetAreaAsync();
        result.ShouldBeEmpty();
    }
}
