using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.Publications.Commands.UpdatePublication;

/// <summary>
/// Actualiza título, datos y tipo de una publicación.<br/>
/// Solo el usuario que sea autor de la publicación puede modificarla.
/// </summary>
public record UpdatePublicationCommand : IRequest<Result>
{
    public string Id { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string PublicationData { get; init; } = default!;
    public string PublicationTypeId { get; init; } = default!;
    public string? UrlDoi { get; init; }
}

public class UpdatePublicationCommandHandler : IRequestHandler<UpdatePublicationCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public UpdatePublicationCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdatePublicationCommand request, CancellationToken cancellationToken)
    {
        // Verificar que el usuario tiene perfil de autor y es autor de esta publicación
        var isAuthor = await _context.AuthorPublications
            .AnyAsync(ap =>
                ap.PublicationId == request.Id &&
                ap.Author.UserId == _currentUser.Id,
                cancellationToken);

        if (!isAuthor)
            return Result.Failure(["Publicación no encontrada o no tienes permiso para editarla."]);

        var publication = await _context.Publications.FindAsync([request.Id], cancellationToken);
        if (publication == null)
            return Result.Failure(["Publicación no encontrada."]);

        // Validar que el nuevo tipo existe
        var typeExists = await _context.PublicationTypes
            .AnyAsync(pt => pt.Id == request.PublicationTypeId, cancellationToken);
        if (!typeExists)
            return Result.Failure(["Tipo de publicación no encontrado."]);

        publication.Title = request.Title.Trim();
        publication.PublicationData = request.PublicationData;
        publication.PublicationTypeId = request.PublicationTypeId;
        publication.UrlDoi = string.IsNullOrWhiteSpace(request.UrlDoi) ? null : request.UrlDoi.Trim();

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
