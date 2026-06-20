using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Documents.Reports;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Domain.Enums;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Documents;

/// <summary>
/// Tests unitarios para <see cref="AnexoPublicacionesReport"/>.
///
/// Verifican que el filtro por área funciona correctamente:
/// solo deben aparecer publicaciones que tengan al menos un autor
/// cuyo usuario vinculado pertenezca al área del solicitante.
/// </summary>
[TestFixture]
public class AnexoPublicacionesReportTests
{
    // ── helpers ───────────────────────────────────────────────────────────────

    private static ApplicationDbContext BuildDb(string dbName)
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(opts);
    }

    private static AnexoPublicacionesReport BuildReport(ApplicationDbContext db, string userId)
    {
        var currentUser = new Mock<IUser>();
        currentUser.Setup(u => u.Id).Returns(userId);
        return new AnexoPublicacionesReport(db, currentUser.Object);
    }

    /// <summary>
    /// Crea una publicación de tipo Diario (revista) con un JournalPublication del grupo indicado
    /// y la vincula al autor dado.
    /// </summary>
    private static Publication CreateJournalPublication(string title, int group, Author author, string publishedDate = "2024")
    {
        var pub = new Publication
        {
            Id = Guid.NewGuid().ToString(),
            Title = title,
            PublicationData = $"Datos de {title}",
            PublishedDate = publishedDate,
            PublicationType = PublicationType.Diario,
        };
        var jp = new JournalPublication
        {
            PublicationId = pub.Id,
            Group = group,
            BaseDeDatos = new BaseDeDatosPublicacion { Nombre = "WoS" },
        };
        pub.JournalPublication = jp;
        pub.AuthorPublications.Add(new AuthorPublication
        {
            AuthorId = author.Id,
            PublicationId = pub.Id,
            Author = author,
            Publication = pub,
        });
        return pub;
    }

    /// <summary>
    /// Crea una publicación indexada del tipo indicado y la vincula al autor dado.
    /// </summary>
    private static Publication CreateIndexedPublication(string title, PublicationType type, Author author)
    {
        var pub = new Publication
        {
            Id = Guid.NewGuid().ToString(),
            Title = title,
            PublicationData = $"Datos de {title}",
            PublishedDate = "2024",
            PublicationType = type,
        };
        var ip = new IndexedPublication
        {
            PublicationId = pub.Id,
            Index = 0,
        };
        pub.IndexedPublication = ip;
        pub.AuthorPublications.Add(new AuthorPublication
        {
            AuthorId = author.Id,
            PublicationId = pub.Id,
            Author = author,
            Publication = pub,
        });
        return pub;
    }

    // ── escenario base ────────────────────────────────────────────────────────

    /// <summary>
    /// Dado un usuario del Área A, solo deben aparecer las publicaciones
    /// que tengan al menos un autor vinculado a un usuario del Área A.
    /// Las publicaciones de autores de otras áreas o sin usuario no deben incluirse.
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_OnlyReturnsPublicationsFromSameArea()
    {
        await using var db = BuildDb(nameof(GatherVariablesAsync_OnlyReturnsPublicationsFromSameArea));

        var areaA = new Area { Id = "area-a", Nombre = "Área A" };
        var areaB = new Area { Id = "area-b", Nombre = "Área B" };

        var userA = new User { Id = "user-a", UserName = "userA", UserLastName1 = "A", Email = "a@test.cu", AreaId = areaA.Id };
        var userB = new User { Id = "user-b", UserName = "userB", UserLastName1 = "B", Email = "b@test.cu", AreaId = areaB.Id };

        var authorA = new Author { Id = "author-a", LastName = "López", Name = "López, Ana", SearchKey = "lopez ana", LastNameKey = "lopez", UserId = userA.Id, User = userA };
        var authorB = new Author { Id = "author-b", LastName = "García", Name = "García, Berto", SearchKey = "garcia berto", LastNameKey = "garcia", UserId = userB.Id, User = userB };
        var authorNoUser = new Author { Id = "author-x", LastName = "Externo", Name = "Externo", SearchKey = "externo", LastNameKey = "externo" };

        var pubAreaA = CreateJournalPublication("Pub Área A - G1", group: 1, authorA);
        var pubAreaB = CreateJournalPublication("Pub Área B - G1", group: 1, authorB);
        var pubNoUser = CreateJournalPublication("Pub Sin Usuario - G1", group: 1, authorNoUser);

        db.Areas.AddRange(areaA, areaB);
        db.Users.AddRange(userA, userB);
        db.Authors.AddRange(authorA, authorB, authorNoUser);
        db.Publications.AddRange(pubAreaA, pubAreaB, pubNoUser);
        await db.SaveChangesAsync();

        var report = BuildReport(db, userA.Id);
        var variables = await report.GatherVariablesAsync(null, CancellationToken.None);

        var g1 = (List<PublicacionG1RowDto>)variables["G1"];
        g1.Count.ShouldBe(1);
        g1[0].Titulo.ShouldBe("Pub Área A - G1");
    }

    /// <summary>
    /// Publicación con autores de distintas áreas: si al menos uno es del área A,
    /// la publicación debe aparecer en el reporte del área A.
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_IncludesPublicationWhenAnyAuthorMatchesArea()
    {
        await using var db = BuildDb(nameof(GatherVariablesAsync_IncludesPublicationWhenAnyAuthorMatchesArea));

        var areaA = new Area { Id = "area-a", Nombre = "Área A" };
        var areaB = new Area { Id = "area-b", Nombre = "Área B" };

        var userA = new User { Id = "user-a", UserName = "userA", UserLastName1 = "A", Email = "a@test.cu", AreaId = areaA.Id };
        var userB = new User { Id = "user-b", UserName = "userB", UserLastName1 = "B", Email = "b@test.cu", AreaId = areaB.Id };

        var authorA = new Author { Id = "author-a", LastName = "López", Name = "López, Ana", SearchKey = "lopez ana", LastNameKey = "lopez", UserId = userA.Id, User = userA };
        var authorB = new Author { Id = "author-b", LastName = "García", Name = "García, Berto", SearchKey = "garcia berto", LastNameKey = "garcia", UserId = userB.Id, User = userB };

        // Pub con ambos autores (A y B)
        var pub = new Publication
        {
            Id = "pub-mixed",
            Title = "Pub Mixta",
            PublicationData = "Datos mixtos",
            PublishedDate = "2024",
            PublicationType = PublicationType.Diario,
            JournalPublication = new JournalPublication { PublicationId = "pub-mixed", Group = 2, BaseDeDatos = new BaseDeDatosPublicacion { Nombre = "Scopus" } },
        };
        pub.AuthorPublications.Add(new AuthorPublication { AuthorId = authorA.Id, PublicationId = pub.Id, Author = authorA, Publication = pub });
        pub.AuthorPublications.Add(new AuthorPublication { AuthorId = authorB.Id, PublicationId = pub.Id, Author = authorB, Publication = pub });

        db.Areas.AddRange(areaA, areaB);
        db.Users.AddRange(userA, userB);
        db.Authors.AddRange(authorA, authorB);
        db.Publications.Add(pub);
        await db.SaveChangesAsync();

        var report = BuildReport(db, userA.Id);
        var variables = await report.GatherVariablesAsync(null, CancellationToken.None);

        var g2 = (List<PublicacionJournalRowDto>)variables["G2"];
        g2.Count.ShouldBe(1);
        g2[0].Titulo.ShouldBe("Pub Mixta");
    }

    /// <summary>
    /// Si el usuario solicitante no tiene área asignada (AreaId == null),
    /// no debe devolver ninguna publicación.
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_ReturnsEmptyWhenRequestingUserHasNoArea()
    {
        await using var db = BuildDb(nameof(GatherVariablesAsync_ReturnsEmptyWhenRequestingUserHasNoArea));

        var userNoArea = new User { Id = "user-no-area", UserName = "noArea", UserLastName1 = "Z", Email = "z@test.cu", AreaId = null };
        var authorA = new Author { Id = "author-a", LastName = "López", Name = "López, Ana", SearchKey = "lopez ana", LastNameKey = "lopez", UserId = userNoArea.Id, User = userNoArea };
        var pub = CreateJournalPublication("Alguna Pub", group: 1, authorA);

        db.Users.Add(userNoArea);
        db.Authors.Add(authorA);
        db.Publications.Add(pub);
        await db.SaveChangesAsync();

        var report = BuildReport(db, userNoArea.Id);
        var variables = await report.GatherVariablesAsync(null, CancellationToken.None);

        ((List<PublicacionG1RowDto>)variables["G1"]).ShouldBeEmpty();
        ((List<PublicacionJournalRowDto>)variables["G2"]).ShouldBeEmpty();
        ((List<PublicacionJournalRowDto>)variables["G3"]).ShouldBeEmpty();
        ((List<PublicacionJournalRowDto>)variables["G4"]).ShouldBeEmpty();
        ((List<PublicacionIndexadaRowDto>)variables["Libros"]).ShouldBeEmpty();
        ((List<PublicacionIndexadaRowDto>)variables["Monografias"]).ShouldBeEmpty();
        ((List<PublicacionIndexadaRowDto>)variables["Capitulos"]).ShouldBeEmpty();
        ((List<PublicacionDivulgacionRowDto>)variables["ArticulosDivulgacion"]).ShouldBeEmpty();
    }

    /// <summary>
    /// Verifica que las publicaciones se clasifican correctamente entre los grupos
    /// G1, G2, G3 y G4 según el campo <see cref="JournalPublication.Group"/>.
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_ClassifiesJournalPublicationsByGroup()
    {
        await using var db = BuildDb(nameof(GatherVariablesAsync_ClassifiesJournalPublicationsByGroup));

        var areaA = new Area { Id = "area-a", Nombre = "Área A" };
        var userA = new User { Id = "user-a", UserName = "userA", UserLastName1 = "A", Email = "a@test.cu", AreaId = areaA.Id };
        var authorA = new Author { Id = "author-a", LastName = "López", Name = "López, Ana", SearchKey = "lopez ana", LastNameKey = "lopez", UserId = userA.Id, User = userA };

        var pubG1 = CreateJournalPublication("Pub G1", group: 1, authorA);
        var pubG2 = CreateJournalPublication("Pub G2", group: 2, authorA);
        var pubG3 = CreateJournalPublication("Pub G3", group: 3, authorA);
        var pubG4 = CreateJournalPublication("Pub G4", group: 4, authorA);

        db.Areas.Add(areaA);
        db.Users.Add(userA);
        db.Authors.Add(authorA);
        db.Publications.AddRange(pubG1, pubG2, pubG3, pubG4);
        await db.SaveChangesAsync();

        var report = BuildReport(db, userA.Id);
        var variables = await report.GatherVariablesAsync(null, CancellationToken.None);

        ((List<PublicacionG1RowDto>)variables["G1"]).Count.ShouldBe(1);
        ((List<PublicacionJournalRowDto>)variables["G2"]).Count.ShouldBe(1);
        ((List<PublicacionJournalRowDto>)variables["G3"]).Count.ShouldBe(1);
        ((List<PublicacionJournalRowDto>)variables["G4"]).Count.ShouldBe(1);
    }

    /// <summary>
    /// Verifica que los libros, monografías, capítulos y artículos de divulgación
    /// se clasifican en sus variables correspondientes.
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_ClassifiesIndexedPublicationsByType()
    {
        await using var db = BuildDb(nameof(GatherVariablesAsync_ClassifiesIndexedPublicationsByType));

        var areaA = new Area { Id = "area-a", Nombre = "Área A" };
        var userA = new User { Id = "user-a", UserName = "userA", UserLastName1 = "A", Email = "a@test.cu", AreaId = areaA.Id };
        var authorA = new Author { Id = "author-a", LastName = "López", Name = "López, Ana", SearchKey = "lopez ana", LastNameKey = "lopez", UserId = userA.Id, User = userA };

        var libro = CreateIndexedPublication("Mi Libro", PublicationType.Libro, authorA);
        var mono = CreateIndexedPublication("Mi Monografía", PublicationType.Monografía, authorA);
        var cap = CreateIndexedPublication("Mi Capítulo", PublicationType.Capítulo, authorA);
        var divul = CreateIndexedPublication("Mi Artículo", PublicationType.Artículo_de_Divulgación, authorA);

        db.Areas.Add(areaA);
        db.Users.Add(userA);
        db.Authors.Add(authorA);
        db.Publications.AddRange(libro, mono, cap, divul);
        await db.SaveChangesAsync();

        var report = BuildReport(db, userA.Id);
        var variables = await report.GatherVariablesAsync(null, CancellationToken.None);

        ((List<PublicacionIndexadaRowDto>)variables["Libros"]).Count.ShouldBe(1);
        ((List<PublicacionIndexadaRowDto>)variables["Monografias"]).Count.ShouldBe(1);
        ((List<PublicacionIndexadaRowDto>)variables["Capitulos"]).Count.ShouldBe(1);
        ((List<PublicacionDivulgacionRowDto>)variables["ArticulosDivulgacion"]).Count.ShouldBe(1);
        // Las variables de revistas deben estar vacías
        ((List<PublicacionG1RowDto>)variables["G1"]).ShouldBeEmpty();
    }

    // ── filtro de rango de fechas ─────────────────────────────────────────────

    /// <summary>
    /// Solo deben incluirse las publicaciones cuya fecha esté dentro del rango [from, to].
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_OnlyReturnsPublicationsWithinDateRange()
    {
        await using var db = BuildDb(nameof(GatherVariablesAsync_OnlyReturnsPublicationsWithinDateRange));

        var area = new Area { Id = "area-a", Nombre = "Área A" };
        var user = new User { Id = "user-a", UserName = "u", UserLastName1 = "A", Email = "a@test.cu", AreaId = area.Id };
        var author = new Author { Id = "auth-a", LastName = "A", Name = "A", SearchKey = "a", LastNameKey = "a", UserId = user.Id, User = user };

        var inside  = CreateJournalPublication("Dentro del rango",  1, author, "2023-06-15");
        var before  = CreateJournalPublication("Antes del rango",   1, author, "2022-12-31");
        var after   = CreateJournalPublication("Después del rango", 1, author, "2024-01-01");
        var yearOnly = CreateJournalPublication("Solo año dentro",  1, author, "2023");

        db.Areas.Add(area);
        db.Users.Add(user);
        db.Authors.Add(author);
        db.Publications.AddRange(inside, before, after, yearOnly);
        await db.SaveChangesAsync();

        var parameters = new Dictionary<string, string>
        {
            ["from"] = "2023-01-01",
            ["to"]   = "2023-12-31",
        };

        var report = BuildReport(db, user.Id);
        var variables = await report.GatherVariablesAsync(parameters, CancellationToken.None);

        var g1 = (List<PublicacionG1RowDto>)variables["G1"];
        g1.Count.ShouldBe(2);
        g1.ShouldContain(r => r.Titulo == "Dentro del rango");
        g1.ShouldContain(r => r.Titulo == "Solo año dentro");
    }

    /// <summary>
    /// Si no se proporciona ningún parámetro de fecha, se devuelven todas las
    /// publicaciones del área (comportamiento anterior sin filtro de fecha).
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_WithNullParameters_ReturnsAllAreaPublications()
    {
        await using var db = BuildDb(nameof(GatherVariablesAsync_WithNullParameters_ReturnsAllAreaPublications));

        var area = new Area { Id = "area-a", Nombre = "Área A" };
        var user = new User { Id = "user-a", UserName = "u", UserLastName1 = "A", Email = "a@test.cu", AreaId = area.Id };
        var author = new Author { Id = "auth-a", LastName = "A", Name = "A", SearchKey = "a", LastNameKey = "a", UserId = user.Id, User = user };

        var pub2020 = CreateJournalPublication("Pub 2020", 1, author, "2020");
        var pub2023 = CreateJournalPublication("Pub 2023", 1, author, "2023");
        var pub2025 = CreateJournalPublication("Pub 2025", 1, author, "2025");

        db.Areas.Add(area);
        db.Users.Add(user);
        db.Authors.Add(author);
        db.Publications.AddRange(pub2020, pub2023, pub2025);
        await db.SaveChangesAsync();

        var report = BuildReport(db, user.Id);
        var variables = await report.GatherVariablesAsync(null, CancellationToken.None);

        ((List<PublicacionG1RowDto>)variables["G1"]).Count.ShouldBe(3);
    }

    /// <summary>
    /// Con solo el límite inferior (from), deben incluirse todas las publicaciones
    /// a partir de esa fecha.
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_WithOnlyFrom_FiltersCorrectly()
    {
        await using var db = BuildDb(nameof(GatherVariablesAsync_WithOnlyFrom_FiltersCorrectly));

        var area = new Area { Id = "area-a", Nombre = "Área A" };
        var user = new User { Id = "user-a", UserName = "u", UserLastName1 = "A", Email = "a@test.cu", AreaId = area.Id };
        var author = new Author { Id = "auth-a", LastName = "A", Name = "A", SearchKey = "a", LastNameKey = "a", UserId = user.Id, User = user };

        var old = CreateJournalPublication("Vieja",   1, author, "2019-05-01");
        var recent = CreateJournalPublication("Nueva", 1, author, "2023-03-01");

        db.Areas.Add(area);
        db.Users.Add(user);
        db.Authors.Add(author);
        db.Publications.AddRange(old, recent);
        await db.SaveChangesAsync();

        var parameters = new Dictionary<string, string> { ["from"] = "2022-01-01" };

        var report = BuildReport(db, user.Id);
        var variables = await report.GatherVariablesAsync(parameters, CancellationToken.None);

        var g1 = (List<PublicacionG1RowDto>)variables["G1"];
        g1.Count.ShouldBe(1);
        g1[0].Titulo.ShouldBe("Nueva");
    }

    // ── variables de conteo ───────────────────────────────────────────────────

    /// <summary>
    /// Los contadores escalares del diccionario (G1Count, G2Count, etc.) deben
    /// coincidir con la longitud real de las listas correspondientes.
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_CountVariables_MatchListLengths()
    {
        await using var db = BuildDb(nameof(GatherVariablesAsync_CountVariables_MatchListLengths));

        var area = new Area { Id = "area-a", Nombre = "Área A" };
        var user = new User { Id = "user-a", UserName = "u", UserLastName1 = "U", Email = "u@test.cu", AreaId = area.Id };
        var author = new Author { Id = "auth-a", LastName = "A", Name = "A", SearchKey = "a", LastNameKey = "a", UserId = user.Id, User = user };

        var g1 = CreateJournalPublication("G1", group: 1, author);
        var g2 = CreateJournalPublication("G2", group: 2, author);
        var libro = CreateIndexedPublication("Libro", PublicationType.Libro, author);
        var cap = CreateIndexedPublication("Cap", PublicationType.Capítulo, author);
        var divul = CreateIndexedPublication("Divul", PublicationType.Artículo_de_Divulgación, author);

        db.Areas.Add(area);
        db.Users.Add(user);
        db.Authors.Add(author);
        db.Publications.AddRange(g1, g2, libro, cap, divul);
        await db.SaveChangesAsync();

        var report = BuildReport(db, user.Id);
        var variables = await report.GatherVariablesAsync(null, CancellationToken.None);

        ((int)variables["G1Count"]).ShouldBe(((List<PublicacionG1RowDto>)variables["G1"]).Count);
        ((int)variables["G2Count"]).ShouldBe(((List<PublicacionJournalRowDto>)variables["G2"]).Count);
        ((int)variables["CapitulosCount"]).ShouldBe(((List<PublicacionIndexadaRowDto>)variables["Capitulos"]).Count);
        ((int)variables["LibrosMonografiasCount"]).ShouldBe(
            ((List<PublicacionIndexadaRowDto>)variables["Libros"]).Count +
            ((List<PublicacionIndexadaRowDto>)variables["Monografias"]).Count);
        ((int)variables["ArticulosDivulgacionCount"]).ShouldBe(((List<PublicacionDivulgacionRowDto>)variables["ArticulosDivulgacion"]).Count);
    }

    // ── campos de la fila G1 ──────────────────────────────────────────────────

    /// <summary>
    /// La fila G1 debe incluir el cuartil procedente de <see cref="JournalGroup1Publication"/>.
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_G1Row_HasCuartilFromJournalGroup1Publication()
    {
        await using var db = BuildDb(nameof(GatherVariablesAsync_G1Row_HasCuartilFromJournalGroup1Publication));

        var area = new Area { Id = "area-a", Nombre = "Área A" };
        var user = new User { Id = "user-a", UserName = "u", UserLastName1 = "U", Email = "u@test.cu", AreaId = area.Id };
        var author = new Author { Id = "auth-a", LastName = "A", Name = "A", SearchKey = "a", LastNameKey = "a", UserId = user.Id, User = user };

        var pubId = Guid.NewGuid().ToString();
        var pub = new Publication
        {
            Id = pubId,
            Title = "Pub G1 con Cuartil",
            PublicationData = "Datos",
            PublishedDate = "2024",
            PublicationType = PublicationType.Diario,
            JournalPublication = new JournalPublication
            {
                PublicationId = pubId,
                Group = 1,
                BaseDeDatos = new BaseDeDatosPublicacion { Nombre = "WoS" },
                JournalGroup1Publication = new JournalGroup1Publication
                {
                    PublicationId = pubId,
                    Cuartil = "Q1",
                },
            },
        };
        pub.AuthorPublications.Add(new AuthorPublication { AuthorId = author.Id, PublicationId = pubId, Author = author, Publication = pub });

        db.Areas.Add(area);
        db.Users.Add(user);
        db.Authors.Add(author);
        db.Publications.Add(pub);
        await db.SaveChangesAsync();

        var report = BuildReport(db, user.Id);
        var variables = await report.GatherVariablesAsync(null, CancellationToken.None);

        var g1 = (List<PublicacionG1RowDto>)variables["G1"];
        g1.Count.ShouldBe(1);
        g1[0].Cuartil.ShouldBe("Q1");
    }

    // ── BuildPublicationDetails ───────────────────────────────────────────────

    /// <summary>
    /// Cuando la publicación tiene DOI/URL, debe concatenarse al final de
    /// <c>DatosPublicacion</c> con el prefijo " DOI/URL: ".
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_BuildPublicationDetails_AppendsDoiWhenPresent()
    {
        await using var db = BuildDb(nameof(GatherVariablesAsync_BuildPublicationDetails_AppendsDoiWhenPresent));

        var area = new Area { Id = "area-a", Nombre = "Área A" };
        var user = new User { Id = "user-a", UserName = "u", UserLastName1 = "U", Email = "u@test.cu", AreaId = area.Id };
        var author = new Author { Id = "auth-a", LastName = "A", Name = "A", SearchKey = "a", LastNameKey = "a", UserId = user.Id, User = user };

        var pubWithDoi = CreateJournalPublication("Pub Con DOI", group: 1, author);
        pubWithDoi.UrlDoi = "https://doi.org/10.1234/test";
        pubWithDoi.PublicationData = "Revista X, Vol. 1";

        var pubNoDoi = CreateJournalPublication("Pub Sin DOI", group: 2, author);
        pubNoDoi.PublicationData = "Revista Y, Vol. 2";

        db.Areas.Add(area);
        db.Users.Add(user);
        db.Authors.Add(author);
        db.Publications.AddRange(pubWithDoi, pubNoDoi);
        await db.SaveChangesAsync();

        var report = BuildReport(db, user.Id);
        var variables = await report.GatherVariablesAsync(null, CancellationToken.None);

        var g1 = (List<PublicacionG1RowDto>)variables["G1"];
        g1.Count.ShouldBe(1);
        g1[0].DatosPublicacion.ShouldBe("Revista X, Vol. 1 DOI/URL: https://doi.org/10.1234/test");

        var g2 = (List<PublicacionJournalRowDto>)variables["G2"];
        g2.Count.ShouldBe(1);
        g2[0].DatosPublicacion.ShouldBe("Revista Y, Vol. 2");
    }

    // ── BuildAuthorsSummary ───────────────────────────────────────────────────

    /// <summary>
    /// Los autores deben listarse en orden alfabético, independientemente del orden
    /// en que fueron añadidos a la publicación.
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_AuthorSummary_IsAlphabeticallyOrdered()
    {
        await using var db = BuildDb(nameof(GatherVariablesAsync_AuthorSummary_IsAlphabeticallyOrdered));

        var area = new Area { Id = "area-a", Nombre = "Área A" };
        var user = new User { Id = "user-a", UserName = "u", UserLastName1 = "U", Email = "u@test.cu", AreaId = area.Id };
        // authorZ está vinculado al usuario del área solicitante → la pub se incluye
        var authorZ = new Author { Id = "auth-z", LastName = "Zorrilla", Name = "Zorrilla, Carlos", SearchKey = "zorrilla carlos", LastNameKey = "zorrilla", UserId = user.Id, User = user };
        var authorA = new Author { Id = "auth-a", LastName = "Álvarez", Name = "Álvarez, Ana", SearchKey = "alvarez ana", LastNameKey = "alvarez" };

        var pubId = Guid.NewGuid().ToString();
        var pub = new Publication
        {
            Id = pubId,
            Title = "Pub Multiautor",
            PublicationData = "Datos",
            PublishedDate = "2024",
            PublicationType = PublicationType.Diario,
            JournalPublication = new JournalPublication { PublicationId = pubId, Group = 1, BaseDeDatos = new BaseDeDatosPublicacion { Nombre = "WoS" } },
        };
        // Z añadido antes de A para verificar que el reporte ordena alfabéticamente
        pub.AuthorPublications.Add(new AuthorPublication { AuthorId = authorZ.Id, PublicationId = pubId, Author = authorZ, Publication = pub });
        pub.AuthorPublications.Add(new AuthorPublication { AuthorId = authorA.Id, PublicationId = pubId, Author = authorA, Publication = pub });

        db.Areas.Add(area);
        db.Users.Add(user);
        db.Authors.AddRange(authorZ, authorA);
        db.Publications.Add(pub);
        await db.SaveChangesAsync();

        var report = BuildReport(db, user.Id);
        var variables = await report.GatherVariablesAsync(null, CancellationToken.None);

        var g1 = (List<PublicacionG1RowDto>)variables["G1"];
        g1.Count.ShouldBe(1);
        g1[0].RelacionAutoria.ShouldBe("Álvarez, Ana, Zorrilla, Carlos");
    }
}
