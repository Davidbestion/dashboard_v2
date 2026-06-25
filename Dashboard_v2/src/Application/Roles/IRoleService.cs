using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dashboard_v2.Application.Roles;

/// <summary>
/// Application service for retrieving available system roles for assignment in the admin UI.
/// </summary>
public interface IRoleService
{
    /// <summary>Returns the list of roles that can be assigned to users by an administrator.</summary>
    Task<List<RoleDto>> GetAssignableRolesAsync();
}
