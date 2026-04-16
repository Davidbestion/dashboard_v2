using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.Proyectos.Commands.DeleteProyecto;

public record DeleteProyectoCommand(string Id) : IRequest<Result>;

public class DeleteProyectoCommandHandler : IRequestHandler<DeleteProyectoCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public DeleteProyectoCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteProyectoCommand request, CancellationToken cancellationToken)
    {
        var proyecto = await _context.Proyectos.FindAsync([request.Id], cancellationToken);
        if (proyecto is null)
            return Result.Failure(["Proyecto no encontrado."]);

        var ownerFilter = ProyectoHelper.GetOwnerFilter(_currentUser);
        if (ownerFilter is not null && proyecto.JefeId != ownerFilter)
            return Result.Failure(["No tiene permiso para eliminar este proyecto."]);

        _context.Proyectos.Remove(proyecto);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
