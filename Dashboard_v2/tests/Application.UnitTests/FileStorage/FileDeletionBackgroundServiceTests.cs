using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.FileStorage;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Infrastructure.BackgroundServices;
using Dashboard_v2.Infrastructure.Configuration;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.FileStorage;

/// <summary>
/// Tests unitarios para <see cref="FileDeletionBackgroundService"/>.
///
/// <para>Llaman directamente al método <c>internal ProcessPendingJobsAsync</c> para evitar
/// depender del bucle de tiempo de <c>ExecuteAsync</c>. El método es accesible porque
/// el ensamblado Infrastructure declara <c>InternalsVisibleTo</c> para este proyecto de tests.</para>
/// </summary>
[TestFixture]
public class FileDeletionBackgroundServiceTests
{
    /// <summary>
    /// TimeProvider con tiempo fijo para tests deterministas del backoff exponencial.
    /// </summary>
    private sealed class FakeTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;
        public FakeTimeProvider(DateTimeOffset now) => _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────

    private static FileDeletionBackgroundService BuildService(
        ApplicationDbContext db,
        Mock<IFileStorageService> storage,
        FileDeletionOptions? opts                             = null,
        TimeProvider? timeProvider                            = null,
        Mock<ILogger<FileDeletionBackgroundService>>? logger  = null)
    {
        opts ??= new FileDeletionOptions
        {
            IntervalSeconds      = 60,
            MaxAttempts          = 5,
            RetryDelaySeconds    = 30,
            MaxRetryDelaySeconds = 21_600,
        };

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider
            .Setup(p => p.GetService(typeof(IApplicationDbContext)))
            .Returns(db);
        serviceProvider
            .Setup(p => p.GetService(typeof(IFileStorageService)))
            .Returns(storage.Object);

        var scope = new Mock<IServiceScope>();
        scope.Setup(s => s.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

        return new FileDeletionBackgroundService(
            scopeFactory.Object,
            (logger ?? new Mock<ILogger<FileDeletionBackgroundService>>()).Object,
            Options.Create(opts),
            timeProvider ?? TimeProvider.System);
    }

    private static ApplicationDbContext BuildDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static StoredFile AddStoredFile(ApplicationDbContext db, string objectKey, string bucket = "dashboard-documents")
    {
        var f = new StoredFile
        {
            FileName     = "test.pdf",
            ObjectKey    = objectKey,
            BucketName   = bucket,
            ContentType  = "application/pdf",
            SizeBytes    = 1024,
            UploadedById = "u1",
        };
        db.StoredFiles.Add(f);
        db.SaveChanges();
        return f;
    }

    private static FileDeletionJob AddJob(
        ApplicationDbContext db,
        StoredFile? file      = null,
        int attempts          = 0,
        DateTime? lastAttempt = null)
    {
        var job = new FileDeletionJob
        {
            StoredFileId  = file?.Id,
            ObjectKey     = file?.ObjectKey ?? "key/orphan.pdf",
            BucketName    = file?.BucketName ?? "dashboard-documents",
            ScheduledAt   = DateTime.UtcNow,
            Attempts      = attempts,
            LastAttemptAt = lastAttempt,
        };
        db.FileDeletionJobs.Add(job);
        db.SaveChanges();
        return job;
    }

    // ─── Sin jobs ─────────────────────────────────────────────────────────────

    [Test]
    public async Task ProcessPendingJobsAsync_NoJobs_DoesNotCallDeleteAsync()
    {
        await using var db = BuildDb();
        var storage        = new Mock<IFileStorageService>();

        await BuildService(db, storage).ProcessPendingJobsAsync(CancellationToken.None);

        storage.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── Borrado exitoso ──────────────────────────────────────────────────────

    [Test]
    public async Task ProcessPendingJobsAsync_SuccessfulDeletion_RemovesJobAndStoredFile()
    {
        await using var db = BuildDb();
        var file           = AddStoredFile(db, "premios/cert.pdf");
        AddJob(db, file);
        var storage        = new Mock<IFileStorageService>();
        storage.Setup(s => s.DeleteAsync("premios/cert.pdf", It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        await BuildService(db, storage).ProcessPendingJobsAsync(CancellationToken.None);

        (await db.FileDeletionJobs.CountAsync()).ShouldBe(0, "el job debe eliminarse tras un borrado exitoso");
        (await db.StoredFiles.CountAsync()).ShouldBe(0, "el StoredFile debe eliminarse tras borrar el objeto de MinIO");
    }

    [Test]
    public async Task ProcessPendingJobsAsync_SuccessfulDeletion_WhenStoredFileIdIsNull_RemovesJobOnly()
    {
        // El StoredFile puede haber sido eliminado por otra vía; el job tiene ObjectKey copiado
        // y puede igualmente borrar el objeto de MinIO.
        await using var db = BuildDb();
        var job            = AddJob(db, file: null);
        var storage        = new Mock<IFileStorageService>();
        storage.Setup(s => s.DeleteAsync(job.ObjectKey, It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        await BuildService(db, storage).ProcessPendingJobsAsync(CancellationToken.None);

        (await db.FileDeletionJobs.CountAsync()).ShouldBe(0);
        storage.Verify(s => s.DeleteAsync(job.ObjectKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Fallo de MinIO ───────────────────────────────────────────────────────

    [Test]
    public async Task ProcessPendingJobsAsync_MinioUnavailable_IncrementsAttemptsAndKeepsJob()
    {
        await using var db = BuildDb();
        var file           = AddStoredFile(db, "eventos/ev.pdf");
        var job            = AddJob(db, file, attempts: 0);
        var storage        = new Mock<IFileStorageService>();
        storage.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new FileStorageUnavailableException("MinIO is down"));

        await BuildService(db, storage).ProcessPendingJobsAsync(CancellationToken.None);

        var updated = await db.FileDeletionJobs.FindAsync(job.Id);
        updated.ShouldNotBeNull();
        updated!.Attempts.ShouldBe(1);
        updated.LastAttemptAt.ShouldNotBeNull();
        (await db.StoredFiles.AnyAsync(f => f.Id == file.Id)).ShouldBeTrue("el StoredFile no debe borrarse si MinIO falla");
    }

    [Test]
    public async Task ProcessPendingJobsAsync_UnexpectedException_IncrementsAttemptsAndKeepsJob()
    {
        // El catch genérico también debe incrementar intentos y mantener el job.
        await using var db = BuildDb();
        var file           = AddStoredFile(db, "publicaciones/art.pdf");
        var job            = AddJob(db, file, attempts: 0);
        var storage        = new Mock<IFileStorageService>();
        storage.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new InvalidOperationException("error inesperado"));

        await BuildService(db, storage).ProcessPendingJobsAsync(CancellationToken.None);

        var updated = await db.FileDeletionJobs.FindAsync(job.Id);
        updated.ShouldNotBeNull();
        updated!.Attempts.ShouldBe(1);
    }

    [Test]
    public async Task ProcessPendingJobsAsync_MinioUnavailable_MultipleRetries_IncrementsEachTime()
    {
        await using var db = BuildDb();
        var file           = AddStoredFile(db, "publicaciones/art.pdf");
        // Partimos de 2 intentos previos con lastAttempt hace 1h para que el retardo haya expirado
        AddJob(db, file, attempts: 2, lastAttempt: DateTime.UtcNow.AddHours(-1));
        var storage        = new Mock<IFileStorageService>();
        storage.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new FileStorageUnavailableException("down"));

        // RetryDelaySeconds = 0 para no depender del cálculo de backoff en este test
        await BuildService(db, storage, new FileDeletionOptions { MaxAttempts = 5, RetryDelaySeconds = 0 })
              .ProcessPendingJobsAsync(CancellationToken.None);

        (await db.FileDeletionJobs.SingleAsync()).Attempts.ShouldBe(3);
    }

    // ─── Umbral de alerta (MaxAttempts) ──────────────────────────────────────

    [Test]
    public async Task ProcessPendingJobsAsync_WhenAttemptsEqualMaxAttempts_JobIsStillRetried()
    {
        // Con backoff exponencial, el job permanece en cola indefinidamente.
        // MaxAttempts ya NO es una barrera de parada: el job sigue reintentándose.
        await using var db = BuildDb();
        var file           = AddStoredFile(db, "registros/reg.pdf");
        // Attempts == MaxAttempts y sin LastAttemptAt → job elegible de inmediato
        AddJob(db, file, attempts: 5, lastAttempt: null);
        var storage        = new Mock<IFileStorageService>();
        storage.Setup(s => s.DeleteAsync(file.ObjectKey, It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        await BuildService(db, storage, new FileDeletionOptions { MaxAttempts = 5, RetryDelaySeconds = 30 })
              .ProcessPendingJobsAsync(CancellationToken.None);

        // El job DEBE procesarse y eliminarse — ya no se ignora por haber llegado al umbral
        storage.Verify(s => s.DeleteAsync(file.ObjectKey, It.IsAny<CancellationToken>()), Times.Once);
        (await db.FileDeletionJobs.CountAsync()).ShouldBe(0);
    }

    [Test]
    public async Task ProcessPendingJobsAsync_WhenAttemptsReachMaxAttempts_EmitsLogCritical()
    {
        // Cuando Attempts pasa de MaxAttempts-1 a MaxAttempts, se debe emitir un LogCritical
        // para alertar al operador. El job sigue en la cola para reintentos posteriores.
        await using var db = BuildDb();
        var file           = AddStoredFile(db, "normas/n.pdf");
        // Partimos de MaxAttempts-1 = 4; el próximo fallo lleva a 5 (= MaxAttempts) → Critical
        AddJob(db, file, attempts: 4, lastAttempt: null);
        var storage        = new Mock<IFileStorageService>();
        storage.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new FileStorageUnavailableException("down"));

        var loggerMock = new Mock<ILogger<FileDeletionBackgroundService>>();
        await BuildService(db, storage, new FileDeletionOptions { MaxAttempts = 5, RetryDelaySeconds = 0 }, logger: loggerMock)
              .ProcessPendingJobsAsync(CancellationToken.None);

        // El job debe seguir en la cola con Attempts = 5
        var updated = await db.FileDeletionJobs.SingleAsync();
        updated.Attempts.ShouldBe(5);

        // Y debe haberse emitido exactamente un LogCritical
        loggerMock.Verify(
            l => l.Log(
                It.Is<LogLevel>(lv => lv == LogLevel.Critical),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ─── Backoff exponencial ──────────────────────────────────────────────────

    [Test]
    public async Task ProcessPendingJobsAsync_ExponentialBackoff_JobNotYetReady_IsSkipped()
    {
        // Con RetryDelaySeconds=30 y Attempts=2, el retardo es min(30 * 2^2, techo) = 120s.
        // Si el último intento fue hace 119s, el job NO debe procesarse todavía.
        await using var db = BuildDb();
        var fakeNow        = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var file           = AddStoredFile(db, "patentes/pat.pdf");
        AddJob(db, file, attempts: 2, lastAttempt: fakeNow.UtcDateTime.AddSeconds(-119));

        var storage = new Mock<IFileStorageService>();
        var opts    = new FileDeletionOptions { MaxAttempts = 5, RetryDelaySeconds = 30, MaxRetryDelaySeconds = 21_600 };

        await BuildService(db, storage, opts, new FakeTimeProvider(fakeNow))
              .ProcessPendingJobsAsync(CancellationToken.None);

        storage.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never,
            "el retardo de 120s no ha expirado (solo han pasado 119s)");
    }

    [Test]
    public async Task ProcessPendingJobsAsync_ExponentialBackoff_JobNowReady_IsProcessed()
    {
        // Con RetryDelaySeconds=30 y Attempts=2, el retardo es 120s.
        // Si el último intento fue hace 121s, el job SÍ debe procesarse.
        await using var db = BuildDb();
        var fakeNow        = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var file           = AddStoredFile(db, "patentes/pat2.pdf");
        AddJob(db, file, attempts: 2, lastAttempt: fakeNow.UtcDateTime.AddSeconds(-121));

        var storage = new Mock<IFileStorageService>();
        storage.Setup(s => s.DeleteAsync(file.ObjectKey, It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);
        var opts = new FileDeletionOptions { MaxAttempts = 5, RetryDelaySeconds = 30, MaxRetryDelaySeconds = 21_600 };

        await BuildService(db, storage, opts, new FakeTimeProvider(fakeNow))
              .ProcessPendingJobsAsync(CancellationToken.None);

        storage.Verify(s => s.DeleteAsync(file.ObjectKey, It.IsAny<CancellationToken>()), Times.Once,
            "el retardo de 120s ha expirado (han pasado 121s)");
        (await db.FileDeletionJobs.CountAsync()).ShouldBe(0);
    }

    [Test]
    public async Task ProcessPendingJobsAsync_ExponentialBackoff_JobWithNoLastAttempt_IsProcessedImmediately()
    {
        // Un job sin LastAttemptAt (primer intento) nunca espera retardo.
        await using var db = BuildDb();
        var file           = AddStoredFile(db, "areas/a.pdf");
        AddJob(db, file, attempts: 3, lastAttempt: null); // attempts>0 pero sin LastAttemptAt

        var storage = new Mock<IFileStorageService>();
        storage.Setup(s => s.DeleteAsync(file.ObjectKey, It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        await BuildService(db, storage).ProcessPendingJobsAsync(CancellationToken.None);

        storage.Verify(s => s.DeleteAsync(file.ObjectKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Retardo entre reintentos (flat, tests de compatibilidad) ─────────────

    [Test]
    public async Task ProcessPendingJobsAsync_RecentlyAttemptedJob_IsSkippedDueToRetryDelay()
    {
        await using var db = BuildDb();
        var fakeNow        = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var file           = AddStoredFile(db, "normas/n.pdf");
        // Attempts=1, delay = 30*2^1 = 60s. Último intento hace 5s → debe omitirse.
        AddJob(db, file, attempts: 1, lastAttempt: fakeNow.UtcDateTime.AddSeconds(-5));

        var storage = new Mock<IFileStorageService>();
        await BuildService(db, storage, new FileDeletionOptions { MaxAttempts = 5, RetryDelaySeconds = 30 }, new FakeTimeProvider(fakeNow))
              .ProcessPendingJobsAsync(CancellationToken.None);

        storage.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ProcessPendingJobsAsync_JobWithExpiredRetryDelay_IsProcessed()
    {
        await using var db = BuildDb();
        var fakeNow        = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var file           = AddStoredFile(db, "patentes/pat.pdf");
        // Attempts=1, delay = 30*2^1 = 60s. Último intento hace 120s → debe procesarse.
        AddJob(db, file, attempts: 1, lastAttempt: fakeNow.UtcDateTime.AddSeconds(-120));

        var storage = new Mock<IFileStorageService>();
        storage.Setup(s => s.DeleteAsync(file.ObjectKey, It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);
        await BuildService(db, storage, new FileDeletionOptions { MaxAttempts = 5, RetryDelaySeconds = 30 }, new FakeTimeProvider(fakeNow))
              .ProcessPendingJobsAsync(CancellationToken.None);

        storage.Verify(s => s.DeleteAsync(file.ObjectKey, It.IsAny<CancellationToken>()), Times.Once);
        (await db.FileDeletionJobs.CountAsync()).ShouldBe(0, "el job debe eliminarse tras un borrado exitoso aunque fuera un reintento");
    }

    // ─── Múltiples jobs ───────────────────────────────────────────────────────

    [Test]
    public async Task ProcessPendingJobsAsync_MultipleJobs_ProcessesAllSuccessfully()
    {
        await using var db = BuildDb();
        var file1          = AddStoredFile(db, "a/f1.pdf");
        var file2          = AddStoredFile(db, "b/f2.pdf");
        AddJob(db, file1);
        AddJob(db, file2);
        var storage        = new Mock<IFileStorageService>();
        storage.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        await BuildService(db, storage).ProcessPendingJobsAsync(CancellationToken.None);

        storage.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        (await db.FileDeletionJobs.CountAsync()).ShouldBe(0);
    }

    [Test]
    public async Task ProcessPendingJobsAsync_OneJobFailsOneSucceeds_ProcessesBothIndependently()
    {
        await using var db = BuildDb();
        var file1          = AddStoredFile(db, "ok/file.pdf");
        var file2          = AddStoredFile(db, "fail/file.pdf");
        AddJob(db, file1);
        var job2           = AddJob(db, file2);
        var storage        = new Mock<IFileStorageService>();
        storage.Setup(s => s.DeleteAsync("ok/file.pdf",   It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        storage.Setup(s => s.DeleteAsync("fail/file.pdf", It.IsAny<CancellationToken>())).ThrowsAsync(new FileStorageUnavailableException("down"));

        await BuildService(db, storage).ProcessPendingJobsAsync(CancellationToken.None);

        // Job1 resuelto, job2 reintentable con Attempts=1
        var remaining = await db.FileDeletionJobs.ToListAsync();
        remaining.ShouldHaveSingleItem();
        remaining[0].Id.ShouldBe(job2.Id);
        remaining[0].Attempts.ShouldBe(1);
    }
}
