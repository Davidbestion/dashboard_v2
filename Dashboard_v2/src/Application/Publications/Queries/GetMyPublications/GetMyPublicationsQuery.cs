using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Publications;

namespace Dashboard_v2.Application.Publications.Queries.GetMyPublications;

/// <summary>
/// Retorna todas las publicaciones donde el usuario autenticado figura como autor.
/// Si el usuario no tiene perfil de autor todavía, retorna lista vacía.
/// </summary>
public record GetMyPublicationsQuery : IRequest<List<PublicationDto>>;

public class GetMyPublicationsQueryHandler : IRequestHandler<GetMyPublicationsQuery, List<PublicationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public GetMyPublicationsQueryHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<PublicationDto>> Handle(GetMyPublicationsQuery request, CancellationToken cancellationToken)
    {
        // Buscar el perfil de autor vinculado al usuario actual
        var authorId = await _context.Authors
            .AsNoTracking()
            .Where(a => a.UserId == _currentUser.Id)
            .Select(a => a.Id)
            .FirstOrDefaultAsync(cancellationToken);

        // El usuario no ha registrado ninguna publicación aún
        if (authorId == null)
            return [];

        return await _context.Publications
            .AsNoTracking()
            .Where(p => p.AuthorPublications.Any(ap => ap.AuthorId == authorId))
            .Select(p => new PublicationDto
            {
                Id = p.Id,
                Title = p.Title,
                PublicationData = p.PublicationData,
                UrlDoi = p.UrlDoi,
                PublicationType = (int)p.PublicationType,
                Authors = p.AuthorPublications
                    .Select(ap => new AuthorDto
                    {
                        Id = ap.Author.Id,
                        Name = ap.Author.Name,
                        UserId = ap.Author.UserId
                    })
                    .ToList(),
                IndexedPublication = p.IndexedPublication == null ? null : new IndexedPublicationDto
                {
                    Index = p.IndexedPublication.Index
                },
                JournalPublication = p.JournalPublication == null ? null : new JournalPublicationDto
                {
                    DataBase = p.JournalPublication.DataBase,
                    Group = p.JournalPublication.Group,
                    Cuartil = p.JournalPublication.JournalGroup1Publication != null
                        ? p.JournalPublication.JournalGroup1Publication.Cuartil
                        : null
                },
                ProyectoId = p.ProyectoId
            })
            .OrderBy(p => p.Title)
            .ToListAsync(cancellationToken);
    }
}
