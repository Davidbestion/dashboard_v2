using Dashboard_v2.Application.Awards;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.FileStorage;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Awards;

/// <summary>
/// Tests unitarios para la lógica de cola de borrado diferido en <see cref="AwardService.UpdateAsync"/>.
///
/// <para>Verifican que, al cambiar el <c>EvidenceFileId</c> de un <see cref="UserAwarded"/>,
/// el servicio encola el borrado del archivo anterior mediante <see cref="IFileDeletionQueueService"/>
/// dentro de la misma unidad de trabajo (antes del <c>SaveChangesAsync</c>).</para>
/// </summary>
[TestFixture]
public class AwardServiceDeletionQueueTests
{
    private static ApplicationDbContext BuildDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static AwardService BuildService(
        ApplicationDbContext db,
        string userId,
        Mock<IFileDeletionQueueService>? deletionQueue = null)
    {
        var currentUser = new Mock<IUser>();
        currentUser.Setup(u => u.Id).Returns(userId);
        return new AwardService(db, currentUser.Object, deletionQueue?.Object);
    }

    private static StoredFile AddFile(ApplicationDbContext db, string objectKey)
    {
        var f = new StoredFile
        {
            FileName    = "cert.pdf",
            ObjectKey   = objectKey,
            BucketName  = "dashboard-documents",
            ContentType = "application/pdf",
            SizeBytes   = 512,
            UploadedById = "u1",
        };
        db.StoredFiles.Add(f);
        db.SaveChanges();
        return f;
    }

    private static async Task SeedCatalogAsync(ApplicationDbContext db)
    {
        db.Users.Add(new User { Id = "u1", UserName = "alice", UserLastName1 = "A", Email = "a@test.cu" });
        db.AwardTypes.Add(new AwardType { Id = 1, Name = "Nacional" });
        db.Awards.Add(new Award { Id = 1, Name = "Premio A", AwardTypeId = 1 });
        await db.SaveChangesAsync();
    }

    // ─── Casos que SÍ deben encolar ───────────────────────────────────────────

    [Test]
    public async Task UpdateAsync_WhenFileReplaced_EnqueuesOldFileDeletion()
    {
        await using var db = BuildDb();
        await SeedCatalogAsync(db);

        var oldFile = AddFile(db, "premios/viejo.pdf");
        var newFile = AddFile(db, "premios/nuevo.pdf");

        var awarded = new UserAwarded { UserId = "u1", AwardId = 1, AwardedAt = DateTime.UtcNow, EvidenceFileId = oldFile.Id };
        db.UserAwardees.Add(awarded);
        await db.SaveChangesAsync();

        var queue = new Mock<IFileDeletionQueueService>();
        queue.Setup(q => q.EnqueueAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var svc    = BuildService(db, "u1", queue);
        var result = await svc.UpdateAsync(awarded.Id, new UpdateAwardRequest
        {
            AwardId        = 1,
            AwardedAt      = DateTime.UtcNow,
            EvidenceFileId = newFile.Id,  // reemplaza oldFile por newFile
        });

        result.Succeeded.ShouldBeTrue();
        // Se debe encolar la eliminación del archivo anterior
        queue.Verify(q => q.EnqueueAsync(
            It.Is<StoredFile>(f => f.Id == oldFile.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_WhenFileRemoved_EnqueuesOldFileDeletion()
    {
        await using var db = BuildDb();
        await SeedCatalogAsync(db);

        var oldFile = AddFile(db, "premios/cert.pdf");
        var awarded = new UserAwarded { UserId = "u1", AwardId = 1, AwardedAt = DateTime.UtcNow, EvidenceFileId = oldFile.Id };
        db.UserAwardees.Add(awarded);
        await db.SaveChangesAsync();

        var queue = new Mock<IFileDeletionQueueService>();
        queue.Setup(q => q.EnqueueAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var svc    = BuildService(db, "u1", queue);
        var result = await svc.UpdateAsync(awarded.Id, new UpdateAwardRequest
        {
            AwardId        = 1,
            AwardedAt      = DateTime.UtcNow,
            EvidenceFileId = null, // el usuario quitó el archivo
        });

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
        await SeedCatalogAsync(db);

        var newFile = AddFile(db, "premios/nuevo.pdf");
        // Sin archivo previo
        var awarded = new UserAwarded { UserId = "u1", AwardId = 1, AwardedAt = DateTime.UtcNow, EvidenceFileId = null };
        db.UserAwardees.Add(awarded);
        await db.SaveChangesAsync();

        var queue = new Mock<IFileDeletionQueueService>();
        var svc   = BuildService(db, "u1", queue);

        var result = await svc.UpdateAsync(awarded.Id, new UpdateAwardRequest
        {
            AwardId        = 1,
            AwardedAt      = DateTime.UtcNow,
            EvidenceFileId = newFile.Id, // se añade archivo por primera vez
        });

        result.Succeeded.ShouldBeTrue();
        queue.Verify(q => q.EnqueueAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_WhenFileUnchanged_DoesNotEnqueue()
    {
        await using var db = BuildDb();
        await SeedCatalogAsync(db);

        var file    = AddFile(db, "premios/mismo.pdf");
        var awarded = new UserAwarded { UserId = "u1", AwardId = 1, AwardedAt = DateTime.UtcNow, EvidenceFileId = file.Id };
        db.UserAwardees.Add(awarded);
        await db.SaveChangesAsync();

        var queue = new Mock<IFileDeletionQueueService>();
        var svc   = BuildService(db, "u1", queue);

        // Se guarda el mismo archivo — sin cambio
        var result = await svc.UpdateAsync(awarded.Id, new UpdateAwardRequest
        {
            AwardId        = 1,
            AwardedAt      = DateTime.UtcNow,
            EvidenceFileId = file.Id,
        });

        result.Succeeded.ShouldBeTrue();
        queue.Verify(q => q.EnqueueAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── Sin cola configurada (MinIO no disponible) ────────────────────────────

    [Test]
    public async Task UpdateAsync_WithoutDeletionQueue_Succeeds_AndDoesNotThrow()
    {
        await using var db = BuildDb();
        await SeedCatalogAsync(db);

        var oldFile = AddFile(db, "premios/cert.pdf");
        var awarded = new UserAwarded { UserId = "u1", AwardId = 1, AwardedAt = DateTime.UtcNow, EvidenceFileId = oldFile.Id };
        db.UserAwardees.Add(awarded);
        await db.SaveChangesAsync();

        var svc    = BuildService(db, "u1", deletionQueue: null);
        var result = await svc.UpdateAsync(awarded.Id, new UpdateAwardRequest
        {
            AwardId        = 1,
            AwardedAt      = DateTime.UtcNow,
            EvidenceFileId = null,
        });

        result.Succeeded.ShouldBeTrue();
        var updated = await db.UserAwardees.FindAsync(awarded.Id);
        updated!.EvidenceFileId.ShouldBeNull();
    }

    // ─── DeleteAsync: encolado al eliminar la entidad ─────────────────────────

    [Test]
    public async Task DeleteAsync_WithEvidenceFile_EnqueuesFileDeletion()
    {
        await using var db = BuildDb();
        await SeedCatalogAsync(db);

        var file    = AddFile(db, "premios/evidencia.pdf");
        var awarded = new UserAwarded { UserId = "u1", AwardId = 1, AwardedAt = DateTime.UtcNow, EvidenceFileId = file.Id };
        db.UserAwardees.Add(awarded);
        await db.SaveChangesAsync();

        var queue = new Mock<IFileDeletionQueueService>();
        queue.Setup(q => q.EnqueueAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var svc    = BuildService(db, "u1", queue);
        var result = await svc.DeleteAsync(awarded.Id);

        result.Succeeded.ShouldBeTrue();
        queue.Verify(q => q.EnqueueAsync(
            It.Is<StoredFile>(f => f.Id == file.Id),
            It.IsAny<CancellationToken>()), Times.Once);
        (await db.UserAwardees.CountAsync()).ShouldBe(0);
    }

    [Test]
    public async Task DeleteAsync_WithoutEvidenceFile_DoesNotEnqueue()
    {
        await using var db = BuildDb();
        await SeedCatalogAsync(db);

        var awarded = new UserAwarded { UserId = "u1", AwardId = 1, AwardedAt = DateTime.UtcNow, EvidenceFileId = null };
        db.UserAwardees.Add(awarded);
        await db.SaveChangesAsync();

        var queue = new Mock<IFileDeletionQueueService>();
        var svc   = BuildService(db, "u1", queue);
        var result = await svc.DeleteAsync(awarded.Id);

        result.Succeeded.ShouldBeTrue();
        queue.Verify(q => q.EnqueueAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_WithoutDeletionQueue_DeletesEntityAndDoesNotThrow()
    {
        await using var db = BuildDb();
        await SeedCatalogAsync(db);

        var file    = AddFile(db, "premios/cert.pdf");
        var awarded = new UserAwarded { UserId = "u1", AwardId = 1, AwardedAt = DateTime.UtcNow, EvidenceFileId = file.Id };
        db.UserAwardees.Add(awarded);
        await db.SaveChangesAsync();

        var svc    = BuildService(db, "u1", deletionQueue: null);
        var result = await svc.DeleteAsync(awarded.Id);

        result.Succeeded.ShouldBeTrue();
        (await db.UserAwardees.CountAsync()).ShouldBe(0);
    }
}
