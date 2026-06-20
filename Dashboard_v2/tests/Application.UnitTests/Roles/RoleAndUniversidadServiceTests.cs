using Dashboard_v2.Application.Roles;
using Dashboard_v2.Application.Universidades;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Roles;

[TestFixture]
public class RoleServiceTests
{
    private RoleService _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new RoleService();

    [Test]
    public async Task GetAssignableRolesAsync_ExcludesNoneAndSuperuser()
    {
        var roles = await _sut.GetAssignableRolesAsync();

        roles.ShouldNotContain(r => r.Name == "None");
        roles.ShouldNotContain(r => r.Name == "Superuser");
    }

    [Test]
    public async Task GetAssignableRolesAsync_ReturnsOrderedList()
    {
        var roles = await _sut.GetAssignableRolesAsync();

        roles.ShouldNotBeEmpty();
        var names = roles.Select(r => r.Name).ToList();
        names.ShouldBe(names.OrderBy(n => n).ToList());
    }

    [Test]
    public async Task GetAssignableRolesAsync_ContainsExpectedRoles()
    {
        var roles = await _sut.GetAssignableRolesAsync();

        var names = roles.Select(r => r.Name).ToList();
        names.ShouldContain("Profesor");
        names.ShouldContain("Jefe_de_Proyecto");
        names.ShouldContain("Jefe_de_Grupo_de_investigacion");
    }
}

[TestFixture]
public class UniversidadServiceTests
{
    private ApplicationDbContext _db = null!;
    private UniversidadService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(opts);
        _sut = new UniversidadService(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllAsync_EmptyDb_ReturnsEmptyList()
    {
        var result = await _sut.GetAllAsync();
        result.ShouldBeEmpty();
    }

    [Test]
    public async Task GetAllAsync_WithItems_ReturnsOrderedByNombre()
    {
        _db.Universidades.AddRange(
            new Domain.Entities.Universidad { Id = "u1", Nombre = "Zeta U" },
            new Domain.Entities.Universidad { Id = "u2", Nombre = "Alpha U" });
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync();

        result.Count.ShouldBe(2);
        result[0].Nombre.ShouldBe("Alpha U");
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task CreateAsync_ValidNombre_ReturnsSuccessAndId()
    {
        var (result, id) = await _sut.CreateAsync("UH");

        result.Succeeded.ShouldBeTrue();
        id.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public async Task CreateAsync_EmptyNombre_ReturnsFailure()
    {
        var (result, id) = await _sut.CreateAsync("  ");

        result.Succeeded.ShouldBeFalse();
        id.ShouldBeNull();
    }

    [Test]
    public async Task CreateAsync_TrimsNombre()
    {
        var (_, id) = await _sut.CreateAsync("  UCLV  ");

        var entity = await _db.Universidades.FindAsync(id);
        entity!.Nombre.ShouldBe("UCLV");
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task UpdateAsync_ValidRequest_UpdatesNombre()
    {
        var (_, id) = await _sut.CreateAsync("Old U");

        var result = await _sut.UpdateAsync(id!, "New U");

        result.Succeeded.ShouldBeTrue();
        var entity = await _db.Universidades.FindAsync(id);
        entity!.Nombre.ShouldBe("New U");
    }

    [Test]
    public async Task UpdateAsync_EmptyNombre_ReturnsFailure()
    {
        var result = await _sut.UpdateAsync("u1", "");
        result.Succeeded.ShouldBeFalse();
    }

    [Test]
    public async Task UpdateAsync_NonExistentId_ReturnsFailure()
    {
        var result = await _sut.UpdateAsync("no-existe", "Test");
        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("no encontrada"));
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteAsync_ExistingId_Removes()
    {
        var (_, id) = await _sut.CreateAsync("UH");

        var result = await _sut.DeleteAsync(id!);

        result.Succeeded.ShouldBeTrue();
        (await _db.Universidades.CountAsync()).ShouldBe(0);
    }

    [Test]
    public async Task DeleteAsync_NonExistentId_ReturnsFailure()
    {
        var result = await _sut.DeleteAsync("no-existe");
        result.Succeeded.ShouldBeFalse();
    }
}
