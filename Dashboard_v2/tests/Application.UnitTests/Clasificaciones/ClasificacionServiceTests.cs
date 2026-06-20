using Dashboard_v2.Application.Clasificaciones;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Clasificaciones;

[TestFixture]
public class ClasificacionServiceTests
{
    private ApplicationDbContext _db = null!;
    private ClasificacionService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(opts);
        _sut = new ClasificacionService(_db);
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
        _db.Clasificaciones.AddRange(
            new Domain.Entities.Clasificacion { Id = "c1", Nombre = "Zeta" },
            new Domain.Entities.Clasificacion { Id = "c2", Nombre = "Alpha" });
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync();

        result.Count.ShouldBe(2);
        result[0].Nombre.ShouldBe("Alpha");
        result[1].Nombre.ShouldBe("Zeta");
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsSuccessAndId()
    {
        var (result, id) = await _sut.CreateAsync(new CreateClasificacionRequest { Nombre = "Nueva" });

        result.Succeeded.ShouldBeTrue();
        id.ShouldNotBeNullOrEmpty();
        (await _db.Clasificaciones.CountAsync()).ShouldBe(1);
    }

    [Test]
    public async Task CreateAsync_EmptyNombre_ReturnsFailure()
    {
        var (result, id) = await _sut.CreateAsync(new CreateClasificacionRequest { Nombre = "  " });

        result.Succeeded.ShouldBeFalse();
        id.ShouldBeNull();
    }

    [Test]
    public async Task CreateAsync_TrimsNombre()
    {
        var (_, id) = await _sut.CreateAsync(new CreateClasificacionRequest { Nombre = "  Trimmed  " });

        var entity = await _db.Clasificaciones.FindAsync(id);
        entity!.Nombre.ShouldBe("Trimmed");
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task UpdateAsync_ExistingId_UpdatesNombre()
    {
        _db.Clasificaciones.Add(new Domain.Entities.Clasificacion { Id = "c1", Nombre = "Old" });
        await _db.SaveChangesAsync();

        var result = await _sut.UpdateAsync("c1", new UpdateClasificacionRequest { Nombre = "New" });

        result.Succeeded.ShouldBeTrue();
        var entity = await _db.Clasificaciones.FindAsync("c1");
        entity!.Nombre.ShouldBe("New");
    }

    [Test]
    public async Task UpdateAsync_EmptyNombre_ReturnsFailure()
    {
        var result = await _sut.UpdateAsync("c1", new UpdateClasificacionRequest { Nombre = "" });
        result.Succeeded.ShouldBeFalse();
    }

    [Test]
    public async Task UpdateAsync_NonExistentId_ReturnsFailure()
    {
        var result = await _sut.UpdateAsync("no-existe", new UpdateClasificacionRequest { Nombre = "Test" });
        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("no encontrada"));
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteAsync_ExistingId_Removes()
    {
        _db.Clasificaciones.Add(new Domain.Entities.Clasificacion { Id = "c1", Nombre = "Test" });
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteAsync("c1");

        result.Succeeded.ShouldBeTrue();
        (await _db.Clasificaciones.CountAsync()).ShouldBe(0);
    }

    [Test]
    public async Task DeleteAsync_NonExistentId_ReturnsFailure()
    {
        var result = await _sut.DeleteAsync("no-existe");
        result.Succeeded.ShouldBeFalse();
    }
}
