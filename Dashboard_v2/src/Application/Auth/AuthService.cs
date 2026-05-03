using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.Auth;

/// <summary>
/// Servicio que encapsula la lógica de autenticación sin exponer commands/queries a la capa Web.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly IIdentityService _identityService;
    private readonly IRequestValidationService _validationService;
    private readonly IUser _currentUser;
    private readonly IApplicationDbContext _context;

    public AuthService(
        IIdentityService identityService,
        IRequestValidationService validationService,
        IUser currentUser,
        IApplicationDbContext context)
    {
        _identityService = identityService;
        _validationService = validationService;
        _currentUser = currentUser;
        _context = context;
    }

    public async Task<Result> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        await _validationService.ValidateAndThrowAsync(request, ct);

        var (result, _) = await _identityService.CreateUserAsync(
            request.UserName,
            request.UserLastName1,
            request.UserLastName2,
            request.Email,
            request.Password,
            request.BirthDate,
            request.IsTrained,
            request.TeachingCategory,
            request.ScientificCategory,
            request.InvestigationCategory);

        return result;
    }

    public async Task<(Result Result, LoginResponse? Response)> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        await _validationService.ValidateAndThrowAsync(request, ct);
        return await _identityService.LoginAsync(request.Email, request.Password, request.SelectedRole, request.SelectedAreaId);
    }

    public Task<Result> LogoutAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success());
    }

    public async Task<CurrentUserDto?> GetCurrentUserAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.Id))
        {
            return null;
        }

        var userEntity = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == _currentUser.Id)
            .Select(u => new { u.Id, u.UserName, u.Email, u.AreaId, AreaNombre = u.Area != null ? u.Area.Nombre : null })
            .FirstOrDefaultAsync(ct);

        if (userEntity == null || string.IsNullOrWhiteSpace(userEntity.Id))
            return null;

        var hasLinkedAuthor = await _context.Authors
            .AsNoTracking()
            .AnyAsync(a => a.UserId == _currentUser.Id, ct);

        return new CurrentUserDto
        {
            Id = userEntity.Id,
            UserName = userEntity.UserName ?? string.Empty,
            Email = userEntity.Email ?? string.Empty,
            Role = _currentUser.Roles?.FirstOrDefault(),
            AreaId = userEntity.AreaId,
            AreaNombre = userEntity.AreaNombre,
            HasLinkedAuthor = hasLinkedAuthor
        };
    }
}
