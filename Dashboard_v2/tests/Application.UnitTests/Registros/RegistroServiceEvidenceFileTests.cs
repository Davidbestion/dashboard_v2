using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.FileStorage;
using Dashboard_v2.Application.Registros;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Registros;

/// <summary>
/// Tests unitarios para la lógica de evidencia y cola de borrado diferido en
/// <see cref="RegistroService.UpdateAsync"/>.
///
/// <para>Verifican tanto que el <c>EvidenceFileId</c> se persiste correctamente como
/// que el archivo anterior se encola para borrado cuando el campo cambia.</para>
/// </summary>
[TestFixture]
public class RegistroServiceEvidenceFileTests
{
    private ApplicationDbContext _db = null!;
    private Mock<IAuthorResolutionService> _authorResolution = null!;
    private Mock<IProductionCreatorService> _creatorService = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        _authorResolution = new Mock<IAuthorResolutionService>();
        _creatorService   = new Mock<IProductionCreatorService>();

        // AddAdditionalCreatorsAsync no hace nada en estos tests
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

    // ─── Helpers ───────────────────────────────────────────────────────────────

    private RegistroService MakeService(string userId, Mock<IFileDeletionQueueService>? queue = null)
    {
        var user = new Mock<IUser>();
        user.Setup(u => u.Id).Returns(userId);
        user.Setup(u => u.Roles).Returns(new List<string> { "Superuser" }); // simplifica permisos

        _authorResolution
            .Setup(s => s.GetOrCreateForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Author
            {
                Id          = $"author-{userId}",
                Name        = "Test",
                LastName    = "User",
                LastNameKey = "user",
                SearchKey   = "user test",
                UserId      = userId,
            });

        return new RegistroService(_db, user.Object, _authorResolution.Object, _creatorService.Object, queue?.Object);
    }

    private async Task<(Country Country, Institution Institution)> SeedCatalogAsync()
    {
        var country     = new Country { Id = 1, Name = "Cuba" };
        var institution = new Institution { Id = "inst-1", Nombre = "MATCOM" };
        _db.Countries.Add(country);
        _db.Institutions.Add(institution);
        await _db.SaveChangesAsync();
        return (country, institution);
    }

    private StoredFile AddFile(string objectKey)
    {
        var f = new StoredFile
        {
            FileName    = "cert.pdf",
            ObjectKey   = objectKey,
            BucketName  = "dashboard-documents",
            ContentType = "application/pdf",
            SizeBytes   = 1024,
            UploadedById = "u1",
        };
        _db.StoredFiles.Add(f);
        _db.SaveChanges();
        return f;
    }

    private async Task<Registro> AddRegistroAsync(int? evidenceFileId = null)
    {
        var r = new Registro
        {
            Titulo            = "Registro inicial",
            NumeroCertificado = "RC-001",
            EsInformatico     = false,
            CountryId         = 1,
            InstitutionId     = "inst-1",
            EvidenceFileId    = evidenceFileId,
        };
        _db.Registros.Add(r);
        await _db.SaveChangesAsync();
        return r;
    }

    // ─── Persistencia básica ──────────────────────────────────────────────────

    [Test]
    public async Task UpdateAsync_SetsEvidenceFileId_WhenAdded()
    {
        await SeedCatalogAsync();
        var file    = AddFile("registros/new.pdf");
        var registro = await AddRegistroAsync(evidenceFileId: null);

        var svc    = MakeService("u1");
        var result = await svc.UpdateAsync(registro.Id, new UpdateRegistroBody(
            Titulo: "Actualizado", NumeroCertificado: "RC-002",
            EsInformatico: false, CountryId: 1, InstitutionId: "inst-1",
            EvidenceFileId: file.Id));

        result.Succeeded.ShouldBeTrue();
        var updated = await _db.Registros.FindAsync(registro.Id);
        updated!.EvidenceFileId.ShouldBe(file.Id);
    }

    [Test]
    public async Task UpdateAsync_ClearsEvidenceFileId_WhenRemoved()
    {
        await SeedCatalogAsync();
        var file    = AddFile("registros/viejo.pdf");
        var registro = await AddRegistroAsync(evidenceFileId: file.Id);

        var svc    = MakeService("u1");
        var result = await svc.UpdateAsync(registro.Id, new UpdateRegistroBody(
            Titulo: "Actualizado", NumeroCertificado: "RC-003",
            EsInformatico: false, CountryId: 1, InstitutionId: "inst-1",
            EvidenceFileId: null));

        result.Succeeded.ShouldBeTrue();
        var updated = await _db.Registros.FindAsync(registro.Id);
        updated!.EvidenceFileId.ShouldBeNull();
    }

    // ─── Cola de borrado ──────────────────────────────────────────────────────

    [Test]
    public async Task UpdateAsync_WhenFileReplaced_EnqueuesOldFileDeletion()
    {
        await SeedCatalogAsync();
        var oldFile  = AddFile("registros/viejo.pdf");
        var newFile  = AddFile("registros/nuevo.pdf");
        var registro = await AddRegistroAsync(oldFile.Id);

        var queue = new Mock<IFileDeletionQueueService>();
        queue.Setup(q => q.EnqueueAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var svc    = MakeService("u1", queue);
        var result = await svc.UpdateAsync(registro.Id, new UpdateRegistroBody(
            Titulo: "Updated", NumeroCertificado: "RC-004",
            EsInformatico: false, CountryId: 1, InstitutionId: "inst-1",
            EvidenceFileId: newFile.Id));

        result.Succeeded.ShouldBeTrue();
        queue.Verify(q => q.EnqueueAsync(
            It.Is<StoredFile>(f => f.Id == oldFile.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_WhenFileRemoved_EnqueuesOldFileDeletion()
    {
        await SeedCatalogAsync();
        var oldFile  = AddFile("registros/cert.pdf");
        var registro = await AddRegistroAsync(oldFile.Id);

        var queue = new Mock<IFileDeletionQueueService>();
        queue.Setup(q => q.EnqueueAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var svc    = MakeService("u1", queue);
        var result = await svc.UpdateAsync(registro.Id, new UpdateRegistroBody(
            Titulo: "Updated", NumeroCertificado: "RC-005",
            EsInformatico: false, CountryId: 1, InstitutionId: "inst-1",
            EvidenceFileId: null));

        result.Succeeded.ShouldBeTrue();
        queue.Verify(q => q.EnqueueAsync(
            It.Is<StoredFile>(f => f.Id == oldFile.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_WhenFileAdded_DoesNotEnqueue()
    {
        await SeedCatalogAsync();
        var newFile  = AddFile("registros/nuevo.pdf");
        var registro = await AddRegistroAsync(evidenceFileId: null);

        var queue = new Mock<IFileDeletionQueueService>();
        var svc   = MakeService("u1", queue);

        var result = await svc.UpdateAsync(registro.Id, new UpdateRegistroBody(
            Titulo: "Updated", NumeroCertificado: "RC-006",
            EsInformatico: false, CountryId: 1, InstitutionId: "inst-1",
            EvidenceFileId: newFile.Id));

        result.Succeeded.ShouldBeTrue();
        queue.Verify(q => q.EnqueueAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_WhenFileUnchanged_DoesNotEnqueue()
    {
        await SeedCatalogAsync();
        var file     = AddFile("registros/mismo.pdf");
        var registro = await AddRegistroAsync(file.Id);

        var queue = new Mock<IFileDeletionQueueService>();
        var svc   = MakeService("u1", queue);

        var result = await svc.UpdateAsync(registro.Id, new UpdateRegistroBody(
            Titulo: "Updated", NumeroCertificado: "RC-007",
            EsInformatico: false, CountryId: 1, InstitutionId: "inst-1",
            EvidenceFileId: file.Id));

        result.Succeeded.ShouldBeTrue();
        queue.Verify(q => q.EnqueueAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_WithoutDeletionQueue_Succeeds()
    {
        await SeedCatalogAsync();
        var oldFile  = AddFile("registros/old.pdf");
        var registro = await AddRegistroAsync(oldFile.Id);

        var svc    = MakeService("u1", queue: null);
        var result = await svc.UpdateAsync(registro.Id, new UpdateRegistroBody(
            Titulo: "Updated", NumeroCertificado: "RC-008",
            EsInformatico: false, CountryId: 1, InstitutionId: "inst-1",
            EvidenceFileId: null));

        result.Succeeded.ShouldBeTrue();
        var updated = await _db.Registros.FindAsync(registro.Id);
        updated!.EvidenceFileId.ShouldBeNull();
    }

    // ─── DeleteAsync: encolado al eliminar la entidad ─────────────────────────

    [Test]
    public async Task DeleteAsync_WithEvidenceFile_EnqueuesFileDeletion()
    {
        await SeedCatalogAsync();
        var file     = AddFile("registros/evidencia.pdf");
        var registro = await AddRegistroAsync(file.Id);

        var queue = new Mock<IFileDeletionQueueService>();
        queue.Setup(q => q.EnqueueAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var svc    = MakeService("u1", queue);
        var result = await svc.DeleteAsync(registro.Id);

        result.Succeeded.ShouldBeTrue();
        queue.Verify(q => q.EnqueueAsync(
            It.Is<StoredFile>(f => f.Id == file.Id),
            It.IsAny<CancellationToken>()), Times.Once);
        (await _db.Registros.CountAsync()).ShouldBe(0);
    }

    [Test]
    public async Task DeleteAsync_WithoutEvidenceFile_DoesNotEnqueue()
    {
        await SeedCatalogAsync();
        var registro = await AddRegistroAsync(evidenceFileId: null);

        var queue = new Mock<IFileDeletionQueueService>();
        var svc   = MakeService("u1", queue);
        var result = await svc.DeleteAsync(registro.Id);

        result.Succeeded.ShouldBeTrue();
        queue.Verify(q => q.EnqueueAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_WithoutDeletionQueue_DeletesEntityAndDoesNotThrow()
    {
        await SeedCatalogAsync();
        var file     = AddFile("registros/cert.pdf");
        var registro = await AddRegistroAsync(file.Id);

        var svc    = MakeService("u1", queue: null);
        var result = await svc.DeleteAsync(registro.Id);

        result.Succeeded.ShouldBeTrue();
        (await _db.Registros.CountAsync()).ShouldBe(0);
    }
}
