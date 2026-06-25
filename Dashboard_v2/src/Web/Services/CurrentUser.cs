using System.Security.Claims;

using Dashboard_v2.Application.Common.Interfaces;

namespace Dashboard_v2.Web.Services;

/// <summary>
/// Extracts the currently authenticated user's identity from HTTP context claims.
/// </summary>
public class CurrentUser : IUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>The user's unique identifier from the 'nameidentifier' claim. Null if unauthenticated.</summary>
    public string? Id => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>List of role claim values for the authenticated user. Null if unauthenticated.</summary>
    public List<string>? Roles => _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role).Select(x => x.Value).ToList();

}
