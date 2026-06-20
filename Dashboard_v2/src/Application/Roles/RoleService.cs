using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dashboard_v2.Application.Roles;
using RolesEnum = global::Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Application.Roles;

public sealed class RoleService : IRoleService
{
    public Task<List<RoleDto>> GetAssignableRolesAsync()
    {
        var roles = System.Enum.GetValues<RolesEnum>()
            .Cast<RolesEnum>()
            .Where(r => r != RolesEnum.None && r != RolesEnum.Superuser)
            .Select(r => new RoleDto { Name = r.ToString() })
            .OrderBy(r => r.Name)
            .ToList();

        return Task.FromResult(roles);
    }
}
