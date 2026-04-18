using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Publications;

namespace Dashboard_v2.Application.Publications.Queries.GetPublicationById;

/// <summary>
/// Retorna una publicación por ID, solo si el usuario autenticado es uno de sus autores.
/// Devuelve null si la publicación no existe o el usuario no es autor (evita revelar existencia).
/// </summary>
public record GetPublicationByIdQuery(string Id) : IRequest<PublicationDto?>;

public class GetPublicationByIdQueryHandler : IRequestHandler<GetPublicationByIdQuery, PublicationDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public GetPublicationByIdQueryHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PublicationDto?> Handle(GetPublicationByIdQuery request, CancellationToken cancellationToken)
    {
        var authorId = await _context.Authors
            .AsNoTracking()
            .Where(a => a.UserId == _currentUser.Id)
            .Select(a => a.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (authorId == null)
            return null;

        return await _context.Publications
            .AsNoTracking()
            .Where(p => p.Id == request.Id && p.AuthorPublications.Any(ap => ap.AuthorId == authorId))
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
            .FirstOrDefaultAsync(cancellationToken);
    }
}
