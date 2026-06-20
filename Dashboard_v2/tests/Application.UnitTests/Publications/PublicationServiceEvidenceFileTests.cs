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

/// <summary>
/// Tests unitarios para la funcionalidad de evidencia/certificado en <see cref="PublicationService"/>.
/// </summary>
[TestFixture]
public class PublicationServiceEvidenceFileTests
{
    private static ApplicationDbContext BuildDb()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(opts);
    }

    private static PublicationService BuildService(
        ApplicationDbContext db,
        string userId,
        Author author)
    {
        var currentUser = new Mock<IUser>();
        currentUser.Setup(u => u.Id).Returns(userId);

        var authorResolution = new Mock<IAuthorResolutionService>();
        authorResolution
            .Setup(x => x.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var authorCleanup = new Mock<IAuthorCleanupService>();

        return new PublicationService(
            db,
            currentUser.Object,
            new Mock<ICrossRefClient>().Object,
            new Mock<IOpenAireClient>().Object,
            authorResolution.Object,
            authorCleanup.Object);
    }

    [Test]
    public async Task CreateAsync_WithEvidenceFileId_PersistsEvidenceFileId()
    {
        await using var db = BuildDb();

        var user = new User { Id = "u1", UserName = "rosa", UserLastName1 = "Pérez", Email = "rosa@test.cu" };
        var author = new Author { Id = "a1", Name = "Rosa", LastName = "Pérez", LastNameKey = "perez", SearchKey = "perez rosa", UserId = "u1" };
        var storedFile = new StoredFile
        {
            FileName = "articulo.pdf",
            ObjectKey = "publicaciones/art.pdf",
            BucketName = "dashboard-documents",
            ContentType = "application/pdf",
            SizeBytes = 4096,
            UploadedById = "u1",
        };

        db.Users.Add(user);
        db.Authors.Add(author);
        db.StoredFiles.Add(storedFile);
        await db.SaveChangesAsync();

        var service = BuildService(db, user.Id, author);
        var request = new CreatePublicationRequest
        {
            Title = "Inteligencia Artificial en la Medicina",
            PublicationData = "Journal of AI Medicine",
            PublicationType = PublicationType.Libro,
            PublishedDate = "2024",
            Index = 1,
            EvidenceFileId = storedFile.Id,
        };

        var (result, publicationId) = await service.CreateAsync(request);

        result.Succeeded.ShouldBeTrue();
        publicationId.ShouldNotBeNull();

        var created = await db.Publications.FindAsync(publicationId);
        created.ShouldNotBeNull();
        created!.EvidenceFileId.ShouldBe(storedFile.Id);
    }

    [Test]
    public async Task CreateAsync_WithoutEvidenceFileId_PersistsNull()
    {
        await using var db = BuildDb();

        var user = new User { Id = "u2", UserName = "omar", UserLastName1 = "Cruz", Email = "omar@test.cu" };
        var author = new Author { Id = "a2", Name = "Omar", LastName = "Cruz", LastNameKey = "cruz", SearchKey = "cruz omar", UserId = "u2" };

        db.Users.Add(user);
        db.Authors.Add(author);
        await db.SaveChangesAsync();

        var service = BuildService(db, user.Id, author);
        var request = new CreatePublicationRequest
        {
            Title = "Redes Neuronales",
            PublicationData = "Conferencia 2024",
            PublicationType = PublicationType.Libro,
            PublishedDate = "2024-05",
            Index = 2,
            EvidenceFileId = null,
        };

        var (result, publicationId) = await service.CreateAsync(request);

        result.Succeeded.ShouldBeTrue();
        var created = await db.Publications.FindAsync(publicationId);
        created!.EvidenceFileId.ShouldBeNull();
    }

    [Test]
    public async Task UpdateAsync_SetsEvidenceFileId()
    {
        await using var db = BuildDb();

        var user = new User { Id = "u3", UserName = "lucia", UserLastName1 = "Reyes", Email = "lucia@test.cu" };
        var author = new Author { Id = "a3", Name = "Lucia", LastName = "Reyes", LastNameKey = "reyes", SearchKey = "reyes lucia", UserId = "u3" };
        var storedFile = new StoredFile
        {
            FileName = "evidencia.pdf",
            ObjectKey = "publicaciones/ev.pdf",
            BucketName = "dashboard-documents",
            ContentType = "application/pdf",
            SizeBytes = 2048,
            UploadedById = "u3",
        };
        var publication = new Publication
        {
            Title = "Publicación original",
            PublicationData = "Datos",
            PublicationType = PublicationType.Libro,
            PublishedDate = "2023",
            AuthorPublications = new List<AuthorPublication>
            {
                new() { AuthorId = "a3" }
            },
            IndexedPublication = new IndexedPublication { Index = 1 },
        };

        db.Users.Add(user);
        db.Authors.Add(author);
        db.StoredFiles.Add(storedFile);
        db.Publications.Add(publication);
        await db.SaveChangesAsync();

        var service = BuildService(db, user.Id, author);
        var request = new UpdatePublicationRequest
        {
            Id = publication.Id,
            Title = "Publicación actualizada",
            PublicationData = "Datos actualizados",
            PublicationType = PublicationType.Libro,
            PublishedDate = "2024",
            Index = 1,
            EvidenceFileId = storedFile.Id,
        };

        var result = await service.UpdateAsync(request);

        result.Succeeded.ShouldBeTrue();
        var updated = await db.Publications.FindAsync(publication.Id);
        updated!.EvidenceFileId.ShouldBe(storedFile.Id);
    }

    [Test]
    public async Task UpdateAsync_ClearsEvidenceFileId_WhenSetToNull()
    {
        await using var db = BuildDb();

        var user = new User { Id = "u4", UserName = "mario", UserLastName1 = "Silva", Email = "mario@test.cu" };
        var author = new Author { Id = "a4", Name = "Mario", LastName = "Silva", LastNameKey = "silva", SearchKey = "silva mario", UserId = "u4" };
        var storedFile = new StoredFile
        {
            FileName = "cert.pdf",
            ObjectKey = "publicaciones/cert.pdf",
            BucketName = "dashboard-documents",
            ContentType = "application/pdf",
            SizeBytes = 1024,
            UploadedById = "u4",
        };
        db.Users.Add(user);
        db.Authors.Add(author);
        db.StoredFiles.Add(storedFile);
        await db.SaveChangesAsync();

        var publication = new Publication
        {
            Title = "Pub con evidencia",
            PublicationData = "Datos",
            PublicationType = PublicationType.Libro,
            PublishedDate = "2022",
            EvidenceFileId = storedFile.Id,
            AuthorPublications = new List<AuthorPublication>
            {
                new() { AuthorId = "a4" }
            },
            IndexedPublication = new IndexedPublication { Index = 2 },
        };
        db.Publications.Add(publication);
        await db.SaveChangesAsync();

        var service = BuildService(db, user.Id, author);
        var request = new UpdatePublicationRequest
        {
            Id = publication.Id,
            Title = "Pub sin evidencia",
            PublicationData = "Datos",
            PublicationType = PublicationType.Libro,
            PublishedDate = "2022",
            Index = 2,
            EvidenceFileId = null,
        };

        var result = await service.UpdateAsync(request);

        result.Succeeded.ShouldBeTrue();
        var updated = await db.Publications.FindAsync(publication.Id);
        updated!.EvidenceFileId.ShouldBeNull();
    }
}
