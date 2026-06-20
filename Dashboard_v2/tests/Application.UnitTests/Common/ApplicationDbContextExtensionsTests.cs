using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Common;

/// <summary>
/// Tests del helper compartido <see cref="ApplicationDbContextExtensions.GetUserAreaIdAsync"/>,
/// usado por ProyectoService, EventService, RedService y los endpoints de producción
/// para resolver el área de un usuario sin duplicar la consulta.
/// </summary>
[TestFixture]
public class ApplicationDbContextExtensionsTests
{
    private ApplicationDbContext _db = null!;

    [SetUp]
    public void SetUp()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(opts);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task GetUserAreaIdAsync_UserWithArea_ReturnsAreaId()
    {
        _db.Users.Add(new User { Id = "u1", UserName = "u1", UserLastName1 = "L", Email = "u1@uh.cu", AreaId = "area-1" });
        await _db.SaveChangesAsync();

        var result = await _db.GetUserAreaIdAsync("u1");

        result.ShouldBe("area-1");
    }

    [Test]
    public async Task GetUserAreaIdAsync_UserWithoutArea_ReturnsNull()
    {
        _db.Users.Add(new User { Id = "u2", UserName = "u2", UserLastName1 = "L", Email = "u2@uh.cu" });
        await _db.SaveChangesAsync();

        var result = await _db.GetUserAreaIdAsync("u2");

        result.ShouldBeNull();
    }

    [Test]
    public async Task GetUserAreaIdAsync_UnknownUserId_ReturnsNull()
    {
        var result = await _db.GetUserAreaIdAsync("no-existe");

        result.ShouldBeNull();
    }

    [Test]
    public async Task GetUserAreaIdAsync_NullUserId_ReturnsNull()
    {
        var result = await _db.GetUserAreaIdAsync(null);

        result.ShouldBeNull();
    }
}
