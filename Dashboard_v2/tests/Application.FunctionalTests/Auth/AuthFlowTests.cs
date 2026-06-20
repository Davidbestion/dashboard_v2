using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Dashboard_v2.Application.FunctionalTests.Auth;

using static Testing;

/// <summary>
/// Tests de flujo HTTP para la autenticación: registro e inicio de sesión.
/// Verifica que el API de Auth responde correctamente ante credenciales
/// válidas e inválidas, y que la validación de campos funciona.
/// </summary>
[TestFixture]
public class AuthFlowTests : BaseTestFixture
{
    // ── Registro ──────────────────────────────────────────────────────────────

    [Test]
    public async Task Register_ValidData_Returns200()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/Auth/register", new
        {
            userName = "NuevoUsuario",
            userLastName1 = "García",
            userLastName2 = "López",
            email = "nuevo.usuario@test.local",
            password = "Password123!",
            birthDate = new DateTime(1990, 5, 15, 0, 0, 0, DateTimeKind.Utc)
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().ShouldNotBeNullOrEmpty();
    }

    [Test]
    public async Task Register_InvalidEmail_Returns400()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/Auth/register", new
        {
            userName = "UsuarioMalEmail",
            userLastName1 = "Pérez",
            email = "esto-no-es-un-email",
            password = "Password123!",
            birthDate = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Register_WeakPassword_Returns400()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/Auth/register", new
        {
            userName = "UsuarioContraseñaDebil",
            userLastName1 = "Rodríguez",
            email = "usuario.debil@test.local",
            password = "123",
            birthDate = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Register_DuplicateEmail_Returns400()
    {
        using var client = CreateClient();
        const string email = "duplicado@test.local";

        // Primer registro
        await client.PostAsJsonAsync("/api/Auth/register", new
        {
            userName = "UsuarioUno",
            userLastName1 = "Martínez",
            email,
            password = "Password123!",
            birthDate = new DateTime(1985, 3, 20, 0, 0, 0, DateTimeKind.Utc)
        });

        // Segundo registro con mismo email
        var response = await client.PostAsJsonAsync("/api/Auth/register", new
        {
            userName = "UsuarioDos",
            userLastName1 = "Fernández",
            email,
            password = "Password456!",
            birthDate = new DateTime(1990, 6, 10, 0, 0, 0, DateTimeKind.Utc)
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task Login_ValidCredentials_Returns200()
    {
        using var client = CreateClient();
        const string email = "login.valido@test.local";
        const string password = "Password789!";

        // Registrar primero
        await client.PostAsJsonAsync("/api/Auth/register", new
        {
            userName = "LoginValido",
            userLastName1 = "Sánchez",
            email,
            password,
            birthDate = new DateTime(1988, 7, 4, 0, 0, 0, DateTimeKind.Utc)
        });

        // Hacer login
        var response = await client.PostAsJsonAsync("/api/Auth/login", new
        {
            email,
            password
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Login_WrongPassword_Returns400()
    {
        using var client = CreateClient();
        const string email = "login.malo@test.local";

        // Registrar con contraseña correcta
        await client.PostAsJsonAsync("/api/Auth/register", new
        {
            userName = "LoginMalo",
            userLastName1 = "Torres",
            email,
            password = "Password123!",
            birthDate = new DateTime(1992, 2, 28, 0, 0, 0, DateTimeKind.Utc)
        });

        // Login con contraseña incorrecta
        var response = await client.PostAsJsonAsync("/api/Auth/login", new
        {
            email,
            password = "ContraseñaIncorrecta999!"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Login_NonExistentEmail_Returns400()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/Auth/login", new
        {
            email = "no.existe.nunca@test.local",
            password = "Password123!"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Login_InvalidEmailFormat_Returns400()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/Auth/login", new
        {
            email = "formato-invalido",
            password = "Password123!"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── Me (perfil) ───────────────────────────────────────────────────────────

    [Test]
    public async Task Logout_AuthenticatedUser_Returns200()
    {
        await RunAsUserAsync("prof.logout@local", "Testing1234!", ["Profesor"]);
        using var client = CreateClient();

        var response = await client.PostAsync("/api/Auth/logout", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
