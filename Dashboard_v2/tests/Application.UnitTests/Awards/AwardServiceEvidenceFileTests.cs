using Dashboard_v2.Application.Awards;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Awards;

/// <summary>
/// Tests unitarios para la funcionalidad de evidencia/certificado en <see cref="AwardService"/>.
/// </summary>
[TestFixture]
public class AwardServiceEvidenceFileTests
{
    private static ApplicationDbContext BuildDb()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(opts);
    }

    private static AwardService BuildService(ApplicationDbContext db, string userId)
    {
        var currentUser = new Mock<IUser>();
        currentUser.Setup(u => u.Id).Returns(userId);
        return new AwardService(db, currentUser.Object);
    }

    [Test]
    public async Task CreateAsync_WithEvidenceFileId_PersistsEvidenceFileId()
    {
        await using var db = BuildDb();

        var user = new User { Id = "u1", UserName = "juan", UserLastName1 = "Pérez", Email = "juan@test.cu" };
        var awardType = new AwardType { Id = 1, Name = "Científico" };
        var award = new Award { Id = 1, Name = "Premio Nacional", AwardTypeId = 1, AwardType = awardType };
        var storedFile = new StoredFile
        {
            FileName = "certificado.pdf",
            ObjectKey = "premios/cert.pdf",
            BucketName = "dashboard-documents",
            ContentType = "application/pdf",
            SizeBytes = 1024,
            UploadedById = "u1",
        };

        db.Users.Add(user);
        db.AwardTypes.Add(awardType);
        db.Awards.Add(award);
        db.StoredFiles.Add(storedFile);
        await db.SaveChangesAsync();

        var service = BuildService(db, user.Id);
        var request = new CreateAwardRequest
        {
            AwardId = award.Id,
            AwardedAt = DateTime.UtcNow,
            EvidenceFileId = storedFile.Id,
        };

        var (result, awardedId) = await service.CreateAsync(request);

        result.Succeeded.ShouldBeTrue();
        awardedId.ShouldNotBeNull();

        var created = await db.UserAwardees.FindAsync(awardedId);
        created.ShouldNotBeNull();
        created!.EvidenceFileId.ShouldBe(storedFile.Id);
    }

    [Test]
    public async Task CreateAsync_WithoutEvidenceFileId_PersistsNullEvidenceFileId()
    {
        await using var db = BuildDb();

        var user = new User { Id = "u2", UserName = "maria", UserLastName1 = "López", Email = "maria@test.cu" };
        var awardType = new AwardType { Id = 1, Name = "Científico" };
        var award = new Award { Id = 1, Name = "Premio Regional", AwardTypeId = 1, AwardType = awardType };

        db.Users.Add(user);
        db.AwardTypes.Add(awardType);
        db.Awards.Add(award);
        await db.SaveChangesAsync();

        var service = BuildService(db, user.Id);
        var request = new CreateAwardRequest
        {
            AwardId = award.Id,
            AwardedAt = DateTime.UtcNow,
            EvidenceFileId = null,
        };

        var (result, awardedId) = await service.CreateAsync(request);

        result.Succeeded.ShouldBeTrue();
        var created = await db.UserAwardees.FindAsync(awardedId);
        created.ShouldNotBeNull();
        created!.EvidenceFileId.ShouldBeNull();
    }

    [Test]
    public async Task UpdateAsync_SetsEvidenceFileId()
    {
        await using var db = BuildDb();

        var user = new User { Id = "u3", UserName = "pedro", UserLastName1 = "Gómez", Email = "pedro@test.cu" };
        var awardType = new AwardType { Id = 1, Name = "Deportivo" };
        var award = new Award { Id = 1, Name = "Campeón", AwardTypeId = 1, AwardType = awardType };
        var storedFile = new StoredFile
        {
            FileName = "evidencia.pdf",
            ObjectKey = "premios/ev.pdf",
            BucketName = "dashboard-documents",
            ContentType = "application/pdf",
            SizeBytes = 512,
            UploadedById = "u3",
        };
        var userAwarded = new UserAwarded { UserId = "u3", AwardId = 1, AwardedAt = DateTime.UtcNow };

        db.Users.Add(user);
        db.AwardTypes.Add(awardType);
        db.Awards.Add(award);
        db.StoredFiles.Add(storedFile);
        db.UserAwardees.Add(userAwarded);
        await db.SaveChangesAsync();

        var service = BuildService(db, user.Id);
        var request = new UpdateAwardRequest
        {
            AwardId = award.Id,
            AwardedAt = DateTime.UtcNow,
            EvidenceFileId = storedFile.Id,
        };

        var result = await service.UpdateAsync(userAwarded.Id, request);

        result.Succeeded.ShouldBeTrue();
        var updated = await db.UserAwardees.FindAsync(userAwarded.Id);
        updated!.EvidenceFileId.ShouldBe(storedFile.Id);
    }

    [Test]
    public async Task UpdateAsync_RemovesEvidenceFileId_WhenSetToNull()
    {
        await using var db = BuildDb();

        var user = new User { Id = "u4", UserName = "ana", UserLastName1 = "Torres", Email = "ana@test.cu" };
        var awardType = new AwardType { Id = 1, Name = "Cultural" };
        var award = new Award { Id = 1, Name = "Premio Arte", AwardTypeId = 1, AwardType = awardType };
        var storedFile = new StoredFile
        {
            FileName = "cert.pdf",
            ObjectKey = "premios/cert2.pdf",
            BucketName = "dashboard-documents",
            ContentType = "application/pdf",
            SizeBytes = 256,
            UploadedById = "u4",
        };

        db.Users.Add(user);
        db.AwardTypes.Add(awardType);
        db.Awards.Add(award);
        db.StoredFiles.Add(storedFile);
        await db.SaveChangesAsync();

        var userAwarded = new UserAwarded { UserId = "u4", AwardId = 1, AwardedAt = DateTime.UtcNow, EvidenceFileId = storedFile.Id };
        db.UserAwardees.Add(userAwarded);
        await db.SaveChangesAsync();

        var service = BuildService(db, user.Id);
        var request = new UpdateAwardRequest
        {
            AwardId = award.Id,
            AwardedAt = DateTime.UtcNow,
            EvidenceFileId = null,
        };

        var result = await service.UpdateAsync(userAwarded.Id, request);

        result.Succeeded.ShouldBeTrue();
        var updated = await db.UserAwardees.FindAsync(userAwarded.Id);
        updated!.EvidenceFileId.ShouldBeNull();
    }
}
