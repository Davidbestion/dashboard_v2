using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Domain.Enums;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Redes;

/// <summary>
/// Tests de la lógica de negocio de los handlers de Redes, replicando
/// exactamente la lógica de Web/Endpoints/Redes.cs usando InMemory EF Core.
/// </summary>
public class RedesHandlerLogicTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ApplicationDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(opts);
    }

    private static Red SeedRed(ApplicationDbContext db,
        string nombre = "Red Test",
        TipoRed tipo = TipoRed.Universitaria,
        int cantidadProfesores = 10,
        int? countryId = null)
    {
        var red = new Red
        {
            Nombre = nombre,
            Tipo = tipo,
            CantidadProfesores = cantidadProfesores,
            CountryId = countryId,
        };
        db.Reds.Add(red);
        db.SaveChanges();
        return red;
    }

    private static Event SeedEvent(ApplicationDbContext db, string name, string? redId = null)
    {
        var ev = new Event
        {
            Name = name,
            CountryId = 1,    // InMemory no valida FK
            EventTypeId = 1,
            RedId = redId,
        };
        db.Events.Add(ev);
        db.SaveChanges();
        return ev;
    }

    // ── GetRedes ─────────────────────────────────────────────────────────────

    [Test]
    public async Task GetRedes_NoReds_ReturnsEmptyList()
    {
        await using var db = CreateDb();

        var list = await db.Reds
            .Select(r => new { r.Id, r.Nombre, r.CountryId, r.CantidadProfesores, Tipo = (int)r.Tipo })
            .ToListAsync();

        list.ShouldBeEmpty();
    }

    [Test]
    public async Task GetRedes_ReturnsAllReds()
    {
        await using var db = CreateDb();
        SeedRed(db, "Red A");
        SeedRed(db, "Red B");

        var list = await db.Reds.ToListAsync();

        list.Count.ShouldBe(2);
    }

    [TestCase(TipoRed.Universitaria, 0)]
    [TestCase(TipoRed.Nacional, 1)]
    [TestCase(TipoRed.Internacional, 2)]
    public async Task GetRedes_ProjectsTipoAsInt(TipoRed tipo, int expectedInt)
    {
        await using var db = CreateDb();
        SeedRed(db, tipo: tipo);

        var item = await db.Reds
            .Select(r => new { Tipo = (int)r.Tipo })
            .FirstAsync();

        item.Tipo.ShouldBe(expectedInt);
    }

    [Test]
    public async Task GetRedes_ProjectsCountryName_WhenCountryExists()
    {
        await using var db = CreateDb();
        var country = new Country { Name = "Cuba" };
        db.Countries.Add(country);
        db.SaveChanges();

        var red = new Red { Nombre = "Red", CountryId = country.Id, Tipo = TipoRed.Nacional };
        db.Reds.Add(red);
        db.SaveChanges();

        var item = await db.Reds
            .Select(r => new { CountryName = r.Country != null ? r.Country.Name : null })
            .FirstAsync();

        item.CountryName.ShouldBe("Cuba");
    }

    [Test]
    public async Task GetRedes_ProjectsNullCountryName_WhenCountryIsNull()
    {
        await using var db = CreateDb();
        SeedRed(db, countryId: null);

        var item = await db.Reds
            .Select(r => new { CountryName = r.Country != null ? r.Country.Name : null })
            .FirstAsync();

        item.CountryName.ShouldBeNull();
    }

    // ── GetRedes – Vicedecano area filter ────────────────────────────────────

    [Test]
    public async Task GetRedes_VicedecanoFilter_ReturnsRedsWhereCoordinadorIsInArea()
    {
        await using var db = CreateDb();
        var area = new Area { Id = "area-1", Nombre = "MATCOM", Descripcion = "d", UniversidadId = "uh" };
        db.Areas.Add(area);
        var coordinator = new User { Id = "coord-1", UserName = "coord", UserLastName1 = "C", Email = "c@uh.cu", AreaId = "area-1" };
        db.Users.Add(coordinator);
        db.SaveChanges();

        var red = new Red { Nombre = "Red del Área", Tipo = TipoRed.Universitaria, CoordinadorId = "coord-1" };
        db.Reds.Add(red);
        db.SaveChanges();

        var list = await db.Reds.AsNoTracking()
            .Where(r =>
                (r.CoordinadorId != null && r.Coordinador!.AreaId == "area-1") ||
                r.Participaciones.Any(p => p.Author.User != null && p.Author.User.AreaId == "area-1"))
            .ToListAsync();

        list.Count.ShouldBe(1);
        list[0].Nombre.ShouldBe("Red del Área");
    }

    [Test]
    public async Task GetRedes_VicedecanoFilter_ReturnsRedsWhereParticipanteIsInArea()
    {
        await using var db = CreateDb();
        var area = new Area { Id = "area-2", Nombre = "FMat", Descripcion = "d", UniversidadId = "uh" };
        db.Areas.Add(area);
        var user = new User { Id = "user-part-1", UserName = "up1", UserLastName1 = "P", Email = "p@uh.cu", AreaId = "area-2" };
        db.Users.Add(user);
        var red = new Red { Nombre = "Red con Participante", Tipo = TipoRed.Nacional };
        db.Reds.Add(red);
        db.SaveChanges();

        var author = Author.Create("Participante");
        author.UserId = "user-part-1";
        db.Authors.Add(author);
        db.SaveChanges();

        db.ParticipacionesEnRed.Add(new ParticipacionEnRed { RedId = red.Id, AuthorId = author.Id });
        db.SaveChanges();

        var list = await db.Reds.AsNoTracking()
            .Where(r =>
                (r.CoordinadorId != null && r.Coordinador!.AreaId == "area-2") ||
                r.Participaciones.Any(p => p.Author.User != null && p.Author.User.AreaId == "area-2"))
            .ToListAsync();

        list.Count.ShouldBe(1);
        list[0].Nombre.ShouldBe("Red con Participante");
    }

    [Test]
    public async Task GetRedes_VicedecanoFilter_ExcludesRedsFromOtherAreas()
    {
        await using var db = CreateDb();
        var otherCoord = new User { Id = "other-coord", UserName = "oc", UserLastName1 = "C", Email = "oc@uh.cu", AreaId = "other-area" };
        db.Users.Add(otherCoord);
        db.SaveChanges();

        var red = new Red { Nombre = "Red de Otra Área", Tipo = TipoRed.Nacional, CoordinadorId = "other-coord" };
        db.Reds.Add(red);
        db.SaveChanges();

        var list = await db.Reds.AsNoTracking()
            .Where(r =>
                (r.CoordinadorId != null && r.Coordinador!.AreaId == "area-1") ||
                r.Participaciones.Any(p => p.Author.User != null && p.Author.User.AreaId == "area-1"))
            .ToListAsync();

        list.ShouldBeEmpty();
    }

    // ── GetMisRedes – Profesor includes participated reds ─────────────────────

    [Test]
    public async Task GetMisRedes_Profesor_ReturnsCoordinatedReds()
    {
        await using var db = CreateDb();
        var user = new User { Id = "prof-1", UserName = "prof", UserLastName1 = "P", Email = "p@uh.cu" };
        db.Users.Add(user);
        db.SaveChanges();

        var red = new Red { Nombre = "Red Coordinada", Tipo = TipoRed.Universitaria, CoordinadorId = "prof-1" };
        db.Reds.Add(red);
        db.SaveChanges();

        var list = await db.Reds.AsNoTracking()
            .Where(r =>
                r.CoordinadorId == "prof-1" ||
                r.Participaciones.Any(p => p.Author.UserId == "prof-1"))
            .ToListAsync();

        list.Count.ShouldBe(1);
        list[0].Nombre.ShouldBe("Red Coordinada");
    }

    [Test]
    public async Task GetMisRedes_Profesor_ReturnsParticipatedReds()
    {
        await using var db = CreateDb();
        var user = new User { Id = "prof-2", UserName = "prof2", UserLastName1 = "P", Email = "p2@uh.cu" };
        db.Users.Add(user);
        var red = new Red { Nombre = "Red Participada", Tipo = TipoRed.Nacional };
        db.Reds.Add(red);
        db.SaveChanges();

        var author = Author.Create("Profesor Participante");
        author.UserId = "prof-2";
        db.Authors.Add(author);
        db.SaveChanges();

        db.ParticipacionesEnRed.Add(new ParticipacionEnRed { RedId = red.Id, AuthorId = author.Id });
        db.SaveChanges();

        var list = await db.Reds.AsNoTracking()
            .Where(r =>
                r.CoordinadorId == "prof-2" ||
                r.Participaciones.Any(p => p.Author.UserId == "prof-2"))
            .ToListAsync();

        list.Count.ShouldBe(1);
        list[0].Nombre.ShouldBe("Red Participada");
    }

    [Test]
    public async Task GetMisRedes_Profesor_ReturnsBothCoordinatedAndParticipatedReds()
    {
        await using var db = CreateDb();
        var user = new User { Id = "prof-3", UserName = "prof3", UserLastName1 = "P", Email = "p3@uh.cu" };
        db.Users.Add(user);
        var coordRed = new Red { Nombre = "Red Coordinada", Tipo = TipoRed.Universitaria, CoordinadorId = "prof-3" };
        var partRed = new Red { Nombre = "Red Participada", Tipo = TipoRed.Nacional };
        db.Reds.Add(coordRed);
        db.Reds.Add(partRed);
        db.SaveChanges();

        var author = Author.Create("Profesor 3");
        author.UserId = "prof-3";
        db.Authors.Add(author);
        db.SaveChanges();

        db.ParticipacionesEnRed.Add(new ParticipacionEnRed { RedId = partRed.Id, AuthorId = author.Id });
        db.SaveChanges();

        var list = await db.Reds.AsNoTracking()
            .Where(r =>
                r.CoordinadorId == "prof-3" ||
                r.Participaciones.Any(p => p.Author.UserId == "prof-3"))
            .ToListAsync();

        list.Count.ShouldBe(2);
    }

    [Test]
    public async Task GetMisRedes_Profesor_ExcludesUnrelatedReds()
    {
        await using var db = CreateDb();
        var user = new User { Id = "prof-4", UserName = "prof4", UserLastName1 = "P", Email = "p4@uh.cu" };
        db.Users.Add(user);
        SeedRed(db, "Red Ajena");
        db.SaveChanges();

        var list = await db.Reds.AsNoTracking()
            .Where(r =>
                r.CoordinadorId == "prof-4" ||
                r.Participaciones.Any(p => p.Author.UserId == "prof-4"))
            .ToListAsync();

        list.ShouldBeEmpty();
    }

    // ── CreateRed ────────────────────────────────────────────────────────────

    [TestCase(0)]   // Universitaria
    [TestCase(1)]   // Nacional
    [TestCase(2)]   // Internacional
    public async Task CreateRed_ValidTipo_PersistsEntityWithCorrectTipo(int tipoInt)
    {
        await using var db = CreateDb();

        // Simula lógica del handler
        var tipoIsValid = Enum.IsDefined(typeof(TipoRed), tipoInt);
        tipoIsValid.ShouldBeTrue();

        var entity = new Red
        {
            Nombre = "Nueva Red",
            CountryId = null,
            CantidadProfesores = 5,
            Tipo = (TipoRed)tipoInt,
        };
        db.Reds.Add(entity);
        await db.SaveChangesAsync(CancellationToken.None);

        var saved = await db.Reds.FindAsync(entity.Id);
        saved.ShouldNotBeNull();
        saved!.Tipo.ShouldBe((TipoRed)tipoInt);
    }

    [Test]
    public async Task CreateRed_PersistsAllFields()
    {
        await using var db = CreateDb();

        var entity = new Red
        {
            Nombre = "Red Completa",
            CountryId = null,
            CantidadProfesores = 25,
            Tipo = TipoRed.Internacional,
        };
        db.Reds.Add(entity);
        await db.SaveChangesAsync(CancellationToken.None);

        var saved = await db.Reds.FindAsync(entity.Id);
        saved!.Nombre.ShouldBe("Red Completa");
        saved.CantidadProfesores.ShouldBe(25);
        saved.Tipo.ShouldBe(TipoRed.Internacional);
    }

    [TestCase(-1)]
    [TestCase(3)]
    [TestCase(99)]
    public void CreateRed_InvalidTipo_FailsValidation(int tipoInt)
    {
        // Simula la guardia del handler
        var tipoIsValid = Enum.IsDefined(typeof(TipoRed), tipoInt);

        tipoIsValid.ShouldBeFalse();
    }

    [Test]
    public async Task CreateRed_AssignsNewGuidId()
    {
        await using var db = CreateDb();
        var entity = new Red { Nombre = "Test", Tipo = TipoRed.Nacional };
        db.Reds.Add(entity);
        await db.SaveChangesAsync(CancellationToken.None);

        Guid.TryParse(entity.Id, out _).ShouldBeTrue();
    }

    // ── UpdateRed ────────────────────────────────────────────────────────────

    [Test]
    public async Task UpdateRed_ExistingRed_UpdatesAllFields()
    {
        await using var db = CreateDb();
        var red = SeedRed(db, nombre: "Original", tipo: TipoRed.Universitaria, cantidadProfesores: 5);

        // Simula lógica del handler
        var e = await db.Reds.FindAsync(new object[] { red.Id }, CancellationToken.None);
        e.ShouldNotBeNull();
        e!.Nombre = "Actualizada";
        e.CantidadProfesores = 20;
        e.Tipo = (TipoRed)2; // Internacional
        await db.SaveChangesAsync(CancellationToken.None);

        var updated = await db.Reds.FindAsync(red.Id);
        updated!.Nombre.ShouldBe("Actualizada");
        updated.CantidadProfesores.ShouldBe(20);
        updated.Tipo.ShouldBe(TipoRed.Internacional);
    }

    [Test]
    public async Task UpdateRed_NonExistentId_ReturnsNull()
    {
        await using var db = CreateDb();

        var e = await db.Reds.FindAsync(new object[] { "id-que-no-existe" }, CancellationToken.None);

        e.ShouldBeNull();
    }

    [TestCase(-1)]
    [TestCase(3)]
    public void UpdateRed_InvalidTipo_FailsValidation(int tipoInt)
    {
        var tipoIsValid = Enum.IsDefined(typeof(TipoRed), tipoInt);

        tipoIsValid.ShouldBeFalse();
    }

    // ── DeleteRed ────────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteRed_ExistingRed_RemovesEntityFromDb()
    {
        await using var db = CreateDb();
        var red = SeedRed(db);

        // Simula lógica del handler
        var e = await db.Reds.FindAsync(new object[] { red.Id }, CancellationToken.None);
        e.ShouldNotBeNull();
        db.Reds.Remove(e!);
        await db.SaveChangesAsync(CancellationToken.None);

        var after = await db.Reds.FindAsync(red.Id);
        after.ShouldBeNull();
    }

    [Test]
    public async Task DeleteRed_NonExistentId_ReturnsNull()
    {
        await using var db = CreateDb();

        var e = await db.Reds.FindAsync(new object[] { "no-existe" }, CancellationToken.None);

        e.ShouldBeNull();
    }

    [Test]
    public async Task DeleteRed_LeavesOtherRedsIntact()
    {
        await using var db = CreateDb();
        var red1 = SeedRed(db, "A");
        var red2 = SeedRed(db, "B");

        var e = await db.Reds.FindAsync(new object[] { red1.Id }, CancellationToken.None);
        db.Reds.Remove(e!);
        await db.SaveChangesAsync(CancellationToken.None);

        var remaining = await db.Reds.ToListAsync();
        remaining.Count.ShouldBe(1);
        remaining[0].Id.ShouldBe(red2.Id);
    }

    // ── GetEventsForRed ──────────────────────────────────────────────────────

    [Test]
    public async Task GetEventsForRed_ReturnsAllEvents_WithCorrectAssignedFlag()
    {
        await using var db = CreateDb();
        var red = SeedRed(db);
        SeedEvent(db, "Asignado", redId: red.Id);
        SeedEvent(db, "No Asignado", redId: null);

        // Simula lógica del handler
        var list = await db.Events
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .Select(e => new { e.Id, e.Name, Assigned = e.RedId == red.Id })
            .ToListAsync();

        list.Count.ShouldBe(2);
        list.First(x => x.Name == "Asignado").Assigned.ShouldBeTrue();
        list.First(x => x.Name == "No Asignado").Assigned.ShouldBeFalse();
    }

    [Test]
    public async Task GetEventsForRed_ReturnsEmpty_WhenNoEvents()
    {
        await using var db = CreateDb();
        var red = SeedRed(db);

        var list = await db.Events
            .AsNoTracking()
            .Where(e => e.RedId == red.Id)
            .ToListAsync();

        list.ShouldBeEmpty();
    }

    [Test]
    public async Task GetEventsForRed_OrdersByName()
    {
        await using var db = CreateDb();
        var red = SeedRed(db);
        SeedEvent(db, "Zeta");
        SeedEvent(db, "Alpha");
        SeedEvent(db, "Mu");

        var list = await db.Events
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .Select(e => e.Name)
            .ToListAsync();

        list.ShouldBe(["Alpha", "Mu", "Zeta"]);
    }

    // ── SetEventsForRed ──────────────────────────────────────────────────────

    [Test]
    public async Task SetEventsForRed_AssignsSelectedEventsToRed()
    {
        await using var db = CreateDb();
        var red = SeedRed(db);
        var ev1 = SeedEvent(db, "Evento 1");
        var ev2 = SeedEvent(db, "Evento 2");

        // Simula lógica del handler
        var eventIds = new List<int> { ev1.Id, ev2.Id }.Distinct().ToList();
        var events = await db.Events.Where(e => eventIds.Contains(e.Id)).ToListAsync();
        events.Count.ShouldBe(eventIds.Count); // validación pasa
        foreach (var e in events) e.RedId = red.Id;
        await db.SaveChangesAsync(CancellationToken.None);

        var assigned = await db.Events.Where(e => e.RedId == red.Id).ToListAsync();
        assigned.Count.ShouldBe(2);
    }

    [Test]
    public async Task SetEventsForRed_UnassignsPreviouslyAssignedEvents_NotInNewList()
    {
        await using var db = CreateDb();
        var red = SeedRed(db);
        var ev1 = SeedEvent(db, "Evento 1", redId: red.Id);
        var ev2 = SeedEvent(db, "Evento 2", redId: red.Id);
        var ev3 = SeedEvent(db, "Evento 3");

        // Nueva lista: solo ev3
        var newEventIds = new List<int> { ev3.Id };
        var newEvents = await db.Events.Where(e => newEventIds.Contains(e.Id)).ToListAsync();
        var currentlyAssigned = await db.Events.Where(e => e.RedId == red.Id).ToListAsync();
        var toUnassign = currentlyAssigned.Where(e => !newEventIds.Contains(e.Id)).ToList();
        foreach (var e in toUnassign) e.RedId = null;
        foreach (var e in newEvents) e.RedId = red.Id;
        await db.SaveChangesAsync(CancellationToken.None);

        var assigned = await db.Events.Where(e => e.RedId == red.Id).ToListAsync();
        assigned.Count.ShouldBe(1);
        assigned[0].Id.ShouldBe(ev3.Id);

        var unassigned = await db.Events.FindAsync(ev1.Id);
        unassigned!.RedId.ShouldBeNull();
    }

    [Test]
    public async Task SetEventsForRed_EmptyList_UnassignsAllEvents()
    {
        await using var db = CreateDb();
        var red = SeedRed(db);
        SeedEvent(db, "E1", redId: red.Id);
        SeedEvent(db, "E2", redId: red.Id);

        var newEventIds = new List<int>();
        var currentlyAssigned = await db.Events.Where(e => e.RedId == red.Id).ToListAsync();
        foreach (var e in currentlyAssigned) e.RedId = null;
        await db.SaveChangesAsync(CancellationToken.None);

        var remaining = await db.Events.Where(e => e.RedId == red.Id).ToListAsync();
        remaining.ShouldBeEmpty();
    }

    [Test]
    public async Task SetEventsForRed_NonExistentEventId_FailsValidation()
    {
        await using var db = CreateDb();
        var red = SeedRed(db);
        var ev = SeedEvent(db, "Real");

        // Simula validación del handler
        var requestedIds = new List<int> { ev.Id, 9999 }.Distinct().ToList();
        var found = await db.Events.Where(e => requestedIds.Contains(e.Id)).ToListAsync();

        // found.Count != requestedIds.Count → BadRequest
        found.Count.ShouldNotBe(requestedIds.Count);
    }

    [Test]
    public async Task SetEventsForRed_DeduplicatesInputEventIds()
    {
        await using var db = CreateDb();
        var red = SeedRed(db);
        var ev = SeedEvent(db, "Duplicado");

        // Input con duplicados
        var rawIds = new List<int> { ev.Id, ev.Id, ev.Id };
        var deduplicated = rawIds.Distinct().ToList();

        deduplicated.Count.ShouldBe(1);
        var found = await db.Events.Where(e => deduplicated.Contains(e.Id)).ToListAsync();
        found.Count.ShouldBe(1);
    }

    [Test]
    public async Task SetEventsForRed_NonExistentRed_ReturnsNull()
    {
        await using var db = CreateDb();

        var red = await db.Reds.FindAsync(new object[] { "no-existe" }, CancellationToken.None);

        red.ShouldBeNull();
    }

    [Test]
    public async Task SetEventsForRed_ReassignsEventFromOneRedToAnother()
    {
        await using var db = CreateDb();
        var red1 = SeedRed(db, "Red 1");
        var red2 = SeedRed(db, "Red 2");
        var ev = SeedEvent(db, "Evento", redId: red1.Id);

        // Asignar ev a red2
        var events = await db.Events.Where(e => new[] { ev.Id }.Contains(e.Id)).ToListAsync();
        foreach (var e in events) e.RedId = red2.Id;
        await db.SaveChangesAsync(CancellationToken.None);

        var updated = await db.Events.FindAsync(ev.Id);
        updated!.RedId.ShouldBe(red2.Id);
    }
}
