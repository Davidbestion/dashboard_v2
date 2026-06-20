using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Dashboard_v2.Application.FunctionalTests.Dashboard;

using static Testing;

/// <summary>
/// Tests del perfil de investigación personal: verifican que al autenticarse
/// el usuario recibe su información personalizada correctamente agregada.
/// Cubre el endpoint /api/Auth/me y los endpoints de estadísticas del DashboardHome
/// según el rol del investigador.
/// </summary>
[TestFixture]
public class PerfilInvestigacionTests : BaseTestFixture
{
    // ── GET /api/Auth/me — datos del perfil ───────────────────────────────────

    [Test]
    public async Task GetMe_AuthenticatedProfesor_ReturnsCorrectProfileFields()
    {
        await RunAsUserAsync("prof.me@local", "Testing1234!", ["Profesor"]);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/Auth/me");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("userName").GetString().ShouldBe("prof.me@local");
        body.GetProperty("email").GetString().ShouldBe("prof.me@local");
        body.GetProperty("role").GetString().ShouldBe("Profesor");
        body.GetProperty("hasLinkedAuthor").GetBoolean().ShouldBeFalse(
            "Un usuario recién creado no tiene entidad Author vinculada");
    }

    [Test]
    public async Task GetMe_Unauthenticated_Returns401()
    {
        // Sin llamar RunAsUserAsync → _userId = null → TestAuthHandler devuelve NoResult → 401
        using var client = CreateClient();

        var response = await client.GetAsync("/api/Auth/me");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetMe_ReturnsSameId_AsAuthenticatedUser()
    {
        var userId = await RunAsUserAsync("prof.id@local", "Testing1234!", ["Profesor"]);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/Auth/me");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        body.GetProperty("id").GetString().ShouldBe(userId);
    }

    // ── Estadísticas personalizadas para Profesor ─────────────────────────────

    [Test]
    public async Task ProfesorHome_Publications_OnlyShowsOwnData()
    {
        // Usuario A crea 2 publicaciones
        await RunAsUserAsync("prof.a@local", "Testing1234!", ["Profesor"]);
        using var clientA = CreateClient();

        for (int i = 1; i <= 2; i++)
        {
            var r = await clientA.PostAsJsonAsync("/api/Publications", new
            {
                title = $"Publicación personal del prof A #{i}",
                publicationData = "Revista de Informática",
                publicationType = 4,
                publishedDate = "2024",
                additionalAuthorIds = Array.Empty<string>(),
                additionalAuthorNames = Array.Empty<string>(),
                additionalUserIds = Array.Empty<string>()
            });
            r.StatusCode.ShouldBe(HttpStatusCode.Created);
        }

        // Usuario A ve exactamente sus 2 publicaciones
        var pubsResponse = await clientA.GetAsync("/api/Publications");
        pubsResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var pubs = await pubsResponse.Content.ReadFromJsonAsync<JsonElement>();
        pubs.GetArrayLength().ShouldBe(2, "El Profesor debe ver solo sus propias publicaciones");
    }

    [Test]
    public async Task ProfesorHome_MultipleStatEndpoints_AllReturn200()
    {
        await RunAsUserAsync("prof.stats@local", "Testing1234!", ["Profesor"]);
        using var client = CreateClient();

        // Todos los endpoints que consume ProfesorHome deben responder correctamente
        var responses = await Task.WhenAll(
            client.GetAsync("/api/Publications"),
            client.GetAsync("/api/Presentations"),
            client.GetAsync("/api/Events"),
            client.GetAsync("/api/Awards")
        );

        foreach (var r in responses)
            r.StatusCode.ShouldBe(HttpStatusCode.OK,
                $"El endpoint {r.RequestMessage?.RequestUri?.PathAndQuery} debe devolver 200 para un Profesor");
    }

    [Test]
    public async Task ProfesorHome_Publications_CountMatchesCreated()
    {
        await RunAsUserAsync("prof.count@local", "Testing1234!", ["Profesor"]);
        using var client = CreateClient();

        // Crear 3 publicaciones
        for (int i = 1; i <= 3; i++)
        {
            await client.PostAsJsonAsync("/api/Publications", new
            {
                title = $"Pub conteo #{i}",
                publicationData = "Datos",
                publicationType = i % 5,
                publishedDate = "2025",
                additionalAuthorIds = Array.Empty<string>(),
                additionalAuthorNames = Array.Empty<string>(),
                additionalUserIds = Array.Empty<string>()
            });
        }

        var response = await client.GetAsync("/api/Publications");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        body.GetArrayLength().ShouldBe(3,
            "El conteo de publicaciones del perfil debe coincidir con las creadas");
    }

    // ── Estadísticas personalizadas para Jefe de Proyecto ────────────────────

    [Test]
    public async Task JefeProyectoHome_ProyectosEndpoint_Returns200()
    {
        await RunAsUserAsync("jefe.home@local", "Testing1234!", ["Jefe_de_Proyecto"]);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/Proyectos");

        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            "El Jefe de Proyecto debe poder acceder a la lista de proyectos para su dashboard");
    }

    // ── Aislamiento de datos entre usuarios ───────────────────────────────────

    [Test]
    public async Task ProfesorHome_DoesNotSeeOtherUsersPublications()
    {
        // Usuario A crea publicaciones
        await RunAsUserAsync("prof.isolation.a@local", "Testing1234!", ["Profesor"]);
        using var clientA = CreateClient();
        await clientA.PostAsJsonAsync("/api/Publications", new
        {
            title = "Publicación exclusiva del usuario A",
            publicationData = "Datos A",
            publicationType = 4,
            publishedDate = "2024",
            additionalAuthorIds = Array.Empty<string>(),
            additionalAuthorNames = Array.Empty<string>(),
            additionalUserIds = Array.Empty<string>()
        });

        // Usuario B (distinto) no debe ver publicaciones del usuario A
        await RunAsUserAsync("prof.isolation.b@local", "Testing1234!", ["Profesor"]);
        using var clientB = CreateClient();
        var responseB = await clientB.GetAsync("/api/Publications");
        var bodyB = await responseB.Content.ReadFromJsonAsync<JsonElement>();

        bodyB.GetArrayLength().ShouldBe(0,
            "Un Profesor no debe ver publicaciones de otro usuario en su perfil personal");
    }
}
