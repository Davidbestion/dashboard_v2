using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Domain.Enums;

namespace Dashboard_v2.Application.Publications.Commands.CreatePublication;

/// <summary>
/// Crea una nueva publicación y registra al Profesor actual como uno de sus autores.<br/>
/// Si el usuario no tiene perfil de autor, se crea automáticamente usando su nombre completo.<br/>
/// Los autores adicionales se pasan como lista de nombres; se crean como autores sin cuenta.
/// </summary>
public record CreatePublicationCommand : IRequest<(Result Result, string? PublicationId)>
{
    public string Title { get; init; } = default!;
    public string PublicationData { get; init; } = default!;
    public PublicationType PublicationType { get; init; }
    public string? UrlDoi { get; init; }
    /// <summary>IDs de autores ya existentes en BD que son coautores.</summary>
    public List<string> AdditionalAuthorIds { get; init; } = [];
    /// <summary>Nombres de coautores nuevos (no existían en la BD).</summary>
    public List<string> AdditionalAuthorNames { get; init; } = [];
    /// <summary>
    /// IDs de usuarios registrados que serán coautores.<br/>
    /// Si el usuario ya tiene perfil de autor se reutiliza; si no, se crea automáticamente.
    /// </summary>
    public List<string> AdditionalUserIds { get; init; } = [];

    // ── Campos de especialización ─────────────────────────────────────────────────────────────
    /// <summary>Indexación. Obligatorio para tipos Libro, Monografía, Capítulo, Artículo de Divulgación.</summary>
    public string? Index { get; init; }
    /// <summary>Nombre de la revista. Obligatorio para tipo Diario.</summary>
    public string? JournalName { get; init; }
    /// <summary>Base de datos donde aparece la revista. Obligatorio para tipo Diario.</summary>
    public string? DataBase { get; init; }
    /// <summary>Grupo de la revista (1–4). Obligatorio para tipo Diario.</summary>
    public int? Group { get; init; }
    /// <summary>Cuartil Scimago. Obligatorio para tipo Diario con grupo 1.</summary>
    public Cuartil? Cuartil { get; init; }
}

public class CreatePublicationCommandHandler : IRequestHandler<CreatePublicationCommand, (Result Result, string? PublicationId)>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public CreatePublicationCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<(Result Result, string? PublicationId)> Handle(
        CreatePublicationCommand request, CancellationToken cancellationToken)
    {
        // Validar que el tipo de publicación es válido
        if (!Enum.IsDefined(typeof(PublicationType), request.PublicationType))
            return (Result.Failure(["Tipo de publicación no válido."]), null);

        // Validar campos de especialización
        if (request.PublicationType == PublicationType.Diario)
        {
            if (string.IsNullOrWhiteSpace(request.JournalName) ||
                string.IsNullOrWhiteSpace(request.DataBase) ||
                request.Group is null or < 1 or > 4)
                return (Result.Failure(["Datos de la revista son obligatorios: nombre, base de datos y grupo (1–4)."]), null);
            if (request.Group == 1 && (request.Cuartil is null || !Enum.IsDefined(typeof(Cuartil), request.Cuartil.Value)))
                return (Result.Failure(["Cuartil es obligatorio para revistas de grupo 1."]), null);
        }
        else if (string.IsNullOrWhiteSpace(request.Index))
        {
            return (Result.Failure(["La indexación es obligatoria para este tipo de publicación."]), null);
        }

        // Obtener o crear el perfil de autor del usuario actual
        var author = await _context.Authors
            .FirstOrDefaultAsync(a => a.UserId == _currentUser.Id, cancellationToken);

        if (author == null)
        {
            // Crear perfil de autor usando el nombre completo del usuario
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == _currentUser.Id, cancellationToken);

            if (user == null)
                return (Result.Failure(["Usuario no encontrado."]), null);

            author = new Author
            {
                Name = $"{user.UserName} {user.UserLastName1}{(string.IsNullOrEmpty(user.UserLastName2) ? string.Empty : " " + user.UserLastName2)}".Trim(),
                UserId = user.Id
            };
            _context.Authors.Add(author);
            // Guardar primero para tener el Id del autor antes de crear la publicación
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Construir la publicación con el usuario actual como primer autor
        var publication = new Publication
        {
            Title = request.Title.Trim(),
            PublicationData = request.PublicationData,
            PublicationType = request.PublicationType,
            UrlDoi = string.IsNullOrWhiteSpace(request.UrlDoi) ? null : request.UrlDoi.Trim(),
            AuthorPublications = [new AuthorPublication { AuthorId = author.Id }]
        };

        // Agregar coautores existentes por ID
        foreach (var authorId in request.AdditionalAuthorIds.Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            // Verificar que el autor no sea el mismo usuario actual y que exista
            if (authorId != author.Id && await _context.Authors.AnyAsync(a => a.Id == authorId, cancellationToken))
            {
                publication.AuthorPublications.Add(new AuthorPublication { AuthorId = authorId });
            }
        }

        // Agregar coautores nuevos por nombre (se crean como autores sin cuenta vinculada)
        foreach (var name in request.AdditionalAuthorNames.Where(n => !string.IsNullOrWhiteSpace(n)))
        {
            publication.AuthorPublications.Add(new AuthorPublication
            {
                Author = new Author { Name = name.Trim() }
            });
        }

        // Agregar coautores referenciados como usuarios (find-or-create author vinculado)
        foreach (var userId in request.AdditionalUserIds.Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            if (userId == _currentUser.Id) continue; // ya es el autor principal

            var coAuthor = await _context.Authors
                .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

            if (coAuthor == null)
            {
                var coUser = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
                if (coUser == null) continue;

                coAuthor = new Author
                {
                    Name = $"{coUser.UserName} {coUser.UserLastName1}{(string.IsNullOrEmpty(coUser.UserLastName2) ? string.Empty : " " + coUser.UserLastName2)}".Trim(),
                    UserId = coUser.Id
                };
                _context.Authors.Add(coAuthor);
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (publication.AuthorPublications.All(ap => ap.AuthorId != coAuthor.Id))
                publication.AuthorPublications.Add(new AuthorPublication { AuthorId = coAuthor.Id });
        }

        // Agregar especialización según el tipo
        if (request.PublicationType == PublicationType.Diario)
        {
            publication.JournalPublication = new JournalPublication
            {
                PublicationId = publication.Id,
                Name = request.JournalName!.Trim(),
                DataBase = request.DataBase!.Trim(),
                Group = request.Group!.Value,
                JournalGroup1Publication = request.Group == 1
                    ? new JournalGroup1Publication { PublicationId = publication.Id, Cuartil = request.Cuartil!.Value }
                    : null
            };
        }
        else
        {
            publication.IndexedPublication = new IndexedPublication
            {
                PublicationId = publication.Id,
                Index = request.Index!.Trim()
            };
        }

        _context.Publications.Add(publication);
        await _context.SaveChangesAsync(cancellationToken);

        return (Result.Success(), publication.Id);
    }
}
