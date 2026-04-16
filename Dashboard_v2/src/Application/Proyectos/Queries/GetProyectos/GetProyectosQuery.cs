using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Entities;

namespace Dashboard_v2.Application.Proyectos.Queries.GetProyectos;

public record GetProyectosQuery : IRequest<List<ProyectoResumenDto>>;

public class GetProyectosQueryHandler : IRequestHandler<GetProyectosQuery, List<ProyectoResumenDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public GetProyectosQueryHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<ProyectoResumenDto>> Handle(GetProyectosQuery request, CancellationToken ct)
    {
        var ownerFilter = ProyectoHelper.GetOwnerFilter(_currentUser);

        // Consultas separadas por tipo para evitar el LEFT JOIN masivo que genera TPT
        // al consultar el DbSet base polimórficamente.
        var enRevision = await _context.Proyectos.OfType<ProyectoEnRevision>()
            .Include(p => p.Clasificacion)
            .Include(p => p.JefeUsuario)
            .Where(p => ownerFilter == null || p.JefeId == ownerFilter)
            .Select(p => new ProyectoResumenDto
            {
                Id = p.Id, Titulo = p.Titulo,
                JefeId = p.JefeId,
                Jefe = p.JefeUsuario.UserName + " " + p.JefeUsuario.UserLastName1 + (p.JefeUsuario.UserLastName2 != null ? " " + p.JefeUsuario.UserLastName2 : ""),
                CorreoJefe = p.JefeUsuario.Email,
                NumeroMiembros = p.NumeroMiembros,
                ClasificacionId = p.ClasificacionId, ClasificacionNombre = p.Clasificacion.Nombre,
                Tipo = "en-revision",
                Situacion = p.Situacion,
            }).ToListAsync(ct);

        var empresariales = await _context.Proyectos.OfType<ProyectoEmpresarial>()
            .Include(p => p.Clasificacion)
            .Include(p => p.JefeUsuario)
            .Where(p => ownerFilter == null || p.JefeId == ownerFilter)
            .Select(p => new ProyectoResumenDto
            {
                Id = p.Id, Titulo = p.Titulo,
                JefeId = p.JefeId,
                Jefe = p.JefeUsuario.UserName + " " + p.JefeUsuario.UserLastName1 + (p.JefeUsuario.UserLastName2 != null ? " " + p.JefeUsuario.UserLastName2 : ""),
                CorreoJefe = p.JefeUsuario.Email,
                NumeroMiembros = p.NumeroMiembros,
                ClasificacionId = p.ClasificacionId, ClasificacionNombre = p.Clasificacion.Nombre,
                Tipo = "empresariales",
                CodigoProyecto = p.CodigoProyecto, EstadoDeEjecucion = p.EstadoDeEjecucion,
            }).ToListAsync(ct);

        var apoyoPrograma = await _context.Proyectos.OfType<ProyectoApoyoPrograma>()
            .Include(p => p.Clasificacion)
            .Include(p => p.JefeUsuario)
            .Where(p => ownerFilter == null || p.JefeId == ownerFilter)
            .Select(p => new ProyectoResumenDto
            {
                Id = p.Id, Titulo = p.Titulo,
                JefeId = p.JefeId,
                Jefe = p.JefeUsuario.UserName + " " + p.JefeUsuario.UserLastName1 + (p.JefeUsuario.UserLastName2 != null ? " " + p.JefeUsuario.UserLastName2 : ""),
                CorreoJefe = p.JefeUsuario.Email,
                NumeroMiembros = p.NumeroMiembros,
                ClasificacionId = p.ClasificacionId, ClasificacionNombre = p.Clasificacion.Nombre,
                Tipo = "apoyo-programa",
                CodigoProyecto = p.CodigoProyecto, EstadoDeEjecucion = p.EstadoDeEjecucion,
            }).ToListAsync(ct);

        var desarrolloLocal = await _context.Proyectos.OfType<ProyectoDesarrolloLocal>()
            .Include(p => p.Clasificacion)
            .Include(p => p.JefeUsuario)
            .Where(p => ownerFilter == null || p.JefeId == ownerFilter)
            .Select(p => new ProyectoResumenDto
            {
                Id = p.Id, Titulo = p.Titulo,
                JefeId = p.JefeId,
                Jefe = p.JefeUsuario.UserName + " " + p.JefeUsuario.UserLastName1 + (p.JefeUsuario.UserLastName2 != null ? " " + p.JefeUsuario.UserLastName2 : ""),
                CorreoJefe = p.JefeUsuario.Email,
                NumeroMiembros = p.NumeroMiembros,
                ClasificacionId = p.ClasificacionId, ClasificacionNombre = p.Clasificacion.Nombre,
                Tipo = "desarrollo-local",
                CodigoProyecto = p.CodigoProyecto, EstadoDeEjecucion = p.EstadoDeEjecucion,
            }).ToListAsync(ct);

        var noEmpresariales = await _context.Proyectos.OfType<ProyectoNoEmpresarial>()
            .Include(p => p.Clasificacion)
            .Include(p => p.JefeUsuario)
            .Where(p => ownerFilter == null || p.JefeId == ownerFilter)
            .Select(p => new ProyectoResumenDto
            {
                Id = p.Id, Titulo = p.Titulo,
                JefeId = p.JefeId,
                Jefe = p.JefeUsuario.UserName + " " + p.JefeUsuario.UserLastName1 + (p.JefeUsuario.UserLastName2 != null ? " " + p.JefeUsuario.UserLastName2 : ""),
                CorreoJefe = p.JefeUsuario.Email,
                NumeroMiembros = p.NumeroMiembros,
                ClasificacionId = p.ClasificacionId, ClasificacionNombre = p.Clasificacion.Nombre,
                Tipo = "no-empresariales",
                CodigoProyecto = p.CodigoProyecto, EstadoDeEjecucion = p.EstadoDeEjecucion,
            }).ToListAsync(ct);

        var colabInternacional = await _context.Proyectos.OfType<ProyectoColabInternacional>()
            .Include(p => p.Clasificacion)
            .Include(p => p.JefeUsuario)
            .Where(p => ownerFilter == null || p.JefeId == ownerFilter)
            .Select(p => new ProyectoResumenDto
            {
                Id = p.Id, Titulo = p.Titulo,
                JefeId = p.JefeId,
                Jefe = p.JefeUsuario.UserName + " " + p.JefeUsuario.UserLastName1 + (p.JefeUsuario.UserLastName2 != null ? " " + p.JefeUsuario.UserLastName2 : ""),
                CorreoJefe = p.JefeUsuario.Email,
                NumeroMiembros = p.NumeroMiembros,
                ClasificacionId = p.ClasificacionId, ClasificacionNombre = p.Clasificacion.Nombre,
                Tipo = "colaboracion-internacional",
                CodigoProyecto = p.CodigoProyecto, EstadoDeEjecucion = p.EstadoDeEjecucion,
            }).ToListAsync(ct);

        var pnap = await _context.Proyectos.OfType<ProyectoPNAP>()
            .Include(p => p.Clasificacion)
            .Include(p => p.JefeUsuario)
            .Where(p => ownerFilter == null || p.JefeId == ownerFilter)
            .Select(p => new ProyectoResumenDto
            {
                Id = p.Id, Titulo = p.Titulo,
                JefeId = p.JefeId,
                Jefe = p.JefeUsuario.UserName + " " + p.JefeUsuario.UserLastName1 + (p.JefeUsuario.UserLastName2 != null ? " " + p.JefeUsuario.UserLastName2 : ""),
                CorreoJefe = p.JefeUsuario.Email,
                NumeroMiembros = p.NumeroMiembros,
                ClasificacionId = p.ClasificacionId, ClasificacionNombre = p.Clasificacion.Nombre,
                Tipo = "pnap",
                CodigoProyecto = p.CodigoProyecto, EstadoDeEjecucion = p.EstadoDeEjecucion,
            }).ToListAsync(ct);

        return enRevision
            .Concat(empresariales)
            .Concat(apoyoPrograma)
            .Concat(desarrolloLocal)
            .Concat(noEmpresariales)
            .Concat(colabInternacional)
            .Concat(pnap)
            .OrderBy(p => p.Titulo)
            .ToList();
    }
}
