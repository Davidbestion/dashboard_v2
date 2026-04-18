using Dashboard_v2.Application.Common.Interfaces;
using RolesEnum = global::Dashboard_v2.Domain.Enums.Roles;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Application.Users.Queries.GetJefesDeProyecto;

/// <summary>DTO reducido para seleccionar un Jefe de Proyecto en el formulario de proyectos.</summary>
public record JefeDeProyectoDto
{
    public string Id { get; init; } = default!;
    public string NombreCompleto { get; init; } = default!;
    public string Email { get; init; } = default!;
}

/// <summary>
/// Consulta MediatR que retorna todos los usuarios activos con el rol <c>Jefe_de_Proyecto</c>.
/// Usada por el frontend para poblar el selector de jefe al crear o editar un proyecto.
/// </summary>
public record GetJefesDeProyectoQuery : IRequest<List<JefeDeProyectoDto>>;

/// <summary>
/// Retorna los usuarios activos con rol <c>Jefe_de_Proyecto</c>, ordenados por nombre.
/// La autorización se aplica en el endpoint Web (Superuser y Jefe_de_Proyecto pueden consultar).
/// </summary>
public class GetJefesDeProyectoQueryHandler : IRequestHandler<GetJefesDeProyectoQuery, List<JefeDeProyectoDto>>
{
    private readonly IApplicationDbContext _context;

    public GetJefesDeProyectoQueryHandler(IApplicationDbContext context)
        => _context = context;

    public async Task<List<JefeDeProyectoDto>> Handle(
        GetJefesDeProyectoQuery request, CancellationToken cancellationToken)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.UserRoles.Any(r => r.Role == RolesEnum.Jefe_de_Proyecto))
            .Select(u => new JefeDeProyectoDto
            {
                Id = u.Id,
                NombreCompleto = u.UserName + " " + u.UserLastName1 +
                    (u.UserLastName2 != null ? " " + u.UserLastName2 : ""),
                Email = u.Email,
            })
            .OrderBy(u => u.NombreCompleto)
            .ToListAsync(cancellationToken);
    }
}
