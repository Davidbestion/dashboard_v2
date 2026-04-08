using Dashboard_v2.Application.Common.Interfaces;

namespace Dashboard_v2.Application.GruposDeInvestigacion.Queries.GetGruposDeInvestigacion;

public record GetGruposDeInvestigacionQuery : IRequest<List<GrupoDeInvestigacionDto>>;

public class GetGruposDeInvestigacionQueryHandler : IRequestHandler<GetGruposDeInvestigacionQuery, List<GrupoDeInvestigacionDto>>
{
    private readonly IApplicationDbContext _context;

    public GetGruposDeInvestigacionQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<GrupoDeInvestigacionDto>> Handle(
        GetGruposDeInvestigacionQuery request, CancellationToken cancellationToken)
    {
        return await _context.GruposDeInvestigacion
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
