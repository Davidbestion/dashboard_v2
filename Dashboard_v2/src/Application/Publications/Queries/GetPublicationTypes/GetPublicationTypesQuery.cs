using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Publications;

namespace Dashboard_v2.Application.Publications.Queries.GetPublicationTypes;

/// <summary>Retorna todos los tipos de publicación disponibles (para el selector al crear/editar).</summary>
public record GetPublicationTypesQuery : IRequest<List<PublicationTypeDto>>;

public class GetPublicationTypesQueryHandler : IRequestHandler<GetPublicationTypesQuery, List<PublicationTypeDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPublicationTypesQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<List<PublicationTypeDto>> Handle(GetPublicationTypesQuery request, CancellationToken cancellationToken)
    {
        return await _context.PublicationTypes
            .AsNoTracking()
            .Select(pt => new PublicationTypeDto { Id = pt.Id, Name = pt.Name })
            .OrderBy(pt => pt.Name)
            .ToListAsync(cancellationToken);
    }
}
