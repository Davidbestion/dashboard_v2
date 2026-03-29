using RolesEnum = global::Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Application.Roles.Queries.GetRoles;

/// <summary>DTO de respuesta con el nombre de un rol del sistema (para serializar a JSON).</summary>
public record RoleDto
{
    public string Name { get; init; } = default!;
}

/// <summary>Consulta MediatR que retorna los roles asignables (excluye <c>None</c> y <c>Superuser</c>).</summary>
public record GetRolesQuery : IRequest<List<RoleDto>>;

/// <summary>
/// Devuelve los roles disponibles para asignar a usuarios ordinarios.<br/>
/// Los roles se obtienen del enum en memoria — no requiere consulta a la BD.<br/>
/// Excluye <c>None</c> (valor nulo del enum) y <c>Superuser</c> (solo se asigna en el seed inicial).
/// </summary>
public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, List<RoleDto>>
{
    /// <summary>Itera el enum Roles, filtra los excluidos, convierte a DTO y ordena alfabéticamente.</summary>
    public Task<List<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = Enum.GetValues<RolesEnum>()
            .Where(r => r != RolesEnum.None && r != RolesEnum.Superuser)
            .Select(r => new RoleDto { Name = r.ToString() })
            .OrderBy(r => r.Name)
            .ToList();

        return Task.FromResult(roles);
    }
}
