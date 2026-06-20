using Dashboard_v2.Application.Auth;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Auth;

[TestFixture]
public class AuthServiceTests
{
    private ApplicationDbContext _db = null!;
    private Mock<IIdentityService> _identityMock = null!;
    private Mock<IRequestValidationService> _validationMock = null!;
    private Mock<IUser> _userMock = null!;
    private AuthService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(opts);
        _identityMock = new Mock<IIdentityService>();
        _validationMock = new Mock<IRequestValidationService>();
        _userMock = new Mock<IUser>();

        _validationMock
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _sut = new AuthService(_identityMock.Object, _validationMock.Object, _userMock.Object, _db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    // ── RegisterAsync ────────────────────────────────────────────────────────

    [Test]
    public async Task RegisterAsync_CallsValidationAndIdentityService()
    {
        var request = new RegisterRequest
        {
            UserName = "john",
            UserLastName1 = "Doe",
            Email = "john@example.com",
            Password = "secret1",
            BirthDate = new DateTime(1990, 1, 1),
        };

        _identityMock
            .Setup(i => i.CreateUserAsync(
                request.UserName, request.UserLastName1, request.UserLastName2,
                request.Email, request.Password, request.BirthDate, request.IsTrained,
                request.TeachingCategory, request.ScientificCategory, request.InvestigationCategory))
            .ReturnsAsync((Result.Success(), "new-user-id"));

        var result = await _sut.RegisterAsync(request);

        result.Succeeded.ShouldBeTrue();
        _validationMock.Verify(v => v.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RegisterAsync_IdentityFailure_ReturnsFailure()
    {
        var request = new RegisterRequest
        {
            UserName = "john",
            UserLastName1 = "Doe",
            Email = "john@example.com",
            Password = "secret1",
            BirthDate = new DateTime(1990, 1, 1),
        };

        _identityMock
            .Setup(i => i.CreateUserAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<bool>(),
                It.IsAny<TeachingCategory>(), It.IsAny<ScientificCategory>(), It.IsAny<InvestigationCategory>()))
            .ReturnsAsync((Result.Failure(["Email already exists."]), string.Empty));

        var result = await _sut.RegisterAsync(request);

        result.Succeeded.ShouldBeFalse();
    }

    // ── LoginAsync ───────────────────────────────────────────────────────────

    [Test]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccess()
    {
        var request = new LoginRequest { Email = "user@test.com", Password = "pass" };
        var expectedResponse = new LoginResponse { Token = "jwt-token" };

        _identityMock
            .Setup(i => i.LoginAsync(request.Email, request.Password, request.SelectedRole, request.SelectedAreaId))
            .ReturnsAsync((Result.Success(), expectedResponse));

        var (result, response) = await _sut.LoginAsync(request);

        result.Succeeded.ShouldBeTrue();
        response.ShouldNotBeNull();
        response.Token.ShouldBe("jwt-token");
    }

    // ── LogoutAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task LogoutAsync_AlwaysReturnsSuccess()
    {
        var result = await _sut.LogoutAsync();
        result.Succeeded.ShouldBeTrue();
    }

    // ── GetCurrentUserAsync ──────────────────────────────────────────────────

    [Test]
    public async Task GetCurrentUserAsync_NullUserId_ReturnsNull()
    {
        _userMock.Setup(u => u.Id).Returns((string?)null);

        var result = await _sut.GetCurrentUserAsync();

        result.ShouldBeNull();
    }

    [Test]
    public async Task GetCurrentUserAsync_EmptyUserId_ReturnsNull()
    {
        _userMock.Setup(u => u.Id).Returns("  ");

        var result = await _sut.GetCurrentUserAsync();

        result.ShouldBeNull();
    }

    [Test]
    public async Task GetCurrentUserAsync_UserNotInDb_ReturnsNull()
    {
        _userMock.Setup(u => u.Id).Returns("user-not-in-db");

        var result = await _sut.GetCurrentUserAsync();

        result.ShouldBeNull();
    }

    [Test]
    public async Task GetCurrentUserAsync_ValidUser_ReturnsDto()
    {
        const string userId = "user-1";
        _db.Users.Add(new User
        {
            Id = userId,
            UserName = "john",
            Email = "john@test.com",
            UserLastName1 = "Doe",
        });
        await _db.SaveChangesAsync();

        _userMock.Setup(u => u.Id).Returns(userId);
        _userMock.Setup(u => u.Roles).Returns(["Admin"]);

        var result = await _sut.GetCurrentUserAsync();

        result.ShouldNotBeNull();
        result.Id.ShouldBe(userId);
        result.UserName.ShouldBe("john");
        result.Role.ShouldBe("Admin");
        result.HasLinkedAuthor.ShouldBeFalse();
    }

    [Test]
    public async Task GetCurrentUserAsync_UserWithLinkedAuthor_ReturnsHasLinkedAuthorTrue()
    {
        const string userId = "user-linked";
        _db.Users.Add(new User
        {
            Id = userId,
            UserName = "jane",
            Email = "jane@test.com",
            UserLastName1 = "Doe",
        });
        _db.Authors.Add(new Author
        {
            Id = "author-1",
            UserId = userId,
            LastName = "Doe",
            Name = "Doe, Jane",
            SearchKey = "jane doe",
            LastNameKey = "doe",
        });
        await _db.SaveChangesAsync();

        _userMock.Setup(u => u.Id).Returns(userId);
        _userMock.Setup(u => u.Roles).Returns((List<string>?)null);

        var result = await _sut.GetCurrentUserAsync();

        result.ShouldNotBeNull();
        result.HasLinkedAuthor.ShouldBeTrue();
    }
}
