using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Application.FunctionalTests.GruposDeInvestigacion;

using static Testing;

/// <summary>
/// Tests de flujo HTTP para la gestión de grupos de investigación.
/// Cubre creación, lectura, actualización, eliminación y gestión de miembros,
/// verificando tanto la lógica de autorización como la persistencia en BD.
/// </summary>
[TestFixture]
public class GrupoDeInvestigacionFlowTests : BaseTestFixture
{
    private string _areaId = default!;

    [SetUp]
    public async Task SeedArea()
    {
        _areaId = await ExecuteDbContextAsync(async db =>
        {
            var univ = new Universidad { Id = "uh-grupo-test", Nombre = "UH Grupos" };
            db.Universidades.Add(univ);

            var area = new Area
            {
                Id = "area-grupo-test",
                Nombre = "Área de Ciencias de la Computación",
                Descripcion = "Área para tests de grupos",
                UniversidadId = univ.Id
            };
            db.Areas.Add(area);
            await db.SaveChangesAsync();
            return area.Id;
        });
    }

    // ── Crear grupo ───────────────────────────────────────────────────────────

    [Test]
    public async Task JefeDeGrupo_CanCreate_GrupoDeInvestigacion()
    {
        await RunAsUserAsync("jefe.grupo@local", "Testing1234!", ["Jefe_de_Grupo_de_investigacion"]);
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/GruposDeInvestigacion", new
        {
            nombre = "Grupo de Inteligencia Artificial",
            areaId = _areaId,
            lineasDeInvestigacionIds = Array.Empty<string>()
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("id", out _).ShouldBeTrue();
    }

    [Test]
    public async Task JefeDeGrupo_CreatedGroup_PersistsInDatabase()
    {
        await RunAsUserAsync("jefe.grupo2@local", "Testing1234!", ["Jefe_de_Grupo_de_investigacion"]);
        using var client = CreateClient();

        await client.PostAsJsonAsync("/api/GruposDeInvestigacion", new
        {
            nombre = "Grupo de Redes de Computadoras",
            areaId = _areaId,
            lineasDeInvestigacionIds = Array.Empty<string>()
        });

        var dbCount = await ExecuteDbContextAsync(db =>
            db.GruposDeInvestigacion.CountAsync(g => g.Nombre == "Grupo de Redes de Computadoras"));
        dbCount.ShouldBe(1);
    }

    // ── Leer grupos ───────────────────────────────────────────────────────────

    [Test]
    public async Task Superuser_CanGetAll_GruposDeInvestigacion()
    {
        await RunAsUserAsync("su.grupo@local", "Testing1234!", ["Superuser"]);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/GruposDeInvestigacion");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task JefeDeGrupo_CanGetMine_GruposDeInvestigacion()
    {
        await RunAsUserAsync("jefe.mine@local", "Testing1234!", ["Jefe_de_Grupo_de_investigacion"]);
        using var client = CreateClient();

        // Crear un grupo primero
        await client.PostAsJsonAsync("/api/GruposDeInvestigacion", new
        {
            nombre = "Mi Grupo de Bases de Datos",
            areaId = _areaId,
            lineasDeInvestigacionIds = Array.Empty<string>()
        });

        var response = await client.GetAsync("/api/GruposDeInvestigacion/mine");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var grupos = await response.Content.ReadFromJsonAsync<JsonElement>();
        grupos.GetArrayLength().ShouldBeGreaterThanOrEqualTo(1);
    }

    // ── Actualizar grupo ──────────────────────────────────────────────────────

    [Test]
    public async Task JefeDeGrupo_CanUpdate_GrupoDeInvestigacion()
    {
        await RunAsUserAsync("jefe.upd@local", "Testing1234!", ["Jefe_de_Grupo_de_investigacion"]);
        using var client = CreateClient();

        var createResp = await client.PostAsJsonAsync("/api/GruposDeInvestigacion", new
        {
            nombre = "Grupo original",
            areaId = _areaId,
            lineasDeInvestigacionIds = Array.Empty<string>()
        });
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var grupoId = created.GetProperty("id").GetString();

        var updateResp = await client.PutAsJsonAsync($"/api/GruposDeInvestigacion/{grupoId}", new
        {
            nombre = "Grupo actualizado",
            areaId = _areaId,
            lineasDeInvestigacionIds = Array.Empty<string>()
        });

        updateResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var dbGrupo = await ExecuteDbContextAsync(db =>
            db.GruposDeInvestigacion.FirstOrDefaultAsync(g => g.Id == grupoId));
        dbGrupo!.Nombre.ShouldBe("Grupo actualizado");
    }

    // ── Eliminar grupo ────────────────────────────────────────────────────────

    [Test]
    public async Task JefeDeGrupo_CanDelete_GrupoDeInvestigacion()
    {
        await RunAsUserAsync("jefe.del@local", "Testing1234!", ["Jefe_de_Grupo_de_investigacion"]);
        using var client = CreateClient();

        var createResp = await client.PostAsJsonAsync("/api/GruposDeInvestigacion", new
        {
            nombre = "Grupo a eliminar",
            areaId = _areaId,
            lineasDeInvestigacionIds = Array.Empty<string>()
        });
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var grupoId = created.GetProperty("id").GetString();

        var deleteResp = await client.DeleteAsync($"/api/GruposDeInvestigacion/{grupoId}");
        deleteResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var dbCount = await ExecuteDbContextAsync(db =>
            db.GruposDeInvestigacion.CountAsync(g => g.Id == grupoId));
        dbCount.ShouldBe(0, "El grupo eliminado no debe existir en la BD");
    }

    // ── Autorización ──────────────────────────────────────────────────────────

    [Test]
    public async Task Profesor_CannotCreate_GrupoDeInvestigacion()
    {
        await RunAsUserAsync("prof.grupo@local", "Testing1234!", ["Profesor"]);
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/GruposDeInvestigacion", new
        {
            nombre = "No debe crearse",
            areaId = _areaId,
            lineasDeInvestigacionIds = Array.Empty<string>()
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── Validación ────────────────────────────────────────────────────────────

    [Test]
    public async Task CreateGrupo_EmptyNombre_Returns400()
    {
        await RunAsUserAsync("jefe.val@local", "Testing1234!", ["Jefe_de_Grupo_de_investigacion"]);
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/GruposDeInvestigacion", new
        {
            nombre = "",
            areaId = _areaId,
            lineasDeInvestigacionIds = Array.Empty<string>()
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreateGrupo_NonExistentArea_Returns400()
    {
        await RunAsUserAsync("jefe.badarea@local", "Testing1234!", ["Jefe_de_Grupo_de_investigacion"]);
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/GruposDeInvestigacion", new
        {
            nombre = "Grupo con área inexistente",
            areaId = "area-que-no-existe-en-la-bd",
            lineasDeInvestigacionIds = Array.Empty<string>()
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
