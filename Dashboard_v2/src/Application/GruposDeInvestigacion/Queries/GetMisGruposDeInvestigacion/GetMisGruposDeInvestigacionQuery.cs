using Dashboard_v2.Application.Common.Interfaces;

namespace Dashboard_v2.Application.GruposDeInvestigacion.Queries.GetMisGruposDeInvestigacion;

/// <summary>
/// Retorna los Grupos de Investigación a los que pertenece el usuario autenticado.
/// Accesible para cualquier usuario con sesión activa.
/// </summary>
public record GetMisGruposDeInvestigacionQuery : IRequest<List<GrupoDeInvestigacionDto>>;

public class GetMisGruposDeInvestigacionQueryHandler
    : IRequestHandler<GetMisGruposDeInvestigacionQuery, List<GrupoDeInvestigacionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public GetMisGruposDeInvestigacionQueryHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<GrupoDeInvestigacionDto>> Handle(
        GetMisGruposDeInvestigacionQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.Id;

        return await _context.GruposDeInvestigacion
            .Where(g => g.Usuarios.Any(u => u.Id == userId))
            .OrderBy(g => g.Nombre)
            .Select(g => new GrupoDeInvestigacionDto
            {
                Id = g.Id,
                Nombre = g.Nombre,
                AreaId = g.AreaId,
                AreaNombre = g.Area.Nombre,
                LineasDeInvestigacionIds = g.LineasDeInvestigacion.Select(l => l.Id).ToList(),
                UsuariosIds = g.Usuarios.Select(u => u.Id).ToList(),
                CreadorId = g.CreadorId
            })
            .ToListAsync(cancellationToken);
    }
}
