using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.FileStorage;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Infrastructure.Data;
using Dashboard_v2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.FileStorage;

/// <summary>
/// Tests unitarios para <see cref="FileDeletionQueueService"/>.
///
/// <para>Verifican que el servicio añade un <see cref="FileDeletionJob"/> al contexto EF
/// sin llamar a <c>SaveChangesAsync</c>, de modo que el guardado quede en manos del
/// llamador y se realice en la misma transacción que el cambio de entidad.</para>
/// </summary>
[TestFixture]
public class FileDeletionQueueServiceTests
{
    /// <summary>
    /// TimeProvider con tiempo fijo para tests deterministas.
    /// </summary>
    private sealed class FakeTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;
        public FakeTimeProvider(DateTimeOffset now) => _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
    }

    private static ApplicationDbContext BuildDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<StoredFile> SeedFileAsync(ApplicationDbContext db)
    {
        var file = new StoredFile
        {
            FileName     = "cert.pdf",
            ObjectKey    = "premios/cert.pdf",
            BucketName   = "dashboard-documents",
            ContentType  = "application/pdf",
            SizeBytes    = 1024,
            UploadedById = "u1",
        };
        db.StoredFiles.Add(file);
        await db.SaveChangesAsync();
        return file;
    }

    // ─── EnqueueAsync: contenido del job ──────────────────────────────────────

    [Test]
    public async Task EnqueueAsync_AddsJobToChangeTracker_WithCorrectFields()
    {
        await using var db   = BuildDb();
        var file             = await SeedFileAsync(db);
        var fixedTime        = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var svc              = new FileDeletionQueueService(db, new FakeTimeProvider(fixedTime));

        await svc.EnqueueAsync(file);

        var addedJobs = db.ChangeTracker.Entries<FileDeletionJob>()
            .Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Added)
            .Select(e => e.Entity)
            .ToList();

        addedJobs.ShouldHaveSingleItem();
        var job = addedJobs[0];
        job.StoredFileId.ShouldBe(file.Id);
        job.ObjectKey.ShouldBe(file.ObjectKey);
        job.BucketName.ShouldBe(file.BucketName);
        job.Attempts.ShouldBe(0);
        job.LastAttemptAt.ShouldBeNull();
        // ScheduledAt debe coincidir exactamente con el tiempo del FakeTimeProvider
        job.ScheduledAt.ShouldBe(fixedTime.UtcDateTime);
    }

    [Test]
    public async Task EnqueueAsync_SetsScheduledAt_FromInjectedTimeProvider()
    {
        // Verifica que ScheduledAt viene del TimeProvider inyectado, no de DateTime.UtcNow directamente.
        await using var db = BuildDb();
        var file           = await SeedFileAsync(db);
        var specificTime   = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var svc            = new FileDeletionQueueService(db, new FakeTimeProvider(specificTime));

        await svc.EnqueueAsync(file);

        var job = db.ChangeTracker.Entries<FileDeletionJob>().Single().Entity;
        job.ScheduledAt.ShouldBe(specificTime.UtcDateTime);
    }

    // ─── EnqueueAsync: no persiste sin SaveChangesAsync ───────────────────────

    [Test]
    public async Task EnqueueAsync_DoesNotPersistJobWithoutExplicitSaveChanges()
    {
        await using var db = BuildDb();
        var file           = await SeedFileAsync(db);
        var svc            = new FileDeletionQueueService(db, TimeProvider.System);

        // Encolar pero NO llamar a SaveChangesAsync
        await svc.EnqueueAsync(file);

        // Con EF InMemory, CountAsync() solo cuenta lo guardado en el store, no lo pendiente.
        var jobsInDb = await db.FileDeletionJobs.CountAsync();
        jobsInDb.ShouldBe(0, "el job no debe estar en el store hasta que el llamador llame a SaveChangesAsync");

        // El change tracker SÍ debe tener el job pendiente
        var pending = db.ChangeTracker.Entries<FileDeletionJob>()
            .Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Added)
            .ToList();
        pending.ShouldHaveSingleItem("el job debe estar en el change tracker pendiente de guardado");
    }

    [Test]
    public async Task EnqueueAsync_AfterSaveChanges_PersistsJobToDatabase()
    {
        await using var db = BuildDb();
        var file           = await SeedFileAsync(db);
        var svc            = new FileDeletionQueueService(db, TimeProvider.System);

        await svc.EnqueueAsync(file);

        // El llamador es responsable de guardar — simulamos ese comportamiento
        await db.SaveChangesAsync();

        var saved = await db.FileDeletionJobs.SingleAsync();
        saved.ObjectKey.ShouldBe(file.ObjectKey);
        saved.BucketName.ShouldBe(file.BucketName);
        saved.StoredFileId.ShouldBe(file.Id);
    }
}
