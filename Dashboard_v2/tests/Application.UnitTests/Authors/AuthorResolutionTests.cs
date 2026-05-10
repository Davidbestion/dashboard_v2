using Dashboard_v2.Application.Authors;
using Dashboard_v2.Application.Common;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Common;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Authors;

/// <summary>
/// Tests for author name normalization, parsing, and the two resolution services.
///
/// All DB-dependent tests use the EF Core in-memory provider so they run
/// without a real PostgreSQL instance and in milliseconds.
/// </summary>
[TestFixture]
public class AuthorResolutionTests
{
    // ── helpers ───────────────────────────────────────────────────────────────

    private static ApplicationDbContext BuildDb(string dbName)
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(opts);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TextNormalizer
    // ─────────────────────────────────────────────────────────────────────────

    [TestCase("Damián",       "damian")]
    [TestCase("García López", "garcia lopez")]
    [TestCase("Ñoño",         "nono")]
    [TestCase("MÜLLER",       "muller")]
    [TestCase("",             "")]
    [TestCase(null,           "")]
    public void TextNormalizer_StripsDiacriticsAndLowercases(string? input, string expected)
    {
        TextNormalizer.Normalize(input).ShouldBe(expected);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AuthorNameParser
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void Parse_WithComma_SplitsIntoLastAndFirst()
    {
        var (ln, fn) = AuthorNameParser.Parse("García López, Juan Manuel");
        ln.ShouldBe("García López");
        fn.ShouldBe("Juan Manuel");
    }

    [Test]
    public void Parse_NoComma_EntireStringIsLastName()
    {
        var (ln, fn) = AuthorNameParser.Parse("Valdés Santiago");
        ln.ShouldBe("Valdés Santiago");
        fn.ShouldBeNull();
    }

    [Test]
    public void Parse_TrailingCommaOnly_FirstNameIsNull()
    {
        var (ln, fn) = AuthorNameParser.Parse("García,");
        ln.ShouldBe("García");
        fn.ShouldBeNull();
    }

    [Test]
    public void Parse_EmptyString_ReturnsBothEmpty()
    {
        var (ln, fn) = AuthorNameParser.Parse("  ");
        ln.ShouldBe(string.Empty);
        fn.ShouldBeNull();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Author.Create — SearchKey / LastNameKey / FirstNameKey
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void AuthorCreate_WithFirstName_SetsAllKeys()
    {
        var a = Author.Create("Valdés Santiago", "Damián");

        a.Name.ShouldBe("Valdés Santiago, Damián");
        a.SearchKey.ShouldBe("valdes santiago, damian");
        a.LastNameKey.ShouldBe("valdes santiago");
        a.FirstNameKey.ShouldBe("damian");
    }

    [Test]
    public void AuthorCreate_WithoutFirstName_FirstNameKeyIsNull()
    {
        var a = Author.Create("García López");

        a.Name.ShouldBe("García López");
        a.SearchKey.ShouldBe("garcia lopez");
        a.LastNameKey.ShouldBe("garcia lopez");
        a.FirstNameKey.ShouldBeNull();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AuthorResolutionService.ResolveByNameAsync
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task ResolveByName_ExactAccentMatch_ReturnsExistingAuthor()
    {
        // Existing author stored with "Damián" (accent)
        await using var db = BuildDb(nameof(ResolveByName_ExactAccentMatch_ReturnsExistingAuthor));
        var existing = Author.Create("Valdés Santiago", "Damián");
        db.Authors.Add(existing);
        await db.SaveChangesAsync();

        var service = new AuthorResolutionService(db);

        // Input arrives without accent (e.g. from CrossRef)
        var resolved = await service.ResolveByNameAsync("Valdés Santiago, Damian");

        resolved.Id.ShouldBe(existing.Id);
        resolved.Name.ShouldBe("Valdés Santiago, Damián");
    }

    [Test]
    public async Task ResolveByName_StructuredAccentMatch_ReturnsExistingAuthor()
    {
        // Author stored without accent in firstName
        await using var db = BuildDb(nameof(ResolveByName_StructuredAccentMatch_ReturnsExistingAuthor));
        var existing = Author.Create("Badia Albanes", "Valentina");
        db.Authors.Add(existing);
        await db.SaveChangesAsync();

        var service = new AuthorResolutionService(db);

        // Input has accents in lastName
        var resolved = await service.ResolveByNameAsync("Badía Albanés, Valentina");

        resolved.Id.ShouldBe(existing.Id);
    }

    [Test]
    public async Task ResolveByName_NoMatch_CreatesNewAuthor()
    {
        await using var db = BuildDb(nameof(ResolveByName_NoMatch_CreatesNewAuthor));

        var service = new AuthorResolutionService(db);
        var resolved = await service.ResolveByNameAsync("Pérez, Juan");

        resolved.ShouldNotBeNull();
        resolved.LastName.ShouldBe("Pérez");
        resolved.FirstName.ShouldBe("Juan");

        // Must be persisted
        var inDb = await db.Authors.FindAsync(resolved.Id);
        inDb.ShouldNotBeNull();
    }

    [Test]
    public async Task ResolveByName_SameNameTwice_ReturnsSameAuthor()
    {
        await using var db = BuildDb(nameof(ResolveByName_SameNameTwice_ReturnsSameAuthor));

        var service = new AuthorResolutionService(db);
        var first  = await service.ResolveByNameAsync("Fernández, Luis");
        var second = await service.ResolveByNameAsync("Fernández, Luis");

        first.Id.ShouldBe(second.Id);
        (await db.Authors.CountAsync()).ShouldBe(1);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AuthorService.ResolveExternalAuthorsAsync
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task ResolveExternal_FindsAuthorWithAccentMismatch()
    {
        await using var db = BuildDb(nameof(ResolveExternal_FindsAuthorWithAccentMismatch));

        // Author stored with "Damián" (accent on i)
        var author = Author.Create("Valdés Santiago", "Damián");
        db.Authors.Add(author);
        await db.SaveChangesAsync();

        var service = new AuthorService(db, new Mock<IUser>().Object);

        // CrossRef returns the name without the accent
        var results = await service.ResolveExternalAuthorsAsync(["Valdés Santiago, Damian"]);

        results.Count.ShouldBe(1);
        results[0].ExternalName.ShouldBe("Valdés Santiago, Damian");
        results[0].Match.ShouldNotBeNull();
        results[0].Match!.Id.ShouldBe(author.Id);
    }

    [Test]
    public async Task ResolveExternal_ReturnsNullMatchWhenNoAuthorFound()
    {
        await using var db = BuildDb(nameof(ResolveExternal_ReturnsNullMatchWhenNoAuthorFound));

        var service = new AuthorService(db, new Mock<IUser>().Object);

        var results = await service.ResolveExternalAuthorsAsync(["Alguien Desconocido, Ana"]);

        results.Count.ShouldBe(1);
        results[0].Match.ShouldBeNull();
    }

    [Test]
    public async Task ResolveExternal_StructuredMatch_FindsAuthorByLastAndFirstName()
    {
        await using var db = BuildDb(nameof(ResolveExternal_StructuredMatch_FindsAuthorByLastAndFirstName));

        // Author stored with accented surname; external name comes without accents
        var author = Author.Create("Badía Albanés", "Valentina");
        db.Authors.Add(author);
        await db.SaveChangesAsync();

        var service = new AuthorService(db, new Mock<IUser>().Object);

        var results = await service.ResolveExternalAuthorsAsync(["Badia Albanes, Valentina"]);

        results[0].Match.ShouldNotBeNull();
        results[0].Match!.Id.ShouldBe(author.Id);
    }

    [Test]
    public async Task ResolveExternal_EmptyList_ReturnsEmptyResults()
    {
        await using var db = BuildDb(nameof(ResolveExternal_EmptyList_ReturnsEmptyResults));
        var service = new AuthorService(db, new Mock<IUser>().Object);

        var results = await service.ResolveExternalAuthorsAsync([]);

        results.ShouldBeEmpty();
    }

    [Test]
    public async Task ResolveExternal_MultipleNames_ReturnsOneEntryPerName()
    {
        await using var db = BuildDb(nameof(ResolveExternal_MultipleNames_ReturnsOneEntryPerName));

        var known = Author.Create("Valdés Santiago", "Damián");
        db.Authors.Add(known);
        await db.SaveChangesAsync();

        var service = new AuthorService(db, new Mock<IUser>().Object);

        var results = await service.ResolveExternalAuthorsAsync(
            ["Valdés Santiago, Damian", "Desconocido, Persona"]);

        results.Count.ShouldBe(2);
        results.First(r => r.ExternalName == "Valdés Santiago, Damian").Match.ShouldNotBeNull();
        results.First(r => r.ExternalName == "Desconocido, Persona").Match.ShouldBeNull();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AuthorService.GetPotentialAuthorMatchesAsync
    //
    // Regression: user "Valdes / Santiago / Damian" (no accent) was not
    // matching author "Valdés Santiago, Damian" (with accent on é) because
    // the old query used .ToLower() which doesn't strip diacritics.
    // ─────────────────────────────────────────────────────────────────────────

    private static AuthorService BuildAuthorService(ApplicationDbContext db, string userId)
    {
        var user = new Mock<IUser>();
        user.Setup(u => u.Id).Returns(userId);
        return new AuthorService(db, user.Object);
    }

    [Test]
    public async Task PotentialMatches_UserWithoutAccents_MatchesAuthorWithAccents()
    {
        // Reproduces: user Valdes/Santiago/Damian  ←→  author "Valdés Santiago, Damian"
        await using var db = BuildDb(nameof(PotentialMatches_UserWithoutAccents_MatchesAuthorWithAccents));

        var userId = Guid.NewGuid().ToString();
        db.Users.Add(new User
        {
            Id            = userId,
            UserName      = "Damian",
            UserLastName1 = "Valdes",
            UserLastName2 = "Santiago",
            Email         = "damian@test.com",
        });
        db.Authors.Add(Author.Create("Valdés Santiago", "Damian"));
        await db.SaveChangesAsync();

        var service = BuildAuthorService(db, userId);
        var result  = await service.GetPotentialAuthorMatchesAsync();

        // Should find the author and classify it as an exact match
        (result.ExactMatches.Count + result.FuzzyMatches.Count).ShouldBeGreaterThan(0);
        result.ExactMatches.Count.ShouldBe(1);
        result.ExactMatches[0].Name.ShouldBe("Valdés Santiago, Damian");
    }

    [Test]
    public async Task PotentialMatches_UserAlreadyLinked_ReturnsEmpty()
    {
        await using var db = BuildDb(nameof(PotentialMatches_UserAlreadyLinked_ReturnsEmpty));

        var userId = Guid.NewGuid().ToString();
        db.Users.Add(new User
        {
            Id = userId, UserName = "Pedro", UserLastName1 = "López", Email = "p@test.com"
        });
        var author = Author.Create("López", "Pedro");
        author.UserId = userId;
        db.Authors.Add(author);
        await db.SaveChangesAsync();

        var service = BuildAuthorService(db, userId);
        var result  = await service.GetPotentialAuthorMatchesAsync();

        result.ExactMatches.ShouldBeEmpty();
        result.FuzzyMatches.ShouldBeEmpty();
    }

    [Test]
    public async Task PotentialMatches_NoSimilarAuthor_ReturnsEmpty()
    {
        await using var db = BuildDb(nameof(PotentialMatches_NoSimilarAuthor_ReturnsEmpty));

        var userId = Guid.NewGuid().ToString();
        db.Users.Add(new User
        {
            Id = userId, UserName = "Carlos", UserLastName1 = "Martínez", Email = "c@test.com"
        });
        db.Authors.Add(Author.Create("García", "Luis"));
        await db.SaveChangesAsync();

        var service = BuildAuthorService(db, userId);
        var result  = await service.GetPotentialAuthorMatchesAsync();

        result.ExactMatches.ShouldBeEmpty();
        result.FuzzyMatches.ShouldBeEmpty();
    }

    [Test]
    public async Task PotentialMatches_AuthorAlreadyLinkedToOtherUser_NotSuggested()
    {
        await using var db = BuildDb(nameof(PotentialMatches_AuthorAlreadyLinkedToOtherUser_NotSuggested));

        var userId      = Guid.NewGuid().ToString();
        var otherUserId = Guid.NewGuid().ToString();

        db.Users.Add(new User
        {
            Id = userId, UserName = "Damian", UserLastName1 = "Valdes",
            UserLastName2 = "Santiago", Email = "d@test.com"
        });
        var author = Author.Create("Valdés Santiago", "Damian");
        author.UserId = otherUserId;   // already claimed by someone else
        db.Authors.Add(author);
        await db.SaveChangesAsync();

        var service = BuildAuthorService(db, userId);
        var result  = await service.GetPotentialAuthorMatchesAsync();

        // UserId != null → must be excluded from suggestions
        result.ExactMatches.ShouldBeEmpty();
        result.FuzzyMatches.ShouldBeEmpty();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AuthorResolutionService.GetOrCreateForUserAsync
    // Verifica que al crear una entidad (Registro, Norma, etc.) un usuario sin
    // Author previo obtiene uno creado automáticamente y vinculado a él.
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetOrCreateForUser_UserWithNoAuthor_CreatesAuthorWithUserId()
    {
        await using var db = BuildDb(nameof(GetOrCreateForUser_UserWithNoAuthor_CreatesAuthorWithUserId));

        var userId = Guid.NewGuid().ToString();
        db.Users.Add(new User
        {
            Id            = userId,
            UserName      = "Ana",
            UserLastName1 = "Pérez",
            UserLastName2 = "Ruiz",
            Email         = "ana@test.com",
        });
        await db.SaveChangesAsync();

        var service = new AuthorResolutionService(db);
        var author = await service.GetOrCreateForUserAsync(userId);

        author.ShouldNotBeNull();
        author!.UserId.ShouldBe(userId);
        author.LastName.ShouldBe("Pérez Ruiz");
        author.FirstName.ShouldBe("Ana");

        // Must be persisted in DB
        var inDb = await db.Authors.FindAsync(author.Id);
        inDb.ShouldNotBeNull();
    }

    [Test]
    public async Task GetOrCreateForUser_CalledTwice_ReturnsSameAuthor()
    {
        await using var db = BuildDb(nameof(GetOrCreateForUser_CalledTwice_ReturnsSameAuthor));

        var userId = Guid.NewGuid().ToString();
        db.Users.Add(new User
        {
            Id = userId, UserName = "Luis", UserLastName1 = "García", Email = "luis@test.com"
        });
        await db.SaveChangesAsync();

        var service = new AuthorResolutionService(db);
        var first  = await service.GetOrCreateForUserAsync(userId);
        var second = await service.GetOrCreateForUserAsync(userId);

        first!.Id.ShouldBe(second!.Id);
        (await db.Authors.CountAsync()).ShouldBe(1);
    }

    [Test]
    public async Task GetOrCreateForUser_UserAlreadyHasAuthor_ReturnsExisting()
    {
        await using var db = BuildDb(nameof(GetOrCreateForUser_UserAlreadyHasAuthor_ReturnsExisting));

        var userId = Guid.NewGuid().ToString();
        db.Users.Add(new User
        {
            Id = userId, UserName = "Carlos", UserLastName1 = "López", Email = "c@test.com"
        });
        var existing = Author.Create("López", "Carlos");
        existing.UserId = userId;
        db.Authors.Add(existing);
        await db.SaveChangesAsync();

        var service = new AuthorResolutionService(db);
        var result = await service.GetOrCreateForUserAsync(userId);

        result!.Id.ShouldBe(existing.Id);
        (await db.Authors.CountAsync()).ShouldBe(1);
    }

    [Test]
    public async Task GetOrCreateForUser_NonExistentUserId_ReturnsNull()
    {
        await using var db = BuildDb(nameof(GetOrCreateForUser_NonExistentUserId_ReturnsNull));

        var service = new AuthorResolutionService(db);
        var result = await service.GetOrCreateForUserAsync(Guid.NewGuid().ToString());

        result.ShouldBeNull();
    }

    [Test]
    public async Task GetOrCreateForUser_UserWithTwoLastNames_NameFormattedCorrectly()
    {
        await using var db = BuildDb(nameof(GetOrCreateForUser_UserWithTwoLastNames_NameFormattedCorrectly));

        var userId = Guid.NewGuid().ToString();
        db.Users.Add(new User
        {
            Id            = userId,
            UserName      = "María",
            UserLastName1 = "González",
            UserLastName2 = "Fernández",
            Email         = "maria@test.com",
        });
        await db.SaveChangesAsync();

        var service = new AuthorResolutionService(db);
        var author = await service.GetOrCreateForUserAsync(userId);

        author!.LastName.ShouldBe("González Fernández");
        author.FirstName.ShouldBe("María");
        author.Name.ShouldBe("González Fernández, María");
    }

    [Test]
    public async Task GetOrCreateForUser_UserWithOnlyOneLastName_NameFormattedCorrectly()
    {
        await using var db = BuildDb(nameof(GetOrCreateForUser_UserWithOnlyOneLastName_NameFormattedCorrectly));

        var userId = Guid.NewGuid().ToString();
        db.Users.Add(new User
        {
            Id            = userId,
            UserName      = "Pedro",
            UserLastName1 = "Martínez",
            Email         = "pedro@test.com",
        });
        await db.SaveChangesAsync();

        var service = new AuthorResolutionService(db);
        var author = await service.GetOrCreateForUserAsync(userId);

        author!.LastName.ShouldBe("Martínez");
        author.FirstName.ShouldBe("Pedro");
        author.Name.ShouldBe("Martínez, Pedro");
    }
}
