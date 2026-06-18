using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.ProductosComercializados;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.ProductosComercializados;

[TestFixture]
public class ProductoComercializadoServiceTests
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
                It.IsAny<ICollection<AuthorProductoComercializado>>(),
                It.IsAny<string>(),
                It.IsAny<Func<string, AuthorProductoComercializado>>(),
                It.IsAny<Func<AuthorProductoComercializado, string>>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private ProductoComercializadoService MakeService(string userId, params string[] roles)
    {
        var user = new Mock<IUser>();
        user.Setup(u => u.Id).Returns(userId);
        user.Setup(u => u.Roles).Returns(roles.ToList());
        return new ProductoComercializadoService(_db, user.Object, _authorResolution.Object, _creatorService.Object);
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

    private TipoProductoComercializado SeedTipo(string id = "tipo-1")
    {
        var tipo = new TipoProductoComercializado { Id = id, Nombre = "Software" };
        _db.TipoProductosComercializados.Add(tipo);
        _db.SaveChanges();
        return tipo;
    }

    private ProductoComercializado SeedProducto(string titulo = "Producto", string institutionId = "inst-1", string tipoId = "tipo-1")
    {
        var p = new ProductoComercializado { Titulo = titulo, InstitutionId = institutionId, TipoProductoComercializadoId = tipoId };
        _db.ProductosComercializados.Add(p);
        _db.SaveChanges();
        return p;
    }

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllAsync_NonVicedecano_ReturnsAllProductos()
    {
        SeedInstitution();
        SeedTipo();
        SeedProducto();
        SeedProducto("Producto 2");

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
        SeedTipo();
        var productoInArea = SeedProducto("En Área");
        SeedProducto("Fuera del Área");
        _db.AuthorProductosComercializados.Add(new AuthorProductoComercializado { AuthorId = authorInArea.Id, ProductoComercializadoId = productoInArea.Id });
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
        SeedTipo();
        SeedProducto();

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
    public async Task GetMisAsync_AuthorWithProductos_ReturnsThem()
    {
        var author = SeedAuthor("auth-1", "prof-1");
        SeedInstitution();
        SeedTipo();
        var producto = SeedProducto("Mi Producto");
        _db.AuthorProductosComercializados.Add(new AuthorProductoComercializado { AuthorId = author.Id, ProductoComercializadoId = producto.Id });
        await _db.SaveChangesAsync();
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync("prof-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var result = await MakeService("prof-1", "Profesor").GetMisAsync();

        result.Count.ShouldBe(1);
        result[0].Titulo.ShouldBe("Mi Producto");
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task CreateAsync_NoAuthor_ReturnsFailure()
    {
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync("prof-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Author?)null);

        var (result, id) = await MakeService("prof-1", "Profesor")
            .CreateAsync(new CreateProductoBody("T", "tipo-1", "inst-1"));

        result.Succeeded.ShouldBeFalse();
        id.ShouldBeNull();
    }

    [Test]
    public async Task CreateAsync_Valid_PersistsAndReturnsId()
    {
        SeedInstitution();
        SeedTipo();
        var author = SeedAuthor("auth-1", "prof-1");
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync("prof-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var (result, id) = await MakeService("prof-1", "Profesor")
            .CreateAsync(new CreateProductoBody("Nuevo Producto", "tipo-1", "inst-1"));

        result.Succeeded.ShouldBeTrue();
        id.ShouldNotBeNull();
        (await _db.ProductosComercializados.AnyAsync(p => p.Id == id)).ShouldBeTrue();
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task UpdateAsync_NotFound_ReturnsFailure()
    {
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SeedAuthor("auth-x"));

        var result = await MakeService("prof-1", "Profesor").UpdateAsync("no-existe", new UpdateProductoBody("T", "tipo", "inst"));

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("Producto no encontrado.");
    }

    [Test]
    public async Task UpdateAsync_NotCreator_ReturnsForbidden()
    {
        var author = SeedAuthor("auth-other", "prof-1");
        SeedInstitution();
        SeedTipo();
        var producto = SeedProducto("Original");
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync("prof-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var result = await MakeService("prof-1", "Profesor").UpdateAsync(producto.Id, new UpdateProductoBody("Nuevo", "tipo-1", "inst-1"));

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("No tiene permisos sobre este producto.");
    }

    [Test]
    public async Task UpdateAsync_Superuser_CanUpdateAny()
    {
        var author = SeedAuthor("auth-super", "super-1");
        SeedInstitution();
        SeedTipo();
        var producto = SeedProducto("Original");
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync("super-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var result = await MakeService("super-1", "Superuser").UpdateAsync(producto.Id, new UpdateProductoBody("Actualizado", "tipo-1", "inst-1"));

        result.Succeeded.ShouldBeTrue();
        (await _db.ProductosComercializados.FindAsync(producto.Id))!.Titulo.ShouldBe("Actualizado");
    }

    [Test]
    public async Task UpdateAsync_Creator_CanUpdateOwn()
    {
        var author = SeedAuthor("auth-prof", "prof-1");
        SeedInstitution();
        SeedTipo();
        var producto = SeedProducto("Original");
        _db.AuthorProductosComercializados.Add(new AuthorProductoComercializado { AuthorId = author.Id, ProductoComercializadoId = producto.Id });
        await _db.SaveChangesAsync();
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync("prof-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var result = await MakeService("prof-1", "Profesor").UpdateAsync(producto.Id, new UpdateProductoBody("Actualizado", "tipo-1", "inst-1"));

        result.Succeeded.ShouldBeTrue();
        (await _db.ProductosComercializados.FindAsync(producto.Id))!.Titulo.ShouldBe("Actualizado");
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteAsync_NotFound_ReturnsFailure()
    {
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SeedAuthor("auth-x"));

        var result = await MakeService("prof-1", "Profesor").DeleteAsync("no-existe");

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("Producto no encontrado.");
    }

    [Test]
    public async Task DeleteAsync_NotCreator_ReturnsForbidden()
    {
        var author = SeedAuthor("auth-other", "prof-1");
        SeedInstitution();
        SeedTipo();
        var producto = SeedProducto();
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync("prof-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var result = await MakeService("prof-1", "Profesor").DeleteAsync(producto.Id);

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("No tiene permisos sobre este producto.");
    }

    [Test]
    public async Task DeleteAsync_Superuser_CanDeleteAny()
    {
        var author = SeedAuthor("auth-super", "super-1");
        SeedInstitution();
        SeedTipo();
        var producto = SeedProducto();
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync("super-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var result = await MakeService("super-1", "Superuser").DeleteAsync(producto.Id);

        result.Succeeded.ShouldBeTrue();
        (await _db.ProductosComercializados.FindAsync(producto.Id)).ShouldBeNull();
    }

    [Test]
    public async Task DeleteAsync_Creator_CanDeleteOwn()
    {
        var author = SeedAuthor("auth-prof", "prof-1");
        SeedInstitution();
        SeedTipo();
        var producto = SeedProducto();
        _db.AuthorProductosComercializados.Add(new AuthorProductoComercializado { AuthorId = author.Id, ProductoComercializadoId = producto.Id });
        await _db.SaveChangesAsync();
        _authorResolution.Setup(s => s.GetOrCreateForUserAsync("prof-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var result = await MakeService("prof-1", "Profesor").DeleteAsync(producto.Id);

        result.Succeeded.ShouldBeTrue();
        (await _db.ProductosComercializados.FindAsync(producto.Id)).ShouldBeNull();
    }
}
