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
/// Implementación de <see cref="IIdentityService"/> que autentica usuarios contra un servidor LDAP.
/// Las credenciales (email/contraseña) son validadas por el directorio; los roles y el perfil
/// académico se gestionan localmente en PostgreSQL.
///
/// Flujo de login (patrón "search then bind"):
///   1. El admin se conecta al LDAP para buscar el DN del usuario filtrando por atributo "mail" = email.
///   2. Se reintenta el bind con ese DN y la contraseña que introdujo el usuario.
///   3. Si el bind tiene éxito, busca o crea el usuario en la BD local (auto-provisioning).
///   4. Carga sus roles desde la BD y emite un JWT.
/// </summary>
public class LdapAuthService : IIdentityService
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly string _host;
    private readonly int _port;
    private readonly string _usersDn;
    private readonly string _adminDn;
    private readonly string _adminPassword;

    public LdapAuthService(IApplicationDbContext context, IJwtService jwtService, IConfiguration configuration)
    {
        _context = context;
        _jwtService = jwtService;
        _host = configuration["Auth:Ldap:Host"] ?? "localhost";
        _port = int.TryParse(configuration["Auth:Ldap:Port"], out var p) ? p : 389;
        _usersDn = configuration["Auth:Ldap:UsersDn"] ?? "ou=people,dc=matcom,dc=uh,dc=cu";
        _adminDn = configuration["Auth:Ldap:AdminDn"] ?? $"cn=admin,{configuration["Auth:Ldap:BaseDn"] ?? "dc=matcom,dc=uh,dc=cu"}";
        _adminPassword = configuration["Auth:Ldap:AdminPassword"] ?? "admin";
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
    /// Autentica al usuario contra el servidor LDAP y emite un JWT.
    /// Usa el patrón "search then bind":
    ///   1. El admin se conecta al LDAP para buscar el DN del usuario por su email (atributo mail).
    ///   2. Se reintenta el bind con ese DN y la contraseña que introdujo el usuario.
    /// Así el formulario siempre usa email + contraseña, independientemente del uid LDAP.
    /// </summary>
    public async Task<(Result Result, LoginResponse? Response)> LoginAsync(
        string email, string password, string? selectedRole = null)
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
            return (Result.Success(), new LoginResponse
            {
                RequiresRoleSelection = true,
                AvailableRoles = roles
            });
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
    /// Patrón "search then bind":
    ///   1. Se conecta al LDAP como admin.
    ///   2. Busca el DN del usuario filtrando por atributo "mail" = <paramref name="email"/>.
    ///   3. Reintenta el bind con ese DN y la contraseña del usuario.
    ///   4. Si el rebind tiene éxito, lee los atributos del usuario (cn, sn, uid) y devuelve true.
    /// Esto permite que el formulario use siempre email + contraseña.
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
