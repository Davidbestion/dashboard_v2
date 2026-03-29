using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.Publications.Commands.DeletePublication;

/// <summary>
/// Elimina una publicación y todas sus relaciones de autoría.<br/>
/// Solo puede hacerlo un usuario que sea autor de esa publicación.
/// </summary>
public record DeletePublicationCommand(string Id) : IRequest<Result>;

public class DeletePublicationCommandHandler : IRequestHandler<DeletePublicationCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public DeletePublicationCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeletePublicationCommand request, CancellationToken cancellationToken)
    {
        // Verificar autoría antes de buscar y eliminar
        var isAuthor = await _context.AuthorPublications
            .AnyAsync(ap =>
                ap.PublicationId == request.Id &&
                ap.Author.UserId == _currentUser.Id,
                cancellationToken);

        if (!isAuthor)
            return Result.Failure(["Publicación no encontrada o no tienes permiso para eliminarla."]);

        var publication = await _context.Publications.FindAsync([request.Id], cancellationToken);
        if (publication == null)
            return Result.Failure(["Publicación no encontrada."]);

        // El Cascade en AuthorPublicationConfiguration elimina las filas de AuthorPublications automáticamente
        _context.Publications.Remove(publication);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
