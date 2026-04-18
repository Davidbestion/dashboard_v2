using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.Clasificaciones.Commands.UpdateClasificacion;

public record UpdateClasificacionCommand(string Id, string Nombre) : IRequest<Result>;

public class UpdateClasificacionCommandHandler : IRequestHandler<UpdateClasificacionCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateClasificacionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateClasificacionCommand request, CancellationToken cancellationToken)
    {
        var clasificacion = await _context.Clasificaciones.FindAsync([request.Id], cancellationToken);
        if (clasificacion is null)
            return Result.Failure(["Clasificación no encontrada."]);

        if (string.IsNullOrWhiteSpace(request.Nombre))
            return Result.Failure(["El nombre es obligatorio."]);

        if (await _context.Clasificaciones.AnyAsync(
                c => c.Nombre == request.Nombre.Trim() && c.Id != request.Id, cancellationToken))
            return Result.Failure(["Ya existe una clasificación con ese nombre."]);

        clasificacion.Nombre = request.Nombre.Trim();
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
