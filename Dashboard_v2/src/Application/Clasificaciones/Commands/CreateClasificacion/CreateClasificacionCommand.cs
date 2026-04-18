using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Domain.Entities;

namespace Dashboard_v2.Application.Clasificaciones.Commands.CreateClasificacion;

public record CreateClasificacionCommand(string Nombre) : IRequest<(Result Result, string? Id)>;

public class CreateClasificacionCommandHandler : IRequestHandler<CreateClasificacionCommand, (Result Result, string? Id)>
{
    private readonly IApplicationDbContext _context;

    public CreateClasificacionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(Result Result, string? Id)> Handle(
        CreateClasificacionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return (Result.Failure(["El nombre es obligatorio."]), null);

        if (await _context.Clasificaciones.AnyAsync(c => c.Nombre == request.Nombre.Trim(), cancellationToken))
            return (Result.Failure(["Ya existe una clasificación con ese nombre."]), null);

        var clasificacion = new Clasificacion { Nombre = request.Nombre.Trim() };
        _context.Clasificaciones.Add(clasificacion);
        await _context.SaveChangesAsync(cancellationToken);
        return (Result.Success(), clasificacion.Id);
    }
}
