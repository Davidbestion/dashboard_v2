using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Patentes;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Patentes;

[TestFixture]
public class PatenteServiceTests
{
    private ApplicationDbContext _db = null!;
    private Mock<IAuthorResolutionService> _authorResolution = null!;
    private Mock<IProductionCreatorService> _creatorService = null!;

    [SetUp]
    public void SetUp()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(opts);
        _authorResolution = new Mock<IAuthorResolutionService>();
        _creatorService = new Mock<IProductionCreatorService>();
        _creatorService
            .Setup(s => s.AddAdditionalCreatorsAsync(
                It.IsAny<ICollection<AuthorPatente>>(),
                It.IsAny<string>(),
                It.IsAny<Func<string, AuthorPatente>>(),
                It.IsAny<Func<AuthorPatente, string>>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private PatenteService MakeService(string userId, params string[] roles)
    {
        var user = new Mock<IUser>();
        user.Setup(u => u.Id).Returns(userId);
        user.Setup(u => u.Roles).Returns(roles.ToList());
        return new PatenteService(_db, user.Object, _authorResolution.Object, _creatorService.Object);
    }

    private Author SeedAuthor(string id, string? userId = null)
    {
        var author = Author.Create($"Author{id}");
        author.Id = id;
        author.UserId = userId;
        _db.Authors.Add(author);
        _db.SaveChanges();
        return author;
    }

    private User SeedUser(string id, string? areaId = null)
    {
        var user = new User { Id = id, UserName = id, UserLastName1 = "L", Email = $"{id}@test.cu", AreaId = areaId };
        _db.Users.Add(user);
        _db.SaveChanges();
        return user;
    }

    private Patente SeedPatente(string titulo = "Patente", bool esNacional = true)
    {
        var p = new Patente { Titulo = titulo, NumeroSolicitudConcesion = "SOL-001", EsNacional = esNacional };
        _db.Patentes.Add(p);
        _db.SaveChanges();
        return p;
    }

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllAsync_NonVicedecano_ReturnsAllPatentes()
    {
        SeedPatente("P1");
        SeedPatente("P2");

        var result = await MakeService("prof-1", "Profesor").GetAllAsync();

        result.Count.ShouldBe(2);
    }

    [Test]
    public async Task GetAllAsync_Vicedecano_FiltersByCreatorArea()
    {
        SeedUser("vice-1", "area-1");
        var userInArea = SeedUser("prof-area", "area-1");
        var authorInArea = SeedAuthor("auth-a", userInArea.Id);
        var patenteInArea = SeedPatente("En Área");
        SeedPatente("Fuera del Área");
        _db.AuthorPatentes.Add(new AuthorPatente { AuthorId = authorInArea.Id, PatenteId = patenteInArea.Id });
        await _db.SaveChangesAsync();

        var result = await MakeService("vice-1", "Vicedecano_de_investigacion").GetAllAsync();

        result.Count.ShouldBe(1);
        result[0].Titulo.ShouldBe("En Área");
    }

    [Test]
    public async Task GetAllAsync_Vicedecano_WithoutArea_ReturnsAll()
    {
        SeedUser("vice-2", null);
        SeedPatente("P1");

        var result = await MakeService("vice-2", "Vicedecano_de_investigacion").GetAllAsync();

        result.Count.ShouldBe(1);
    }

    // ── GetMisAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetMisAsync_NoLinkedAuthor_ReturnsEmpty()
    {
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync("prof-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Author?)null);

        var result = await MakeService("prof-1", "Profesor").GetMisAsync();

        result.ShouldBeEmpty();
    }

    [Test]
    public async Task GetMisAsync_AuthorWithPatentes_ReturnsThem()
    {
        var author = SeedAuthor("auth-1", "prof-1");
        var p = SeedPatente("Mi Patente");
        _db.AuthorPatentes.Add(new AuthorPatente { AuthorId = author.Id, PatenteId = p.Id });
        await _db.SaveChangesAsync();
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync("prof-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var result = await MakeService("prof-1", "Profesor").GetMisAsync();

        result.Count.ShouldBe(1);
        result[0].Titulo.ShouldBe("Mi Patente");
    }

    // ── GetProyectosDeAsync ──────────────────────────────────────────────────

    [Test]
    public async Task GetProyectosDeAsync_PatenteNotFound_ReturnsFalse()
    {
        var (found, _) = await MakeService("prof-1", "Profesor").GetProyectosDeAsync("no-existe");

        found.ShouldBeFalse();
    }

    [Test]
    public async Task GetProyectosDeAsync_Found_ReturnsProyectos()
    {
        var patente = SeedPatente("P");
        var result = await MakeService("prof-1", "Profesor").GetProyectosDeAsync(patente.Id);

        result.Found.ShouldBeTrue();
        result.Proyectos.ShouldBeEmpty();
    }

    // ── LinkProyectoAsync ────────────────────────────────────────────────────

    [Test]
    public async Task LinkProyectoAsync_PatenteNotFound_ReturnsFailure()
    {
        var result = await MakeService("prof-1", "Profesor").LinkProyectoAsync("no-existe", "proy-1");

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("Patente no encontrada.");
    }

    [Test]
    public async Task LinkProyectoAsync_ProyectoNotFound_ReturnsFailure()
    {
        var patente = SeedPatente();

        var result = await MakeService("prof-1", "Profesor").LinkProyectoAsync(patente.Id, "no-existe");

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("Proyecto no encontrado.");
    }

    [Test]
    public async Task LinkProyectoAsync_NotCreator_ReturnsForbidden()
    {
        var author = SeedAuthor("auth-other", "prof-1");
        var patente = SeedPatente();
        var proyecto = new ProyectoEnRevision { Titulo = "P", JefeId = "jefe-x", ClasificacionId = "cl-1", Tipo = "PE" };
        _db.Proyectos.Add(proyecto);
        await _db.SaveChangesAsync();
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync("prof-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var result = await MakeService("prof-1", "Profesor").LinkProyectoAsync(patente.Id, proyecto.Id);

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("No tiene permisos sobre esta patente.");
    }

    [Test]
    public async Task LinkProyectoAsync_JefeDeProyecto_CanLinkWithoutBeingCreator()
    {
        var author = SeedAuthor("auth-jefe", "jefe-1");
        var patente = SeedPatente();
        var proyecto = new ProyectoEnRevision { Titulo = "P", JefeId = "jefe-x", ClasificacionId = "cl-1", Tipo = "PE" };
        _db.Proyectos.Add(proyecto);
        await _db.SaveChangesAsync();
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync("jefe-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var result = await MakeService("jefe-1", "Jefe_de_Proyecto").LinkProyectoAsync(patente.Id, proyecto.Id);

        result.Succeeded.ShouldBeTrue();
    }

    [Test]
    public async Task LinkProyectoAsync_AlreadyLinked_ReturnsFailure()
    {
        var patente = SeedPatente();
        var proyecto = new ProyectoEnRevision { Titulo = "P", JefeId = "jefe-x", ClasificacionId = "cl-1", Tipo = "PE" };
        _db.Proyectos.Add(proyecto);
        _db.ProyectoPatentes.Add(new ProyectoPatente { PatenteId = patente.Id, ProyectoId = proyecto.Id });
        await _db.SaveChangesAsync();

        var result = await MakeService("super-1", "Superuser").LinkProyectoAsync(patente.Id, proyecto.Id);

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("El vinculo ya existe.");
    }

    // ── UnlinkProyectoAsync ──────────────────────────────────────────────────

    [Test]
    public async Task UnlinkProyectoAsync_LinkNotFound_ReturnsFailure()
    {
        var result = await MakeService("prof-1", "Profesor").UnlinkProyectoAsync("pat-x", "proy-x");

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("Vinculo no encontrado.");
    }

    [Test]
    public async Task UnlinkProyectoAsync_Superuser_RemovesLink()
    {
        var patente = SeedPatente();
        var proyecto = new ProyectoEnRevision { Titulo = "P", JefeId = "jefe-x", ClasificacionId = "cl-1", Tipo = "PE" };
        _db.Proyectos.Add(proyecto);
        _db.ProyectoPatentes.Add(new ProyectoPatente { PatenteId = patente.Id, ProyectoId = proyecto.Id });
        await _db.SaveChangesAsync();

        var result = await MakeService("super-1", "Superuser").UnlinkProyectoAsync(patente.Id, proyecto.Id);

        result.Succeeded.ShouldBeTrue();
        (await _db.ProyectoPatentes.AnyAsync(pp => pp.PatenteId == patente.Id)).ShouldBeFalse();
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task CreateAsync_NoAuthor_ReturnsFailure()
    {
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync("prof-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Author?)null);

        var (result, id) = await MakeService("prof-1", "Profesor").CreateAsync(new CreatePatenteBody("T", "S-1", true));

        result.Succeeded.ShouldBeFalse();
        id.ShouldBeNull();
    }

    [Test]
    public async Task CreateAsync_Valid_PersistsAndReturnsId()
    {
        var author = SeedAuthor("auth-1", "prof-1");
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync("prof-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var (result, id) = await MakeService("prof-1", "Profesor").CreateAsync(new CreatePatenteBody("Nueva", "S-42", true));

        result.Succeeded.ShouldBeTrue();
        id.ShouldNotBeNull();
        (await _db.Patentes.AnyAsync(p => p.Id == id)).ShouldBeTrue();
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task UpdateAsync_NotFound_ReturnsFailure()
    {
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SeedAuthor("auth-x"));

        var result = await MakeService("prof-1", "Profesor").UpdateAsync("no-existe", new UpdatePatenteBody("T", "S", false));

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("Patente no encontrada.");
    }

    [Test]
    public async Task UpdateAsync_NotCreator_ReturnsForbidden()
    {
        var author = SeedAuthor("auth-other", "prof-1");
        var patente = SeedPatente("Original");
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync("prof-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var result = await MakeService("prof-1", "Profesor").UpdateAsync(patente.Id, new UpdatePatenteBody("Nueva", "S", false));

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("No tiene permisos sobre esta patente.");
    }

    [Test]
    public async Task UpdateAsync_Superuser_CanUpdateAny()
    {
        var author = SeedAuthor("auth-super", "super-1");
        var patente = SeedPatente("Original");
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync("super-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var result = await MakeService("super-1", "Superuser").UpdateAsync(patente.Id, new UpdatePatenteBody("Actualizada", "S-99", false));

        result.Succeeded.ShouldBeTrue();
        (await _db.Patentes.FindAsync(patente.Id))!.Titulo.ShouldBe("Actualizada");
    }

    [Test]
    public async Task UpdateAsync_Creator_CanUpdateOwn()
    {
        var author = SeedAuthor("auth-prof", "prof-1");
        var patente = SeedPatente("Original");
        _db.AuthorPatentes.Add(new AuthorPatente { AuthorId = author.Id, PatenteId = patente.Id });
        await _db.SaveChangesAsync();
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync("prof-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var result = await MakeService("prof-1", "Profesor").UpdateAsync(patente.Id, new UpdatePatenteBody("Actualizada", "S-99", true));

        result.Succeeded.ShouldBeTrue();
        (await _db.Patentes.FindAsync(patente.Id))!.Titulo.ShouldBe("Actualizada");
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteAsync_NotFound_ReturnsFailure()
    {
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SeedAuthor("auth-x"));

        var result = await MakeService("prof-1", "Profesor").DeleteAsync("no-existe");

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("Patente no encontrada.");
    }

    [Test]
    public async Task DeleteAsync_NotCreator_ReturnsForbidden()
    {
        var author = SeedAuthor("auth-other", "prof-1");
        var patente = SeedPatente();
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync("prof-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var result = await MakeService("prof-1", "Profesor").DeleteAsync(patente.Id);

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("No tiene permisos sobre esta patente.");
    }

    [Test]
    public async Task DeleteAsync_Superuser_CanDeleteAny()
    {
        var author = SeedAuthor("auth-super", "super-1");
        var patente = SeedPatente();
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync("super-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var result = await MakeService("super-1", "Superuser").DeleteAsync(patente.Id);

        result.Succeeded.ShouldBeTrue();
        (await _db.Patentes.FindAsync(patente.Id)).ShouldBeNull();
    }

    [Test]
    public async Task DeleteAsync_Creator_CanDeleteOwn()
    {
        var author = SeedAuthor("auth-prof", "prof-1");
        var patente = SeedPatente();
        _db.AuthorPatentes.Add(new AuthorPatente { AuthorId = author.Id, PatenteId = patente.Id });
        await _db.SaveChangesAsync();
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync("prof-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var result = await MakeService("prof-1", "Profesor").DeleteAsync(patente.Id);

        result.Succeeded.ShouldBeTrue();
        (await _db.Patentes.FindAsync(patente.Id)).ShouldBeNull();
    }
}
