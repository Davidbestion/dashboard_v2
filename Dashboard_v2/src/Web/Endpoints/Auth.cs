using Dashboard_v2.Application.Auth;
using Dashboard_v2.Web.Infrastructure;

namespace Dashboard_v2.Web.Endpoints;

/// <summary>
/// API endpoints for authentication: login, logout, current user info.
/// </summary>
public class Auth : EndpointGroupBase
{
    /// <summary>Registers the Auth route group with login, logout, register, and current-user endpoints.</summary>
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost("register", Register)
            .AllowAnonymous()
            .WithName("Register")
            .Produces(200)
            .ProducesProblem(400);

        groupBuilder.MapPost("login", Login)
            .AllowAnonymous()
            .WithName("Login")
            .Produces(200)
            .ProducesProblem(400);

        groupBuilder.MapPost("logout", Logout)
            .WithName("Logout")
            .Produces(200);

        groupBuilder.MapGet("me", GetCurrentUser)
            .WithName("GetCurrentUser")
            .Produces<CurrentUserDto>(200)
            .ProducesProblem(401);
    }

    /// <summary>
    /// POST /api/Auth/register — Registra un nuevo usuario. No requiere autenticación.<br/>
    /// Devuelve 200 con mensaje de éxito o 400 con lista de errores de validación.
    /// </summary>
    private static async Task<IResult> Register(IAuthService authService, RegisterRequest request, CancellationToken ct)
    {
        var result = await authService.RegisterAsync(request, ct);

        if (!result.Succeeded)
        {
            return Results.BadRequest(new { errors = result.Errors });
        }

        return Results.Ok(new { message = "Usuario registrado exitosamente." });
    }

    /// <summary>
    /// POST /api/Auth/login — Autentica al usuario.<br/>
    /// • Un solo rol → establece cookie HttpOnly <c>access_token</c> con el JWT y retorna 200.<br/>
    /// • Múltiples roles, sin selectedRole → retorna <c>{ requiresRoleSelection: true, availableRoles: [...] }</c>.<br/>
    /// La cookie es HttpOnly (JavaScript no puede leerla) y SameSite=Strict (protección CSRF).
    /// </summary>
    private static async Task<IResult> Login(IAuthService authService, LoginRequest request, HttpContext httpContext, CancellationToken ct)
    {
        var (result, response) = await authService.LoginAsync(request, ct);

        if (!result.Succeeded)
        {
            return Results.BadRequest(new { errors = result.Errors });
        }

        // El usuario tiene múltiples roles y debe seleccionar uno antes de obtener la cookie.
        if (response!.RequiresRoleSelection)
        {
            return Results.Ok(new { requiresRoleSelection = true, availableRoles = response.AvailableRoles });
        }

        // El usuario no pertenece a un Área y debe seleccionar una antes de obtener la cookie.
        if (response!.RequiresAreaSelection)
        {
            return Results.Ok(new { requiresAreaSelection = true, availableAreas = response.AvailableAreas });
        }

        // Login completo: guardar el token en una cookie HttpOnly.
        // Secure = true en HTTPS, SameSite=Strict evita ataques CSRF de otros orígenes.
        httpContext.Response.Cookies.Append("access_token", response.Token!, new CookieOptions
        {
            HttpOnly = true,
            Secure = httpContext.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddHours(8)
        });

        return Results.Ok(new { message = "Login exitoso." });
    }

    /// <summary>
    /// POST /api/Auth/logout — Cierra la sesión eliminando la cookie <c>access_token</c>.<br/>
    /// Requiere sesión activa y delega cualquier trabajo adicional de cierre de sesión al servicio de autenticación.
    /// </summary>
    private static async Task<IResult> Logout(IAuthService authService, HttpContext httpContext, CancellationToken ct)
    {
        await authService.LogoutAsync(ct);

        httpContext.Response.Cookies.Delete("access_token");

        return Results.Ok(new { message = "Sesión cerrada exitosamente." });
    }

    /// <summary>
    /// GET /api/Auth/me — Devuelve el DTO con los datos del usuario actualmente autenticado.<br/>
    /// El servicio lee el claim <c>sub</c> del JWT para identificar al usuario.<br/>
    /// Retorna 401 si la cookie no existe o el JWT expirado/inválido.
    /// </summary>
    private static async Task<IResult> GetCurrentUser(IAuthService authService, CancellationToken ct)
    {
        var user = await authService.GetCurrentUserAsync(ct);

        if (user == null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(user);
    }
}
