using Dashboard_v2.Application.Common.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dashboard_v2.Application.Users;

/// <summary>
/// Application service for user management: listing, role assignment, activation/deactivation, and creation.
/// </summary>
public interface IUserService
{
    Task<List<UserWithRolesDto>> GetAllAsync(CancellationToken ct = default);
    Task<Result> AssignRoleAsync(string userId, string roleName, CancellationToken ct = default);
    Task<Result> RemoveRoleAsync(string userId, string roleName, CancellationToken ct = default);
    Task<Result> SetActiveAsync(string userId, bool active, CancellationToken ct = default);
    Task<(Result Result, string? UserId)> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<List<JefeDeProyectoDto>> GetJefesDeProyectoAsync(CancellationToken ct = default);
}
