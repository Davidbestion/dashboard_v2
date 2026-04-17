using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Domain.Entities;

namespace Dashboard_v2.Application.Proyectos.Commands.UpdateProyectoDesarrolloLocal;

/// <summary>Actualiza los campos de un <see cref="Dashboard_v2.Domain.Entities.ProyectoDesarrolloLocal"/> existente.</summary>
public record UpdateProyectoDesarrolloLocalCommand : IRequest<Result>, IProyectoCommand
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
    public DateOnly FechaInicio { get; init; }
    public DateOnly? FechaCierre { get; init; }
    public string EstadoDeEjecucion { get; init; } = default!;
    public string CodigoProyecto { get; init; } = default!;
    public string EntidadEjecutoraPrincipal { get; init; } = default!;
    public string? EntidadEjecutoraParticipante { get; init; }
    public string? ContribucionSectoresEstrategicos { get; init; }
    public string? ContribucionEjesEstrategicos { get; init; }
    public string Municipio { get; init; } = default!;
}

/// <summary>Manejador de <see cref="UpdateProyectoDesarrolloLocalCommand"/>.</summary>
public class UpdateProyectoDesarrolloLocalCommandHandler
    : IRequestHandler<UpdateProyectoDesarrolloLocalCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public UpdateProyectoDesarrolloLocalCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        UpdateProyectoDesarrolloLocalCommand request, CancellationToken cancellationToken)
    {
        var proyecto = await _context.Proyectos.OfType<ProyectoDesarrolloLocal>()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (proyecto is null)
            return Result.Failure(["Proyecto no encontrado."]);

        var ownerFilter = ProyectoHelper.GetOwnerFilter(_currentUser);
        if (ownerFilter is not null && proyecto.JefeId != ownerFilter)
            return Result.Failure(["No tiene permiso para modificar este proyecto."]);

        var jefeId = ProyectoHelper.ResolveJefeId(request.JefeId, _currentUser);
        var jefeValidation = await ProyectoHelper.ValidateJefeAsync(_context, jefeId, cancellationToken);
        if (jefeValidation is not null)
            return jefeValidation;

        ProyectoHelper.SetBase(proyecto, request.Titulo, jefeId,
            request.NumeroMiembros, request.CantidadMiembrosUH, request.CantidadEstudiantes,
            request.CantidadEstudiantesContratados, request.TributaFormacionDoctoral,
            request.ClasificacionId);
        ProyectoHelper.SetEjecucion(proyecto, request.FechaInicio, request.FechaCierre,
            request.EstadoDeEjecucion, request.CodigoProyecto, request.EntidadEjecutoraPrincipal,
            request.EntidadEjecutoraParticipante, request.ContribucionSectoresEstrategicos,
            // PDL siempre tributa al desarrollo local — no se expone al usuario.
            request.ContribucionEjesEstrategicos, tributaDesarrolloLocal: true);

        proyecto.Municipio = request.Municipio?.Trim() ?? string.Empty;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
