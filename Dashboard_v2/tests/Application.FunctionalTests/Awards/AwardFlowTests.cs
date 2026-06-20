using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Application.FunctionalTests.Awards;

using static Testing;

/// <summary>
/// Tests de flujo HTTP para la gestión de premios científicos.
/// Cubre creación, lectura y eliminación de premios por un Profesor,
/// incluyendo la creación de nuevos premios en el catálogo.
/// </summary>
[TestFixture]
public class AwardFlowTests : BaseTestFixture
{
    private int _awardTypeId;

    [SetUp]
    public async Task SeedAwardType()
    {
        _awardTypeId = await ExecuteDbContextAsync(async db =>
        {
            var at = new AwardType { Name = "Premio MES Test" };
            db.AwardTypes.Add(at);
            await db.SaveChangesAsync();
            return at.Id;
        });
    }

    // ── Crear premio ──────────────────────────────────────────────────────────

    [Test]
    public async Task Profesor_CanCreate_Award_WithNewName()
    {
        await RunAsUserAsync("prof.award@local", "Testing1234!", ["Profesor"]);
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/Awards", new
        {
            newAwardName = "Premio Nacional de Ciencias 2024",
            awardTypeId = _awardTypeId,
            awardedAt = DateTime.UtcNow
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Verificar que el UserAwarded se creó en BD
        var userId = GetUserId();
        var dbCount = await ExecuteDbContextAsync(db =>
            db.UserAwardeds.CountAsync(ua => ua.UserId == userId));
        dbCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task Profesor_CanCreate_Award_UsingExistingCatalogEntry()
    {
        // Sembrar un premio en el catálogo directamente
        var existingAwardId = await ExecuteDbContextAsync(async db =>
        {
            var award = new Award { Name = "Premio de Catálogo", AwardTypeId = _awardTypeId };
            db.Awards.Add(award);
            await db.SaveChangesAsync();
            return award.Id;
        });

        await RunAsUserAsync("prof.award2@local", "Testing1234!", ["Profesor"]);
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/Awards", new
        {
            awardId = existingAwardId,
            awardedAt = DateTime.UtcNow
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    // ── Leer premios ──────────────────────────────────────────────────────────

    [Test]
    public async Task Profesor_GetMyAwards_Returns200_WithPersonalAwards()
    {
        await RunAsUserAsync("prof.getaward@local", "Testing1234!", ["Profesor"]);
        using var client = CreateClient();

        // Crear un premio primero
        await client.PostAsJsonAsync("/api/Awards", new
        {
            newAwardName = "Premio de Lectura Test",
            awardTypeId = _awardTypeId,
            awardedAt = DateTime.UtcNow
        });

        var response = await client.GetAsync("/api/Awards");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetArrayLength().ShouldBeGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task Profesor_GetAwardCatalog_Returns200()
    {
        await RunAsUserAsync("prof.catalog@local", "Testing1234!", ["Profesor"]);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/Awards/catalogo");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── Actualizar premio ─────────────────────────────────────────────────────

    [Test]
    public async Task Profesor_CanUpdate_Award()
    {
        await RunAsUserAsync("prof.updaward@local", "Testing1234!", ["Profesor"]);
        using var client = CreateClient();

        var createResp = await client.PostAsJsonAsync("/api/Awards", new
        {
            newAwardName = "Premio a actualizar",
            awardTypeId = _awardTypeId,
            awardedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var userAwardedId = created.GetProperty("id").GetInt32();

        var updateResp = await client.PutAsJsonAsync($"/api/Awards/{userAwardedId}", new
        {
            newAwardName = "Premio actualizado",
            awardTypeId = _awardTypeId,
            awardedAt = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc)
        });

        updateResp.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── Eliminar premio ───────────────────────────────────────────────────────

    [Test]
    public async Task Profesor_CanDelete_Award()
    {
        await RunAsUserAsync("prof.delaward@local", "Testing1234!", ["Profesor"]);
        using var client = CreateClient();

        var createResp = await client.PostAsJsonAsync("/api/Awards", new
        {
            newAwardName = "Premio a eliminar",
            awardTypeId = _awardTypeId,
            awardedAt = DateTime.UtcNow
        });
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var userAwardedId = created.GetProperty("id").GetInt32();

        var deleteResp = await client.DeleteAsync($"/api/Awards/{userAwardedId}");
        deleteResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var dbCount = await ExecuteDbContextAsync(db =>
            db.UserAwardeds.CountAsync(ua => ua.Id == userAwardedId));
        dbCount.ShouldBe(0, "El premio eliminado no debe existir en la BD");
    }

    // ── Superuser ve todos los premios ────────────────────────────────────────

    [Test]
    public async Task Superuser_GetAllAwards_Returns200()
    {
        await RunAsUserAsync("su.award@local", "Testing1234!", ["Superuser"]);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/Awards/todas");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── Autorización ──────────────────────────────────────────────────────────

    [Test]
    public async Task Vicedecano_CannotCreate_Award()
    {
        await RunAsUserAsync("vice.award@local", "Testing1234!", ["Vicedecano_de_investigacion"]);
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/Awards", new
        {
            newAwardName = "No debe crearse",
            awardTypeId = _awardTypeId,
            awardedAt = DateTime.UtcNow
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
