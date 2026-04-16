using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Entities;

namespace Dashboard_v2.Application.Proyectos.Queries.GetProyectoEnRevision;

public record GetProyectoEnRevisionQuery(string Id) : IRequest<ProyectoEnRevisionDto?>;

public class GetProyectoEnRevisionQueryHandler : IRequestHandler<GetProyectoEnRevisionQuery, ProyectoEnRevisionDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    public GetProyectoEnRevisionQueryHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ProyectoEnRevisionDto?> Handle(GetProyectoEnRevisionQuery request, CancellationToken ct)
    {
        var ownerFilter = ProyectoHelper.GetOwnerFilter(_currentUser);
        var p = await _context.Proyectos.OfType<ProyectoEnRevision>()
            .Include(x => x.Clasificacion)
            .Include(x => x.JefeUsuario)
            .FirstOrDefaultAsync(x => x.Id == request.Id && (ownerFilter == null || x.JefeId == ownerFilter), ct);

        if (p is null) return null;

        return new ProyectoEnRevisionDto
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
            Situacion = p.Situacion, Tipo = p.Tipo,
        };
    }
}
