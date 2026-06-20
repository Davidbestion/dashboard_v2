using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dashboard_v2.Application.Roles;

public interface IRoleService
{
    Task<List<RoleDto>> GetAssignableRolesAsync();
}
