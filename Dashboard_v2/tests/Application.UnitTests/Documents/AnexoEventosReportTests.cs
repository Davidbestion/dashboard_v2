using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Documents.Reports;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Documents;

/// <summary>
/// Tests unitarios para <see cref="AnexoEventosReport"/>.
///
/// Verifican que la clasificación por tipo, las columnas de ubicación,
/// los eventos coauspiciados y los conteos de ponencias funcionan correctamente.
/// </summary>
[TestFixture]
public class AnexoEventosReportTests
{
    private const int Internacional = 0;
    private const int Nacional = 1;

    // ── helpers ───────────────────────────────────────────────────────────────

    private static ApplicationDbContext BuildDb(string dbName)
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(opts);
    }

    private static AnexoEventosReport BuildReport(ApplicationDbContext db, string userId)
    {
        var currentUser = new Mock<IUser>();
        currentUser.Setup(u => u.Id).Returns(userId);
        return new AnexoEventosReport(db, currentUser.Object);
    }

    // ── clasificación por tipo ────────────────────────────────────────────────

    /// <summary>
    /// Los eventos internacionales (EventTypeId == 0) deben aparecer solo en
    /// EventosInternacionales y los nacionales (EventTypeId == 1) solo en EventosNacionales.
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_ClassifiesEventsAsInternacionalOrNacional()
    {
        await using var db = BuildDb(nameof(GatherVariablesAsync_ClassifiesEventsAsInternacionalOrNacional));

        var cuba = new Country { Id = 1, Name = "Cuba" };
        var user = new User { Id = "u", UserName = "u", UserLastName1 = "U", Email = "u@test.cu" };
        var intlEv = new Event { Name = "Congreso Internacional", CountryId = cuba.Id, EventTypeId = Internacional };
        var natlEv = new Event { Name = "Taller Nacional", CountryId = cuba.Id, EventTypeId = Nacional };

        db.Countries.Add(cuba);
        db.Users.Add(user);
        db.Events.AddRange(intlEv, natlEv);
        await db.SaveChangesAsync();

        var report = BuildReport(db, user.Id);
        var variables = await report.GatherVariablesAsync(null, CancellationToken.None);

        var intlList = (List<EventoInternacionalRowDto>)variables["EventosInternacionales"];
        var natlList = (List<EventoNacionalRowDto>)variables["EventosNacionales"];

        intlList.Count.ShouldBe(1);
        intlList[0].NombreEventoInternacional.ShouldBe("Congreso Internacional");
        natlList.Count.ShouldBe(1);
        natlList[0].NombreEventoNacional.ShouldBe("Taller Nacional");
    }

    // ── columnas de ubicación para eventos internacionales ────────────────────

    /// <summary>
    /// Evento internacional celebrado fuera de Cuba: debe rellenarse la columna
    /// PaisSiFueEnElExtranjero y dejarse vacía EnCuba.
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_InternacionalEventAbroad_FillsPaisExtranjeroColumn()
    {
        await using var db = BuildDb(nameof(GatherVariablesAsync_InternacionalEventAbroad_FillsPaisExtranjeroColumn));

        var spain = new Country { Id = 2, Name = "España" };
        var user = new User { Id = "u", UserName = "u", UserLastName1 = "U", Email = "u@test.cu" };
        var ev = new Event { Name = "Evento en España", CountryId = spain.Id, EventTypeId = Internacional };

        db.Countries.Add(spain);
        db.Users.Add(user);
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var report = BuildReport(db, user.Id);
        var variables = await report.GatherVariablesAsync(null, CancellationToken.None);

        var row = ((List<EventoInternacionalRowDto>)variables["EventosInternacionales"])[0];
        row.PaisSiFueEnElExtranjero.ShouldBe("España");
        row.EnCuba.ShouldBeEmpty();
    }

    /// <summary>
    /// Evento internacional celebrado en Cuba: debe rellenarse la columna EnCuba
    /// (con el nombre del país) y dejarse vacía PaisSiFueEnElExtranjero.
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_InternacionalEventInCuba_FillsEnCubaColumn()
    {
        await using var db = BuildDb(nameof(GatherVariablesAsync_InternacionalEventInCuba_FillsEnCubaColumn));

        var cuba = new Country { Id = 1, Name = "Cuba" };
        var user = new User { Id = "u", UserName = "u", UserLastName1 = "U", Email = "u@test.cu" };
        var ev = new Event { Name = "Evento Internacional en Cuba", CountryId = cuba.Id, EventTypeId = Internacional };

        db.Countries.Add(cuba);
        db.Users.Add(user);
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var report = BuildReport(db, user.Id);
        var variables = await report.GatherVariablesAsync(null, CancellationToken.None);

        var row = ((List<EventoInternacionalRowDto>)variables["EventosInternacionales"])[0];
        row.PaisSiFueEnElExtranjero.ShouldBeEmpty();
        row.EnCuba.ShouldBe("Cuba");
    }

    // ── resumen de instituciones ──────────────────────────────────────────────

    /// <summary>
    /// El resumen de instituciones de un evento nacional debe deduplicar nombres
    /// iguales (case-insensitive) y ordenarlos alfabéticamente.
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_EventosNacionales_ShowsDeduplicatedAndOrderedInstitutions()
    {
        await using var db = BuildDb(nameof(GatherVariablesAsync_EventosNacionales_ShowsDeduplicatedAndOrderedInstitutions));

        var cuba = new Country { Id = 1, Name = "Cuba" };
        var user = new User { Id = "u", UserName = "u", UserLastName1 = "U", Email = "u@test.cu" };
        var inst1 = new Institution { Nombre = "UH" };
        var inst2 = new Institution { Nombre = "UH" }; // nombre duplicado
        var inst3 = new Institution { Nombre = "MES" };
        var ev = new Event
        {
            Name = "Taller Nacional",
            CountryId = cuba.Id,
            EventTypeId = Nacional,
            Institutions = new List<Institution> { inst1, inst2, inst3 },
        };

        db.Countries.Add(cuba);
        db.Users.Add(user);
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var report = BuildReport(db, user.Id);
        var variables = await report.GatherVariablesAsync(null, CancellationToken.None);

        var row = ((List<EventoNacionalRowDto>)variables["EventosNacionales"])[0];
        // Deduplicado y ordenado: MES, UH
        row.InstitucionQueLoOrganizo.ShouldBe("MES, UH");
    }

    // ── eventos coauspiciados ────────────────────────────────────────────────

    /// <summary>
    /// Solo deben aparecer en EventosCoauspiciados los eventos que tienen como área
    /// patrocinadora el área del usuario solicitante.
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_EventosCoauspiciados_ReturnsOnlyEventsPatrocinatedByUserArea()
    {
        await using var db = BuildDb(nameof(GatherVariablesAsync_EventosCoauspiciados_ReturnsOnlyEventsPatrocinatedByUserArea));

        var cuba = new Country { Id = 1, Name = "Cuba" };
        var areaA = new Area { Id = "area-a", Nombre = "Área A" };
        var areaB = new Area { Id = "area-b", Nombre = "Área B" };
        var user = new User { Id = "user-a", UserName = "u", UserLastName1 = "U", Email = "u@test.cu", AreaId = areaA.Id };
        var evSi = new Event { Name = "Evento Coauspiciado", CountryId = cuba.Id, EventTypeId = Nacional };
        var evNo = new Event { Name = "Evento Sin Auspicio", CountryId = cuba.Id, EventTypeId = Nacional };

        db.Countries.Add(cuba);
        db.Areas.AddRange(areaA, areaB);
        db.Users.Add(user);
        db.Events.AddRange(evSi, evNo);
        await db.SaveChangesAsync();

        db.EventAreasPatrocinio.Add(new EventAreaPatrocinio { EventId = evSi.Id, AreaId = areaA.Id });
        db.EventAreasPatrocinio.Add(new EventAreaPatrocinio { EventId = evNo.Id, AreaId = areaB.Id });
        await db.SaveChangesAsync();

        var report = BuildReport(db, user.Id);
        var variables = await report.GatherVariablesAsync(null, CancellationToken.None);

        var coauspiciados = (List<EventoCoauspiciadoRowDto>)variables["EventosCoauspiciados"];
        coauspiciados.Count.ShouldBe(1);
        coauspiciados[0].EventoCoauspiciado.ShouldBe("Evento Coauspiciado");
    }

    /// <summary>
    /// Si el usuario solicitante no tiene área asignada, EventosCoauspiciados debe
    /// estar vacío aunque existan eventos con áreas patrocinadoras.
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_EventosCoauspiciados_IsEmptyWhenUserHasNoArea()
    {
        await using var db = BuildDb(nameof(GatherVariablesAsync_EventosCoauspiciados_IsEmptyWhenUserHasNoArea));

        var cuba = new Country { Id = 1, Name = "Cuba" };
        var area = new Area { Id = "area-a", Nombre = "Área A" };
        var user = new User { Id = "user-no-area", UserName = "u", UserLastName1 = "U", Email = "u@test.cu", AreaId = null };
        var ev = new Event { Name = "Evento", CountryId = cuba.Id, EventTypeId = Nacional };

        db.Countries.Add(cuba);
        db.Areas.Add(area);
        db.Users.Add(user);
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        db.EventAreasPatrocinio.Add(new EventAreaPatrocinio { EventId = ev.Id, AreaId = area.Id });
        await db.SaveChangesAsync();

        var report = BuildReport(db, user.Id);
        var variables = await report.GatherVariablesAsync(null, CancellationToken.None);

        ((List<EventoCoauspiciadoRowDto>)variables["EventosCoauspiciados"]).ShouldBeEmpty();
    }

    /// <summary>
    /// Las columnas Internacional y Nacional de EventosCoauspiciados deben marcarse
    /// con "X" según el tipo del evento, y dejarse vacías en caso contrario.
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_EventosCoauspiciados_MarksInternacionalAndNacionalColumns()
    {
        await using var db = BuildDb(nameof(GatherVariablesAsync_EventosCoauspiciados_MarksInternacionalAndNacionalColumns));

        var cuba = new Country { Id = 1, Name = "Cuba" };
        var spain = new Country { Id = 2, Name = "España" };
        var area = new Area { Id = "area-a", Nombre = "Área A" };
        var user = new User { Id = "user-a", UserName = "u", UserLastName1 = "U", Email = "u@test.cu", AreaId = area.Id };
        var intlEv = new Event { Name = "Evento Internacional Coauspiciado", CountryId = spain.Id, EventTypeId = Internacional };
        var natlEv = new Event { Name = "Evento Nacional Coauspiciado", CountryId = cuba.Id, EventTypeId = Nacional };

        db.Countries.AddRange(cuba, spain);
        db.Areas.Add(area);
        db.Users.Add(user);
        db.Events.AddRange(intlEv, natlEv);
        await db.SaveChangesAsync();

        db.EventAreasPatrocinio.AddRange(
            new EventAreaPatrocinio { EventId = intlEv.Id, AreaId = area.Id },
            new EventAreaPatrocinio { EventId = natlEv.Id, AreaId = area.Id });
        await db.SaveChangesAsync();

        var report = BuildReport(db, user.Id);
        var variables = await report.GatherVariablesAsync(null, CancellationToken.None);

        var coauspiciados = (List<EventoCoauspiciadoRowDto>)variables["EventosCoauspiciados"];
        coauspiciados.Count.ShouldBe(2);

        var intlRow = coauspiciados.First(r => r.EventoCoauspiciado == "Evento Internacional Coauspiciado");
        intlRow.Internacional.ShouldBe("X");
        intlRow.Nacional.ShouldBeEmpty();

        var natlRow = coauspiciados.First(r => r.EventoCoauspiciado == "Evento Nacional Coauspiciado");
        natlRow.Internacional.ShouldBeEmpty();
        natlRow.Nacional.ShouldBe("X");
    }

    // ── conteos de ponencias ─────────────────────────────────────────────────

    /// <summary>
    /// Los contadores de ponencias deben clasificar correctamente por tipo de evento
    /// y país de celebración.
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_PresentationCounts_AreCorrect()
    {
        await using var db = BuildDb(nameof(GatherVariablesAsync_PresentationCounts_AreCorrect));

        var cuba = new Country { Id = 1, Name = "Cuba" };
        var spain = new Country { Id = 2, Name = "España" };
        var user = new User { Id = "u", UserName = "u", UserLastName1 = "U", Email = "u@test.cu" };
        // 1 evento internacional en el extranjero (2 ponencias)
        var intlAbroad = new Event { Name = "Intl Abroad", CountryId = spain.Id, EventTypeId = Internacional };
        // 1 evento internacional en Cuba (1 ponencia)
        var intlCuba = new Event { Name = "Intl Cuba", CountryId = cuba.Id, EventTypeId = Internacional };
        // 1 evento nacional (3 ponencias)
        var natl = new Event { Name = "Natl", CountryId = cuba.Id, EventTypeId = Nacional };

        db.Countries.AddRange(cuba, spain);
        db.Users.Add(user);
        db.Events.AddRange(intlAbroad, intlCuba, natl);
        await db.SaveChangesAsync();

        db.Presentations.AddRange(
            new Presentation { Name = "P1", EventId = intlAbroad.Id },
            new Presentation { Name = "P2", EventId = intlAbroad.Id },
            new Presentation { Name = "P3", EventId = intlCuba.Id },
            new Presentation { Name = "P4", EventId = natl.Id },
            new Presentation { Name = "P5", EventId = natl.Id },
            new Presentation { Name = "P6", EventId = natl.Id });
        await db.SaveChangesAsync();

        var report = BuildReport(db, user.Id);
        var variables = await report.GatherVariablesAsync(null, CancellationToken.None);

        ((int)variables["PonenciasInternacionalesExtranjero"]).ShouldBe(2);
        ((int)variables["PonenciasInternacionalesCuba"]).ShouldBe(1);
        ((int)variables["PonenciasNacionalesCuba"]).ShouldBe(3);
        ((int)variables["PonenciasTotal"]).ShouldBe(6);
    }

    // ── datos de ponencias ───────────────────────────────────────────────────

    /// <summary>
    /// DatosPonencias debe incluir el nombre de la ponencia, el evento, el país de
    /// celebración y los autores de cada ponencia.
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_DatosPonencias_ContainsAllPresentationsWithCorrectData()
    {
        await using var db = BuildDb(nameof(GatherVariablesAsync_DatosPonencias_ContainsAllPresentationsWithCorrectData));

        var cuba = new Country { Id = 1, Name = "Cuba" };
        var user = new User { Id = "u", UserName = "u", UserLastName1 = "U", Email = "u@test.cu" };
        var author = new Author { Id = "auth-a", LastName = "López", Name = "López, Ana", SearchKey = "lopez ana", LastNameKey = "lopez" };
        var ev = new Event { Name = "Mi Evento", CountryId = cuba.Id, EventTypeId = Nacional };

        db.Countries.Add(cuba);
        db.Users.Add(user);
        db.Authors.Add(author);
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var pres = new Presentation { Name = "Mi Ponencia", EventId = ev.Id };
        db.Presentations.Add(pres);
        await db.SaveChangesAsync();

        db.AuthorPresentations.Add(new AuthorPresentation { AuthorId = author.Id, PresentationId = pres.Id });
        await db.SaveChangesAsync();

        var report = BuildReport(db, user.Id);
        var variables = await report.GatherVariablesAsync(null, CancellationToken.None);

        var datos = (List<DatosPonenciaRowDto>)variables["DatosPonencias"];
        datos.Count.ShouldBe(1);
        datos[0].NombrePonencia.ShouldBe("Mi Ponencia");
        datos[0].NombreEventoOActividadCientifica.ShouldBe("Mi Evento");
        datos[0].PaisDeCelebracion.ShouldBe("Cuba");
        datos[0].NombreAutores.ShouldBe("López, Ana");
    }

    /// <summary>
    /// DatosPonencias debe incluir todas las ponencias de todos los eventos,
    /// no solo las del primer evento.
    /// </summary>
    [Test]
    public async Task GatherVariablesAsync_DatosPonencias_AggregatesAcrossAllEvents()
    {
        await using var db = BuildDb(nameof(GatherVariablesAsync_DatosPonencias_AggregatesAcrossAllEvents));

        var cuba = new Country { Id = 1, Name = "Cuba" };
        var user = new User { Id = "u", UserName = "u", UserLastName1 = "U", Email = "u@test.cu" };
        var ev1 = new Event { Name = "Evento 1", CountryId = cuba.Id, EventTypeId = Nacional };
        var ev2 = new Event { Name = "Evento 2", CountryId = cuba.Id, EventTypeId = Internacional };

        db.Countries.Add(cuba);
        db.Users.Add(user);
        db.Events.AddRange(ev1, ev2);
        await db.SaveChangesAsync();

        db.Presentations.AddRange(
            new Presentation { Name = "Ponencia A", EventId = ev1.Id },
            new Presentation { Name = "Ponencia B", EventId = ev2.Id },
            new Presentation { Name = "Ponencia C", EventId = ev2.Id });
        await db.SaveChangesAsync();

        var report = BuildReport(db, user.Id);
        var variables = await report.GatherVariablesAsync(null, CancellationToken.None);

        var datos = (List<DatosPonenciaRowDto>)variables["DatosPonencias"];
        datos.Count.ShouldBe(3);
        datos.ShouldContain(d => d.NombrePonencia == "Ponencia A");
        datos.ShouldContain(d => d.NombrePonencia == "Ponencia B");
        datos.ShouldContain(d => d.NombrePonencia == "Ponencia C");
    }
}
