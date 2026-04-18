using Dashboard_v2.Application.Common.Interfaces;

namespace Dashboard_v2.Application.Clasificaciones.Queries.GetClasificaciones;

public record GetClasificacionesQuery : IRequest<List<ClasificacionDto>>;

public class GetClasificacionesQueryHandler : IRequestHandler<GetClasificacionesQuery, List<ClasificacionDto>>
{
    private readonly IApplicationDbContext _context;

    public GetClasificacionesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ClasificacionDto>> Handle(GetClasificacionesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Clasificaciones
            .OrderBy(c => c.Nombre)
            .Select(c => new ClasificacionDto { Id = c.Id, Nombre = c.Nombre })
            .ToListAsync(cancellationToken);
    }
}
