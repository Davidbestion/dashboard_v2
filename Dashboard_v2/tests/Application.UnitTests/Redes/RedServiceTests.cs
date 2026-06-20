using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Redes;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Domain.Enums;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Redes;

/// <summary>
/// Tests del <see cref="RedService"/> extraído del endpoint Redes.cs para mantener
/// la lógica de negocio en la capa de aplicación (consistente con ProyectoService/EventService).
/// </summary>
[TestFixture]
public class RedServiceTests
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

    private RedService MakeService(string userId, params string[] roles)
    {
        var userMock = new Mock<IUser>();
        userMock.Setup(u => u.Id).Returns(userId);
        userMock.Setup(u => u.Roles).Returns(roles.ToList());
        return new RedService(_db, userMock.Object);
    }

    private User SeedUser(string id, string? areaId = null)
    {
        var user = new User { Id = id, UserName = id, UserLastName1 = "L", Email = $"{id}@uh.cu", AreaId = areaId };
        _db.Users.Add(user);
        _db.SaveChanges();
        return user;
    }

    private Author SeedAuthorForUser(string userId, string name = "Autor")
    {
        var author = Author.Create(name);
        author.UserId = userId;
        _db.Authors.Add(author);
        _db.SaveChanges();
        return author;
    }

    // ── GetRedesAsync ────────────────────────────────────────────────────────

    [Test]
    public async Task GetRedesAsync_NonVicedecano_ReturnsAllReds()
    {
        _db.Reds.Add(new Red { Nombre = "Red A", Tipo = TipoRed.Universitaria });
        _db.Reds.Add(new Red { Nombre = "Red B", Tipo = TipoRed.Nacional });
        await _db.SaveChangesAsync();

        var result = await MakeService("prof-1", "Profesor").GetRedesAsync();

        result.Count.ShouldBe(2);
    }

    [Test]
    public async Task GetRedesAsync_Vicedecano_FiltersByCoordinadorArea()
    {
        SeedUser("vice-1", areaId: "area-1");
        var coord = SeedUser("coord-1", areaId: "area-1");
        var otherCoord = SeedUser("coord-2", areaId: "area-2");
        _db.Reds.Add(new Red { Nombre = "Red del Área", Tipo = TipoRed.Universitaria, CoordinadorId = coord.Id });
        _db.Reds.Add(new Red { Nombre = "Red de Otra Área", Tipo = TipoRed.Nacional, CoordinadorId = otherCoord.Id });
        await _db.SaveChangesAsync();

        var result = await MakeService("vice-1", "Vicedecano_de_investigacion").GetRedesAsync();

        result.Count.ShouldBe(1);
        result[0].Nombre.ShouldBe("Red del Área");
    }

    [Test]
    public async Task GetRedesAsync_Vicedecano_FiltersByParticipanteArea()
    {
        SeedUser("vice-2", areaId: "area-2");
        var participanteUser = SeedUser("part-user", areaId: "area-2");
        var red = new Red { Nombre = "Red con Participante", Tipo = TipoRed.Internacional };
        _db.Reds.Add(red);
        await _db.SaveChangesAsync();
        var author = SeedAuthorForUser(participanteUser.Id);
        _db.ParticipacionesEnRed.Add(new ParticipacionEnRed { RedId = red.Id, AuthorId = author.Id });
        await _db.SaveChangesAsync();

        var result = await MakeService("vice-2", "Vicedecano_de_investigacion").GetRedesAsync();

        result.Count.ShouldBe(1);
        result[0].Nombre.ShouldBe("Red con Participante");
    }

    [Test]
    public async Task GetRedesAsync_Vicedecano_WithoutArea_ReturnsAllReds()
    {
        SeedUser("vice-3", areaId: null);
        _db.Reds.Add(new Red { Nombre = "Cualquier Red", Tipo = TipoRed.Nacional });
        await _db.SaveChangesAsync();

        var result = await MakeService("vice-3", "Vicedecano_de_investigacion").GetRedesAsync();

        result.Count.ShouldBe(1);
    }

    // ── GetMisRedesAsync ─────────────────────────────────────────────────────

    [Test]
    public async Task GetMisRedesAsync_Superuser_ReturnsAllReds()
    {
        _db.Reds.Add(new Red { Nombre = "Red A", Tipo = TipoRed.Universitaria });
        _db.Reds.Add(new Red { Nombre = "Red B", Tipo = TipoRed.Nacional });
        await _db.SaveChangesAsync();

        var result = await MakeService("super-1", "Superuser").GetMisRedesAsync();

        result.Count.ShouldBe(2);
    }

    [Test]
    public async Task GetMisRedesAsync_JefeDeRedes_WithoutArea_ReturnsEmpty()
    {
        SeedUser("jefe-1", areaId: null);

        var result = await MakeService("jefe-1", "Jefe_de_Redes").GetMisRedesAsync();

        result.ShouldBeEmpty();
    }

    [Test]
    public async Task GetMisRedesAsync_JefeDeRedes_ReturnsRedsFromArea()
    {
        SeedUser("jefe-2", areaId: "area-x");
        var coord = SeedUser("coord-x", areaId: "area-x");
        _db.Reds.Add(new Red { Nombre = "Red del Jefe", Tipo = TipoRed.Universitaria, CoordinadorId = coord.Id });
        await _db.SaveChangesAsync();

        var result = await MakeService("jefe-2", "Jefe_de_Redes").GetMisRedesAsync();

        result.Count.ShouldBe(1);
        result[0].Nombre.ShouldBe("Red del Jefe");
    }

    [Test]
    public async Task GetMisRedesAsync_Profesor_ReturnsCoordinatedAndParticipatedReds()
    {
        SeedUser("prof-3");
        var coordRed = new Red { Nombre = "Coordinada", Tipo = TipoRed.Universitaria, CoordinadorId = "prof-3" };
        var partRed = new Red { Nombre = "Participada", Tipo = TipoRed.Nacional };
        _db.Reds.Add(coordRed);
        _db.Reds.Add(partRed);
        await _db.SaveChangesAsync();
        var author = SeedAuthorForUser("prof-3");
        _db.ParticipacionesEnRed.Add(new ParticipacionEnRed { RedId = partRed.Id, AuthorId = author.Id });
        await _db.SaveChangesAsync();

        var result = await MakeService("prof-3", "Profesor").GetMisRedesAsync();

        result.Count.ShouldBe(2);
        result.ShouldContain(r => r.Nombre == "Coordinada" && r.CoordinadorId == "prof-3");
        result.ShouldContain(r => r.Nombre == "Participada");
    }

    [Test]
    public async Task GetMisRedesAsync_Profesor_ExcludesUnrelatedReds()
    {
        SeedUser("prof-4");
        _db.Reds.Add(new Red { Nombre = "Ajena", Tipo = TipoRed.Universitaria });
        await _db.SaveChangesAsync();

        var result = await MakeService("prof-4", "Profesor").GetMisRedesAsync();

        result.ShouldBeEmpty();
    }

    // ── SetCoordinadorAsync ──────────────────────────────────────────────────

    [Test]
    public async Task SetCoordinadorAsync_RedNotFound_ReturnsFailure()
    {
        var result = await MakeService("jefe-1", "Jefe_de_Redes").SetCoordinadorAsync("no-existe", "user-1");

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("Red no encontrada.");
    }

    [Test]
    public async Task SetCoordinadorAsync_UnknownUser_ReturnsFailure()
    {
        var red = new Red { Nombre = "Red", Tipo = TipoRed.Nacional };
        _db.Reds.Add(red);
        await _db.SaveChangesAsync();

        var result = await MakeService("jefe-1", "Jefe_de_Redes").SetCoordinadorAsync(red.Id, "no-existe");

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("Usuario coordinador no encontrado.");
    }

    [Test]
    public async Task SetCoordinadorAsync_ValidUser_UpdatesCoordinador()
    {
        var coord = SeedUser("coord-9");
        var red = new Red { Nombre = "Red", Tipo = TipoRed.Nacional };
        _db.Reds.Add(red);
        await _db.SaveChangesAsync();

        var result = await MakeService("jefe-1", "Jefe_de_Redes").SetCoordinadorAsync(red.Id, coord.Id);

        result.Succeeded.ShouldBeTrue();
        (await _db.Reds.FindAsync(red.Id))!.CoordinadorId.ShouldBe(coord.Id);
    }

    [Test]
    public async Task SetCoordinadorAsync_NullId_ClearsCoordinador()
    {
        var coord = SeedUser("coord-10");
        var red = new Red { Nombre = "Red", Tipo = TipoRed.Nacional, CoordinadorId = coord.Id };
        _db.Reds.Add(red);
        await _db.SaveChangesAsync();

        var result = await MakeService("jefe-1", "Jefe_de_Redes").SetCoordinadorAsync(red.Id, null);

        result.Succeeded.ShouldBeTrue();
        (await _db.Reds.FindAsync(red.Id))!.CoordinadorId.ShouldBeNull();
    }

    // ── GetParticipantesAsync ────────────────────────────────────────────────

    [Test]
    public async Task GetParticipantesAsync_RedNotFound_ReturnsNotFound()
    {
        var (found, _) = await MakeService("jefe-1", "Jefe_de_Redes").GetParticipantesAsync("no-existe");

        found.ShouldBeFalse();
    }

    [Test]
    public async Task GetParticipantesAsync_ReturnsParticipantList()
    {
        var red = new Red { Nombre = "Red", Tipo = TipoRed.Nacional };
        _db.Reds.Add(red);
        await _db.SaveChangesAsync();
        var author = Author.Create("Participante");
        _db.Authors.Add(author);
        await _db.SaveChangesAsync();
        _db.ParticipacionesEnRed.Add(new ParticipacionEnRed { RedId = red.Id, AuthorId = author.Id });
        await _db.SaveChangesAsync();

        var (found, participantes) = await MakeService("jefe-1", "Jefe_de_Redes").GetParticipantesAsync(red.Id);

        found.ShouldBeTrue();
        participantes.Count.ShouldBe(1);
    }

    // ── AddParticipanteAsync / RemoveParticipanteAsync ──────────────────────

    [Test]
    public async Task AddParticipanteAsync_RedNotFound_ReturnsFailure()
    {
        var author = Author.Create("A");
        _db.Authors.Add(author);
        await _db.SaveChangesAsync();

        var result = await MakeService("jefe-1", "Jefe_de_Redes").AddParticipanteAsync("no-existe", author.Id);

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("Red no encontrada.");
    }

    [Test]
    public async Task AddParticipanteAsync_AuthorNotFound_ReturnsFailure()
    {
        var red = new Red { Nombre = "Red", Tipo = TipoRed.Nacional };
        _db.Reds.Add(red);
        await _db.SaveChangesAsync();

        var result = await MakeService("jefe-1", "Jefe_de_Redes").AddParticipanteAsync(red.Id, "no-existe");

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("Autor no encontrado.");
    }

    [Test]
    public async Task AddParticipanteAsync_AlreadyParticipant_ReturnsFailure()
    {
        var red = new Red { Nombre = "Red", Tipo = TipoRed.Nacional };
        _db.Reds.Add(red);
        var author = Author.Create("A");
        _db.Authors.Add(author);
        await _db.SaveChangesAsync();
        _db.ParticipacionesEnRed.Add(new ParticipacionEnRed { RedId = red.Id, AuthorId = author.Id });
        await _db.SaveChangesAsync();

        var result = await MakeService("jefe-1", "Jefe_de_Redes").AddParticipanteAsync(red.Id, author.Id);

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("El autor ya es participante de esta red.");
    }

    [Test]
    public async Task AddParticipanteAsync_Valid_PersistsParticipacion()
    {
        var red = new Red { Nombre = "Red", Tipo = TipoRed.Nacional };
        _db.Reds.Add(red);
        var author = Author.Create("A");
        _db.Authors.Add(author);
        await _db.SaveChangesAsync();

        var result = await MakeService("jefe-1", "Jefe_de_Redes").AddParticipanteAsync(red.Id, author.Id);

        result.Succeeded.ShouldBeTrue();
        (await _db.ParticipacionesEnRed.AnyAsync(p => p.RedId == red.Id && p.AuthorId == author.Id)).ShouldBeTrue();
    }

    [Test]
    public async Task RemoveParticipanteAsync_NotParticipant_ReturnsFailure()
    {
        var result = await MakeService("jefe-1", "Jefe_de_Redes").RemoveParticipanteAsync("red-x", "author-x");

        result.Succeeded.ShouldBeFalse();
    }

    [Test]
    public async Task RemoveParticipanteAsync_Valid_RemovesParticipacion()
    {
        var red = new Red { Nombre = "Red", Tipo = TipoRed.Nacional };
        _db.Reds.Add(red);
        var author = Author.Create("A");
        _db.Authors.Add(author);
        await _db.SaveChangesAsync();
        _db.ParticipacionesEnRed.Add(new ParticipacionEnRed { RedId = red.Id, AuthorId = author.Id });
        await _db.SaveChangesAsync();

        var result = await MakeService("jefe-1", "Jefe_de_Redes").RemoveParticipanteAsync(red.Id, author.Id);

        result.Succeeded.ShouldBeTrue();
        (await _db.ParticipacionesEnRed.AnyAsync(p => p.RedId == red.Id && p.AuthorId == author.Id)).ShouldBeFalse();
    }

    // ── CreateRedAsync / UpdateRedAsync / DeleteRedAsync ────────────────────

    [Test]
    public async Task CreateRedAsync_InvalidTipo_ReturnsFailure()
    {
        var (result, id) = await MakeService("jefe-1", "Jefe_de_Redes")
            .CreateRedAsync(new CreateRedBody("Nueva", 1, 5, 99));

        result.Succeeded.ShouldBeFalse();
        id.ShouldBeNull();
    }

    [Test]
    public async Task CreateRedAsync_Valid_PersistsAndReturnsId()
    {
        var (result, id) = await MakeService("jefe-1", "Jefe_de_Redes")
            .CreateRedAsync(new CreateRedBody("Nueva Red", 1, 10, (int)TipoRed.Internacional));

        result.Succeeded.ShouldBeTrue();
        id.ShouldNotBeNull();
        var saved = await _db.Reds.FindAsync(id);
        saved!.Nombre.ShouldBe("Nueva Red");
        saved.Tipo.ShouldBe(TipoRed.Internacional);
    }

    [Test]
    public async Task UpdateRedAsync_NotFound_ReturnsFailure()
    {
        var result = await MakeService("jefe-1", "Jefe_de_Redes")
            .UpdateRedAsync("no-existe", new UpdateRedBody("X", 1, 1, 0));

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("Red no encontrada.");
    }

    [Test]
    public async Task UpdateRedAsync_InvalidTipo_ReturnsFailure()
    {
        var red = new Red { Nombre = "Red", Tipo = TipoRed.Nacional };
        _db.Reds.Add(red);
        await _db.SaveChangesAsync();

        var result = await MakeService("jefe-1", "Jefe_de_Redes")
            .UpdateRedAsync(red.Id, new UpdateRedBody("X", 1, 1, 99));

        result.Succeeded.ShouldBeFalse();
    }

    [Test]
    public async Task UpdateRedAsync_Valid_UpdatesAllFields()
    {
        var red = new Red { Nombre = "Original", Tipo = TipoRed.Universitaria, CantidadProfesores = 5 };
        _db.Reds.Add(red);
        await _db.SaveChangesAsync();

        var result = await MakeService("jefe-1", "Jefe_de_Redes")
            .UpdateRedAsync(red.Id, new UpdateRedBody("Actualizada", 2, 20, (int)TipoRed.Internacional));

        result.Succeeded.ShouldBeTrue();
        var updated = await _db.Reds.FindAsync(red.Id);
        updated!.Nombre.ShouldBe("Actualizada");
        updated.CantidadProfesores.ShouldBe(20);
        updated.Tipo.ShouldBe(TipoRed.Internacional);
    }

    [Test]
    public async Task DeleteRedAsync_NotFound_ReturnsFailure()
    {
        var result = await MakeService("jefe-1", "Jefe_de_Redes").DeleteRedAsync("no-existe");

        result.Succeeded.ShouldBeFalse();
    }

    [Test]
    public async Task DeleteRedAsync_Valid_RemovesEntity()
    {
        var red = new Red { Nombre = "Red", Tipo = TipoRed.Nacional };
        _db.Reds.Add(red);
        await _db.SaveChangesAsync();

        var result = await MakeService("jefe-1", "Jefe_de_Redes").DeleteRedAsync(red.Id);

        result.Succeeded.ShouldBeTrue();
        (await _db.Reds.FindAsync(red.Id)).ShouldBeNull();
    }

    // ── GetEventsForRedAsync / SetEventsForRedAsync ─────────────────────────

    [Test]
    public async Task GetEventsForRedAsync_MarksAssignedEventsCorrectly()
    {
        var red = new Red { Nombre = "Red", Tipo = TipoRed.Nacional };
        _db.Reds.Add(red);
        await _db.SaveChangesAsync();
        _db.Events.Add(new Event { Name = "Asignado", CountryId = 1, EventTypeId = 1, RedId = red.Id });
        _db.Events.Add(new Event { Name = "Libre", CountryId = 1, EventTypeId = 1 });
        await _db.SaveChangesAsync();

        var result = await MakeService("jefe-1", "Jefe_de_Redes").GetEventsForRedAsync(red.Id);

        result.First(e => e.Name == "Asignado").Assigned.ShouldBeTrue();
        result.First(e => e.Name == "Libre").Assigned.ShouldBeFalse();
    }

    [Test]
    public async Task SetEventsForRedAsync_RedNotFound_ReturnsFailure()
    {
        var result = await MakeService("jefe-1", "Jefe_de_Redes").SetEventsForRedAsync("no-existe", []);

        result.Succeeded.ShouldBeFalse();
    }

    [Test]
    public async Task SetEventsForRedAsync_UnknownEventId_ReturnsFailure()
    {
        var red = new Red { Nombre = "Red", Tipo = TipoRed.Nacional };
        _db.Reds.Add(red);
        await _db.SaveChangesAsync();

        var result = await MakeService("jefe-1", "Jefe_de_Redes").SetEventsForRedAsync(red.Id, [9999]);

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("Uno o más eventos no existen.");
    }

    [Test]
    public async Task SetEventsForRedAsync_AssignsAndUnassignsCorrectly()
    {
        var red = new Red { Nombre = "Red", Tipo = TipoRed.Nacional };
        _db.Reds.Add(red);
        await _db.SaveChangesAsync();
        var ev1 = new Event { Name = "E1", CountryId = 1, EventTypeId = 1, RedId = red.Id };
        var ev2 = new Event { Name = "E2", CountryId = 1, EventTypeId = 1 };
        _db.Events.Add(ev1);
        _db.Events.Add(ev2);
        await _db.SaveChangesAsync();

        var result = await MakeService("jefe-1", "Jefe_de_Redes").SetEventsForRedAsync(red.Id, [ev2.Id]);

        result.Succeeded.ShouldBeTrue();
        (await _db.Events.FindAsync(ev1.Id))!.RedId.ShouldBeNull();
        (await _db.Events.FindAsync(ev2.Id))!.RedId.ShouldBe(red.Id);
    }

    [Test]
    public async Task SetEventsForRedAsync_DeduplicatesEventIds()
    {
        var red = new Red { Nombre = "Red", Tipo = TipoRed.Nacional };
        _db.Reds.Add(red);
        await _db.SaveChangesAsync();
        var ev = new Event { Name = "E", CountryId = 1, EventTypeId = 1 };
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        var result = await MakeService("jefe-1", "Jefe_de_Redes").SetEventsForRedAsync(red.Id, [ev.Id, ev.Id, ev.Id]);

        result.Succeeded.ShouldBeTrue();
        (await _db.Events.FindAsync(ev.Id))!.RedId.ShouldBe(red.Id);
    }
}
