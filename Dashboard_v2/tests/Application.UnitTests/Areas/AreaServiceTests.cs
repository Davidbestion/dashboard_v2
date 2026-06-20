using Dashboard_v2.Application.Areas;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Areas;

[TestFixture]
public class AreaServiceTests
{
    private ApplicationDbContext _db = null!;
    private AreaService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(opts);
        _sut = new AreaService(_db);
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
    public async Task GetAllAsync_WithAreas_ReturnsAllOrderedByNombre()
    {
        _db.Areas.AddRange(
            new Domain.Entities.Area { Id = "a1", Nombre = "Zeta" },
            new Domain.Entities.Area { Id = "a2", Nombre = "Alpha" });
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
        var request = new CreateAreaRequest { Nombre = "Informática" };

        var (result, id) = await _sut.CreateAsync(request);

        result.Succeeded.ShouldBeTrue();
        id.ShouldNotBeNullOrEmpty();
        (await _db.Areas.CountAsync()).ShouldBe(1);
    }

    [Test]
    public async Task CreateAsync_EmptyNombre_ReturnsFailure()
    {
        var request = new CreateAreaRequest { Nombre = "  " };

        var (result, id) = await _sut.CreateAsync(request);

        result.Succeeded.ShouldBeFalse();
        id.ShouldBeNull();
    }

    [Test]
    public async Task CreateAsync_NonExistentUniversidadId_ReturnsFailure()
    {
        var request = new CreateAreaRequest { Nombre = "Test", UniversidadId = "no-existe" };

        var (result, id) = await _sut.CreateAsync(request);

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("universidad"));
    }

    [Test]
    public async Task CreateAsync_TrimsNombre()
    {
        var request = new CreateAreaRequest { Nombre = "  Matemáticas  " };

        var (result, id) = await _sut.CreateAsync(request);

        result.Succeeded.ShouldBeTrue();
        var area = await _db.Areas.FindAsync(id);
        area!.Nombre.ShouldBe("Matemáticas");
    }

    [Test]
    public async Task CreateAsync_WithAreasDelConocimientoIds_AssociatesAreasDelConocimiento()
    {
        var areaConoc = new Domain.Entities.AreaDelConocimiento { Id = "ac-1", Nombre = "Ciencias Exactas" };
        _db.AreasDelConocimiento.Add(areaConoc);
        await _db.SaveChangesAsync();

        var request = new CreateAreaRequest
        {
            Nombre = "Área de Ciencias",
            AreasDelConocimientoIds = new List<string> { "ac-1" }
        };

        var (result, id) = await _sut.CreateAsync(request);

        result.Succeeded.ShouldBeTrue();
        var area = await _db.Areas
            .Include(a => a.AreasDelConocimiento)
            .FirstAsync(a => a.Id == id);
        area.AreasDelConocimiento.Count.ShouldBe(1);
        area.AreasDelConocimiento.First().Id.ShouldBe("ac-1");
    }

    [Test]
    public async Task CreateAsync_WithMultipleAreasDelConocimiento_AssociatesAll()
    {
        _db.AreasDelConocimiento.AddRange(
            new Domain.Entities.AreaDelConocimiento { Id = "ac-1", Nombre = "Matemáticas" },
            new Domain.Entities.AreaDelConocimiento { Id = "ac-2", Nombre = "Física" });
        await _db.SaveChangesAsync();

        var (result, id) = await _sut.CreateAsync(new CreateAreaRequest
        {
            Nombre = "Ciencias",
            AreasDelConocimientoIds = new List<string> { "ac-1", "ac-2" }
        });

        result.Succeeded.ShouldBeTrue();
        var area = await _db.Areas
            .Include(a => a.AreasDelConocimiento)
            .FirstAsync(a => a.Id == id);
        area.AreasDelConocimiento.Count.ShouldBe(2);
    }

    [Test]
    public async Task CreateAsync_WithUnknownAreaDelConocimientoId_IgnoresIt()
    {
        var (result, id) = await _sut.CreateAsync(new CreateAreaRequest
        {
            Nombre = "Test",
            AreasDelConocimientoIds = new List<string> { "ac-inexistente" }
        });

        result.Succeeded.ShouldBeTrue();
        var area = await _db.Areas
            .Include(a => a.AreasDelConocimiento)
            .FirstAsync(a => a.Id == id);
        area.AreasDelConocimiento.ShouldBeEmpty();
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task UpdateAsync_ValidRequest_UpdatesArea()
    {
        _db.Areas.Add(new Domain.Entities.Area { Id = "a1", Nombre = "Old" });
        await _db.SaveChangesAsync();

        var result = await _sut.UpdateAsync("a1", new UpdateAreaRequest { Nombre = "New" });

        result.Succeeded.ShouldBeTrue();
        var area = await _db.Areas.FindAsync("a1");
        area!.Nombre.ShouldBe("New");
    }

    [Test]
    public async Task UpdateAsync_EmptyNombre_ReturnsFailure()
    {
        var result = await _sut.UpdateAsync("a1", new UpdateAreaRequest { Nombre = "" });
        result.Succeeded.ShouldBeFalse();
    }

    [Test]
    public async Task UpdateAsync_NonExistentId_ReturnsFailure()
    {
        var result = await _sut.UpdateAsync("no-existe", new UpdateAreaRequest { Nombre = "Test" });
        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("no encontrada"));
    }

    [Test]
    public async Task UpdateAsync_NonExistentUniversidadId_ReturnsFailure()
    {
        _db.Areas.Add(new Domain.Entities.Area { Id = "a1", Nombre = "Test" });
        await _db.SaveChangesAsync();

        var result = await _sut.UpdateAsync("a1", new UpdateAreaRequest { Nombre = "Test", UniversidadId = "no-existe" });

        result.Succeeded.ShouldBeFalse();
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteAsync_ExistingId_RemovesArea()
    {
        _db.Areas.Add(new Domain.Entities.Area { Id = "a1", Nombre = "Test" });
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteAsync("a1");

        result.Succeeded.ShouldBeTrue();
        (await _db.Areas.CountAsync()).ShouldBe(0);
    }

    [Test]
    public async Task DeleteAsync_NonExistentId_ReturnsFailure()
    {
        var result = await _sut.DeleteAsync("no-existe");
        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("no encontrada"));
    }
}
