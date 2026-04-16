using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Domain.Entities;

namespace Dashboard_v2.Application.Proyectos.Commands.UpdateProyectoEnRevision;

public record UpdateProyectoEnRevisionCommand : IRequest<Result>
{
    public string Id { get; init; } = default!;
    public string Titulo { get; init; } = default!;
    public string JefeId { get; init; } = default!;
    public int NumeroMiembros { get; init; }
    public int CantidadMiembrosUH { get; init; }
    public int CantidadEstudiantes { get; init; }
    public int CantidadEstudiantesContratados { get; init; }
    public bool TributaFormacionDoctoral { get; init; }
    public string ClasificacionId { get; init; } = default!;
    public string Situacion { get; init; } = default!;
    public string Tipo { get; init; } = default!;
}

public class UpdateProyectoEnRevisionCommandHandler
    : IRequestHandler<UpdateProyectoEnRevisionCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public UpdateProyectoEnRevisionCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        UpdateProyectoEnRevisionCommand request, CancellationToken cancellationToken)
    {
        var proyecto = await _context.Proyectos.OfType<ProyectoEnRevision>()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (proyecto is null)
            return Result.Failure(["Proyecto no encontrado."]);

        var ownerFilter = ProyectoHelper.GetOwnerFilter(_currentUser);
        if (ownerFilter is not null && proyecto.JefeId != ownerFilter)
            return Result.Failure(["No tiene permiso para modificar este proyecto."]);

        if (!await _context.Clasificaciones.AnyAsync(c => c.Id == request.ClasificacionId, cancellationToken))
            return Result.Failure(["La clasificación indicada no existe."]);

        var jefeId = ProyectoHelper.ResolveJefeId(request.JefeId, _currentUser);
        var jefeValidation = await ProyectoHelper.ValidateJefeAsync(_context, jefeId, cancellationToken);
        if (jefeValidation is not null)
            return jefeValidation;

        ProyectoHelper.SetBase(proyecto, request.Titulo, jefeId,
            request.NumeroMiembros, request.CantidadMiembrosUH, request.CantidadEstudiantes,
            request.CantidadEstudiantesContratados, request.TributaFormacionDoctoral,
            request.ClasificacionId);

        proyecto.Situacion = request.Situacion?.Trim() ?? string.Empty;
        proyecto.Tipo = request.Tipo?.Trim() ?? string.Empty;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
