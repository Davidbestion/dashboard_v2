using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.FileStorage;
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
/// Tests unitarios para la lógica de cola de borrado diferido en
/// <see cref="PublicationService.UpdateAsync"/>.
///
/// <para>Verifican que el servicio encola el borrado del archivo anterior mediante
/// <see cref="IFileDeletionQueueService"/> cuando <c>EvidenceFileId</c> cambia,
/// y que no encola cuando no hay cambio o cuando la cola no está configurada.</para>
/// </summary>
[TestFixture]
public class PublicationServiceDeletionQueueTests
{
    private static ApplicationDbContext BuildDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static PublicationService BuildService(
        ApplicationDbContext db,
        string userId,
        Author author,
        Mock<IFileDeletionQueueService>? deletionQueue = null)
    {
        var currentUser = new Mock<IUser>();
        currentUser.Setup(u => u.Id).Returns(userId);
        currentUser.Setup(u => u.Roles).Returns(new List<string> { "Superuser" });

        var authorResolution = new Mock<IAuthorResolutionService>();
        authorResolution
            .Setup(x => x.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        return new PublicationService(
            db,
            currentUser.Object,
            new Mock<ICrossRefClient>().Object,
            new Mock<IOpenAireClient>().Object,
            authorResolution.Object,
            new Mock<IAuthorCleanupService>().Object,
            new Mock<IPublicationDatabaseResolver>().Object,
            deletionQueue?.Object);
    }

    private static StoredFile AddFile(ApplicationDbContext db, string objectKey)
    {
        var f = new StoredFile
        {
            FileName    = "art.pdf",
            ObjectKey   = objectKey,
            BucketName  = "dashboard-documents",
            ContentType = "application/pdf",
            SizeBytes   = 2048,
            UploadedById = "u1",
        };
        db.StoredFiles.Add(f);
        db.SaveChanges();
        return f;
    }

    private static async Task<(User User, Author Author)> SeedAuthorAsync(ApplicationDbContext db, string userId)
    {
        var user   = new User { Id = userId, UserName = "test", UserLastName1 = "T", Email = $"{userId}@test.cu" };
        var author = new Author
        {
            Id          = $"a-{userId}",
            Name        = "Test",
            LastName    = "T",
            LastNameKey = "t",
            SearchKey   = "t test",
            UserId      = userId,
        };
        db.Users.Add(user);
        db.Authors.Add(author);
        await db.SaveChangesAsync();
        return (user, author);
    }

    private static async Task<Publication> AddPublicationAsync(
        ApplicationDbContext db,
        string authorId,
        int? evidenceFileId = null)
    {
        var p = new Publication
        {
            Title           = "Publicación original",
            PublicationData = "Datos",
            PublicationType = PublicationType.Libro,
            PublishedDate   = "2023",
            EvidenceFileId  = evidenceFileId,
            AuthorPublications = new List<AuthorPublication>
            {
                new() { AuthorId = authorId },
            },
            IndexedPublication = new IndexedPublication { Index = 1 },
        };
        db.Publications.Add(p);
        await db.SaveChangesAsync();
        return p;
    }

    private static UpdatePublicationRequest BuildRequest(string pubId, int? evidenceFileId) =>
        new()
        {
            Id              = pubId,
            Title           = "Publicación actualizada",
            PublicationData = "Datos actualizados",
            PublicationType = PublicationType.Libro,
            PublishedDate   = "2024",
            Index           = 1,
            EvidenceFileId  = evidenceFileId,
        };

    // ─── Casos que SÍ deben encolar ───────────────────────────────────────────

    [Test]
    public async Task UpdateAsync_WhenFileReplaced_EnqueuesOldFileDeletion()
    {
        await using var db = BuildDb();
        var (_, author) = await SeedAuthorAsync(db, "u1");
        var oldFile = AddFile(db, "publicaciones/viejo.pdf");
        var newFile = AddFile(db, "publicaciones/nuevo.pdf");
        var pub     = await AddPublicationAsync(db, author.Id, oldFile.Id);

        var queue = new Mock<IFileDeletionQueueService>();
        queue.Setup(q => q.EnqueueAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var svc    = BuildService(db, "u1", author, queue);
        var result = await svc.UpdateAsync(BuildRequest(pub.Id, newFile.Id));

        result.Succeeded.ShouldBeTrue();
        queue.Verify(q => q.EnqueueAsync(
            It.Is<StoredFile>(f => f.Id == oldFile.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_WhenFileRemoved_EnqueuesOldFileDeletion()
    {
        await using var db = BuildDb();
        var (_, author) = await SeedAuthorAsync(db, "u2");
        var oldFile = AddFile(db, "publicaciones/cert.pdf");
        var pub     = await AddPublicationAsync(db, author.Id, oldFile.Id);

        var queue = new Mock<IFileDeletionQueueService>();
        queue.Setup(q => q.EnqueueAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var svc    = BuildService(db, "u2", author, queue);
        var result = await svc.UpdateAsync(BuildRequest(pub.Id, null));

        result.Succeeded.ShouldBeTrue();
        queue.Verify(q => q.EnqueueAsync(
            It.Is<StoredFile>(f => f.Id == oldFile.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Casos que NO deben encolar ───────────────────────────────────────────

    [Test]
    public async Task UpdateAsync_WhenFileAdded_DoesNotEnqueue()
    {
        await using var db = BuildDb();
        var (_, author) = await SeedAuthorAsync(db, "u3");
        var newFile = AddFile(db, "publicaciones/nuevo.pdf");
        var pub     = await AddPublicationAsync(db, author.Id, evidenceFileId: null);

        var queue = new Mock<IFileDeletionQueueService>();
        var svc   = BuildService(db, "u3", author, queue);
        var result = await svc.UpdateAsync(BuildRequest(pub.Id, newFile.Id));

        result.Succeeded.ShouldBeTrue();
        queue.Verify(q => q.EnqueueAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_WhenFileUnchanged_DoesNotEnqueue()
    {
        await using var db = BuildDb();
        var (_, author) = await SeedAuthorAsync(db, "u4");
        var file = AddFile(db, "publicaciones/mismo.pdf");
        var pub  = await AddPublicationAsync(db, author.Id, file.Id);

        var queue = new Mock<IFileDeletionQueueService>();
        var svc   = BuildService(db, "u4", author, queue);
        var result = await svc.UpdateAsync(BuildRequest(pub.Id, file.Id));

        result.Succeeded.ShouldBeTrue();
        queue.Verify(q => q.EnqueueAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── Sin cola configurada ─────────────────────────────────────────────────

    [Test]
    public async Task UpdateAsync_WithoutDeletionQueue_Succeeds()
    {
        await using var db = BuildDb();
        var (_, author) = await SeedAuthorAsync(db, "u5");
        var oldFile = AddFile(db, "publicaciones/old.pdf");
        var pub     = await AddPublicationAsync(db, author.Id, oldFile.Id);

        var svc    = BuildService(db, "u5", author, deletionQueue: null);
        var result = await svc.UpdateAsync(BuildRequest(pub.Id, null));

        result.Succeeded.ShouldBeTrue();
        var updated = await db.Publications.FindAsync(pub.Id);
        updated!.EvidenceFileId.ShouldBeNull();
    }

    // ─── DeleteAsync: encolado al eliminar la entidad ─────────────────────────

    [Test]
    public async Task DeleteAsync_WithEvidenceFile_EnqueuesFileDeletion()
    {
        await using var db = BuildDb();
        var (_, author) = await SeedAuthorAsync(db, "u6");
        var file = AddFile(db, "publicaciones/evidencia.pdf");
        var pub  = await AddPublicationAsync(db, author.Id, file.Id);

        var queue = new Mock<IFileDeletionQueueService>();
        queue.Setup(q => q.EnqueueAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var svc    = BuildService(db, "u6", author, queue);
        var result = await svc.DeleteAsync(pub.Id);

        result.Succeeded.ShouldBeTrue();
        queue.Verify(q => q.EnqueueAsync(
            It.Is<StoredFile>(f => f.Id == file.Id),
            It.IsAny<CancellationToken>()), Times.Once);
        (await db.Publications.CountAsync()).ShouldBe(0);
    }

    [Test]
    public async Task DeleteAsync_WithoutEvidenceFile_DoesNotEnqueue()
    {
        await using var db = BuildDb();
        var (_, author) = await SeedAuthorAsync(db, "u7");
        var pub = await AddPublicationAsync(db, author.Id, evidenceFileId: null);

        var queue = new Mock<IFileDeletionQueueService>();
        var svc   = BuildService(db, "u7", author, queue);
        var result = await svc.DeleteAsync(pub.Id);

        result.Succeeded.ShouldBeTrue();
        queue.Verify(q => q.EnqueueAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_WithoutDeletionQueue_DeletesEntityAndDoesNotThrow()
    {
        await using var db = BuildDb();
        var (_, author) = await SeedAuthorAsync(db, "u8");
        var file = AddFile(db, "publicaciones/cert.pdf");
        var pub  = await AddPublicationAsync(db, author.Id, file.Id);

        var svc    = BuildService(db, "u8", author, deletionQueue: null);
        var result = await svc.DeleteAsync(pub.Id);

        result.Succeeded.ShouldBeTrue();
        (await db.Publications.CountAsync()).ShouldBe(0);
    }
}
