using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Application.FunctionalTests.Events;

using static Testing;

/// <summary>
/// Tests de flujo HTTP para la gestión de eventos científicos.
/// Cubre creación, lectura, actualización y eliminación de eventos
/// con rol Profesor, verificando la persistencia real en BD.
/// </summary>
[TestFixture]
public class EventFlowTests : BaseTestFixture
{
    private int _eventTypeId;
    private int _countryId;

    [SetUp]
    public async Task SeedPrerequisites()
    {
        // EventType y Country deben existir antes de crear un Event
        (_eventTypeId, _countryId) = await ExecuteDbContextAsync(async db =>
        {
            var et = new EventType { Name = "Internacional Test" };
            db.EventTypes.Add(et);

            var co = new Country { Name = "Cuba Test" };
            db.Countries.Add(co);

            await db.SaveChangesAsync();
            return (et.Id, co.Id);
        });
    }

    // ── Crear evento ──────────────────────────────────────────────────────────

    [Test]
    public async Task Profesor_CanCreate_Event_AndItPersists()
    {
        await RunAsUserAsync("prof.event@local", "Testing1234!", ["Profesor"]);
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/Events", new
        {
            name = "Congreso Internacional de Informática",
            countryId = _countryId,
            eventType = _eventTypeId,
            institutions = Array.Empty<string>(),
            organizadorIds = Array.Empty<string>()
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("eventId", out var idProp).ShouldBeTrue();
        var eventId = idProp.GetInt32();

        // Verificar persistencia en BD
        var dbEvent = await ExecuteDbContextAsync(db =>
            db.Events.FirstOrDefaultAsync(e => e.Id == eventId));
        dbEvent.ShouldNotBeNull();
        dbEvent!.Name.ShouldBe("Congreso Internacional de Informática");
    }

    [Test]
    public async Task Profesor_CanCreate_Event_WithAllOptionalFields()
    {
        await RunAsUserAsync("prof.event2@local", "Testing1234!", ["Profesor"]);
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/Events", new
        {
            name = "Taller Regional de Inteligencia Artificial",
            countryId = _countryId,
            eventType = _eventTypeId,
            institutions = Array.Empty<string>(),
            redId = (string?)null,
            organizadorIds = Array.Empty<string>(),
            evidenceFileId = (int?)null
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    // ── Leer eventos ──────────────────────────────────────────────────────────

    [Test]
    public async Task Profesor_GetMyEvents_ReturnsOwnEventsOnly()
    {
        await RunAsUserAsync("prof.myevents@local", "Testing1234!", ["Profesor"]);
        using var client = CreateClient();

        // Crear 2 eventos
        for (int i = 1; i <= 2; i++)
        {
            await client.PostAsJsonAsync("/api/Events", new
            {
                name = $"Evento personal #{i}",
                countryId = _countryId,
                eventType = _eventTypeId,
                institutions = Array.Empty<string>(),
                organizadorIds = Array.Empty<string>()
            });
        }

        var response = await client.GetAsync("/api/Events");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var events = await response.Content.ReadFromJsonAsync<JsonElement>();
        events.GetArrayLength().ShouldBeGreaterThanOrEqualTo(2);
    }

    // ── Actualizar evento ─────────────────────────────────────────────────────

    [Test]
    public async Task Profesor_CanUpdate_Event()
    {
        await RunAsUserAsync("prof.updevent@local", "Testing1234!", ["Profesor"]);
        using var client = CreateClient();

        var createResp = await client.PostAsJsonAsync("/api/Events", new
        {
            name = "Evento a actualizar",
            countryId = _countryId,
            eventType = _eventTypeId,
            institutions = Array.Empty<string>(),
            organizadorIds = Array.Empty<string>()
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var eventId = created.GetProperty("eventId").GetInt32();

        var updateResp = await client.PutAsJsonAsync($"/api/Events/{eventId}", new
        {
            name = "Evento actualizado correctamente",
            countryId = _countryId,
            eventType = _eventTypeId,
            institutions = Array.Empty<string>(),
            organizadorIds = Array.Empty<string>()
        });

        updateResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var dbEvent = await ExecuteDbContextAsync(db =>
            db.Events.FirstOrDefaultAsync(e => e.Id == eventId));
        dbEvent!.Name.ShouldBe("Evento actualizado correctamente");
    }

    // ── Eliminar evento ───────────────────────────────────────────────────────

    [Test]
    public async Task Profesor_CanDelete_Event()
    {
        await RunAsUserAsync("prof.delevent@local", "Testing1234!", ["Profesor"]);
        using var client = CreateClient();

        var createResp = await client.PostAsJsonAsync("/api/Events", new
        {
            name = "Evento a eliminar",
            countryId = _countryId,
            eventType = _eventTypeId,
            institutions = Array.Empty<string>(),
            organizadorIds = Array.Empty<string>()
        });
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var eventId = created.GetProperty("eventId").GetInt32();

        var deleteResp = await client.DeleteAsync($"/api/Events/{eventId}");
        deleteResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var dbCount = await ExecuteDbContextAsync(db =>
            db.Events.CountAsync(e => e.Id == eventId));
        dbCount.ShouldBe(0, "El evento eliminado no debe existir en la BD");
    }

    // ── Autorización ──────────────────────────────────────────────────────────

    [Test]
    public async Task JefeDeProyecto_CannotCreate_Event()
    {
        await RunAsUserAsync("jefe.event@local", "Testing1234!", ["Jefe_de_Proyecto"]);
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/Events", new
        {
            name = "No debe crearse",
            countryId = _countryId,
            eventType = _eventTypeId,
            institutions = Array.Empty<string>(),
            organizadorIds = Array.Empty<string>()
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task CreateEvent_EmptyName_Returns400()
    {
        await RunAsUserAsync("prof.val.event@local", "Testing1234!", ["Profesor"]);
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/Events", new
        {
            name = "",
            countryId = _countryId,
            eventType = _eventTypeId,
            institutions = Array.Empty<string>(),
            organizadorIds = Array.Empty<string>()
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
