using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Registros;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Registros;

[TestFixture]
public class RegistroServiceTests
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
                It.IsAny<ICollection<AuthorRegistro>>(),
                It.IsAny<string>(),
                It.IsAny<Func<string, AuthorRegistro>>(),
                It.IsAny<Func<AuthorRegistro, string>>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private RegistroService MakeService(string userId, params string[] roles)
    {
        var user = new Mock<IUser>();
        user.Setup(u => u.Id).Returns(userId);
        user.Setup(u => u.Roles).Returns(roles.ToList());
        return new RegistroService(_db, user.Object, _authorResolution.Object, _creatorService.Object);
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

    private Institution SeedInstitution(string id = "inst-1")
    {
        var inst = new Institution { Id = id, Nombre = "Institución" };
        _db.Institutions.Add(inst);
        _db.SaveChanges();
        return inst;
    }

    private Country SeedCountry(int id = 1)
    {
        var country = new Country { Id = id, Name = "Cuba" };
        _db.Countries.Add(country);
        _db.SaveChanges();
        return country;
    }

    private Registro SeedRegistro(string titulo = "Registro", string institutionId = "inst-1", int countryId = 1)
    {
        var r = new Registro { Titulo = titulo, NumeroCertificado = "CERT-001", EsInformatico = false, InstitutionId = institutionId, CountryId = countryId };
        _db.Registros.Add(r);
        _db.SaveChanges();
        return r;
    }

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllAsync_NonVicedecano_ReturnsAllRegistros()
    {
        SeedInstitution();
        SeedCountry();
        SeedRegistro();
        SeedRegistro("Registro 2");

        var result = await MakeService("prof-1", "Profesor").GetAllAsync();

        result.Count.ShouldBe(2);
    }

    [Test]
    public async Task GetAllAsync_Vicedecano_FiltersByCreatorArea()
    {
        SeedUser("vice-1", "area-1");
        var userInArea = SeedUser("prof-area", "area-1");
        var authorInArea = SeedAuthor("auth-a", userInArea.Id);
        SeedInstitution();
        SeedCountry();
        var registroInArea = SeedRegistro("En Área");
        SeedRegistro("Fuera del Área");
        _db.AuthorRegistros.Add(new AuthorRegistro { AuthorId = authorInArea.Id, RegistroId = registroInArea.Id });
        await _db.SaveChangesAsync();

        var result = await MakeService("vice-1", "Vicedecano_de_investigacion").GetAllAsync();

        result.Count.ShouldBe(1);
        result[0].Titulo.ShouldBe("En Área");
    }

    [Test]
    public async Task GetAllAsync_Vicedecano_WithoutArea_ReturnsAll()
    {
        SeedUser("vice-2", null);
        SeedInstitution();
        SeedCountry();
        SeedRegistro();

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
    public async Task GetMisAsync_AuthorWithRegistros_ReturnsThem()
    {
        var author = SeedAuthor("auth-1", "prof-1");
        SeedInstitution();
        SeedCountry();
        var registro = SeedRegistro("Mi Registro");
        _db.AuthorRegistros.Add(new AuthorRegistro { AuthorId = author.Id, RegistroId = registro.Id });
        await _db.SaveChangesAsync();
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync("prof-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var result = await MakeService("prof-1", "Profesor").GetMisAsync();

        result.Count.ShouldBe(1);
        result[0].Titulo.ShouldBe("Mi Registro");
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task CreateAsync_NoAuthor_ReturnsFailure()
    {
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync("prof-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Author?)null);

        var (result, id) = await MakeService("prof-1", "Profesor")
            .CreateAsync(new CreateRegistroBody("T", "C-1", false, 1, "inst-1"));

        result.Succeeded.ShouldBeFalse();
        id.ShouldBeNull();
    }

    [Test]
    public async Task CreateAsync_Valid_PersistsAndReturnsId()
    {
        SeedInstitution();
        SeedCountry();
        var author = SeedAuthor("auth-1", "prof-1");
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync("prof-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var (result, id) = await MakeService("prof-1", "Profesor")
            .CreateAsync(new CreateRegistroBody("Nuevo Registro", "CERT-001", false, 1, "inst-1"));

        result.Succeeded.ShouldBeTrue();
        id.ShouldNotBeNull();
        (await _db.Registros.AnyAsync(r => r.Id == id)).ShouldBeTrue();
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task UpdateAsync_NotFound_ReturnsFailure()
    {
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SeedAuthor("auth-x"));

        var result = await MakeService("prof-1", "Profesor").UpdateAsync("no-existe", new UpdateRegistroBody("T", "C", false, 1, "i"));

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("Registro no encontrado.");
    }

    [Test]
    public async Task UpdateAsync_NotCreator_ReturnsForbidden()
    {
        var author = SeedAuthor("auth-other", "prof-1");
        SeedInstitution();
        SeedCountry();
        var registro = SeedRegistro("Original");
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync("prof-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var result = await MakeService("prof-1", "Profesor").UpdateAsync(registro.Id, new UpdateRegistroBody("Nueva", "C", false, 1, "inst-1"));

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("No tiene permisos sobre este registro.");
    }

    [Test]
    public async Task UpdateAsync_Superuser_CanUpdateAny()
    {
        var author = SeedAuthor("auth-super", "super-1");
        SeedInstitution();
        SeedCountry();
        var registro = SeedRegistro("Original");
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync("super-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var result = await MakeService("super-1", "Superuser").UpdateAsync(registro.Id, new UpdateRegistroBody("Actualizado", "C-99", true, 1, "inst-1"));

        result.Succeeded.ShouldBeTrue();
        (await _db.Registros.FindAsync(registro.Id))!.Titulo.ShouldBe("Actualizado");
    }

    [Test]
    public async Task UpdateAsync_Creator_CanUpdateOwn()
    {
        var author = SeedAuthor("auth-prof", "prof-1");
        SeedInstitution();
        SeedCountry();
        var registro = SeedRegistro("Original");
        _db.AuthorRegistros.Add(new AuthorRegistro { AuthorId = author.Id, RegistroId = registro.Id });
        await _db.SaveChangesAsync();
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync("prof-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var result = await MakeService("prof-1", "Profesor").UpdateAsync(registro.Id, new UpdateRegistroBody("Actualizado", "C-99", false, 1, "inst-1"));

        result.Succeeded.ShouldBeTrue();
        (await _db.Registros.FindAsync(registro.Id))!.Titulo.ShouldBe("Actualizado");
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteAsync_NotFound_ReturnsFailure()
    {
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SeedAuthor("auth-x"));

        var result = await MakeService("prof-1", "Profesor").DeleteAsync("no-existe");

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("Registro no encontrado.");
    }

    [Test]
    public async Task DeleteAsync_NotCreator_ReturnsForbidden()
    {
        var author = SeedAuthor("auth-other", "prof-1");
        SeedInstitution();
        SeedCountry();
        var registro = SeedRegistro();
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync("prof-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var result = await MakeService("prof-1", "Profesor").DeleteAsync(registro.Id);

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("No tiene permisos sobre este registro.");
    }

    [Test]
    public async Task DeleteAsync_Superuser_CanDeleteAny()
    {
        var author = SeedAuthor("auth-super", "super-1");
        SeedInstitution();
        SeedCountry();
        var registro = SeedRegistro();
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync("super-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var result = await MakeService("super-1", "Superuser").DeleteAsync(registro.Id);

        result.Succeeded.ShouldBeTrue();
        (await _db.Registros.FindAsync(registro.Id)).ShouldBeNull();
    }

    [Test]
    public async Task DeleteAsync_Creator_CanDeleteOwn()
    {
        var author = SeedAuthor("auth-prof", "prof-1");
        SeedInstitution();
        SeedCountry();
        var registro = SeedRegistro();
        _db.AuthorRegistros.Add(new AuthorRegistro { AuthorId = author.Id, RegistroId = registro.Id });
        await _db.SaveChangesAsync();
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync("prof-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var result = await MakeService("prof-1", "Profesor").DeleteAsync(registro.Id);

        result.Succeeded.ShouldBeTrue();
        (await _db.Registros.FindAsync(registro.Id)).ShouldBeNull();
    }
}
