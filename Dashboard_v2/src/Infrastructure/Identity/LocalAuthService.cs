using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Domain.Constants;
using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Infrastructure.Identity;

/// <summary>
/// Implementación de <see cref="IIdentityService"/> que gestiona toda la identidad de usuarios:
/// creación de cuentas, login con JWT, verificación de roles y eliminación.
/// Usa BCrypt para hashear contraseñas y EF Core para persistir en PostgreSQL.
/// No usa ASP.NET Identity — el manejo de usuarios es custom.
/// </summary>
public class LocalAuthService : IIdentityService
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtService _jwtService;

    public LocalAuthService(IApplicationDbContext context, IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    /// <summary>Devuelve el nombre de usuario dado su ID, o <c>null</c> si no existe.</summary>
    public async Task<string?> GetUserNameAsync(string userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);
        return user?.UserName;
    }

    /// <summary>
    /// Comprueba si el usuario tiene el rol indicado (como string, p.ej. "Profesor").
    /// Primero parsea el string al enum <see cref="RolesEnum"/>; si falla, retorna false.
    /// </summary>
    public async Task<bool> IsInRoleAsync(string userId, string role)
    {
        if (!Enum.TryParse<RolesEnum>(role, out var roleEnum))
            return false;
        return await _context.UserRoles
            .AsNoTracking()
            .AnyAsync(ur => ur.UserId == userId && ur.Role == roleEnum);
    }

    /// <summary>
    /// Evalúa una política de autorización.<br/>
    /// Actualmente solo soporta <c>CanPurge</c> (requiere rol Superuser).
    /// Se puede extender aquí para más políticas sin cambiar los endpoints.
    /// </summary>
    public async Task<bool> AuthorizeAsync(string userId, string policyName)
    {
        if (policyName == Policies.CanPurge)
            return await IsInRoleAsync(userId, nameof(RolesEnum.Superuser));

        return false;
    }

    /// <summary>
    /// Sobrecarga de compatibilidad con <see cref="IIdentityService"/> — NO SOPORTADA.
    /// Lanza <see cref="NotSupportedException"/>. Usar la sobrecarga con perfil completo.
    /// </summary>
    public Task<(Result Result, string UserId)> CreateUserAsync(string userName, string password)
        => throw new NotSupportedException("Use CreateUserAsync with full profile parameters.");

    /// <summary>
    /// Crea un nuevo usuario con perfil completo.<br/>
    /// Valida unicidad de email y nombre de usuario.<br/>
    /// Hashea la contraseña con BCrypt antes de persistir (nunca se guarda en texto plano).<br/>
    /// El usuario queda activo pero SIN roles: deben asignarse luego por un Superuser.
    /// </summary>
    public async Task<(Result Result, string UserId)> CreateUserAsync(
        string userName,
        string userLastName1,
        string? userLastName2,
        string email,
        string password,
        DateTime birthDate,
        bool isTrained,
        TeachingCategory teachingCategory,
        ScientificCategory scientificCategory,
        InvestigationCategory investigationCategory)
    {
        if (await _context.Users.AnyAsync(u => u.Email == email))
            return (Result.Failure(["El email ya está en uso."]), string.Empty);

        if (await _context.Users.AnyAsync(u => u.UserName == userName))
            return (Result.Failure(["El nombre de usuario ya está en uso."]), string.Empty);

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = userName,
            UserLastName1 = userLastName1,
            UserLastName2 = userLastName2,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            BirthDate = DateTime.SpecifyKind(birthDate, DateTimeKind.Utc),
            IsTrained = isTrained,
            TeachingCategory = teachingCategory,
            ScientificCategory = scientificCategory,
            InvestigationCategory = investigationCategory,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(CancellationToken.None);

        return (Result.Success(), user.Id);
    }

    /// <summary>
    /// Flujo completo de autenticación:<br/>
    /// 1. Busca al usuario por email y comprueba que esté activo.<br/>
    /// 2. Verifica la contraseña con BCrypt.Verify — si falla, retorna error genérico (no especifica si es email o contraseña).<br/>
    /// 3. Carga los roles del usuario desde la BD.<br/>
    /// 4. Si tiene un solo rol → genera JWT con ese rol.<br/>
    /// 5. Si tiene múltiples roles y no se pasó <paramref name="selectedRole"/> → retorna RequiresRoleSelection=true para que el cliente elija.<br/>
    /// 6. Si se pasó <paramref name="selectedRole"/> → valida que sea suyo y genera JWT con ese rol.<br/>
    /// El JWT NO se almacena en el servidor; el endpoint lo guarda en una cookie HttpOnly.
    /// </summary>
    public async Task<(Result Result, LoginResponse? Response)> LoginAsync(string email, string password, string? selectedRole = null)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || !user.IsActive)
            return (Result.Failure(["Credenciales inválidas."]), null);

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return (Result.Failure(["Credenciales inválidas."]), null);

        var roles = await _context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.Role.ToString())
            .ToListAsync();

        if (roles.Count == 0)
            return (Result.Failure(["El usuario no tiene roles asignados."]), null);

        string roleToUse;

        if (selectedRole != null)
        {
            if (!roles.Contains(selectedRole))
                return (Result.Failure(["El rol seleccionado no está asignado a este usuario."]), null);
            roleToUse = selectedRole;
        }
        else if (roles.Count == 1)
        {
            roleToUse = roles[0];
        }
        else
        {
            // Múltiples roles: el cliente debe elegir uno
            return (Result.Success(), new LoginResponse
            {
                RequiresRoleSelection = true,
                AvailableRoles = roles
            });
        }

        var token = _jwtService.GenerateToken(user.Id, user.UserName, user.Email, [roleToUse]);

        return (Result.Success(), new LoginResponse { Token = token });
    }

    /// <summary>Devuelve Id, nombre y email del usuario. Si no existe retorna una tupla de nulls.</summary>
    public async Task<(string? UserId, string? UserName, string? Email)> GetUserDetailsAsync(string userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        return (user?.Id, user?.UserName, user?.Email);
    }

    /// <summary>
    /// Elimina permanentemente al usuario de la BD.<br/>
    /// Si no existe, retorna éxito silencioso (operación idempotente).
    /// </summary>
    public async Task<Result> DeleteUserAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return Result.Success();

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(CancellationToken.None);
        return Result.Success();
    }
}
