using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.Auth;

/// <summary>
/// Servicio de aplicación CRUD para los casos de uso de autenticación y sesión.
/// </summary>
public interface IAuthService
{
    Task<Result> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<(Result Result, LoginResponse? Response)> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<Result> LogoutAsync(CancellationToken ct = default);
    Task<CurrentUserDto?> GetCurrentUserAsync(CancellationToken ct = default);
}
