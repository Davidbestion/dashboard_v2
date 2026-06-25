namespace Dashboard_v2.Application.Common.Interfaces;

/// <summary>
/// Contract for generating and validating JWT authentication tokens.
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generates a signed JWT for the given user identity and role set.
    /// </summary>
    /// <param name="userId">Unique identifier of the user.</param>
    /// <param name="userName">Display name of the user.</param>
    /// <param name="email">Email address of the user.</param>
    /// <param name="roles">Roles assigned to the user for this session.</param>
    /// <returns>Signed JWT token string.</returns>
    string GenerateToken(string userId, string userName, string email, IEnumerable<string> roles);
}
