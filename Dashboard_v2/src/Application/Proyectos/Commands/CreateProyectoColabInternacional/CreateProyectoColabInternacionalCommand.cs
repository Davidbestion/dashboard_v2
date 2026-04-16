using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Domain.Entities;

namespace Dashboard_v2.Application.Proyectos.Commands.CreateProyectoColabInternacional;

public record CreateProyectoColabInternacionalCommand : IRequest<(Result Result, string? Id)>
{
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
    public bool TributaDesarrolloLocal { get; init; }
    public string FuenteFinanciacion { get; init; } = default!;
    public string TerminosReferencia { get; init; } = default!;
}

public class CreateProyectoColabInternacionalCommandHandler
    : IRequestHandler<CreateProyectoColabInternacionalCommand, (Result Result, string? Id)>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public CreateProyectoColabInternacionalCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<(Result Result, string? Id)> Handle(
        CreateProyectoColabInternacionalCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Titulo))
            return (Result.Failure(["El título es obligatorio."]), null);

        if (!await _context.Clasificaciones.AnyAsync(c => c.Id == request.ClasificacionId, cancellationToken))
            return (Result.Failure(["La clasificación indicada no existe."]), null);

        var jefeId = ProyectoHelper.ResolveJefeId(request.JefeId, _currentUser);
        var jefeValidation = await ProyectoHelper.ValidateJefeAsync(_context, jefeId, cancellationToken);
        if (jefeValidation is not null)
            return (jefeValidation, null);

        var proyecto = new ProyectoColabInternacional();
        ProyectoHelper.SetBase(proyecto, request.Titulo, jefeId,
            request.NumeroMiembros, request.CantidadMiembrosUH, request.CantidadEstudiantes,
            request.CantidadEstudiantesContratados, request.TributaFormacionDoctoral,
            request.ClasificacionId);
        ProyectoHelper.SetEjecucion(proyecto, request.FechaInicio, request.FechaCierre,
            request.EstadoDeEjecucion, request.CodigoProyecto, request.EntidadEjecutoraPrincipal,
            request.EntidadEjecutoraParticipante, request.ContribucionSectoresEstrategicos,
            request.ContribucionEjesEstrategicos, request.TributaDesarrolloLocal);

        proyecto.FuenteFinanciacion = request.FuenteFinanciacion?.Trim() ?? string.Empty;
        proyecto.TerminosReferencia = request.TerminosReferencia?.Trim() ?? string.Empty;

        _context.Proyectos.Add(proyecto);
        await _context.SaveChangesAsync(cancellationToken);

        return (Result.Success(), proyecto.Id);
    }
}
