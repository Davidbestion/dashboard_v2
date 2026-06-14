using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Publications;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Domain.Enums;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Publications;

[TestFixture]
public class PublicationServiceTests
{
    private ApplicationDbContext _db = null!;
    private Mock<IUser> _currentUser = null!;
    private Mock<ICrossRefClient> _crossRefClient = null!;
    private Mock<IOpenAireClient> _openAireClient = null!;
    private Mock<IAuthorResolutionService> _authorResolution = null!;
    private Mock<IAuthorCleanupService> _authorCleanup = null!;
    private PublicationService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _currentUser = new Mock<IUser>();
        _currentUser.Setup(u => u.Id).Returns("user-1");
        _currentUser.Setup(u => u.Roles).Returns(new List<string> { "Profesor" });
        _crossRefClient = new Mock<ICrossRefClient>();
        _openAireClient = new Mock<IOpenAireClient>();
        _authorResolution = new Mock<IAuthorResolutionService>();
        _authorCleanup = new Mock<IAuthorCleanupService>();

        _sut = new PublicationService(_db, _currentUser.Object, _crossRefClient.Object,
            _openAireClient.Object, _authorResolution.Object, _authorCleanup.Object);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    // ─── GetAllPublicationsAsync ───────────────────────────────────────────────

    [Test]
    public async Task GetAllPublicationsAsync_Empty_ReturnsEmpty()
    {
        var result = await _sut.GetAllPublicationsAsync();
        result.ShouldBeEmpty();
    }

    // ─── GetMyPublicationsAsync ───────────────────────────────────────────────

    [Test]
    public async Task GetMyPublicationsAsync_NoLinkedAuthor_ReturnsEmpty()
    {
        var result = await _sut.GetMyPublicationsAsync();
        result.ShouldBeEmpty();
    }

    // ─── GetAreaPublicationsAsync ─────────────────────────────────────────────

    [Test]
    public async Task GetAreaPublicationsAsync_UserWithNoArea_ReturnsEmpty()
    {
        _db.Users.Add(new User { Id = "user-1", UserName = "test", Email = "t@t.com", UserLastName1 = "T" });
        await _db.SaveChangesAsync();

        var result = await _sut.GetAreaPublicationsAsync();
        result.ShouldBeEmpty();
    }

    // ─── GetPublicationTypesAsync ─────────────────────────────────────────────

    [Test]
    public async Task GetPublicationTypesAsync_ReturnsAllTypes()
    {
        var result = await _sut.GetPublicationTypesAsync();
        result.ShouldNotBeEmpty();
    }

    // ─── GetMyRedPublicationsAsync ────────────────────────────────────────────

    [Test]
    public async Task GetMyRedPublicationsAsync_AsNonJefe_NoCoordinatedReds_ReturnsEmpty()
    {
        var result = await _sut.GetMyRedPublicationsAsync();
        result.ShouldBeEmpty();
    }

    [Test]
    public async Task GetMyRedPublicationsAsync_AsJefeDeRedes_ReturnsAllWithRed()
    {
        _currentUser.Setup(u => u.Roles).Returns(new List<string> { "Jefe_de_Redes" });

        var pub = new Publication
        {
            Id = Guid.NewGuid().ToString(), Title = "Pub con Red",
            NormalizedTitle = "pub con red", PublishedDate = "2024",
            PublicationData = "{}",
            RedId = Guid.NewGuid().ToString(),
            AuthorPublications = new List<AuthorPublication>()
        };
        _db.Publications.Add(pub);
        await _db.SaveChangesAsync();

        var result = await _sut.GetMyRedPublicationsAsync();
        result.Count.ShouldBe(1);
    }

    // ─── FindDuplicatesAsync ─────────────────────────────────────────────────

    [Test]
    public async Task FindDuplicatesAsync_NullInputs_ReturnsEmpty()
    {
        var result = await _sut.FindDuplicatesAsync(null, null, null);
        result.ShouldBeEmpty();
    }

    [Test]
    public async Task FindDuplicatesAsync_ByTitle_FindsMatch()
    {
        var pub = new Publication
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Machine Learning en Cuba",
            NormalizedTitle = "machine learning en cuba",
            PublishedDate = "2024",
            PublicationData = "{}",
            AuthorPublications = new List<AuthorPublication>()
        };
        _db.Publications.Add(pub);
        await _db.SaveChangesAsync();

        var result = await _sut.FindDuplicatesAsync("Machine Learning en Cuba", null, null);
        result.ShouldHaveSingleItem();
        result[0].MatchType.ShouldBe("title");
    }

    [Test]
    public async Task FindDuplicatesAsync_ByDoi_FindsMatch()
    {
        var doi = "10.1000/xyz123";
        var pub = new Publication
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Some Paper",
            NormalizedTitle = "some paper",
            UrlDoi = doi,
            NormalizedUrlDoi = doi,
            PublishedDate = "2024",
            PublicationData = "{}",
            AuthorPublications = new List<AuthorPublication>()
        };
        _db.Publications.Add(pub);
        await _db.SaveChangesAsync();

        var result = await _sut.FindDuplicatesAsync(null, doi, null);
        result.ShouldHaveSingleItem();
        result[0].MatchType.ShouldBe("doi");
    }

    [Test]
    public async Task FindDuplicatesAsync_ExcludeId_ExcludesPublication()
    {
        var id = Guid.NewGuid().ToString();
        var pub = new Publication
        {
            Id = id,
            Title = "Same Title",
            NormalizedTitle = "same title",
            PublishedDate = "2024",
            PublicationData = "{}",
            AuthorPublications = new List<AuthorPublication>()
        };
        _db.Publications.Add(pub);
        await _db.SaveChangesAsync();

        var result = await _sut.FindDuplicatesAsync("Same Title", null, null, id);
        result.ShouldBeEmpty();
    }

    // ─── SearchCrossRefCandidatesAsync ────────────────────────────────────────

    [Test]
    public async Task SearchCrossRefCandidatesAsync_NullInputs_ReturnsEmpty()
    {
        var result = await _sut.SearchCrossRefCandidatesAsync(null, null);
        result.ShouldBeEmpty();
    }

    [Test]
    public async Task SearchCrossRefCandidatesAsync_ByTitle_CallsCrossRefSearch()
    {
        _crossRefClient.Setup(c => c.SearchWorksByTitleAsync("test", 10, default))
            .ReturnsAsync(new List<PublicationCrossRefDto>());

        var result = await _sut.SearchCrossRefCandidatesAsync(null, "test");
        _crossRefClient.Verify(c => c.SearchWorksByTitleAsync("test", 10, default), Times.Once);
    }

    // ─── SearchOpenAireCandidatesAsync ────────────────────────────────────────

    [Test]
    public async Task SearchOpenAireCandidatesAsync_NullInputs_ReturnsEmpty()
    {
        var result = await _sut.SearchOpenAireCandidatesAsync(null, null);
        result.ShouldBeEmpty();
    }

    // ─── DeleteAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteAsync_UserNotAuthor_Fails()
    {
        var result = await _sut.DeleteAsync("non-existing-id");
        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("no tienes permiso") || e.Contains("no encontrado"));
    }

    // ─── GetByIdAsync ─────────────────────────────────────────────────────────

    [Test]
    public async Task GetByIdAsync_NoLinkedAuthor_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync("any-id");
        result.ShouldBeNull();
    }

    [Test]
    public async Task GetByIdAsync_WithJournalPublication_ReturnsJournalPublicationDto()
    {
        var author = new Author { Id = "a-gbi", Name = "L", LastName = "M", SearchKey = "m l", LastNameKey = "m", UserId = "user-1" };
        var pubId = "pub-gbi-j";
        var pub = new Publication
        {
            Id = pubId,
            Title = "Journal Pub GetById",
            NormalizedTitle = "journal pub getbyid",
            PublishedDate = "2023",
            PublicationData = "{}",
            PublicationType = PublicationType.Diario,
            JournalPublication = new JournalPublication
            {
                PublicationId = pubId,
                BaseDeDatos = new BaseDeDatosPublicacion { Nombre = "WoS" },
                Group = 1,
                JournalGroup1Publication = new JournalGroup1Publication { PublicationId = pubId, Cuartil = "Q1" }
            }
        };
        pub.AuthorPublications = new List<AuthorPublication> { new() { AuthorId = author.Id, PublicationId = pubId } };
        _db.Authors.Add(author);
        _db.Publications.Add(pub);
        await _db.SaveChangesAsync();

        var dto = await _sut.GetByIdAsync(pubId);

        dto.ShouldNotBeNull();
        dto!.JournalPublication.ShouldNotBeNull();
        dto.JournalPublication!.DataBase.ShouldBe("WoS");
        dto.JournalPublication.Group.ShouldBe(1);
        dto.JournalPublication.Cuartil.ShouldBe("Q1");
        dto.IndexedPublication.ShouldBeNull();
    }

    [Test]
    public async Task GetByIdAsync_WithIndexedPublication_ReturnsIndexedPublicationDto()
    {
        var author = new Author { Id = "a-gbi2", Name = "N", LastName = "O", SearchKey = "o n", LastNameKey = "o", UserId = "user-1" };
        var pubId = "pub-gbi-i";
        var pub = new Publication
        {
            Id = pubId,
            Title = "Indexed Pub GetById",
            NormalizedTitle = "indexed pub getbyid",
            PublishedDate = "2023",
            PublicationData = "{}",
            PublicationType = PublicationType.Libro,
            IndexedPublication = new IndexedPublication { PublicationId = pubId, Index = 2 }
        };
        pub.AuthorPublications = new List<AuthorPublication> { new() { AuthorId = author.Id, PublicationId = pubId } };
        _db.Authors.Add(author);
        _db.Publications.Add(pub);
        await _db.SaveChangesAsync();

        var dto = await _sut.GetByIdAsync(pubId);

        dto.ShouldNotBeNull();
        dto!.IndexedPublication.ShouldNotBeNull();
        dto.IndexedPublication!.Index.ShouldBe(2);
        dto.JournalPublication.ShouldBeNull();
    }

    [Test]
    public async Task GetPublicByIdAsync_WithJournalPublication_ReturnsDto()
    {
        var pubId = "pub-public-j";
        var pub = new Publication
        {
            Id = pubId,
            Title = "Public Journal",
            NormalizedTitle = "public journal",
            PublishedDate = "2022",
            PublicationData = "{}",
            PublicationType = PublicationType.Diario,
            JournalPublication = new JournalPublication
            {
                PublicationId = pubId,
                BaseDeDatos = new BaseDeDatosPublicacion { Nombre = "Scopus" },
                Group = 2
            }
        };
        _db.Publications.Add(pub);
        await _db.SaveChangesAsync();

        var dto = await _sut.GetPublicByIdAsync(pubId);

        dto.ShouldNotBeNull();
        dto!.JournalPublication.ShouldNotBeNull();
        dto.JournalPublication!.DataBase.ShouldBe("Scopus");
    }

    // ─── AddCurrentUserAsCoauthorAsync ───────────────────────────────────────

    [Test]
    public async Task AddCurrentUserAsCoauthorAsync_AuthorResolutionReturnsNull_Fails()
    {
        _authorResolution.Setup(a => a.GetOrCreateForUserAsync("user-1", default))
            .ReturnsAsync((Author?)null);

        var result = await _sut.AddCurrentUserAsCoauthorAsync("pub-id");
        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("no encontrado"));
    }

    [Test]
    public async Task AddCurrentUserAsCoauthorAsync_PublicationNotFound_Fails()
    {
        var author = new Author { Id = "a1", LastName = "X", Name = "X", SearchKey = "x", LastNameKey = "x" };
        _authorResolution.Setup(a => a.GetOrCreateForUserAsync("user-1", default))
            .ReturnsAsync(author);

        var result = await _sut.AddCurrentUserAsCoauthorAsync("nonexistent-pub");
        result.Succeeded.ShouldBeFalse();
    }

    // ─── CreateAsync Validations ──────────────────────────────────────────────

    [Test]
    public async Task CreateAsync_InvalidPublicationType_Fails()
    {
        var request = new CreatePublicationRequest
        {
            Title = "Test",
            PublishedDate = "2024",
            PublicationType = (PublicationType)999,
            AdditionalAuthorIds = new List<string>(),
            AdditionalAuthorNames = new List<string>(),
            AdditionalUserIds = new List<string>()
        };
        var (result, _) = await _sut.CreateAsync(request);
        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("Tipo") || e.Contains("válido"));
    }

    [Test]
    public async Task CreateAsync_InvalidDate_Fails()
    {
        var request = new CreatePublicationRequest
        {
            Title = "Test",
            PublishedDate = "not-a-date",
            PublicationType = PublicationType.Libro,
            Index = 1,
            AdditionalAuthorIds = new List<string>(),
            AdditionalAuthorNames = new List<string>(),
            AdditionalUserIds = new List<string>()
        };
        var (result, _) = await _sut.CreateAsync(request);
        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("fecha"));
    }

    [Test]
    public async Task CreateAsync_Diario_MissingGroup_Fails()
    {
        var request = new CreatePublicationRequest
        {
            Title = "Test",
            PublishedDate = "2024",
            PublicationType = PublicationType.Diario,
            Group = null,
            AdditionalAuthorIds = new List<string>(),
            AdditionalAuthorNames = new List<string>(),
            AdditionalUserIds = new List<string>()
        };
        var (result, _) = await _sut.CreateAsync(request);
        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("grupo"));
    }

    [Test]
    public async Task CreateAsync_Libro_MissingIndex_Fails()
    {
        var request = new CreatePublicationRequest
        {
            Title = "Test",
            PublishedDate = "2024",
            PublicationType = PublicationType.Libro,
            Index = null,
            AdditionalAuthorIds = new List<string>(),
            AdditionalAuthorNames = new List<string>(),
            AdditionalUserIds = new List<string>()
        };
        var (result, _) = await _sut.CreateAsync(request);
        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("indexaci"));
    }

    [Test]
    public async Task CreateAsync_ArticuloDivulgacion_NoIndexNeeded_AuthorResolutionFails()
    {
        _authorResolution.Setup(a => a.GetOrCreateForUserAsync("user-1", default))
            .ReturnsAsync((Author?)null);

        var request = new CreatePublicationRequest
        {
            Title = "Test",
            PublishedDate = "2024",
            PublicationType = PublicationType.Artículo_de_Divulgación,
            AdditionalAuthorIds = new List<string>(),
            AdditionalAuthorNames = new List<string>(),
            AdditionalUserIds = new List<string>()
        };
        var (result, _) = await _sut.CreateAsync(request);
        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("no encontrado"));
    }

    // ─── GetPublicByIdAsync ───────────────────────────────────────────────────

    [Test]
    public async Task GetPublicByIdAsync_NotFound_ReturnsNull()
    {
        var result = await _sut.GetPublicByIdAsync("nonexistent-id");
        result.ShouldBeNull();
    }

    [Test]
    public async Task GetPublicByIdAsync_Found_ReturnsDtoWithTitle()
    {
        var pubId = Guid.NewGuid().ToString();
        _db.Publications.Add(new Publication
        {
            Id = pubId,
            Title = "Pub Pública",
            NormalizedTitle = "pub publica",
            PublishedDate = "2023",
            PublicationData = "{}",
            AuthorPublications = new List<AuthorPublication>()
        });
        await _db.SaveChangesAsync();

        var result = await _sut.GetPublicByIdAsync(pubId);
        result.ShouldNotBeNull();
        result!.Title.ShouldBe("Pub Pública");
    }

    // ─── GetAllPublicationsAsync (with data) ──────────────────────────────────

    [Test]
    public async Task GetAllPublicationsAsync_WithData_ReturnsOrdered()
    {
        _db.Publications.AddRange(
            new Publication { Id = "p1", Title = "Zebra", NormalizedTitle = "zebra", PublishedDate = "2022", PublicationData = "{}", AuthorPublications = new List<AuthorPublication>() },
            new Publication { Id = "p2", Title = "Alpha", NormalizedTitle = "alpha", PublishedDate = "2022", PublicationData = "{}", AuthorPublications = new List<AuthorPublication>() }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllPublicationsAsync();
        result.Count.ShouldBe(2);
        result[0].Title.ShouldBe("Alpha");
    }

    // ─── UpdateAsync ──────────────────────────────────────────────────────────

    private async Task<(Author author, Publication pub)> SeedAuthorAndPublicationAsync(
        PublicationType pubType = PublicationType.Libro, int? index = 1)
    {
        var author = new Author { Id = "a-upd", Name = "Ana", LastName = "García", SearchKey = "garcia ana", LastNameKey = "garcia" };
        var pubId = "pub-upd-1";
        var pub = new Publication
        {
            Id = pubId,
            Title = "Pub Original",
            NormalizedTitle = "pub original",
            PublishedDate = "2023",
            PublicationData = "{}",
            PublicationType = pubType,
            AuthorPublications = new List<AuthorPublication>
            {
                new() { AuthorId = author.Id, PublicationId = pubId }
            }
        };
        if (index.HasValue)
            pub.IndexedPublication = new IndexedPublication { PublicationId = pubId, Index = index };

        _db.Authors.Add(author);
        _db.Publications.Add(pub);
        await _db.SaveChangesAsync();

        _authorResolution.Setup(a => a.GetOrCreateForUserAsync("user-1", default)).ReturnsAsync(author);
        _authorCleanup.Setup(c => c.CleanupIfOrphanedAsync(It.IsAny<List<string>>(), default)).Returns(Task.CompletedTask);
        return (author, pub);
    }

    [Test]
    public async Task UpdateAsync_AuthorResolutionFails_ReturnsFailure()
    {
        _authorResolution.Setup(a => a.GetOrCreateForUserAsync("user-1", default)).ReturnsAsync((Author?)null);

        var result = await _sut.UpdateAsync(new UpdatePublicationRequest
        {
            Id = "any",
            Title = "T",
            PublicationData = "{}",
            PublicationType = PublicationType.Libro,
            PublishedDate = "2024",
            Index = 1
        });

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("permiso") || e.Contains("no encontrada"));
    }

    [Test]
    public async Task UpdateAsync_NotAuthor_ReturnsFailure()
    {
        var author = new Author { Id = "a-other", Name = "Z", LastName = "Z", SearchKey = "z z", LastNameKey = "z" };
        _authorResolution.Setup(a => a.GetOrCreateForUserAsync("user-1", default)).ReturnsAsync(author);

        // Publication exists but NOT linked to this author
        var pub = new Publication
        {
            Id = "pub-x",
            Title = "X",
            NormalizedTitle = "x",
            PublishedDate = "2023",
            PublicationData = "{}",
            AuthorPublications = new List<AuthorPublication>()
        };
        _db.Publications.Add(pub);
        await _db.SaveChangesAsync();

        var result = await _sut.UpdateAsync(new UpdatePublicationRequest
        {
            Id = "pub-x",
            Title = "Updated",
            PublicationData = "{}",
            PublicationType = PublicationType.Libro,
            PublishedDate = "2024",
            Index = 1
        });

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("permiso") || e.Contains("no encontrada"));
    }

    [Test]
    public async Task UpdateAsync_InvalidPublicationType_ReturnsFailure()
    {
        await SeedAuthorAndPublicationAsync();

        var result = await _sut.UpdateAsync(new UpdatePublicationRequest
        {
            Id = "pub-upd-1",
            Title = "T",
            PublicationData = "{}",
            PublicationType = (PublicationType)9999,
            PublishedDate = "2024"
        });

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("válido"));
    }

    [Test]
    public async Task UpdateAsync_InvalidDate_ReturnsFailure()
    {
        await SeedAuthorAndPublicationAsync();

        var result = await _sut.UpdateAsync(new UpdatePublicationRequest
        {
            Id = "pub-upd-1",
            Title = "T",
            PublicationData = "{}",
            PublicationType = PublicationType.Libro,
            PublishedDate = "invalid-date"
        });

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("fecha"));
    }

    [Test]
    public async Task UpdateAsync_Diario_MissingDatabase_ReturnsFailure()
    {
        await SeedAuthorAndPublicationAsync();

        var result = await _sut.UpdateAsync(new UpdatePublicationRequest
        {
            Id = "pub-upd-1",
            Title = "T",
            PublicationData = "{}",
            PublicationType = PublicationType.Diario,
            PublishedDate = "2024",
            DataBase = null,
            Group = null
        });

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("base de datos") || e.Contains("grupo") || e.Contains("Datos"));
    }

    [Test]
    public async Task UpdateAsync_NonJournalNonDivulgacion_MissingIndex_ReturnsFailure()
    {
        await SeedAuthorAndPublicationAsync();

        var result = await _sut.UpdateAsync(new UpdatePublicationRequest
        {
            Id = "pub-upd-1",
            Title = "T",
            PublicationData = "{}",
            PublicationType = PublicationType.Libro,
            PublishedDate = "2024",
            Index = null
        });

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("indexaci"));
    }

    [Test]
    public async Task UpdateAsync_Libro_Valid_Succeeds()
    {
        await SeedAuthorAndPublicationAsync();

        var result = await _sut.UpdateAsync(new UpdatePublicationRequest
        {
            Id = "pub-upd-1",
            Title = "Pub Actualizada",
            PublicationData = "{}",
            PublicationType = PublicationType.Libro,
            PublishedDate = "2024",
            Index = 2,
            AdditionalAuthorIds = new List<string>(),
            AdditionalAuthorNames = new List<string>(),
            AdditionalUserIds = new List<string>()
        });

        result.Succeeded.ShouldBeTrue();
        var updated = await _db.Publications.FindAsync("pub-upd-1");
        updated!.Title.ShouldBe("Pub Actualizada");
    }

    [Test]
    public async Task UpdateAsync_Diario_Valid_Succeeds()
    {
        // Seed a Journal publication
        var author = new Author { Id = "a-j", Name = "B", LastName = "C", SearchKey = "c b", LastNameKey = "c" };
        var pubId = "pub-j-1";
        var pub = new Publication
        {
            Id = pubId,
            Title = "Journal Pub",
            NormalizedTitle = "journal pub",
            PublishedDate = "2023",
            PublicationData = "{}",
            PublicationType = PublicationType.Diario,
            AuthorPublications = new List<AuthorPublication>
            {
                new() { AuthorId = author.Id, PublicationId = pubId }
            },
            JournalPublication = new JournalPublication
            {
                PublicationId = pubId,
                BaseDeDatos = new BaseDeDatosPublicacion { Nombre = "Scopus" },
                Group = 2
            }
        };
        _db.Authors.Add(author);
        _db.Publications.Add(pub);
        await _db.SaveChangesAsync();

        _authorResolution.Setup(a => a.GetOrCreateForUserAsync("user-1", default)).ReturnsAsync(author);
        _authorCleanup.Setup(c => c.CleanupIfOrphanedAsync(It.IsAny<List<string>>(), default)).Returns(Task.CompletedTask);

        var result = await _sut.UpdateAsync(new UpdatePublicationRequest
        {
            Id = pubId,
            Title = "Journal Pub Updated",
            PublicationData = "{}",
            PublicationType = PublicationType.Diario,
            PublishedDate = "2024",
            DataBase = "WoS",
            Group = 3,
            AdditionalAuthorIds = new List<string>(),
            AdditionalAuthorNames = new List<string>(),
            AdditionalUserIds = new List<string>()
        });

        result.Succeeded.ShouldBeTrue();
        var updated = await _db.Publications.FindAsync(pubId);
        updated!.Title.ShouldBe("Journal Pub Updated");
    }

    // ─── CreateAsync success paths ────────────────────────────────────────────

    [Test]
    public async Task CreateAsync_Libro_Valid_Succeeds()
    {
        var author = new Author { Id = "a-create", Name = "P", LastName = "Q", SearchKey = "q p", LastNameKey = "q" };
        _authorResolution.Setup(a => a.GetOrCreateForUserAsync("user-1", default)).ReturnsAsync(author);

        var request = new CreatePublicationRequest
        {
            Title = "Libro Creado",
            PublishedDate = "2024",
            PublicationType = PublicationType.Libro,
            Index = 1,
            PublicationData = "{}",
            AdditionalAuthorIds = new List<string>(),
            AdditionalAuthorNames = new List<string>(),
            AdditionalUserIds = new List<string>()
        };

        var (result, id) = await _sut.CreateAsync(request);

        result.Succeeded.ShouldBeTrue();
        id.ShouldNotBeNullOrWhiteSpace();
        var created = await _db.Publications.FindAsync(id);
        created!.Title.ShouldBe("Libro Creado");
    }

    [Test]
    public async Task CreateAsync_Diario_Valid_Succeeds()
    {
        var author = new Author { Id = "a-diario", Name = "M", LastName = "N", SearchKey = "n m", LastNameKey = "n" };
        _authorResolution.Setup(a => a.GetOrCreateForUserAsync("user-1", default)).ReturnsAsync(author);

        var request = new CreatePublicationRequest
        {
            Title = "Revista Científica",
            PublishedDate = "2024",
            PublicationType = PublicationType.Diario,
            Group = 2,
            DataBase = "Scopus",
            PublicationData = "{}",
            AdditionalAuthorIds = new List<string>(),
            AdditionalAuthorNames = new List<string>(),
            AdditionalUserIds = new List<string>()
        };

        var (result, id) = await _sut.CreateAsync(request);

        result.Succeeded.ShouldBeTrue();
        id.ShouldNotBeNullOrWhiteSpace();
        var journal = await _db.JournalPublications.FindAsync(id);
        journal.ShouldNotBeNull();
        journal!.Group.ShouldBe(2);
    }

    [Test]
    public async Task CreateAsync_Divulgacion_Valid_Succeeds()
    {
        var author = new Author { Id = "a-div", Name = "X", LastName = "Y", SearchKey = "y x", LastNameKey = "y" };
        _authorResolution.Setup(a => a.GetOrCreateForUserAsync("user-1", default)).ReturnsAsync(author);

        var request = new CreatePublicationRequest
        {
            Title = "Artículo de Divulgación",
            PublishedDate = "2024-05",
            PublicationType = PublicationType.Artículo_de_Divulgación,
            Index = 1,
            PublicationData = "{}",
            AdditionalAuthorIds = new List<string>(),
            AdditionalAuthorNames = new List<string>(),
            AdditionalUserIds = new List<string>()
        };

        var (result, id) = await _sut.CreateAsync(request);

        result.Succeeded.ShouldBeTrue();
        id.ShouldNotBeNullOrWhiteSpace();
    }

    // ─── GetMyPublicationsAsync (with data) ───────────────────────────────────

    [Test]
    public async Task GetMyPublicationsAsync_WithLinkedAuthorAndPublications_ReturnsOrdered()
    {
        var author = new Author { Id = "a-my", Name = "Julio", LastName = "Pérez", SearchKey = "perez julio", LastNameKey = "perez", UserId = "user-1" };
        var pub1 = new Publication { Id = "p-my-1", Title = "Zebra Pub", NormalizedTitle = "zebra pub", PublishedDate = "2023", PublicationData = "{}", AuthorPublications = new List<AuthorPublication> { new() { AuthorId = "a-my", PublicationId = "p-my-1" } } };
        var pub2 = new Publication { Id = "p-my-2", Title = "Alpha Pub", NormalizedTitle = "alpha pub", PublishedDate = "2022", PublicationData = "{}", AuthorPublications = new List<AuthorPublication> { new() { AuthorId = "a-my", PublicationId = "p-my-2" } } };
        _db.Authors.Add(author);
        _db.Publications.AddRange(pub1, pub2);
        await _db.SaveChangesAsync();

        var result = await _sut.GetMyPublicationsAsync();
        result.Count.ShouldBe(2);
        result[0].Title.ShouldBe("Alpha Pub");
    }

    // ─── GetAreaPublicationsAsync (with data) ─────────────────────────────────

    [Test]
    public async Task GetAreaPublicationsAsync_UserWithArea_ReturnsPublicationsInArea()
    {
        var area = new Dashboard_v2.Domain.Entities.AreaDelConocimiento { Id = "area-1", Nombre = "CS" };
        var user = new Dashboard_v2.Domain.Entities.User { Id = "user-1", UserName = "u1", Email = "u1@test.com", UserLastName1 = "Pérez", AreaId = "area-1", ScientificCategory = Dashboard_v2.Domain.Entities.ScientificCategory.None, TeachingCategory = Dashboard_v2.Domain.Entities.TeachingCategory.None, InvestigationCategory = Dashboard_v2.Domain.Entities.InvestigationCategory.None };
        var author = new Author { Id = "a-area", Name = "T", LastName = "U", SearchKey = "u t", LastNameKey = "u", UserId = "user-1", User = user };
        var pub = new Publication { Id = "p-area-1", Title = "Area Pub", NormalizedTitle = "area pub", PublishedDate = "2023", PublicationData = "{}", AuthorPublications = new List<AuthorPublication> { new() { AuthorId = "a-area", PublicationId = "p-area-1" } } };
        _db.AreasDelConocimiento.Add(area);
        _db.Users.Add(user);
        _db.Authors.Add(author);
        _db.Publications.Add(pub);
        await _db.SaveChangesAsync();

        var result = await _sut.GetAreaPublicationsAsync();
        result.Count.ShouldBe(1);
        result[0].Title.ShouldBe("Area Pub");
    }

    // ─── AddCurrentUserAsCoauthorAsync ────────────────────────────────────────

    [Test]
    public async Task AddCurrentUserAsCoauthorAsync_AuthorNotFound_Fails()
    {
        _authorResolution.Setup(a => a.GetOrCreateForUserAsync("user-1", default)).ReturnsAsync((Author?)null);
        var result = await _sut.AddCurrentUserAsCoauthorAsync("any-pub");
        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("no encontrado"));
    }

    [Test]
    public async Task AddCurrentUserAsCoauthorAsync_AlreadyCoauthor_ReturnsSuccess()
    {
        var author = new Author { Id = "a-co", Name = "A", LastName = "B", SearchKey = "b a", LastNameKey = "b" };
        _db.Authors.Add(author);
        _db.AuthorPublications.Add(new AuthorPublication { AuthorId = "a-co", PublicationId = "pub-co" });
        await _db.SaveChangesAsync();

        _authorResolution.Setup(a => a.GetOrCreateForUserAsync("user-1", default)).ReturnsAsync(author);
        var result = await _sut.AddCurrentUserAsCoauthorAsync("pub-co");
        result.Succeeded.ShouldBeTrue();
    }
    [Test]
    public async Task AddCurrentUserAsCoauthorAsync_ValidNewCoauthor_Succeeds()
    {
        var author = new Author { Id = "a-co3", Name = "A", LastName = "B", SearchKey = "b a", LastNameKey = "b" };
        var pub = new Publication { Id = "pub-new-co", Title = "X", NormalizedTitle = "x", PublishedDate = "2024", PublicationData = "{}", AuthorPublications = new List<AuthorPublication>() };
        _db.Authors.Add(author);
        _db.Publications.Add(pub);
        await _db.SaveChangesAsync();

        _authorResolution.Setup(a => a.GetOrCreateForUserAsync("user-1", default)).ReturnsAsync(author);
        var result = await _sut.AddCurrentUserAsCoauthorAsync("pub-new-co");
        result.Succeeded.ShouldBeTrue();
        var link = await _db.AuthorPublications.AnyAsync(ap => ap.AuthorId == "a-co3" && ap.PublicationId == "pub-new-co");
        link.ShouldBeTrue();
    }

    // ─── DeleteAsync (success path) ───────────────────────────────────────────

    [Test]
    public async Task DeleteAsync_IsAuthor_Succeeds()
    {
        var author = new Author { Id = "a-del", Name = "D", LastName = "E", SearchKey = "e d", LastNameKey = "e", UserId = "user-1" };
        var user = new Dashboard_v2.Domain.Entities.User { Id = "user-1", UserName = "u1", Email = "u1@test.com", UserLastName1 = "Pérez", ScientificCategory = Dashboard_v2.Domain.Entities.ScientificCategory.None, TeachingCategory = Dashboard_v2.Domain.Entities.TeachingCategory.None, InvestigationCategory = Dashboard_v2.Domain.Entities.InvestigationCategory.None };
        var pub = new Publication { Id = "pub-del-1", Title = "Del Pub", NormalizedTitle = "del pub", PublishedDate = "2024", PublicationData = "{}", AuthorPublications = new List<AuthorPublication> { new() { AuthorId = "a-del", PublicationId = "pub-del-1" } } };
        _db.Users.Add(user);
        _db.Authors.Add(author);
        _db.Publications.Add(pub);
        await _db.SaveChangesAsync();

        _authorCleanup.Setup(c => c.CleanupIfOrphanedAsync(It.IsAny<List<string>>(), default)).Returns(Task.CompletedTask);

        var result = await _sut.DeleteAsync("pub-del-1");
        result.Succeeded.ShouldBeTrue();
        (await _db.Publications.FindAsync("pub-del-1")).ShouldBeNull();
    }

    // ─── GetMyRedPublicationsAsync ────────────────────────────────────────────

    [Test]
    public async Task GetMyRedPublicationsAsync_JefeDeRedes_ReturnsAllRedPublications()
    {
        _currentUser.Setup(u => u.Roles).Returns(new List<string> { "Jefe_de_Redes" });
        _sut = new PublicationService(_db, _currentUser.Object, _crossRefClient.Object,
            _openAireClient.Object, _authorResolution.Object, _authorCleanup.Object);

        _db.Publications.Add(new Publication { Id = "p-red-1", Title = "Red Pub", NormalizedTitle = "red pub", PublishedDate = "2024", PublicationData = "{}", RedId = "red-1", AuthorPublications = new List<AuthorPublication>() });
        _db.Publications.Add(new Publication { Id = "p-no-red", Title = "No Red", NormalizedTitle = "no red", PublishedDate = "2024", PublicationData = "{}", AuthorPublications = new List<AuthorPublication>() });
        await _db.SaveChangesAsync();

        var result = await _sut.GetMyRedPublicationsAsync();
        result.Count.ShouldBe(1);
        result[0].Title.ShouldBe("Red Pub");
    }

    [Test]
    public async Task GetMyRedPublicationsAsync_Coordinador_WithReds_ReturnsFiltered()
    {
        _db.RedesCoordinadas.Add(new Dashboard_v2.Domain.Entities.RedCoordinada { CoordinadorId = "user-1", RedId = "red-2", AreaId = "area-1" });
        _db.Publications.Add(new Publication { Id = "p-red-2", Title = "Red2 Pub", NormalizedTitle = "red2 pub", PublishedDate = "2024", PublicationData = "{}", RedId = "red-2", AuthorPublications = new List<AuthorPublication>() });
        _db.Publications.Add(new Publication { Id = "p-red-3", Title = "Red3 Pub", NormalizedTitle = "red3 pub", PublishedDate = "2024", PublicationData = "{}", RedId = "red-3", AuthorPublications = new List<AuthorPublication>() });
        await _db.SaveChangesAsync();

        var result = await _sut.GetMyRedPublicationsAsync();
        result.Count.ShouldBe(1);
        result[0].Title.ShouldBe("Red2 Pub");
    }

    [Test]
    public async Task GetMyRedPublicationsAsync_Coordinador_NoReds_ReturnsEmpty()
    {
        var result = await _sut.GetMyRedPublicationsAsync();
        result.ShouldBeEmpty();
    }

    // ─── SearchOpenAireCandidatesAsync (doi and title) ────────────────────────

    [Test]
    public async Task SearchOpenAireCandidatesAsync_ByDoi_ReturnsSingle()
    {
        var candidate = new PublicationCrossRefDto { Title = "OA Pub" };
        _openAireClient.Setup(c => c.GetWorkByDoiAsync("10.9999/x", default)).ReturnsAsync(candidate);

        var result = await _sut.SearchOpenAireCandidatesAsync("10.9999/x", null);
        result.Count.ShouldBe(1);
    }

    [Test]
    public async Task SearchOpenAireCandidatesAsync_ByTitle_ReturnsAll()
    {
        var candidates = new List<PublicationCrossRefDto> { new() { Title = "OA A" }, new() { Title = "OA B" } };
        _openAireClient.Setup(c => c.SearchWorksByTitleAsync("oa search", 10, default)).ReturnsAsync(candidates);

        var result = await _sut.SearchOpenAireCandidatesAsync(null, "oa search");
        result.Count.ShouldBe(2);
    }

    // ─── UpdateAsync – coauthor paths ─────────────────────────────────────────

    [Test]
    public async Task UpdateAsync_WithCoauthorByAuthorId_AddsCoauthor()
    {
        await SeedAuthorAndPublicationAsync();
        var coAuthor = new Author { Id = "co-id-1", Name = "Co", LastName = "Author", SearchKey = "author co", LastNameKey = "author" };
        _db.Authors.Add(coAuthor);
        await _db.SaveChangesAsync();

        var result = await _sut.UpdateAsync(new UpdatePublicationRequest
        {
            Id = "pub-upd-1",
            Title = "Updated Co",
            PublicationData = "{}",
            PublicationType = PublicationType.Libro,
            PublishedDate = "2024",
            Index = 1,
            AdditionalAuthorIds = new List<string> { "co-id-1" },
            AdditionalAuthorNames = new List<string>(),
            AdditionalUserIds = new List<string>()
        });

        result.Succeeded.ShouldBeTrue();
        var links = await _db.AuthorPublications.Where(ap => ap.PublicationId == "pub-upd-1").ToListAsync();
        links.Any(ap => ap.AuthorId == "co-id-1").ShouldBeTrue();
    }

    [Test]
    public async Task UpdateAsync_WithCoauthorByName_AddsCoauthor()
    {
        await SeedAuthorAndPublicationAsync();
        var coAuthor = new Author { Id = "co-name-1", Name = "Named", LastName = "Co", SearchKey = "co named", LastNameKey = "co" };
        _authorResolution.Setup(a => a.ResolveByNameAsync("Named Co", default)).ReturnsAsync(coAuthor);

        var result = await _sut.UpdateAsync(new UpdatePublicationRequest
        {
            Id = "pub-upd-1",
            Title = "Updated Named Co",
            PublicationData = "{}",
            PublicationType = PublicationType.Libro,
            PublishedDate = "2024",
            Index = 1,
            AdditionalAuthorIds = new List<string>(),
            AdditionalAuthorNames = new List<string> { "Named Co" },
            AdditionalUserIds = new List<string>()
        });

        result.Succeeded.ShouldBeTrue();
    }

    [Test]
    public async Task UpdateAsync_WithCoauthorByUserId_AddsCoauthor()
    {
        await SeedAuthorAndPublicationAsync();
        var coAuthor = new Author { Id = "co-user-1", Name = "User", LastName = "Co", SearchKey = "co user", LastNameKey = "co" };
        _authorResolution.Setup(a => a.GetOrCreateForUserAsync("user-2", default)).ReturnsAsync(coAuthor);

        var result = await _sut.UpdateAsync(new UpdatePublicationRequest
        {
            Id = "pub-upd-1",
            Title = "Updated User Co",
            PublicationData = "{}",
            PublicationType = PublicationType.Libro,
            PublishedDate = "2024",
            Index = 1,
            AdditionalAuthorIds = new List<string>(),
            AdditionalAuthorNames = new List<string>(),
            AdditionalUserIds = new List<string> { "user-2" }
        });

        result.Succeeded.ShouldBeTrue();
    }

    [Test]
    public async Task UpdateAsync_TransitionFromJournalToLibro_RemovesJournalAndAddsIndex()
    {
        var author = new Author { Id = "a-tr", Name = "T", LastName = "R", SearchKey = "r t", LastNameKey = "r" };
        var pubId = "pub-tr-1";
        var pub = new Publication
        {
            Id = pubId,
            Title = "Journal To Libro",
            NormalizedTitle = "journal to libro",
            PublishedDate = "2023",
            PublicationData = "{}",
            PublicationType = PublicationType.Diario,
            AuthorPublications = new List<AuthorPublication> { new() { AuthorId = "a-tr", PublicationId = pubId } },
            JournalPublication = new JournalPublication { PublicationId = pubId, BaseDeDatos = new BaseDeDatosPublicacion { Nombre = "Scopus" }, Group = 2 }
        };
        _db.Authors.Add(author);
        _db.Publications.Add(pub);
        await _db.SaveChangesAsync();

        _authorResolution.Setup(a => a.GetOrCreateForUserAsync("user-1", default)).ReturnsAsync(author);
        _authorCleanup.Setup(c => c.CleanupIfOrphanedAsync(It.IsAny<List<string>>(), default)).Returns(Task.CompletedTask);

        var result = await _sut.UpdateAsync(new UpdatePublicationRequest
        {
            Id = pubId,
            Title = "Now Libro",
            PublicationData = "{}",
            PublicationType = PublicationType.Libro,
            PublishedDate = "2024",
            Index = 1,
            AdditionalAuthorIds = new List<string>(),
            AdditionalAuthorNames = new List<string>(),
            AdditionalUserIds = new List<string>()
        });

        result.Succeeded.ShouldBeTrue();
        var updated = await _db.Publications.Include(p => p.JournalPublication).Include(p => p.IndexedPublication).FirstAsync(p => p.Id == pubId);
        updated.JournalPublication.ShouldBeNull();
        updated.IndexedPublication.ShouldNotBeNull();
    }

    [Test]
    public async Task UpdateAsync_TransitionFromLibroToJournal_RemovesIndexAndAddsJournal()
    {
        await SeedAuthorAndPublicationAsync(PublicationType.Libro, index: 1);

        var result = await _sut.UpdateAsync(new UpdatePublicationRequest
        {
            Id = "pub-upd-1",
            Title = "Now Diario",
            PublicationData = "{}",
            PublicationType = PublicationType.Diario,
            PublishedDate = "2024",
            DataBase = "Scopus",
            Group = 2,
            AdditionalAuthorIds = new List<string>(),
            AdditionalAuthorNames = new List<string>(),
            AdditionalUserIds = new List<string>()
        });

        result.Succeeded.ShouldBeTrue();
        var updated = await _db.Publications.Include(p => p.JournalPublication).Include(p => p.IndexedPublication).FirstAsync(p => p.Id == "pub-upd-1");
        updated.IndexedPublication.ShouldBeNull();
        updated.JournalPublication.ShouldNotBeNull();
    }

    [Test]
    public async Task UpdateAsync_DiarioGroup1_CreatesJournalGroup1Publication()
    {
        var author = new Author { Id = "a-g1", Name = "G", LastName = "1", SearchKey = "1 g", LastNameKey = "1" };
        var pubId = "pub-g1-1";
        var pub = new Publication
        {
            Id = pubId,
            Title = "Group1 Journal",
            NormalizedTitle = "group1 journal",
            PublishedDate = "2023",
            PublicationData = "{}",
            PublicationType = PublicationType.Diario,
            AuthorPublications = new List<AuthorPublication> { new() { AuthorId = "a-g1", PublicationId = pubId } },
            JournalPublication = new JournalPublication { PublicationId = pubId, BaseDeDatos = new BaseDeDatosPublicacion { Nombre = "WoS" }, Group = 2 }
        };
        _db.Authors.Add(author);
        _db.Publications.Add(pub);
        await _db.SaveChangesAsync();

        _authorResolution.Setup(a => a.GetOrCreateForUserAsync("user-1", default)).ReturnsAsync(author);
        _authorCleanup.Setup(c => c.CleanupIfOrphanedAsync(It.IsAny<List<string>>(), default)).Returns(Task.CompletedTask);

        var result = await _sut.UpdateAsync(new UpdatePublicationRequest
        {
            Id = pubId,
            Title = "Group1 Journal Updated",
            PublicationData = "{}",
            PublicationType = PublicationType.Diario,
            PublishedDate = "2024",
            DataBase = "WoS",
            Group = 1,
            Cuartil = "Q1",
            AdditionalAuthorIds = new List<string>(),
            AdditionalAuthorNames = new List<string>(),
            AdditionalUserIds = new List<string>()
        });

        result.Succeeded.ShouldBeTrue();
        var updated = await _db.Publications.Include(p => p.JournalPublication!).ThenInclude(jp => jp!.JournalGroup1Publication).FirstAsync(p => p.Id == pubId);
        updated.JournalPublication!.JournalGroup1Publication.ShouldNotBeNull();
        updated.JournalPublication.JournalGroup1Publication!.Cuartil.ShouldBe("Q1");
    }

    // ─── Superuser bypass ────────────────────────────────────────────────────

    private void SetupSuperuser()
    {
        _currentUser.Setup(u => u.Id).Returns("super-1");
        _currentUser.Setup(u => u.Roles).Returns(new List<string> { "Superuser" });
    }

    private Author SeedAuthorForUser(string userId)
    {
        var author = new Author
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            LastName = userId,
            Name = userId,
            SearchKey = userId.ToLowerInvariant(),
            LastNameKey = userId.ToLowerInvariant(),
        };
        _db.Authors.Add(author);
        return author;
    }

    private Publication SeedPublicationForAuthor(Author author)
    {
        var pub = new Publication
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Test Pub",
            NormalizedTitle = "test pub",
            PublicationData = "{}",
            PublicationType = PublicationType.Artículo_de_Divulgación,
            PublishedDate = "2024",
            AuthorPublications = new List<AuthorPublication> { new() { AuthorId = author.Id } },
            IndexedPublication = new IndexedPublication { Index = 1 }
        };
        _db.Publications.Add(pub);
        return pub;
    }

    [Test]
    public async Task GetMyPublicationsAsync_Superuser_ReturnsAllPublications()
    {
        var authorA = SeedAuthorForUser("user-a");
        var authorB = SeedAuthorForUser("user-b");
        SeedPublicationForAuthor(authorA);
        SeedPublicationForAuthor(authorB);
        await _db.SaveChangesAsync();

        SetupSuperuser();
        var result = await _sut.GetMyPublicationsAsync();

        result.Count.ShouldBe(2);
    }

    [Test]
    public async Task GetByIdAsync_Superuser_ReturnsPublicationRegardlessOfAuthorship()
    {
        var authorA = SeedAuthorForUser("user-a");
        var pub = SeedPublicationForAuthor(authorA);
        await _db.SaveChangesAsync();

        SetupSuperuser();
        var result = await _sut.GetByIdAsync(pub.Id);

        result.ShouldNotBeNull();
        result!.Id.ShouldBe(pub.Id);
    }

    [Test]
    public async Task CreateAsync_Superuser_WithTargetUserId_CreatesPublicationForTargetUser()
    {
        var targetAuthor = new Author
        {
            Id = "author-target",
            UserId = "user-target",
            LastName = "Target",
            Name = "Target",
            SearchKey = "target",
            LastNameKey = "target",
        };
        _db.Authors.Add(targetAuthor);
        await _db.SaveChangesAsync();

        _authorResolution
            .Setup(r => r.GetOrCreateForUserAsync("user-target", It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetAuthor);

        SetupSuperuser();
        var (result, id) = await _sut.CreateAsync(new CreatePublicationRequest
        {
            Title = "Pub del Superuser",
            PublicationData = "{}",
            PublicationType = PublicationType.Artículo_de_Divulgación,
            PublishedDate = "2024",
            Index = 1,
            TargetUserId = "user-target"
        });

        result.Succeeded.ShouldBeTrue();
        var pub = await _db.Publications.Include(p => p.AuthorPublications).FirstAsync(p => p.Id == id);
        pub.AuthorPublications.ShouldContain(ap => ap.AuthorId == "author-target");
    }

    [Test]
    public async Task CreateAsync_Superuser_WithoutTargetUserId_ReturnsFailure()
    {
        SetupSuperuser();

        var (result, _) = await _sut.CreateAsync(new CreatePublicationRequest
        {
            Title = "Pub Sin Target",
            PublicationData = "{}",
            PublicationType = PublicationType.Artículo_de_Divulgación,
            PublishedDate = "2024"
        });

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("TargetUserId"));
    }

    [Test]
    public async Task UpdateAsync_Superuser_CanUpdatePublicationWithoutBeingAuthor()
    {
        var authorA = SeedAuthorForUser("user-a");
        var pub = SeedPublicationForAuthor(authorA);
        await _db.SaveChangesAsync();

        SetupSuperuser();
        var result = await _sut.UpdateAsync(new UpdatePublicationRequest
        {
            Id = pub.Id,
            Title = "Updated by Superuser",
            PublicationData = "{}",
            PublicationType = PublicationType.Artículo_de_Divulgación,
            PublishedDate = "2025",
            AdditionalAuthorIds = [],
            AdditionalAuthorNames = [],
            AdditionalUserIds = []
        });

        result.Succeeded.ShouldBeTrue();
        (await _db.Publications.FindAsync(pub.Id))!.Title.ShouldBe("Updated by Superuser");
    }

    [Test]
    public async Task DeleteAsync_Superuser_CanDeletePublicationWithoutBeingAuthor()
    {
        var authorA = SeedAuthorForUser("user-a");
        var pub = SeedPublicationForAuthor(authorA);
        await _db.SaveChangesAsync();

        _authorCleanup
            .Setup(c => c.CleanupIfOrphanedAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        SetupSuperuser();
        var result = await _sut.DeleteAsync(pub.Id);

        result.Succeeded.ShouldBeTrue();
        (await _db.Publications.FindAsync(pub.Id)).ShouldBeNull();
    }
}
