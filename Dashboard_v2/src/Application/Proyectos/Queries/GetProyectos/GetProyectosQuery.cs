using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Entities;

namespace Dashboard_v2.Application.Proyectos.Queries.GetProyectos;

/// <summary>Devuelve el listado resumen de todos los proyectos visibles para el usuario actual (filtrado por rol).</summary>
public record GetProyectosQuery : IRequest<List<ProyectoResumenDto>>;

/// <summary>Manejador de <see cref="GetProyectosQuery"/>.</summary>
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
        // Nota: .Include() es innecesario antes de .Select() en EF Core — los JOINs
        // se generan automáticamente a partir de las navegaciones usadas en el .Select().
        var enRevision = await _context.Proyectos.OfType<ProyectoEnRevision>()
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
                PublicacionesDerivadas = p.PublicacionesDerivadas.Select(pub => pub.UrlDoi ?? pub.Title).ToList(),
            }).ToListAsync(ct);

        var empresariales      = await QueryEjecucion(_context.Proyectos.OfType<ProyectoEmpresarial>(),       ownerFilter, "empresariales",             ct);
        var apoyoPrograma      = await QueryEjecucion(_context.Proyectos.OfType<ProyectoApoyoPrograma>(),     ownerFilter, "apoyo-programa",            ct);
        var desarrolloLocal    = await QueryEjecucion(_context.Proyectos.OfType<ProyectoDesarrolloLocal>(),   ownerFilter, "desarrollo-local",          ct);
        var noEmpresariales    = await QueryEjecucion(_context.Proyectos.OfType<ProyectoNoEmpresarial>(),     ownerFilter, "no-empresariales",          ct);
        var colabInternacional = await QueryEjecucion(_context.Proyectos.OfType<ProyectoColabInternacional>(), ownerFilter, "colaboracion-internacional", ct);
        var pnap               = await QueryEjecucion(_context.Proyectos.OfType<ProyectoPNAP>(),              ownerFilter, "pnap",                       ct);

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

    /// <summary>
    /// Proyecta un <see cref="IQueryable{T}"/> de subtipos <see cref="ProyectoEnEjecucion"/> al
    /// <see cref="ProyectoResumenDto"/> común. El constraint <c>where T : ProyectoEnEjecucion</c>
    /// garantiza que <c>CodigoProyecto</c> y <c>EstadoDeEjecucion</c> existen en <c>T</c>,
    /// y que EF Core genera una sola JOIN a la tabla del subtipo (sin LEFT JOINs a todas las tablas TPT).
    /// </summary>
    private static Task<List<ProyectoResumenDto>> QueryEjecucion<T>(
        IQueryable<T> source, string? ownerFilter, string tipo, CancellationToken ct)
        where T : ProyectoEnEjecucion
        => source
            .Where(p => ownerFilter == null || p.JefeId == ownerFilter)
            .Select(p => new ProyectoResumenDto
            {
                Id = p.Id, Titulo = p.Titulo,
                JefeId = p.JefeId,
                Jefe = p.JefeUsuario.UserName + " " + p.JefeUsuario.UserLastName1 + (p.JefeUsuario.UserLastName2 != null ? " " + p.JefeUsuario.UserLastName2 : ""),
                CorreoJefe = p.JefeUsuario.Email,
                NumeroMiembros = p.NumeroMiembros,
                ClasificacionId = p.ClasificacionId, ClasificacionNombre = p.Clasificacion.Nombre,
                Tipo = tipo,
                CodigoProyecto = p.CodigoProyecto, EstadoDeEjecucion = p.EstadoDeEjecucion,
                PublicacionesDerivadas = p.PublicacionesDerivadas.Select(pub => pub.UrlDoi ?? pub.Title).ToList(),
            }).ToListAsync(ct);
}
