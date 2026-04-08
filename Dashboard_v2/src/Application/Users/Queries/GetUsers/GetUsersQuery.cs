using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Application.Users.Queries.GetUsers;

/// <summary>DTO de respuesta que combina datos del usuario con sus roles como strings (para JSON).</summary>
public record UserWithRolesDto
{
    public string Id { get; init; } = default!;
    public string UserName { get; init; } = default!;
    public string UserLastName1 { get; init; } = default!;
    public string? UserLastName2 { get; init; }
    public string Email { get; init; } = default!;
    public bool IsActive { get; init; }
    public bool IsTrained { get; init; }
    public int ScientificCategory { get; init; }
    public int TeachingCategory { get; init; }
    public int InvestigationCategory { get; init; }
    public List<string> Roles { get; init; } = [];
}

/// <summary>Consulta MediatR sin parámetros que retorna todos los usuarios del sistema con sus roles.</summary>
public record GetUsersQuery : IRequest<List<UserWithRolesDto>>;

/// <summary>
/// Obtiene todos los usuarios junto con sus roles, ordenados por nombre.<br/>
/// Solo accesible para Superusers (la autorización se aplica en el endpoint Web).
/// </summary>
public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, List<UserWithRolesDto>>
{
    private readonly IApplicationDbContext _context;

    public GetUsersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>Ejecuta la consulta: SELECT users + sus roles (enum → string), ordenado por UserName.</summary>
    public async Task<List<UserWithRolesDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        return await _context.Users
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
                Roles = u.UserRoles.Select(ur => ur.Role.ToString()).ToList()
            })
            .OrderBy(u => u.UserName)
            .ToListAsync(cancellationToken);
    }
}
