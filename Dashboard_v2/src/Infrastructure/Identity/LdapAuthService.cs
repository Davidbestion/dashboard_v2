using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Domain.Constants;
using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Novell.Directory.Ldap;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Infrastructure.Identity;

/// <summary>
/// Authentication service using OpenLDAP directory. Performs search-then-bind authentication
/// and auto-provisions users on first login. Credentials (email/password) are validated by the
/// directory; roles and the academic profile are managed locally in PostgreSQL.
///
/// Login flow (search-then-bind pattern):
///   1. The admin account searches the directory for the user's DN by the "mail" attribute.
///   2. A re-bind with the found DN and the user's password verifies their credentials.
///   3. On success, the user is found or created in the local database (auto-provisioning).
///   4. Roles are loaded from the local database and a JWT is issued.
/// </summary>
public class LdapAuthService : IIdentityService
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly UserAreaResolutionService _userAreaResolutionService;
    private readonly string _host;
    private readonly int _port;
    private readonly string _usersDn;
    private readonly string _adminDn;
    private readonly string _adminPassword;

    /// <summary>
    /// Inicializa el servicio LDAP con acceso a persistencia, generación de JWT,
    /// configuración del directorio y resolución del área del usuario.
    /// </summary>
    public LdapAuthService(
        IApplicationDbContext context,
        IJwtService jwtService,
        IConfiguration configuration,
        UserAreaResolutionService userAreaResolutionService)
    {
        _context = context;
        _jwtService = jwtService;
        _userAreaResolutionService = userAreaResolutionService;
        _host = configuration["Auth:Ldap:Host"]
            ?? throw new InvalidOperationException("Auth:Ldap:Host is not configured.");
        _port = int.TryParse(configuration["Auth:Ldap:Port"], out var p) ? p : 389;
        _usersDn = configuration["Auth:Ldap:UsersDn"]
            ?? throw new InvalidOperationException("Auth:Ldap:UsersDn is not configured.");
        _adminDn = configuration["Auth:Ldap:AdminDn"]
            ?? throw new InvalidOperationException("Auth:Ldap:AdminDn is not configured.");
        var adminPassword = configuration["Auth:Ldap:AdminPassword"];
        if (string.IsNullOrEmpty(adminPassword))
            throw new InvalidOperationException(
                "Auth:Ldap:AdminPassword is not configured. Set it via environment variable Auth__Ldap__AdminPassword.");
        _adminPassword = adminPassword;
    }

    public async Task<string?> GetUserNameAsync(string userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);
        return user?.UserName;
    }

    public async Task<bool> IsInRoleAsync(string userId, string role)
    {
        if (!Enum.TryParse<RolesEnum>(role, out var roleEnum))
            return false;
        return await _context.UserRoles
            .AsNoTracking()
            .AnyAsync(ur => ur.UserId == userId && ur.Role == roleEnum);
    }

    public async Task<bool> AuthorizeAsync(string userId, string policyName)
    {
        if (policyName == Policies.CanPurge)
            return await IsInRoleAsync(userId, nameof(RolesEnum.Superuser));
        return false;
    }

    /// <summary>No aplicable en modo LDAP. Los usuarios se gestionan en el directorio.</summary>
    public Task<(Result Result, string UserId)> CreateUserAsync(string userName, string password)
        => Task.FromResult((Result.Failure(["En modo LDAP los usuarios se gestionan en el directorio."]), string.Empty));

    /// <summary>No aplicable en modo LDAP. Los usuarios se gestionan en el directorio.</summary>
    public Task<(Result Result, string UserId)> CreateUserAsync(
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
        => Task.FromResult((Result.Failure(["En modo LDAP los usuarios se gestionan en el directorio."]), string.Empty));

    /// <summary>
    /// Authenticates against LDAP via search-then-bind. Auto-provisions the user in the local
    /// database if this is their first login. Returns a JWT token on success or an error result
    /// on failure (invalid credentials, inactive account, or missing role assignment).
    /// </summary>
    public async Task<(Result Result, LoginResponse? Response)> LoginAsync(
        string email, string password, string? selectedRole = null, string? selectedAreaId = null)
    {
        if (!TrySearchThenBind(email, password, out var ldapAttributes))
            return (Result.Failure(["Credenciales inválidas."]), null);

        var user = await ProvisionUserAsync(email, ldapAttributes);

        if (!user.IsActive)
            return (Result.Failure(["La cuenta está desactivada."]), null);

        var roles = await _context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.Role.ToString())
            .ToListAsync();

        if (roles.Count == 0)
            return (Result.Failure(["Tu cuenta aún no tiene un rol asignado. Contacta con el administrador del sistema para que te configure el acceso."]), null);

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
            return (Result.Success(), new LoginResponse
            {
                RequiresRoleSelection = true,
                AvailableRoles = roles
            });
        }

        var areaResolution = await _userAreaResolutionService.EnsureAreaAssignedAsync(
            user.Id,
            selectedAreaId,
            CancellationToken.None);

        if (!areaResolution.Result.Succeeded || areaResolution.Response?.RequiresAreaSelection == true)
        {
            return areaResolution;
        }

        var token = _jwtService.GenerateToken(user.Id, user.UserName, user.Email, [roleToUse]);
        return (Result.Success(), new LoginResponse { Token = token });
    }

    public async Task<(string? UserId, string? UserName, string? Email)> GetUserDetailsAsync(string userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);
        return (user?.Id, user?.UserName, user?.Email);
    }

    public async Task<Result> DeleteUserAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return Result.Success();

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(CancellationToken.None);
        return Result.Success();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Searches the LDAP directory for the user's DN, then binds with their password to verify credentials.
    /// On success, populates <paramref name="attributes"/> with the user's LDAP entry attributes (cn, sn, uid).
    /// Returns <c>true</c> if authentication succeeded; <c>false</c> on any LDAP error or invalid credentials.
    /// </summary>
    private bool TrySearchThenBind(string email, string password, out LdapAttributeSet? attributes)
    {
        attributes = null;
        try
        {
            using var conn = new LdapConnection();
            conn.Connect(_host, _port);

            // Paso 1: bind como admin para poder buscar
            conn.Bind(_adminDn, _adminPassword);

            // Paso 2: buscar el DN del usuario por su email
            var search = conn.Search(
                _usersDn,
                LdapConnection.ScopeSub,
                $"(mail={EscapeLdapFilter(email)})",
                ["dn", "cn", "sn", "uid", "mail"],
                false);

            if (!search.HasMore())
                return false;

            var entry = search.Next();
            var userDn = entry.Dn;

            // Paso 3: rebind con el DN real del usuario y su contraseña
            conn.Bind(userDn, password);

            // Paso 4: si llegamos aquí, el bind fue exitoso
            attributes = entry.GetAttributeSet();
            return true;
        }
        catch (LdapException)
        {
            return false;
        }
    }

    /// <summary>
    /// Escapa caracteres especiales en filtros LDAP para prevenir LDAP injection.
    /// RFC 4515: ( ) \ * \0 NUL deben escaparse.
    /// </summary>
    private static string EscapeLdapFilter(string value) =>
        value
            .Replace("\\", "\\5c")
            .Replace("*",  "\\2a")
            .Replace("(",  "\\28")
            .Replace(")",  "\\29")
            .Replace("\0", "\\00");

    /// <summary>
    /// Busca al usuario en la BD local por email. Si no existe, lo crea (auto-provisioning)
    /// con los datos básicos obtenidos de LDAP. El perfil académico se completa después por un admin.
    /// </summary>
    private async Task<User> ProvisionUserAsync(string email, LdapAttributeSet? ldapAttrs)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user != null)
            return user;

        var uid = ldapAttrs?.GetAttribute("uid")?.StringValue ?? email.Split('@')[0];
        var sn  = ldapAttrs?.GetAttribute("sn")?.StringValue  ?? uid;

        user = new User
        {
            Id            = Guid.NewGuid().ToString(),
            UserName      = uid,
            UserLastName1 = sn,
            Email         = email,
            PasswordHash  = null,   // No se almacena contraseña en modo LDAP
            BirthDate     = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc),
            IsActive      = true,
            CreatedAt     = DateTimeOffset.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(CancellationToken.None);
        return user;
    }
}
