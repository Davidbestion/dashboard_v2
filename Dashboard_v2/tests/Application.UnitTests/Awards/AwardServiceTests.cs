using Dashboard_v2.Application.Awards;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Awards;

[TestFixture]
public class AwardServiceTests
{
    private ApplicationDbContext _db = null!;
    private Mock<IUser> _currentUser = null!;
    private AwardService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _currentUser = new Mock<IUser>();
        _currentUser.Setup(u => u.Id).Returns("user-1");

        _sut = new AwardService(_db, _currentUser.Object);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private async Task SeedBaseDataAsync()
    {
        _db.Users.Add(new User { Id = "user-1", UserName = "alice", Email = "alice@test.com", UserLastName1 = "A" });
        _db.AwardTypes.Add(new AwardType { Id = 1, Name = "Nacional" });
        _db.Awards.Add(new Award { Id = 1, Name = "Premio A", AwardTypeId = 1 });
        await _db.SaveChangesAsync();
    }

    // ─── GetMyAwardsAsync ─────────────────────────────────────────────────────

    [Test]
    public async Task GetMyAwardsAsync_Empty_ReturnsEmptyList()
    {
        await SeedBaseDataAsync();
        var result = await _sut.GetMyAwardsAsync();
        result.ShouldBeEmpty();
    }

    [Test]
    public async Task GetMyAwardsAsync_WithOtherUserAward_ReturnsEmpty()
    {
        await SeedBaseDataAsync();
        _db.Users.Add(new User { Id = "user-2", UserName = "bob", Email = "bob@test.com", UserLastName1 = "B" });
        _db.UserAwardees.Add(new UserAwarded { UserId = "user-2", AwardId = 1, AwardedAt = DateTime.Today });
        await _db.SaveChangesAsync();

        var result = await _sut.GetMyAwardsAsync();
        result.ShouldBeEmpty();
    }

    [Test]
    public async Task GetMyAwardsAsync_WithCurrentUserAward_ReturnsAward()
    {
        await SeedBaseDataAsync();
        _db.UserAwardees.Add(new UserAwarded { UserId = "user-1", AwardId = 1, AwardedAt = DateTime.Today });
        await _db.SaveChangesAsync();

        var result = await _sut.GetMyAwardsAsync();
        result.ShouldHaveSingleItem();
        result[0].AwardName.ShouldBe("Premio A");
    }

    // ─── GetAllAwardsAsync ────────────────────────────────────────────────────

    [Test]
    public async Task GetAllAwardsAsync_Empty_ReturnsEmptyList()
    {
        await SeedBaseDataAsync();
        var result = await _sut.GetAllAwardsAsync();
        result.ShouldBeEmpty();
    }

    [Test]
    public async Task GetAllAwardsAsync_WithMultipleUsers_ReturnsAll()
    {
        await SeedBaseDataAsync();
        _db.Users.Add(new User { Id = "user-2", UserName = "bob", Email = "bob@test.com", UserLastName1 = "B" });
        _db.UserAwardees.Add(new UserAwarded { UserId = "user-1", AwardId = 1, AwardedAt = DateTime.Today });
        _db.UserAwardees.Add(new UserAwarded { UserId = "user-2", AwardId = 1, AwardedAt = DateTime.Today });
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAwardsAsync();
        result.ShouldHaveSingleItem();
        result[0].Grantings[0].Recipients.Count.ShouldBe(2);
    }

    // ─── GetCatalogAsync ──────────────────────────────────────────────────────

    [Test]
    public async Task GetCatalogAsync_Empty_ReturnsEmptyList()
    {
        await SeedBaseDataAsync();
        // No UserAwardeds — catalog should still return awards
        var result = await _sut.GetCatalogAsync();
        result.Count.ShouldBe(1);
        result[0].AwardName.ShouldBe("Premio A");
    }

    [Test]
    public async Task GetCatalogAsync_DuplicateNames_DeduplicatesAwards()
    {
        await SeedBaseDataAsync();
        // Add another Award with same name and same type
        _db.Awards.Add(new Award { Id = 2, Name = "Premio A", AwardTypeId = 1 });
        await _db.SaveChangesAsync();

        var result = await _sut.GetCatalogAsync();
        result.Count.ShouldBe(1); // deduplicated
    }

    // ─── CreateAsync ─────────────────────────────────────────────────────────

    [Test]
    public async Task CreateAsync_ExistingAwardId_Succeeds()
    {
        await SeedBaseDataAsync();
        var request = new CreateAwardRequest { AwardId = 1, AwardedAt = DateTime.Today };

        var (result, id) = await _sut.CreateAsync(request);

        result.Succeeded.ShouldBeTrue();
        id.ShouldNotBeNull();
    }

    [Test]
    public async Task CreateAsync_NonExistingAwardId_Fails()
    {
        await SeedBaseDataAsync();
        var request = new CreateAwardRequest { AwardId = 999, AwardedAt = DateTime.Today };

        var (result, id) = await _sut.CreateAsync(request);

        result.Succeeded.ShouldBeFalse();
        id.ShouldBeNull();
    }

    [Test]
    public async Task CreateAsync_NewAwardName_NoType_Fails()
    {
        await SeedBaseDataAsync();
        var request = new CreateAwardRequest { NewAwardName = "Premio Nuevo", AwardedAt = DateTime.Today };

        var (result, id) = await _sut.CreateAsync(request);

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("tipo"));
    }

    [Test]
    public async Task CreateAsync_NewAwardName_InvalidType_Fails()
    {
        await SeedBaseDataAsync();
        var request = new CreateAwardRequest { NewAwardName = "Premio Nuevo", AwardTypeId = 999, AwardedAt = DateTime.Today };

        var (result, id) = await _sut.CreateAsync(request);

        result.Succeeded.ShouldBeFalse();
    }

    [Test]
    public async Task CreateAsync_NewAwardName_ValidType_CreatesAndSucceeds()
    {
        await SeedBaseDataAsync();
        var request = new CreateAwardRequest { NewAwardName = "Premio Nuevo", AwardTypeId = 1, AwardedAt = DateTime.Today };

        var (result, id) = await _sut.CreateAsync(request);

        result.Succeeded.ShouldBeTrue();
        id.ShouldNotBeNull();
        (await _db.Awards.AnyAsync(a => a.Name == "Premio Nuevo")).ShouldBeTrue();
    }

    [Test]
    public async Task CreateAsync_NewAwardName_ExistingByName_ReusesAward()
    {
        await SeedBaseDataAsync();
        // Premio A already exists with AwardTypeId = 1
        var request = new CreateAwardRequest { NewAwardName = "Premio A", AwardTypeId = 1, AwardedAt = DateTime.Today };

        var (result, id) = await _sut.CreateAsync(request);

        result.Succeeded.ShouldBeTrue();
        // Should not create a new award, still only 1 award
        (await _db.Awards.CountAsync()).ShouldBe(1);
    }

    [Test]
    public async Task CreateAsync_NullAwardIdAndEmptyName_Fails()
    {
        await SeedBaseDataAsync();
        var request = new CreateAwardRequest { AwardedAt = DateTime.Today };

        var (result, id) = await _sut.CreateAsync(request);

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("premio"));
    }

    // ─── UpdateAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task UpdateAsync_NonExistingId_Fails()
    {
        await SeedBaseDataAsync();
        var request = new UpdateAwardRequest { AwardId = 1, AwardedAt = DateTime.Today };

        var result = await _sut.UpdateAsync(999, request);

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("no encontrado"));
    }

    [Test]
    public async Task UpdateAsync_WrongOwner_Fails()
    {
        await SeedBaseDataAsync();
        _db.Users.Add(new User { Id = "user-2", UserName = "bob", Email = "bob@test.com", UserLastName1 = "B" });
        _db.UserAwardees.Add(new UserAwarded { Id = 1, UserId = "user-2", AwardId = 1, AwardedAt = DateTime.Today });
        await _db.SaveChangesAsync();

        var request = new UpdateAwardRequest { AwardId = 1, AwardedAt = DateTime.Today };
        var result = await _sut.UpdateAsync(1, request);

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("permiso"));
    }

    [Test]
    public async Task UpdateAsync_ValidOwner_Succeeds()
    {
        await SeedBaseDataAsync();
        _db.UserAwardees.Add(new UserAwarded { Id = 1, UserId = "user-1", AwardId = 1, AwardedAt = DateTime.Today });
        await _db.SaveChangesAsync();

        var newDate = DateTime.Today.AddDays(-10);
        var request = new UpdateAwardRequest { AwardId = 1, AwardedAt = newDate };
        var result = await _sut.UpdateAsync(1, request);

        result.Succeeded.ShouldBeTrue();
        var ua = await _db.UserAwardees.FindAsync(1);
        ua!.AwardedAt.ShouldBe(newDate);
    }

    // ─── DeleteAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteAsync_NonExistingId_Fails()
    {
        await SeedBaseDataAsync();
        var result = await _sut.DeleteAsync(999);
        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("no encontrado"));
    }

    [Test]
    public async Task DeleteAsync_WrongOwner_Fails()
    {
        await SeedBaseDataAsync();
        _db.Users.Add(new User { Id = "user-2", UserName = "bob", Email = "bob@test.com", UserLastName1 = "B" });
        _db.UserAwardees.Add(new UserAwarded { Id = 1, UserId = "user-2", AwardId = 1, AwardedAt = DateTime.Today });
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteAsync(1);
        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("permiso"));
    }

    [Test]
    public async Task DeleteAsync_ValidOwner_Succeeds()
    {
        await SeedBaseDataAsync();
        _db.UserAwardees.Add(new UserAwarded { Id = 1, UserId = "user-1", AwardId = 1, AwardedAt = DateTime.Today });
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteAsync(1);

        result.Succeeded.ShouldBeTrue();
        (await _db.UserAwardees.AnyAsync(ua => ua.Id == 1)).ShouldBeFalse();
    }

    // ─── Superuser bypass ────────────────────────────────────────────────────

    private void SetupSuperuser(string id = "super-1")
    {
        _currentUser.Setup(u => u.Id).Returns(id);
        _currentUser.Setup(u => u.Roles).Returns(new List<string> { "Superuser" });
    }

    [Test]
    public async Task GetMyAwardsAsync_Superuser_ReturnsAllUsers()
    {
        await SeedBaseDataAsync();
        _db.Users.Add(new User { Id = "user-2", UserName = "bob", Email = "bob@test.com", UserLastName1 = "B" });
        _db.UserAwardees.Add(new UserAwarded { UserId = "user-1", AwardId = 1, AwardedAt = DateTime.Today });
        _db.UserAwardees.Add(new UserAwarded { UserId = "user-2", AwardId = 1, AwardedAt = DateTime.Today });
        await _db.SaveChangesAsync();

        SetupSuperuser();
        var result = await _sut.GetMyAwardsAsync();

        result.ShouldNotBeEmpty();
        result.SelectMany(a => a.Grantings).SelectMany(g => g.Recipients)
            .Select(r => r.UserId)
            .ShouldContain("user-1");
        result.SelectMany(a => a.Grantings).SelectMany(g => g.Recipients)
            .Select(r => r.UserId)
            .ShouldContain("user-2");
    }

    [Test]
    public async Task CreateAsync_Superuser_WithTargetUserId_CreatesForTargetUser()
    {
        await SeedBaseDataAsync();
        _db.Users.Add(new User { Id = "user-2", UserName = "bob", Email = "bob@test.com", UserLastName1 = "B" });
        await _db.SaveChangesAsync();

        SetupSuperuser();
        var (result, id) = await _sut.CreateAsync(new CreateAwardRequest
        {
            AwardId = 1,
            AwardedAt = DateTime.Today,
            TargetUserId = "user-2"
        });

        result.Succeeded.ShouldBeTrue();
        var record = await _db.UserAwardees.FindAsync(id);
        record!.UserId.ShouldBe("user-2");
    }

    [Test]
    public async Task CreateAsync_Superuser_WithoutTargetUserId_ReturnsFailure()
    {
        await SeedBaseDataAsync();
        SetupSuperuser();

        var (result, _) = await _sut.CreateAsync(new CreateAwardRequest
        {
            AwardId = 1,
            AwardedAt = DateTime.Today
        });

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("TargetUserId"));
    }

    [Test]
    public async Task CreateAsync_Superuser_WithNonExistentTargetUser_ReturnsFailure()
    {
        await SeedBaseDataAsync();
        SetupSuperuser();

        var (result, _) = await _sut.CreateAsync(new CreateAwardRequest
        {
            AwardId = 1,
            AwardedAt = DateTime.Today,
            TargetUserId = "ghost-user"
        });

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("destinatario"));
    }

    [Test]
    public async Task UpdateAsync_Superuser_CanUpdateAnotherUsersAward()
    {
        await SeedBaseDataAsync();
        _db.UserAwardees.Add(new UserAwarded { Id = 10, UserId = "user-1", AwardId = 1, AwardedAt = DateTime.Today });
        await _db.SaveChangesAsync();

        SetupSuperuser("super-1");
        var result = await _sut.UpdateAsync(10, new UpdateAwardRequest
        {
            AwardId = 1,
            AwardedAt = DateTime.Today.AddDays(-1)
        });

        result.Succeeded.ShouldBeTrue();
    }

    [Test]
    public async Task DeleteAsync_Superuser_CanDeleteAnotherUsersAward()
    {
        await SeedBaseDataAsync();
        _db.UserAwardees.Add(new UserAwarded { Id = 11, UserId = "user-1", AwardId = 1, AwardedAt = DateTime.Today });
        await _db.SaveChangesAsync();

        SetupSuperuser("super-1");
        var result = await _sut.DeleteAsync(11);

        result.Succeeded.ShouldBeTrue();
        (await _db.UserAwardees.FindAsync(11)).ShouldBeNull();
    }

    // ─── GetAreaAwardsAsync ───────────────────────────────────────────────────

    [Test]
    public async Task GetAreaAwardsAsync_EmptyDb_ReturnsEmpty()
    {
        _currentUser.Setup(u => u.Id).Returns("vd-1");
        _db.Users.Add(new User { Id = "vd-1", UserName = "vd", Email = "vd@test.com", UserLastName1 = "VD", AreaId = "area-1" });
        _db.Areas.Add(new Area { Id = "area-1", Nombre = "Ciencias" });
        await _db.SaveChangesAsync();

        var result = await _sut.GetAreaAwardsAsync();
        result.ShouldBeEmpty();
    }

    [Test]
    public async Task GetAreaAwardsAsync_ReturnsOnlyAwardsForUsersInVicedecanoArea()
    {
        _currentUser.Setup(u => u.Id).Returns("vd-1");
        _db.Areas.AddRange(
            new Area { Id = "area-1", Nombre = "Ciencias" },
            new Area { Id = "area-2", Nombre = "Letras" });
        _db.Users.AddRange(
            new User { Id = "vd-1",  UserName = "vd",   Email = "vd@t.com",  UserLastName1 = "VD", AreaId = "area-1" },
            new User { Id = "u-1",   UserName = "alice", Email = "a@t.com",   UserLastName1 = "A",  AreaId = "area-1" },
            new User { Id = "u-2",   UserName = "bob",   Email = "b@t.com",   UserLastName1 = "B",  AreaId = "area-2" });
        _db.AwardTypes.Add(new AwardType { Id = 1, Name = "Nacional" });
        _db.Awards.Add(new Award { Id = 1, Name = "Premio X", AwardTypeId = 1 });
        _db.UserAwardees.AddRange(
            new UserAwarded { Id = 20, UserId = "u-1", AwardId = 1, AwardedAt = DateTime.Today },
            new UserAwarded { Id = 21, UserId = "u-2", AwardId = 1, AwardedAt = DateTime.Today });
        await _db.SaveChangesAsync();

        var result = await _sut.GetAreaAwardsAsync();

        result.ShouldHaveSingleItem();
        result[0].AwardName.ShouldBe("Premio X");
        result[0].Grantings.SelectMany(g => g.Recipients).ShouldHaveSingleItem();
        result[0].Grantings[0].Recipients[0].UserId.ShouldBe("u-1");
    }

    [Test]
    public async Task GetAreaAwardsAsync_ExcludesAwardsFromOtherAreas()
    {
        _currentUser.Setup(u => u.Id).Returns("vd-1");
        _db.Areas.AddRange(
            new Area { Id = "area-1", Nombre = "Ciencias" },
            new Area { Id = "area-2", Nombre = "Letras" });
        _db.Users.AddRange(
            new User { Id = "vd-1", UserName = "vd",  Email = "vd@t.com", UserLastName1 = "VD", AreaId = "area-1" },
            new User { Id = "u-2",  UserName = "bob", Email = "b@t.com",  UserLastName1 = "B",  AreaId = "area-2" });
        _db.AwardTypes.Add(new AwardType { Id = 1, Name = "Nacional" });
        _db.Awards.Add(new Award { Id = 1, Name = "Premio X", AwardTypeId = 1 });
        _db.UserAwardees.Add(new UserAwarded { Id = 22, UserId = "u-2", AwardId = 1, AwardedAt = DateTime.Today });
        await _db.SaveChangesAsync();

        var result = await _sut.GetAreaAwardsAsync();
        result.ShouldBeEmpty();
    }
}
