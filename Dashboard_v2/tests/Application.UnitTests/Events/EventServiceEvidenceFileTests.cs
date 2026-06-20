using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Events;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Events;

/// <summary>
/// Tests unitarios para la funcionalidad de evidencia/certificado en <see cref="EventService"/>.
/// </summary>
[TestFixture]
public class EventServiceEvidenceFileTests
{
    private static ApplicationDbContext BuildDb()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(opts);
    }

    private static EventService BuildService(ApplicationDbContext db, string userId = "u1")
    {
        var currentUser = new Mock<IUser>();
        currentUser.Setup(u => u.Id).Returns(userId);
        return new EventService(db, currentUser.Object);
    }

    private static async Task SeedCatalogAsync(ApplicationDbContext db)
    {
        if (!await db.Countries.AnyAsync())
            db.Countries.Add(new Country { Id = 1, Name = "Cuba" });
        if (!await db.EventTypes.AnyAsync())
            db.EventTypes.Add(new EventType { Id = 1, Name = "Internacional" });
        await db.SaveChangesAsync();
    }

    [Test]
    public async Task CreateEventAsync_WithEvidenceFileId_PersistsEvidenceFileId()
    {
        await using var db = BuildDb();
        await SeedCatalogAsync(db);

        var user = new User { Id = "u1", UserName = "carlos", UserLastName1 = "Ruiz", Email = "carlos@test.cu" };
        var storedFile = new StoredFile
        {
            FileName = "programa.pdf",
            ObjectKey = "eventos/prog.pdf",
            BucketName = "dashboard-documents",
            ContentType = "application/pdf",
            SizeBytes = 2048,
            UploadedById = "u1",
        };
        db.Users.Add(user);
        db.StoredFiles.Add(storedFile);
        await db.SaveChangesAsync();

        var service = BuildService(db, user.Id);
        var request = new CreateEventRequest
        {
            Name = "Congreso Internacional 2024",
            CountryId = 1,
            EventType = 1,
            Institutions = [],
            OrganizadorIds = [],
            EvidenceFileId = storedFile.Id,
        };

        var (result, eventId) = await service.CreateEventAsync(request);

        result.Succeeded.ShouldBeTrue();
        eventId.ShouldNotBeNull();

        var created = await db.Events.FindAsync(eventId);
        created.ShouldNotBeNull();
        created!.EvidenceFileId.ShouldBe(storedFile.Id);
    }

    [Test]
    public async Task CreateEventAsync_WithoutEvidenceFileId_PersistsNull()
    {
        await using var db = BuildDb();
        await SeedCatalogAsync(db);

        var user = new User { Id = "u2", UserName = "laura", UserLastName1 = "Vega", Email = "laura@test.cu" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = BuildService(db, user.Id);
        var request = new CreateEventRequest
        {
            Name = "Taller Nacional 2024",
            CountryId = 1,
            EventType = 1,
            Institutions = [],
            OrganizadorIds = [],
            EvidenceFileId = null,
        };

        var (result, eventId) = await service.CreateEventAsync(request);

        result.Succeeded.ShouldBeTrue();
        var created = await db.Events.FindAsync(eventId);
        created!.EvidenceFileId.ShouldBeNull();
    }

    [Test]
    public async Task UpdateEventAsync_SetsEvidenceFileId()
    {
        await using var db = BuildDb();
        await SeedCatalogAsync(db);

        var user = new User { Id = "u3", UserName = "sofia", UserLastName1 = "Diaz", Email = "sofia@test.cu" };
        var storedFile = new StoredFile
        {
            FileName = "evidencia.pdf",
            ObjectKey = "eventos/ev.pdf",
            BucketName = "dashboard-documents",
            ContentType = "application/pdf",
            SizeBytes = 1024,
            UploadedById = "u3",
        };
        var ev = new Event { Name = "Evento sin evidencia", CountryId = 1, EventTypeId = 1 };
        db.Users.Add(user);
        db.StoredFiles.Add(storedFile);
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var service = BuildService(db, user.Id);
        var request = new UpdateEventRequest
        {
            Name = "Evento con evidencia",
            CountryId = 1,
            EventType = 1,
            Institutions = [],
            OrganizadorIds = [],
            EvidenceFileId = storedFile.Id,
        };

        var result = await service.UpdateEventAsync(ev.Id, request);

        result.Succeeded.ShouldBeTrue();
        var updated = await db.Events.FindAsync(ev.Id);
        updated!.EvidenceFileId.ShouldBe(storedFile.Id);
    }

    [Test]
    public async Task UpdateEventAsync_ClearsEvidenceFileId_WhenSetToNull()
    {
        await using var db = BuildDb();
        await SeedCatalogAsync(db);

        var user = new User { Id = "u4", UserName = "felix", UserLastName1 = "Mora", Email = "felix@test.cu" };
        var storedFile = new StoredFile
        {
            FileName = "cert.pdf",
            ObjectKey = "eventos/cert.pdf",
            BucketName = "dashboard-documents",
            ContentType = "application/pdf",
            SizeBytes = 512,
            UploadedById = "u4",
        };
        db.Users.Add(user);
        db.StoredFiles.Add(storedFile);
        await db.SaveChangesAsync();

        var ev = new Event { Name = "Evento con cert", CountryId = 1, EventTypeId = 1, EvidenceFileId = storedFile.Id };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var service = BuildService(db, user.Id);
        var request = new UpdateEventRequest
        {
            Name = "Evento sin cert",
            CountryId = 1,
            EventType = 1,
            Institutions = [],
            OrganizadorIds = [],
            EvidenceFileId = null,
        };

        var result = await service.UpdateEventAsync(ev.Id, request);

        result.Succeeded.ShouldBeTrue();
        var updated = await db.Events.FindAsync(ev.Id);
        updated!.EvidenceFileId.ShouldBeNull();
    }
}
