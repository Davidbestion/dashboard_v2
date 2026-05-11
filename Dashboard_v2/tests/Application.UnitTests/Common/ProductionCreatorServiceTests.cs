using Dashboard_v2.Application.Common;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Common;

/// <summary>
/// Tests unitarios para <see cref="ProductionCreatorService"/>.
///
/// Cubre:
///   - Adición por AuthorId válido
///   - Skip de AuthorId inválido (no existe en DB)
///   - Skip si el AuthorId es el del creador actual
///   - Skip si el Author ya está en la colección (no duplicados)
///   - Adición por nombre (vía IAuthorResolutionService)
///   - Skip de nombre vacío/whitespace
///   - Skip de nombre si el autor resuelto es el creador actual
///   - Adición por UserId (vía GetOrCreateForUserAsync)
///   - Skip si GetOrCreateForUser retorna null
///   - Skip si el User resuelto es el creador actual
///   - Combinación: varios paths a la vez no producen duplicados
/// </summary>
[TestFixture]
public class ProductionCreatorServiceTests
{
    private ApplicationDbContext _db = null!;
    private Mock<IAuthorResolutionService> _resolutionMock = null!;
    private ProductionCreatorService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(opts);
        _resolutionMock = new Mock<IAuthorResolutionService>(MockBehavior.Strict);
        _sut = new ProductionCreatorService(_db, _resolutionMock.Object);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static (ICollection<AuthorNorma> creadores, Func<string, AuthorNorma> factory, Func<AuthorNorma, string> getId)
        BuildNormaHelpers(string normaId)
    {
        var creadores = new List<AuthorNorma>();
        return (
            creadores,
            authorId => new AuthorNorma { AuthorId = authorId, NormaId = normaId },
            c => c.AuthorId);
    }

    private async Task AddAuthorToDb(string authorId)
    {
        _db.Authors.Add(new Author { Id = authorId, LastName = "Author", Name = "Author, Test", SearchKey = "author test", LastNameKey = "author" });
        await _db.SaveChangesAsync();
    }

    // ── Tests: AdditionalAuthorIds ───────────────────────────────────────────

    [Test]
    public async Task AddByAuthorId_ValidExistingAuthor_AddsToCollection()
    {
        await AddAuthorToDb("author-2");
        var (creadores, factory, getId) = BuildNormaHelpers("norma-1");
        creadores.Add(new AuthorNorma { AuthorId = "author-1", NormaId = "norma-1" });

        await _sut.AddAdditionalCreatorsAsync(creadores, "author-1", factory, getId,
            additionalAuthorIds: ["author-2"],
            additionalAuthorNames: null, additionalUserIds: null);

        creadores.Count.ShouldBe(2);
        creadores.ShouldContain(c => c.AuthorId == "author-2");
    }

    [Test]
    public async Task AddByAuthorId_AuthorNotInDb_Skips()
    {
        var (creadores, factory, getId) = BuildNormaHelpers("norma-1");
        creadores.Add(new AuthorNorma { AuthorId = "author-1", NormaId = "norma-1" });

        await _sut.AddAdditionalCreatorsAsync(creadores, "author-1", factory, getId,
            additionalAuthorIds: ["nonexistent-author"],
            additionalAuthorNames: null, additionalUserIds: null);

        creadores.Count.ShouldBe(1);
    }

    [Test]
    public async Task AddByAuthorId_IsCurrentAuthor_Skips()
    {
        await AddAuthorToDb("author-1");
        var (creadores, factory, getId) = BuildNormaHelpers("norma-1");
        creadores.Add(new AuthorNorma { AuthorId = "author-1", NormaId = "norma-1" });

        await _sut.AddAdditionalCreatorsAsync(creadores, "author-1", factory, getId,
            additionalAuthorIds: ["author-1"],
            additionalAuthorNames: null, additionalUserIds: null);

        creadores.Count.ShouldBe(1);
    }

    [Test]
    public async Task AddByAuthorId_AlreadyInCollection_NoDuplicate()
    {
        await AddAuthorToDb("author-2");
        var (creadores, factory, getId) = BuildNormaHelpers("norma-1");
        creadores.Add(new AuthorNorma { AuthorId = "author-1", NormaId = "norma-1" });
        creadores.Add(new AuthorNorma { AuthorId = "author-2", NormaId = "norma-1" });

        await _sut.AddAdditionalCreatorsAsync(creadores, "author-1", factory, getId,
            additionalAuthorIds: ["author-2"],
            additionalAuthorNames: null, additionalUserIds: null);

        creadores.Count.ShouldBe(2);
    }

    [Test]
    public async Task AddByAuthorId_EmptyOrWhitespace_Skips()
    {
        var (creadores, factory, getId) = BuildNormaHelpers("norma-1");

        await _sut.AddAdditionalCreatorsAsync(creadores, "author-1", factory, getId,
            additionalAuthorIds: ["", "   "],
            additionalAuthorNames: null, additionalUserIds: null);

        creadores.Count.ShouldBe(0);
    }

    // ── Tests: AdditionalAuthorNames ─────────────────────────────────────────

    [Test]
    public async Task AddByAuthorName_ValidName_ResolvedAndAdded()
    {
        var resolvedAuthor = new Author { Id = "author-2", Name = "García, Juan", SearchKey = "garcia, juan" };
        _resolutionMock.Setup(s => s.ResolveByNameAsync("García, Juan", It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolvedAuthor);

        var (creadores, factory, getId) = BuildNormaHelpers("norma-1");
        creadores.Add(new AuthorNorma { AuthorId = "author-1", NormaId = "norma-1" });

        await _sut.AddAdditionalCreatorsAsync(creadores, "author-1", factory, getId,
            additionalAuthorIds: null,
            additionalAuthorNames: ["García, Juan"],
            additionalUserIds: null);

        creadores.Count.ShouldBe(2);
        creadores.ShouldContain(c => c.AuthorId == "author-2");
    }

    [Test]
    public async Task AddByAuthorName_IsCurrentAuthor_Skips()
    {
        var resolvedAuthor = new Author { Id = "author-1", Name = "Perez, Ana", SearchKey = "perez, ana" };
        _resolutionMock.Setup(s => s.ResolveByNameAsync("Perez, Ana", It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolvedAuthor);

        var (creadores, factory, getId) = BuildNormaHelpers("norma-1");
        creadores.Add(new AuthorNorma { AuthorId = "author-1", NormaId = "norma-1" });

        await _sut.AddAdditionalCreatorsAsync(creadores, "author-1", factory, getId,
            additionalAuthorIds: null,
            additionalAuthorNames: ["Perez, Ana"],
            additionalUserIds: null);

        creadores.Count.ShouldBe(1);
    }

    [Test]
    public async Task AddByAuthorName_EmptyOrWhitespace_Skips()
    {
        var (creadores, factory, getId) = BuildNormaHelpers("norma-1");

        await _sut.AddAdditionalCreatorsAsync(creadores, "author-1", factory, getId,
            additionalAuthorIds: null,
            additionalAuthorNames: ["", "   "],
            additionalUserIds: null);

        creadores.Count.ShouldBe(0);
        _resolutionMock.VerifyNoOtherCalls();
    }

    // ── Tests: AdditionalUserIds ─────────────────────────────────────────────

    [Test]
    public async Task AddByUserId_ValidUser_ResolvedAndAdded()
    {
        var resolvedAuthor = new Author { Id = "author-2", Name = "Lopez, Maria", SearchKey = "lopez, maria" };
        _resolutionMock.Setup(s => s.GetOrCreateForUserAsync("user-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolvedAuthor);

        var (creadores, factory, getId) = BuildNormaHelpers("norma-1");
        creadores.Add(new AuthorNorma { AuthorId = "author-1", NormaId = "norma-1" });

        await _sut.AddAdditionalCreatorsAsync(creadores, "author-1", factory, getId,
            additionalAuthorIds: null, additionalAuthorNames: null,
            additionalUserIds: ["user-2"]);

        creadores.Count.ShouldBe(2);
        creadores.ShouldContain(c => c.AuthorId == "author-2");
    }

    [Test]
    public async Task AddByUserId_UserNotFound_Skips()
    {
        _resolutionMock.Setup(s => s.GetOrCreateForUserAsync("nonexistent-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Author?)null);

        var (creadores, factory, getId) = BuildNormaHelpers("norma-1");
        creadores.Add(new AuthorNorma { AuthorId = "author-1", NormaId = "norma-1" });

        await _sut.AddAdditionalCreatorsAsync(creadores, "author-1", factory, getId,
            additionalAuthorIds: null, additionalAuthorNames: null,
            additionalUserIds: ["nonexistent-user"]);

        creadores.Count.ShouldBe(1);
    }

    [Test]
    public async Task AddByUserId_IsCurrentAuthor_Skips()
    {
        var resolvedAuthor = new Author { Id = "author-1", Name = "Self", SearchKey = "self" };
        _resolutionMock.Setup(s => s.GetOrCreateForUserAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolvedAuthor);

        var (creadores, factory, getId) = BuildNormaHelpers("norma-1");
        creadores.Add(new AuthorNorma { AuthorId = "author-1", NormaId = "norma-1" });

        await _sut.AddAdditionalCreatorsAsync(creadores, "author-1", factory, getId,
            additionalAuthorIds: null, additionalAuthorNames: null,
            additionalUserIds: ["user-1"]);

        creadores.Count.ShouldBe(1);
    }

    // ── Tests: combinaciones ─────────────────────────────────────────────────

    [Test]
    public async Task AllPaths_SameAuthorResolvesToSameId_NoDuplicate()
    {
        // author-2 accessible by id, name, and userId → should only appear once
        await AddAuthorToDb("author-2");
        var sameAuthor = new Author { Id = "author-2", Name = "González, Rosa", SearchKey = "gonzalez, rosa" };
        _resolutionMock.Setup(s => s.ResolveByNameAsync("González, Rosa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sameAuthor);
        _resolutionMock.Setup(s => s.GetOrCreateForUserAsync("user-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sameAuthor);

        var (creadores, factory, getId) = BuildNormaHelpers("norma-1");
        creadores.Add(new AuthorNorma { AuthorId = "author-1", NormaId = "norma-1" });

        await _sut.AddAdditionalCreatorsAsync(creadores, "author-1", factory, getId,
            additionalAuthorIds: ["author-2"],
            additionalAuthorNames: ["González, Rosa"],
            additionalUserIds: ["user-2"]);

        creadores.Count.ShouldBe(2);
        creadores.Count(c => c.AuthorId == "author-2").ShouldBe(1);
    }

    [Test]
    public async Task NullInputs_DoNotThrow()
    {
        var (creadores, factory, getId) = BuildNormaHelpers("norma-1");

        await Should.NotThrowAsync(() =>
            _sut.AddAdditionalCreatorsAsync(creadores, "author-1", factory, getId,
                additionalAuthorIds: null, additionalAuthorNames: null, additionalUserIds: null));
    }
}
