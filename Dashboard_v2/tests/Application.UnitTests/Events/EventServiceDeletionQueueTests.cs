using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Events;
using Dashboard_v2.Application.FileStorage;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Events;

/// <summary>
/// Tests unitarios para la lógica de cola de borrado diferido en <see cref="EventService.UpdateEventAsync"/>.
///
/// <para>Verifican que al cambiar el <c>EvidenceFileId</c> de un <see cref="Event"/>,
/// el servicio encola el borrado del archivo anterior mediante <see cref="IFileDeletionQueueService"/>
/// antes del primer <c>SaveChangesAsync</c>, garantizando atomicidad.</para>
/// </summary>
[TestFixture]
public class EventServiceDeletionQueueTests
{
    private static ApplicationDbContext BuildDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static EventService BuildService(
        ApplicationDbContext db,
        string userId,
        Mock<IFileDeletionQueueService>? deletionQueue = null)
    {
        var currentUser = new Mock<IUser>();
        currentUser.Setup(u => u.Id).Returns(userId);
        return new EventService(db, currentUser.Object, deletionQueue?.Object);
    }

    private static async Task SeedCatalogAsync(ApplicationDbContext db)
    {
        if (!await db.Countries.AnyAsync())
            db.Countries.Add(new Country { Id = 1, Name = "Cuba" });
        if (!await db.EventTypes.AnyAsync())
            db.EventTypes.Add(new EventType { Id = 1, Name = "Internacional" });
        await db.SaveChangesAsync();
    }

    private static StoredFile AddFile(ApplicationDbContext db, string objectKey)
    {
        var f = new StoredFile
        {
            FileName    = "prog.pdf",
            ObjectKey   = objectKey,
            BucketName  = "dashboard-documents",
            ContentType = "application/pdf",
            SizeBytes   = 1024,
            UploadedById = "u1",
        };
        db.StoredFiles.Add(f);
        db.SaveChanges();
        return f;
    }

    private static UpdateEventRequest BuildRequest(int? evidenceFileId) =>
        new()
        {
            Name           = "Congreso 2024",
            CountryId      = 1,
            EventType      = 1,
            Institutions   = [],
            OrganizadorIds = [],
            EvidenceFileId = evidenceFileId,
        };

    // ─── Casos que SÍ deben encolar ───────────────────────────────────────────

    [Test]
    public async Task UpdateEventAsync_WhenFileReplaced_EnqueuesOldFileDeletion()
    {
        await using var db = BuildDb();
        await SeedCatalogAsync(db);

        var oldFile = AddFile(db, "eventos/viejo.pdf");
        var newFile = AddFile(db, "eventos/nuevo.pdf");
        var ev      = new Event { Name = "Evento", CountryId = 1, EventTypeId = 1, EvidenceFileId = oldFile.Id };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var queue = new Mock<IFileDeletionQueueService>();
        queue.Setup(q => q.EnqueueAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var svc    = BuildService(db, "u1", queue);
        var result = await svc.UpdateEventAsync(ev.Id, BuildRequest(newFile.Id));

        result.Succeeded.ShouldBeTrue();
        queue.Verify(q => q.EnqueueAsync(
            It.Is<StoredFile>(f => f.Id == oldFile.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateEventAsync_WhenFileRemoved_EnqueuesOldFileDeletion()
    {
        await using var db = BuildDb();
        await SeedCatalogAsync(db);

        var oldFile = AddFile(db, "eventos/cert.pdf");
        var ev      = new Event { Name = "Evento", CountryId = 1, EventTypeId = 1, EvidenceFileId = oldFile.Id };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var queue = new Mock<IFileDeletionQueueService>();
        queue.Setup(q => q.EnqueueAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var svc    = BuildService(db, "u1", queue);
        var result = await svc.UpdateEventAsync(ev.Id, BuildRequest(null));

        result.Succeeded.ShouldBeTrue();
        queue.Verify(q => q.EnqueueAsync(
            It.Is<StoredFile>(f => f.Id == oldFile.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Casos que NO deben encolar ───────────────────────────────────────────

    [Test]
    public async Task UpdateEventAsync_WhenNoFileAndFileAdded_DoesNotEnqueue()
    {
        await using var db = BuildDb();
        await SeedCatalogAsync(db);

        var newFile = AddFile(db, "eventos/nuevo.pdf");
        var ev      = new Event { Name = "Evento", CountryId = 1, EventTypeId = 1, EvidenceFileId = null };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var queue = new Mock<IFileDeletionQueueService>();
        var svc   = BuildService(db, "u1", queue);
        var result = await svc.UpdateEventAsync(ev.Id, BuildRequest(newFile.Id));

        result.Succeeded.ShouldBeTrue();
        queue.Verify(q => q.EnqueueAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task UpdateEventAsync_WhenFileUnchanged_DoesNotEnqueue()
    {
        await using var db = BuildDb();
        await SeedCatalogAsync(db);

        var file = AddFile(db, "eventos/mismo.pdf");
        var ev   = new Event { Name = "Evento", CountryId = 1, EventTypeId = 1, EvidenceFileId = file.Id };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var queue = new Mock<IFileDeletionQueueService>();
        var svc   = BuildService(db, "u1", queue);
        var result = await svc.UpdateEventAsync(ev.Id, BuildRequest(file.Id));

        result.Succeeded.ShouldBeTrue();
        queue.Verify(q => q.EnqueueAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── Sin cola configurada ─────────────────────────────────────────────────

    [Test]
    public async Task UpdateEventAsync_WithoutDeletionQueue_Succeeds()
    {
        await using var db = BuildDb();
        await SeedCatalogAsync(db);

        var oldFile = AddFile(db, "eventos/old.pdf");
        var ev      = new Event { Name = "Evento", CountryId = 1, EventTypeId = 1, EvidenceFileId = oldFile.Id };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var svc    = BuildService(db, "u1", deletionQueue: null);
        var result = await svc.UpdateEventAsync(ev.Id, BuildRequest(null));

        result.Succeeded.ShouldBeTrue();
        var updated = await db.Events.FindAsync(ev.Id);
        updated!.EvidenceFileId.ShouldBeNull();
    }

    // ─── DeleteEventAsync: encolado al eliminar la entidad ────────────────────

    [Test]
    public async Task DeleteEventAsync_WithEvidenceFile_EnqueuesFileDeletion()
    {
        await using var db = BuildDb();
        await SeedCatalogAsync(db);

        var file = AddFile(db, "eventos/programa.pdf");
        var ev   = new Event { Name = "Evento", CountryId = 1, EventTypeId = 1, EvidenceFileId = file.Id };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var queue = new Mock<IFileDeletionQueueService>();
        queue.Setup(q => q.EnqueueAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var svc    = BuildService(db, "u1", queue);
        var result = await svc.DeleteEventAsync(ev.Id);

        result.Succeeded.ShouldBeTrue();
        queue.Verify(q => q.EnqueueAsync(
            It.Is<StoredFile>(f => f.Id == file.Id),
            It.IsAny<CancellationToken>()), Times.Once);
        (await db.Events.CountAsync()).ShouldBe(0);
    }

    [Test]
    public async Task DeleteEventAsync_WithoutEvidenceFile_DoesNotEnqueue()
    {
        await using var db = BuildDb();
        await SeedCatalogAsync(db);

        var ev = new Event { Name = "Evento", CountryId = 1, EventTypeId = 1, EvidenceFileId = null };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var queue = new Mock<IFileDeletionQueueService>();
        var svc   = BuildService(db, "u1", queue);
        var result = await svc.DeleteEventAsync(ev.Id);

        result.Succeeded.ShouldBeTrue();
        queue.Verify(q => q.EnqueueAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DeleteEventAsync_WithoutDeletionQueue_DeletesEntityAndDoesNotThrow()
    {
        await using var db = BuildDb();
        await SeedCatalogAsync(db);

        var file = AddFile(db, "eventos/cert.pdf");
        var ev   = new Event { Name = "Evento", CountryId = 1, EventTypeId = 1, EvidenceFileId = file.Id };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var svc    = BuildService(db, "u1", deletionQueue: null);
        var result = await svc.DeleteEventAsync(ev.Id);

        result.Succeeded.ShouldBeTrue();
        (await db.Events.CountAsync()).ShouldBe(0);
    }
}
