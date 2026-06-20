using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using RolesEnum = global::Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Application.Users;

public sealed class UserService : IUserService
{
    private readonly IApplicationDbContext _context;

    public UserService(IApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<UserWithRolesDto>> GetAllAsync(CancellationToken ct = default)
    {
        return _context.Users
            .AsNoTracking()
            .Select(u => new UserWithRolesDto
            {
                Id = u.Id,
                UserName = u.UserName,
                UserLastName1 = u.UserLastName1,
                UserLastName2 = u.UserLastName2,
                Email = u.Email,
                IsActive = u.IsActive,
                IsTrained = u.IsTrained,
                ScientificCategory = (int)u.ScientificCategory,
                TeachingCategory = (int)u.TeachingCategory,
                InvestigationCategory = (int)u.InvestigationCategory,
                AreaId = u.AreaId,
                AreaNombre = u.Area != null ? u.Area.Nombre : null,
                UniversidadId = u.Area != null ? u.Area.UniversidadId : null,
                UniversidadNombre = u.Area != null && u.Area.Universidad != null ? u.Area.Universidad.Nombre : null,
                Roles = u.UserRoles.Select(ur => ur.Role.ToString()).ToList()
            })
            .OrderBy(u => u.UserName)
            .ToListAsync(ct);
    }

    public Task<List<JefeDeProyectoDto>> GetJefesDeProyectoAsync(CancellationToken ct = default)
    {
        return _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.UserRoles.Any(r => r.Role == RolesEnum.Jefe_de_Proyecto))
            .Select(u => new JefeDeProyectoDto
            {
                Id = u.Id,
                NombreCompleto = u.UserName + " " + u.UserLastName1 + (u.UserLastName2 != null ? " " + u.UserLastName2 : ""),
                Email = u.Email
            })
            .OrderBy(u => u.NombreCompleto)
            .ToListAsync(ct);
    }

    public async Task<Result> AssignRoleAsync(string userId, string roleName, CancellationToken ct = default)
    {
        if (!System.Enum.TryParse<RolesEnum>(roleName, out var roleEnum) || roleEnum == RolesEnum.None)
            return Result.Failure(new[] { "Rol no válido." });

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
            return Result.Failure(new[] { "Usuario no encontrado." });

        var alreadyAssigned = await _context.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.Role == roleEnum, ct);
        if (alreadyAssigned)
            return Result.Failure(new[] { "El usuario ya tiene este rol asignado." });

        _context.UserRoles.Add(new UserRole { UserId = userId, Role = roleEnum });
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> RemoveRoleAsync(string userId, string roleName, CancellationToken ct = default)
    {
        if (!System.Enum.TryParse<RolesEnum>(roleName, out var roleEnum) || roleEnum == RolesEnum.None)
            return Result.Failure(new[] { "Rol no válido." });

        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.Role == roleEnum, ct);

        if (userRole == null)
            return Result.Success(); // Idempotente

        _context.UserRoles.Remove(userRole);
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
