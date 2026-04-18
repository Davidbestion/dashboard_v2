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
    /// <summary>Base de datos donde aparece la revista. Obligatorio para tipo Diario.</summary>
    public string? DataBase { get; init; }
    /// <summary>Grupo de la revista (1–4). Obligatorio para tipo Diario.</summary>
    public int? Group { get; init; }
    /// <summary>Cuartil Scimago. Obligatorio para tipo Diario con grupo 1.</summary>
    public string? Cuartil { get; init; }
    /// <summary>ID del proyecto del que deriva esta publicación. Null si no está vinculada a ningún proyecto.</summary>
    public string? ProyectoId { get; init; }
}

public class CreatePublicationCommandHandler : IRequestHandler<CreatePublicationCommand, (Result Result, string? PublicationId)>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly IAuthorResolutionService _authorResolution;

    public CreatePublicationCommandHandler(
        IApplicationDbContext context,
        IUser currentUser,
        IAuthorResolutionService authorResolution)
    {
        _context = context;
        _currentUser = currentUser;
        _authorResolution = authorResolution;
    }

    public async Task<(Result Result, string? PublicationId)> Handle(
        CreatePublicationCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(typeof(PublicationType), request.PublicationType))
            return (Result.Failure(["Tipo de publicación no válido."]), null);

        // Validar campos de especialización
        if (request.PublicationType == PublicationType.Diario)
        {
            if (string.IsNullOrWhiteSpace(request.DataBase) ||
                request.Group is null or < 1 or > 4)
                return (Result.Failure(["Datos de la revista son obligatorios: base de datos y grupo (1–4)."]), null);
            if (request.Group == 1 && string.IsNullOrWhiteSpace(request.Cuartil))
                return (Result.Failure(["Cuartil es obligatorio para revistas de grupo 1."]), null);
        }
        else if (string.IsNullOrWhiteSpace(request.Index))
        {
            return (Result.Failure(["La indexación es obligatoria para este tipo de publicación."]), null);
        }

        // Obtener o crear el perfil de autor del usuario actual
        var author = await _authorResolution.GetOrCreateForUserAsync(_currentUser.Id!, cancellationToken);

        if (author == null)
            return (Result.Failure(["Usuario no encontrado."]), null);

        // Construir la publicación con el usuario actual como primer autor
        var publication = new Publication
        {
            Title = request.Title.Trim(),
            PublicationData = request.PublicationData,
            PublicationType = request.PublicationType,
            UrlDoi = string.IsNullOrWhiteSpace(request.UrlDoi) ? null : request.UrlDoi.Trim(),
            ProyectoId = string.IsNullOrWhiteSpace(request.ProyectoId) ? null : request.ProyectoId,
            AuthorPublications = [new AuthorPublication { AuthorId = author.Id }]
        };

        // Agregar coautores existentes por ID
        foreach (var authorId in request.AdditionalAuthorIds.Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            if (authorId != author.Id && await _context.Authors.AnyAsync(a => a.Id == authorId, cancellationToken))
                publication.AuthorPublications.Add(new AuthorPublication { AuthorId = authorId });
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

            var coAuthor = await _authorResolution.GetOrCreateForUserAsync(userId, cancellationToken);
            if (coAuthor == null) continue;

            if (publication.AuthorPublications.All(ap => ap.AuthorId != coAuthor.Id))
                publication.AuthorPublications.Add(new AuthorPublication { AuthorId = coAuthor.Id });
        }

        // Agregar especialización según el tipo
        if (request.PublicationType == PublicationType.Diario)
        {
            publication.JournalPublication = new JournalPublication
            {
                PublicationId = publication.Id,
                DataBase = request.DataBase!.Trim(),
                Group = request.Group!.Value,
                JournalGroup1Publication = request.Group == 1
                    ? new JournalGroup1Publication { PublicationId = publication.Id, Cuartil = request.Cuartil! }
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
