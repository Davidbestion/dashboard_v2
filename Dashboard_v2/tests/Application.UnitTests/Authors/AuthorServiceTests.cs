using Dashboard_v2.Application.Authors;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Authors;

[TestFixture]
public class AuthorServiceTests
{
    private ApplicationDbContext _db = null!;
    private Mock<IUser> _currentUser = null!;
    private AuthorService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _currentUser = new Mock<IUser>();
        _currentUser.Setup(u => u.Id).Returns("user-1");
        _sut = new AuthorService(_db, _currentUser.Object);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    // ─── LinkToUserAsync ──────────────────────────────────────────────────────

    [Test]
    public async Task LinkToUserAsync_UserAlreadyLinked_Fails()
    {
        _db.Authors.Add(new Author { Id = "a1", LastName = "Smith", Name = "Smith, John", SearchKey = "smith john", LastNameKey = "smith", UserId = "user-1" });
        await _db.SaveChangesAsync();

        var result = await _sut.LinkToUserAsync("a-other");
        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("Ya tienes"));
    }

    [Test]
    public async Task LinkToUserAsync_AuthorNotFound_Fails()
    {
        var result = await _sut.LinkToUserAsync("nonexistent");
        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("no encontrado"));
    }

    [Test]
    public async Task LinkToUserAsync_AuthorAlreadyLinkedToOther_Fails()
    {
        _db.Authors.Add(new Author { Id = "a1", LastName = "Smith", Name = "Smith, John", SearchKey = "smith john", LastNameKey = "smith", UserId = "user-99" });
        await _db.SaveChangesAsync();

        var result = await _sut.LinkToUserAsync("a1");
        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("ya está vinculado"));
    }

    [Test]
    public async Task LinkToUserAsync_UnlinkedAuthor_Succeeds()
    {
        _db.Authors.Add(new Author { Id = "a1", LastName = "Smith", Name = "Smith, John", SearchKey = "smith john", LastNameKey = "smith", UserId = null });
        await _db.SaveChangesAsync();

        var result = await _sut.LinkToUserAsync("a1");
        result.Succeeded.ShouldBeTrue();
        var author = await _db.Authors.FindAsync("a1");
        author!.UserId.ShouldBe("user-1");
    }

    // ─── SearchAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task SearchAsync_EmptyQuery_ReturnsEmpty()
    {
        var result = await _sut.SearchAsync("");
        result.ShouldBeEmpty();
    }

    [Test]
    public async Task SearchAsync_ShortQuery_ReturnsEmpty()
    {
        var result = await _sut.SearchAsync("a");
        result.ShouldBeEmpty();
    }

    [Test]
    public async Task SearchAsync_MatchingQuery_ReturnsResults()
    {
        _db.Authors.Add(new Author { Id = "a1", LastName = "Smith", Name = "Smith, John", SearchKey = "smith john", LastNameKey = "smith" });
        _db.Authors.Add(new Author { Id = "a2", LastName = "Jones", Name = "Jones, Mary", SearchKey = "jones mary", LastNameKey = "jones" });
        await _db.SaveChangesAsync();

        var result = await _sut.SearchAsync("smith");
        result.ShouldHaveSingleItem();
        result[0].Name.ShouldBe("Smith, John");
    }

    [Test]
    public async Task SearchAsync_NoMatch_ReturnsEmpty()
    {
        _db.Authors.Add(new Author { Id = "a1", LastName = "Smith", Name = "Smith, John", SearchKey = "smith john", LastNameKey = "smith" });
        await _db.SaveChangesAsync();

        var result = await _sut.SearchAsync("jones");
        result.ShouldBeEmpty();
    }

    [Test]
    public async Task SearchAsync_LimitsTo10Results()
    {
        for (int i = 0; i < 15; i++)
        {
            _db.Authors.Add(new Author
            {
                Id = $"a{i}", LastName = "Smith", Name = $"Smith, Author{i:D2}",
                SearchKey = $"smith author{i:D2}", LastNameKey = "smith"
            });
        }
        await _db.SaveChangesAsync();

        var result = await _sut.SearchAsync("smith");
        result.Count.ShouldBeLessThanOrEqualTo(10);
    }

    // ─── SearchCoauthorsAsync ─────────────────────────────────────────────────

    [Test]
    public async Task SearchCoauthorsAsync_EmptyQuery_ReturnsEmpty()
    {
        var result = await _sut.SearchCoauthorsAsync("");
        result.ShouldBeEmpty();
    }

    [Test]
    public async Task SearchCoauthorsAsync_MatchingAuthor_ReturnsAuthorType()
    {
        _db.Authors.Add(new Author { Id = "a1", LastName = "Garcia", Name = "Garcia, Luis", SearchKey = "garcia luis", LastNameKey = "garcia" });
        await _db.SaveChangesAsync();

        var result = await _sut.SearchCoauthorsAsync("garcia");
        result.ShouldHaveSingleItem();
        result[0].Type.ShouldBe("author");
    }

    [Test]
    public async Task SearchCoauthorsAsync_UserWithoutAuthor_ReturnsUserType()
    {
        _db.Users.Add(new User { Id = "u1", UserName = "Luis", UserLastName1 = "Garcia", Email = "l@a.com" });
        await _db.SaveChangesAsync();

        var result = await _sut.SearchCoauthorsAsync("garcia");
        result.Count.ShouldBeGreaterThan(0);
        result.ShouldContain(r => r.Type == "user");
    }

    // ─── GetPotentialAuthorMatchesAsync ───────────────────────────────────────

    [Test]
    public async Task GetPotentialAuthorMatchesAsync_AlreadyLinked_ReturnsEmptyLists()
    {
        _db.Authors.Add(new Author { Id = "a1", LastName = "X", Name = "X", SearchKey = "x", LastNameKey = "x", UserId = "user-1" });
        await _db.SaveChangesAsync();

        var result = await _sut.GetPotentialAuthorMatchesAsync();
        result.ExactMatches.ShouldBeEmpty();
        result.FuzzyMatches.ShouldBeEmpty();
    }

    [Test]
    public async Task GetPotentialAuthorMatchesAsync_UserNotFound_ReturnsEmptyLists()
    {
        _currentUser.Setup(u => u.Id).Returns("nonexistent-user");
        var result = await _sut.GetPotentialAuthorMatchesAsync();
        result.ExactMatches.ShouldBeEmpty();
        result.FuzzyMatches.ShouldBeEmpty();
    }

    [Test]
    public async Task GetPotentialAuthorMatchesAsync_NoMatches_ReturnsEmpty()
    {
        _db.Users.Add(new User { Id = "user-1", UserName = "Carlos", UserLastName1 = "Ramirez", Email = "c@a.com" });
        await _db.SaveChangesAsync();

        var result = await _sut.GetPotentialAuthorMatchesAsync();
        // No authors to match with
        result.ExactMatches.ShouldBeEmpty();
    }
}
