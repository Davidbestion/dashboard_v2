using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Domain.Entities;

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
    public string PublicationTypeId { get; init; } = default!;
    public string? UrlDoi { get; init; }
    /// <summary>Nombres de coautores que no son el usuario actual (pueden o no tener cuenta).</summary>
    public List<string> AdditionalAuthorNames { get; init; } = [];
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
        // Validar que el tipo de publicación existe
        var typeExists = await _context.PublicationTypes
            .AnyAsync(pt => pt.Id == request.PublicationTypeId, cancellationToken);
        if (!typeExists)
            return (Result.Failure(["Tipo de publicación no encontrado."]), null);

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
                Name = $"{user.UserName} {user.UserLastName}".Trim(),
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
            PublicationTypeId = request.PublicationTypeId,
            UrlDoi = string.IsNullOrWhiteSpace(request.UrlDoi) ? null : request.UrlDoi.Trim(),
            AuthorPublications = [new AuthorPublication { AuthorId = author.Id }]
        };

        // Agregar coautores adicionales (sin cuenta vinculada)
        foreach (var name in request.AdditionalAuthorNames.Where(n => !string.IsNullOrWhiteSpace(n)))
        {
            publication.AuthorPublications.Add(new AuthorPublication
            {
                Author = new Author { Name = name.Trim() }
            });
        }

        _context.Publications.Add(publication);
        await _context.SaveChangesAsync(cancellationToken);

        return (Result.Success(), publication.Id);
    }
}
