using Ardalis.GuardClauses;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.FileStorage;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.FileStorage;

/// <summary>
/// Tests unitarios para <see cref="StoredFileService"/>.
/// </summary>
[TestFixture]
public class StoredFileServiceTests
{
    private static ApplicationDbContext BuildDb()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(opts);
    }

    private static StoredFileService BuildService(
        ApplicationDbContext db,
        Mock<IFileStorageService>? storageMock = null,
        string userId = "user1")
    {
        storageMock ??= new Mock<IFileStorageService>();

        var currentUser = new Mock<IUser>();
        currentUser.Setup(u => u.Id).Returns(userId);

        var validationService = new Mock<IRequestValidationService>();
        validationService
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new StoredFileService(db, storageMock.Object, currentUser.Object, validationService.Object);
    }

    private static StoredFile AddFile(ApplicationDbContext db, string objectKey = "certs/test.pdf", string userId = "user1")
    {
        var file = new StoredFile
        {
            FileName     = "test.pdf",
            ObjectKey    = objectKey,
            BucketName   = "dashboard-documents",
            ContentType  = "application/pdf",
            SizeBytes    = 2048,
            UploadedById = userId,
        };
        db.StoredFiles.Add(file);
        db.SaveChanges();
        return file;
    }

    // ── GetDownloadUrlAsync ───────────────────────────────────────────────────

    [Test]
    public async Task GetDownloadUrlAsync_ReturnsPresignedUrlFromStorage()
    {
        await using var db = BuildDb();
        var storageMock = new Mock<IFileStorageService>();
        var expectedUrl = "https://minio:9000/dashboard-documents/certs/test.pdf?X-Amz-Signature=abc";

        var file = AddFile(db, "certs/test.pdf");

        storageMock
            .Setup(s => s.GetPresignedDownloadUrlAsync(file.ObjectKey, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUrl);

        var service = BuildService(db, storageMock);

        var result = await service.GetDownloadUrlAsync(file.Id);

        result.ShouldBe(expectedUrl);
        storageMock.Verify(s => s.GetPresignedDownloadUrlAsync(file.ObjectKey, 3600, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetDownloadUrlAsync_UsesCustomExpirySeconds()
    {
        await using var db = BuildDb();
        var storageMock = new Mock<IFileStorageService>();
        var expectedUrl = "https://minio:9000/bucket/file.pdf?sig=xyz";

        var file = AddFile(db);

        storageMock
            .Setup(s => s.GetPresignedDownloadUrlAsync(It.IsAny<string>(), 7200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUrl);

        var service = BuildService(db, storageMock);

        var result = await service.GetDownloadUrlAsync(file.Id, expirySeconds: 7200);

        result.ShouldBe(expectedUrl);
        storageMock.Verify(s => s.GetPresignedDownloadUrlAsync(file.ObjectKey, 7200, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetDownloadUrlAsync_ThrowsNotFoundException_WhenFileDoesNotExist()
    {
        await using var db = BuildDb();
        var service = BuildService(db);

        await Should.ThrowAsync<NotFoundException>(() => service.GetDownloadUrlAsync(999));
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetByIdAsync_ReturnsDto_WhenFileExists()
    {
        await using var db = BuildDb();
        var file = AddFile(db, "certs/evidencia.pdf");
        var service = BuildService(db);

        var dto = await service.GetByIdAsync(file.Id);

        dto.ShouldNotBeNull();
        dto.Id.ShouldBe(file.Id);
        dto.FileName.ShouldBe(file.FileName);
        dto.ContentType.ShouldBe(file.ContentType);
        dto.SizeBytes.ShouldBe(file.SizeBytes);
        dto.UploadedById.ShouldBe(file.UploadedById);
    }

    [Test]
    public async Task GetByIdAsync_ThrowsNotFoundException_WhenFileDoesNotExist()
    {
        await using var db = BuildDb();
        var service = BuildService(db);

        await Should.ThrowAsync<NotFoundException>(() => service.GetByIdAsync(404));
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteAsync_RemovesFileFromDbAndStorage()
    {
        await using var db = BuildDb();
        var storageMock = new Mock<IFileStorageService>();
        storageMock
            .Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var file = AddFile(db, "certs/to-delete.pdf");
        var service = BuildService(db, storageMock);

        await service.DeleteAsync(file.Id);

        storageMock.Verify(s => s.DeleteAsync(file.ObjectKey, It.IsAny<CancellationToken>()), Times.Once);
        var remaining = await db.StoredFiles.FindAsync(file.Id);
        remaining.ShouldBeNull();
    }

    [Test]
    public async Task DeleteAsync_ThrowsNotFoundException_WhenFileDoesNotExist()
    {
        await using var db = BuildDb();
        var service = BuildService(db);

        await Should.ThrowAsync<NotFoundException>(() => service.DeleteAsync(404));
    }

    // ── UploadAsync ───────────────────────────────────────────────────────────

    [Test]
    public async Task UploadAsync_SavesMetadataToDb_AndCallsStorageUpload()
    {
        await using var db = BuildDb();
        var storageMock = new Mock<IFileStorageService>();
        storageMock.Setup(s => s.BucketName).Returns("dashboard-documents");
        storageMock
            .Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = BuildService(db, storageMock, userId: "u-upload");
        var request = new Dashboard_v2.Application.FileStorage.DTOs.UploadFileRequest
        {
            FileName    = "certificado.pdf",
            ContentType = "application/pdf",
            SizeBytes   = 1024,
            Category    = "premios",
        };

        await using var stream = new MemoryStream(new byte[1024]);
        var dto = await service.UploadAsync(request, stream);

        dto.ShouldNotBeNull();
        dto.FileName.ShouldBe("certificado.pdf");
        dto.ContentType.ShouldBe("application/pdf");
        dto.SizeBytes.ShouldBe(1024);
        dto.UploadedById.ShouldBe("u-upload");

        var saved = await db.StoredFiles.FindAsync(dto.Id);
        saved.ShouldNotBeNull();
        storageMock.Verify(s => s.UploadAsync(
            It.IsAny<Stream>(), It.IsAny<string>(), "application/pdf", 1024, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task UploadAsync_BuildsObjectKey_WithCategoryPrefixAndExtension()
    {
        await using var db = BuildDb();
        var storageMock = new Mock<IFileStorageService>();
        storageMock.Setup(s => s.BucketName).Returns("dashboard-documents");
        storageMock
            .Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = BuildService(db, storageMock);
        var request = new Dashboard_v2.Application.FileStorage.DTOs.UploadFileRequest
        {
            FileName    = "informe.pdf",
            ContentType = "application/pdf",
            SizeBytes   = 512,
            Category    = "proyectos",
        };

        await using var stream = new MemoryStream(new byte[512]);
        var dto = await service.UploadAsync(request, stream);

        var saved = await db.StoredFiles.FindAsync(dto.Id);
        saved!.ObjectKey.ShouldStartWith("proyectos/");
        saved.ObjectKey.ShouldEndWith(".pdf");
    }

    // ── DownloadAsync ─────────────────────────────────────────────────────────

    [Test]
    public async Task DownloadAsync_ReturnsStream_ContentType_FileName()
    {
        await using var db = BuildDb();
        var storageMock = new Mock<IFileStorageService>();
        var expectedBytes = new byte[] { 1, 2, 3, 4 };
        storageMock
            .Setup(s => s.DownloadAsync("certs/test.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(expectedBytes));

        var file = AddFile(db, "certs/test.pdf");
        var service = BuildService(db, storageMock);

        var (content, contentType, fileName) = await service.DownloadAsync(file.Id);

        content.ShouldNotBeNull();
        contentType.ShouldBe(file.ContentType);
        fileName.ShouldBe(file.FileName);
        await content.DisposeAsync();
    }

    [Test]
    public async Task DownloadAsync_ThrowsNotFoundException_WhenFileDoesNotExist()
    {
        await using var db = BuildDb();
        var service = BuildService(db);

        await Should.ThrowAsync<NotFoundException>(() => service.DownloadAsync(999));
    }

    // ── ReplaceAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task ReplaceAsync_DeletesOldObject_UploadsNew_UpdatesMetadata()
    {
        await using var db = BuildDb();
        var storageMock = new Mock<IFileStorageService>();
        storageMock.Setup(s => s.BucketName).Returns("dashboard-documents");
        storageMock
            .Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        storageMock
            .Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var file = AddFile(db, "certs/old.pdf");
        var service = BuildService(db, storageMock);
        var replaceRequest = new Dashboard_v2.Application.FileStorage.DTOs.UploadFileRequest
        {
            FileName    = "nuevo.pdf",
            ContentType = "application/pdf",
            SizeBytes   = 2048,
            Category    = "premios",
        };

        await using var stream = new MemoryStream(new byte[2048]);
        var dto = await service.ReplaceAsync(file.Id, replaceRequest, stream);

        dto.FileName.ShouldBe("nuevo.pdf");
        dto.SizeBytes.ShouldBe(2048);
        storageMock.Verify(s => s.DeleteAsync("certs/old.pdf", It.IsAny<CancellationToken>()), Times.Once);
        storageMock.Verify(s => s.UploadAsync(
            It.IsAny<Stream>(), It.IsAny<string>(), "application/pdf", 2048, It.IsAny<CancellationToken>()),
            Times.Once);

        var updated = await db.StoredFiles.FindAsync(file.Id);
        updated!.FileName.ShouldBe("nuevo.pdf");
    }

    [Test]
    public async Task ReplaceAsync_ThrowsNotFoundException_WhenFileDoesNotExist()
    {
        await using var db = BuildDb();
        var service = BuildService(db);
        var request = new Dashboard_v2.Application.FileStorage.DTOs.UploadFileRequest
        {
            FileName    = "x.pdf",
            ContentType = "application/pdf",
            SizeBytes   = 1,
            Category    = "test",
        };

        await using var stream = new MemoryStream(new byte[1]);
        await Should.ThrowAsync<NotFoundException>(() => service.ReplaceAsync(999, request, stream));
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllAsync_ReturnsOnlyFilesForCurrentUser()
    {
        await using var db = BuildDb();
        AddFile(db, "certs/u1-a.pdf", "user1");
        AddFile(db, "certs/u1-b.pdf", "user1");
        AddFile(db, "certs/u2-a.pdf", "user2");

        var service = BuildService(db, userId: "user1");
        var files = await service.GetAllAsync();

        files.Count.ShouldBe(2);
        files.ShouldAllBe(f => f.UploadedById == "user1");
    }
}
