using Dashboard_v2.Application.Common.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dashboard_v2.Application.Users;

public interface IUserService
{
    Task<List<UserWithRolesDto>> GetAllAsync(CancellationToken ct = default);
    Task<Result> AssignRoleAsync(string userId, string roleName, CancellationToken ct = default);
    Task<Result> RemoveRoleAsync(string userId, string roleName, CancellationToken ct = default);
    Task<List<JefeDeProyectoDto>> GetJefesDeProyectoAsync(CancellationToken ct = default);
}
