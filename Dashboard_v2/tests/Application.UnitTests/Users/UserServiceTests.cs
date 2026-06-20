using Dashboard_v2.Application.Users;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Domain.Enums;
using DomainRoles = Dashboard_v2.Domain.Enums.Roles;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Users;

[TestFixture]
public class UserServiceTests
{
    private ApplicationDbContext _db = null!;
    private UserService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(opts);
        _sut = new UserService(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private async Task AddUserAsync(string id, string userName = "user", bool isActive = true)
    {
        _db.Users.Add(new User
        {
            Id = id,
            UserName = userName,
            UserLastName1 = "Last",
            Email = $"{id}@test.com",
            IsActive = isActive,
        });
        await _db.SaveChangesAsync();
    }

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllAsync_EmptyDb_ReturnsEmptyList()
    {
        var result = await _sut.GetAllAsync();
        result.ShouldBeEmpty();
    }

    [Test]
    public async Task GetAllAsync_WithUsers_ReturnsOrderedByUserName()
    {
        await AddUserAsync("u1", "zara");
        await AddUserAsync("u2", "alice");

        var result = await _sut.GetAllAsync();

        result.Count.ShouldBe(2);
        result[0].UserName.ShouldBe("alice");
        result[1].UserName.ShouldBe("zara");
    }

    // ── GetJefesDeProyectoAsync ──────────────────────────────────────────────

    [Test]
    public async Task GetJefesDeProyectoAsync_OnlyReturnsActiveJefes()
    {
        await AddUserAsync("u1", "alice", isActive: true);
        await AddUserAsync("u2", "bob", isActive: false);
        await AddUserAsync("u3", "charlie", isActive: true);

        _db.UserRoles.Add(new UserRole { UserId = "u1", Role = DomainRoles.Jefe_de_Proyecto });
        _db.UserRoles.Add(new UserRole { UserId = "u2", Role = DomainRoles.Jefe_de_Proyecto });
        _db.UserRoles.Add(new UserRole { UserId = "u3", Role = DomainRoles.Profesor });
        await _db.SaveChangesAsync();

        var result = await _sut.GetJefesDeProyectoAsync();

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe("u1");
    }

    // ── AssignRoleAsync ──────────────────────────────────────────────────────

    [Test]
    public async Task AssignRoleAsync_InvalidRoleName_ReturnsFailure()
    {
        var result = await _sut.AssignRoleAsync("u1", "NotARealRole");
        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("Rol no válido"));
    }

    [Test]
    public async Task AssignRoleAsync_NoneRole_ReturnsFailure()
    {
        var result = await _sut.AssignRoleAsync("u1", "None");
        result.Succeeded.ShouldBeFalse();
    }

    [Test]
    public async Task AssignRoleAsync_UserNotFound_ReturnsFailure()
    {
        var result = await _sut.AssignRoleAsync("no-existe", "Profesor");
        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("no encontrado"));
    }

    [Test]
    public async Task AssignRoleAsync_AlreadyHasRole_ReturnsFailure()
    {
        await AddUserAsync("u1");
        _db.UserRoles.Add(new UserRole { UserId = "u1", Role = DomainRoles.Profesor });
        await _db.SaveChangesAsync();

        var result = await _sut.AssignRoleAsync("u1", "Profesor");

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("ya tiene este rol"));
    }

    [Test]
    public async Task AssignRoleAsync_ValidRequest_AssignsRole()
    {
        await AddUserAsync("u1");

        var result = await _sut.AssignRoleAsync("u1", "Profesor");

        result.Succeeded.ShouldBeTrue();
        (await _db.UserRoles.AnyAsync(ur => ur.UserId == "u1" && ur.Role == DomainRoles.Profesor)).ShouldBeTrue();
    }

    // ── RemoveRoleAsync ──────────────────────────────────────────────────────

    [Test]
    public async Task RemoveRoleAsync_InvalidRoleName_ReturnsFailure()
    {
        var result = await _sut.RemoveRoleAsync("u1", "BadRole");
        result.Succeeded.ShouldBeFalse();
    }

    [Test]
    public async Task RemoveRoleAsync_RoleNotAssigned_ReturnsSuccess_Idempotent()
    {
        await AddUserAsync("u1");
        var result = await _sut.RemoveRoleAsync("u1", "Profesor");
        result.Succeeded.ShouldBeTrue(); // idempotente
    }

    [Test]
    public async Task RemoveRoleAsync_RoleAssigned_RemovesRole()
    {
        await AddUserAsync("u1");
        _db.UserRoles.Add(new UserRole { UserId = "u1", Role = DomainRoles.Profesor });
        await _db.SaveChangesAsync();

        var result = await _sut.RemoveRoleAsync("u1", "Profesor");

        result.Succeeded.ShouldBeTrue();
        (await _db.UserRoles.AnyAsync(ur => ur.UserId == "u1")).ShouldBeFalse();
    }
}
