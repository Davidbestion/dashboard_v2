using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.Clasificaciones.Commands.DeleteClasificacion;

public record DeleteClasificacionCommand(string Id) : IRequest<Result>;

public class DeleteClasificacionCommandHandler : IRequestHandler<DeleteClasificacionCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteClasificacionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteClasificacionCommand request, CancellationToken cancellationToken)
    {
        var clasificacion = await _context.Clasificaciones.FindAsync([request.Id], cancellationToken);
        if (clasificacion is null)
            return Result.Failure(["Clasificación no encontrada."]);

        if (await _context.Proyectos.AnyAsync(p => p.ClasificacionId == request.Id, cancellationToken))
            return Result.Failure(["No se puede eliminar una clasificación que tiene proyectos asociados."]);

        _context.Clasificaciones.Remove(clasificacion);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
