using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Entities;

namespace Dashboard_v2.Application.Proyectos.Queries.GetProyectoNoEmpresarial;

public record GetProyectoNoEmpresarialQuery(string Id) : IRequest<ProyectoNoEmpresarialDto?>;

public class GetProyectoNoEmpresarialQueryHandler : IRequestHandler<GetProyectoNoEmpresarialQuery, ProyectoNoEmpresarialDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    public GetProyectoNoEmpresarialQueryHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ProyectoNoEmpresarialDto?> Handle(GetProyectoNoEmpresarialQuery request, CancellationToken ct)
    {
        var ownerFilter = ProyectoHelper.GetOwnerFilter(_currentUser);
        var p = await _context.Proyectos.OfType<ProyectoNoEmpresarial>()
            .Include(x => x.Clasificacion)
            .Include(x => x.JefeUsuario)
            .FirstOrDefaultAsync(x => x.Id == request.Id && (ownerFilter == null || x.JefeId == ownerFilter), ct);

        if (p is null) return null;

        return new ProyectoNoEmpresarialDto
        {
            Id = p.Id, Titulo = p.Titulo,
            JefeId = p.JefeId,
            Jefe = p.JefeUsuario.UserName + " " + p.JefeUsuario.UserLastName1 + (p.JefeUsuario.UserLastName2 != null ? " " + p.JefeUsuario.UserLastName2 : ""),
            CorreoJefe = p.JefeUsuario.Email,
            NumeroMiembros = p.NumeroMiembros, CantidadMiembrosUH = p.CantidadMiembrosUH,
            CantidadEstudiantes = p.CantidadEstudiantes,
            CantidadEstudiantesContratados = p.CantidadEstudiantesContratados,
            TributaFormacionDoctoral = p.TributaFormacionDoctoral,
            ClasificacionId = p.ClasificacionId, ClasificacionNombre = p.Clasificacion.Nombre,
            FechaInicio = p.FechaInicio, FechaCierre = p.FechaCierre,
            EstadoDeEjecucion = p.EstadoDeEjecucion, CodigoProyecto = p.CodigoProyecto,
            EntidadEjecutoraPrincipal = p.EntidadEjecutoraPrincipal,
            EntidadEjecutoraParticipante = p.EntidadEjecutoraParticipante,
            ContribucionSectoresEstrategicos = p.ContribucionSectoresEstrategicos,
            ContribucionEjesEstrategicos = p.ContribucionEjesEstrategicos,
            TributaDesarrolloLocal = p.TributaDesarrolloLocal,
            EntidadNoEmpresarial = p.EntidadNoEmpresarial,
        };
    }
}
