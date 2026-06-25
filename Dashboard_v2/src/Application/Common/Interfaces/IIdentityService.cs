using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.Common.Interfaces;

/// <summary>
/// Contract for user authentication operations (login, user creation, password management).
/// </summary>
public interface IIdentityService
{
    Task<string?> GetUserNameAsync(string userId);

    Task<bool> IsInRoleAsync(string userId, string role);

    Task<bool> AuthorizeAsync(string userId, string policyName);

    Task<(Result Result, string UserId)> CreateUserAsync(string userName, string password);

    Task<(Result Result, string UserId)> CreateUserAsync(
        string userName,
        string userLastName1,
        string? userLastName2,
        string email,
        string password,
        DateTime birthDate,
        bool isTrained,
        Dashboard_v2.Domain.Entities.TeachingCategory teachingCategory,
        Dashboard_v2.Domain.Entities.ScientificCategory scientificCategory,
        Dashboard_v2.Domain.Entities.InvestigationCategory investigationCategory);

    /// <summary>
    /// Autentica al usuario. Si tiene múltiples roles y no se especifica <paramref name="selectedRole"/>,
    /// devuelve los roles disponibles para que el cliente elija. Con un único rol o rol ya seleccionado,
    /// genera el JWT directamente.
    /// </summary>
    Task<(Result Result, LoginResponse? Response)> LoginAsync(string email, string password, string? selectedRole = null, string? selectedAreaId = null);

    Task<(string? UserId, string? UserName, string? Email)> GetUserDetailsAsync(string userId);

    Task<Result> DeleteUserAsync(string userId);
}
