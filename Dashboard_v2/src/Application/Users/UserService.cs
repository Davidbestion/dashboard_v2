using System;
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

/// <summary>
/// Application service for user management: listing, role assignment, activation/deactivation, and creation.
/// </summary>
public sealed class UserService : IUserService
{
    private readonly IApplicationDbContext _context;

    public UserService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserWithRolesDto>> GetAllAsync(CancellationToken ct = default)
    {
        var users = await _context.Users
            .AsNoTracking()
            .Include(u => u.Area).ThenInclude(a => a!.Universidad)
            .Include(u => u.UserRoles)
            .OrderBy(u => u.UserName)
            .ToListAsync(ct);

        return users.Select(u => new UserWithRolesDto
        {
            Id = u.Id,
            UserName = u.UserName,
            UserLastName1 = u.UserLastName1,
            UserLastName2 = u.UserLastName2,
            Email = u.Email,
            IsActive = u.IsActive,
            IsTrained = u.IsTrained,
            ScientificCategory = u.ScientificCategory.ToDisplayString(),
            TeachingCategory = u.TeachingCategory.ToDisplayString(),
            InvestigationCategory = u.InvestigationCategory.ToDisplayString(),
            AreaId = u.AreaId,
            AreaNombre = u.Area?.Nombre,
            UniversidadId = u.Area?.UniversidadId,
            UniversidadNombre = u.Area?.Universidad?.Nombre,
            Roles = u.UserRoles.Select(ur => ur.Role.ToString()).ToList()
        }).ToList();
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
            return Result.Failure(["Rol no válido."]);

        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.Role == roleEnum, ct);

        if (userRole == null)
            return Result.Success(); // Idempotente

        _context.UserRoles.Remove(userRole);
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> SetActiveAsync(string userId, bool active, CancellationToken ct = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
            return Result.Failure(["Usuario no encontrado."]);

        user.IsActive = active;
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<(Result Result, string? UserId)> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserName))
            return (Result.Failure(["El nombre de usuario es obligatorio."]), null);
        if (string.IsNullOrWhiteSpace(request.Email))
            return (Result.Failure(["El email es obligatorio."]), null);
        if (!System.Enum.TryParse<RolesEnum>(request.RoleName, out var roleEnum) || roleEnum == RolesEnum.None)
            return (Result.Failure(["Rol no válido."]), null);

        var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email, ct);
        if (emailExists)
            return (Result.Failure(["Ya existe un usuario con ese email."]), null);

        var user = new User
        {
            Id            = Guid.NewGuid().ToString(),
            UserName      = request.UserName.Trim(),
            UserLastName1 = request.UserLastName1.Trim(),
            UserLastName2 = string.IsNullOrWhiteSpace(request.UserLastName2) ? null : request.UserLastName2.Trim(),
            Email         = request.Email.Trim().ToLowerInvariant(),
            PasswordHash  = null,
            BirthDate     = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc),
            IsActive      = true,
            AreaId        = string.IsNullOrWhiteSpace(request.AreaId) ? null : request.AreaId,
            CreatedAt     = DateTimeOffset.UtcNow,
        };

        _context.Users.Add(user);
        _context.UserRoles.Add(new UserRole { UserId = user.Id, Role = roleEnum });
        await _context.SaveChangesAsync(ct);

        return (Result.Success(), user.Id);
    }
}
