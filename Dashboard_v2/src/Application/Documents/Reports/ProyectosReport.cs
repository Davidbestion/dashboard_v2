using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Domain.Enums;

namespace Dashboard_v2.Application.Documents.Reports;

/// <summary>
/// Reporte: Anexo de Proyectos de Investigación (multi-hoja).
/// URL generada: GET /api/Documents/anexo-proyectos
/// Plantilla:    Infrastructure/Templates/AnexoProyectos.xlsx
///
/// Hojas generadas (una por tipo de proyecto en ejecución + una para proyectos en revisión):
///   PAPN, PAPS, PAPT  → ProyectoApoyoPrograma filtrado por TipoPAP
///   PE                → ProyectoEmpresarial
///   PNE               → ProyectoNoEmpresarial
///   PDL               → ProyectoDesarrolloLocal
///   PRCI              → ProyectoColabInternacional
///   PNAP              → ProyectoPNAP
///   NuevasAplicaciones → ProyectoEnRevision
///
/// Cada clave del diccionario devuelto debe coincidir exactamente con el Named Range
/// de la hoja correspondiente en AnexoProyectos.xlsx.
/// </summary>
public sealed class ProyectosReport : IDocumentReport
{
    private readonly IApplicationDbContext _context;

    public ProyectosReport(IApplicationDbContext context) => _context = context;

    public string ReportName   => "anexo-proyectos";
    public string TemplateName => "AnexoProyectos";

    public async Task<IReadOnlyDictionary<string, object>> GatherVariablesAsync(CancellationToken ct)
    {
        // Cargamos todos los proyectos en ejecución con sus relaciones necesarias.
        // OfType<T>() genera un JOIN interno con la tabla de especialización correspondiente.
        var pe = await _context.Proyectos
            .OfType<ProyectoEmpresarial>()
            .OrderBy(p => p.CodigoProyecto)
            .Select(p => new ProyectoPERowDto
            {
                CodigoProyecto                  = p.CodigoProyecto,
                TituloProyecto                  = p.Titulo,
                Empresa                         = p.Empresa,
                JefeProyecto                    = p.JefeUsuario.UserName + " " + p.JefeUsuario.UserLastName1,
                CorreoJefeProyecto              = p.JefeUsuario.Email,
                TotalMiembros                   = p.NumeroMiembros,
                MiembrosUH                      = p.CantidadMiembrosUH,
                Estudiantes                     = p.CantidadEstudiantes,
                EstudiantesContratados          = p.CantidadEstudiantesContratados,
                Clasificacion                   = p.Clasificacion.Nombre,
                TributaFormacionDoctoral        = p.TributaFormacionDoctoral ? "Sí" : "No",
                TributaDesarrolloLocal          = p.TributaDesarrolloLocal ? "Sí" : "No",
                ContribucionSectoresEstrategicos = p.ContribucionSectoresEstrategicos ?? "",
                ContribucionEjesEstrategicos    = p.ContribucionEjesEstrategicos ?? "",
                FechaInicio                     = p.FechaInicio.ToString("dd/MM/yyyy"),
                FechaCierre                     = p.FechaCierre.HasValue ? p.FechaCierre.Value.ToString("dd/MM/yyyy") : "",
                EntidadEjecutoraPrincipal       = p.EntidadEjecutoraPrincipal,
                EntidadEjecutoraParticipante    = p.EntidadEjecutoraParticipante ?? "",
                EstadoEjecucion                 = p.EstadoDeEjecucion,
                PublicacionesDerivadas          = string.Join("; ", p.PublicacionesDerivadas
                    .Select(pub => pub.UrlDoi ?? pub.Title)),
            })
            .ToListAsync(ct);

        var papn = await QueryPAP(TipoPAP.Nacional, ct);
        var paps = await QueryPAP(TipoPAP.Sectorial, ct);
        var papt = await QueryPAP(TipoPAP.Territorial, ct);

        var pne = await _context.Proyectos
            .OfType<ProyectoNoEmpresarial>()
            .OrderBy(p => p.CodigoProyecto)
            .Select(p => new ProyectoPNERowDto
            {
                CodigoProyecto                  = p.CodigoProyecto,
                TituloProyecto                  = p.Titulo,
                EntidadNoEmpresarial            = p.EntidadNoEmpresarial,
                JefeProyecto                    = p.JefeUsuario.UserName + " " + p.JefeUsuario.UserLastName1,
                CorreoJefeProyecto              = p.JefeUsuario.Email,
                TotalMiembros                   = p.NumeroMiembros,
                MiembrosUH                      = p.CantidadMiembrosUH,
                Estudiantes                     = p.CantidadEstudiantes,
                EstudiantesContratados          = p.CantidadEstudiantesContratados,
                Clasificacion                   = p.Clasificacion.Nombre,
                TributaFormacionDoctoral        = p.TributaFormacionDoctoral ? "Sí" : "No",
                TributaDesarrolloLocal          = p.TributaDesarrolloLocal ? "Sí" : "No",
                ContribucionSectoresEstrategicos = p.ContribucionSectoresEstrategicos ?? "",
                ContribucionEjesEstrategicos    = p.ContribucionEjesEstrategicos ?? "",
                FechaInicio                     = p.FechaInicio.ToString("dd/MM/yyyy"),
                FechaCierre                     = p.FechaCierre.HasValue ? p.FechaCierre.Value.ToString("dd/MM/yyyy") : "",
                EntidadEjecutoraPrincipal       = p.EntidadEjecutoraPrincipal,
                EntidadEjecutoraParticipante    = p.EntidadEjecutoraParticipante ?? "",
                EstadoEjecucion                 = p.EstadoDeEjecucion,
                PublicacionesDerivadas          = string.Join("; ", p.PublicacionesDerivadas
                    .Select(pub => pub.UrlDoi ?? pub.Title)),
            })
            .ToListAsync(ct);

        var pdl = await _context.Proyectos
            .OfType<ProyectoDesarrolloLocal>()
            .OrderBy(p => p.CodigoProyecto)
            .Select(p => new ProyectoPDLRowDto
            {
                CodigoProyecto                  = p.CodigoProyecto,
                TituloProyecto                  = p.Titulo,
                Municipio                       = p.Municipio,
                JefeProyecto                    = p.JefeUsuario.UserName + " " + p.JefeUsuario.UserLastName1,
                CorreoJefeProyecto              = p.JefeUsuario.Email,
                TotalMiembros                   = p.NumeroMiembros,
                MiembrosUH                      = p.CantidadMiembrosUH,
                Estudiantes                     = p.CantidadEstudiantes,
                EstudiantesContratados          = p.CantidadEstudiantesContratados,
                Clasificacion                   = p.Clasificacion.Nombre,
                TributaFormacionDoctoral        = p.TributaFormacionDoctoral ? "Sí" : "No",
                TributaDesarrolloLocal          = "Sí", // por definición siempre true para PDL
                ContribucionSectoresEstrategicos = p.ContribucionSectoresEstrategicos ?? "",
                ContribucionEjesEstrategicos    = p.ContribucionEjesEstrategicos ?? "",
                FechaInicio                     = p.FechaInicio.ToString("dd/MM/yyyy"),
                FechaCierre                     = p.FechaCierre.HasValue ? p.FechaCierre.Value.ToString("dd/MM/yyyy") : "",
                EntidadEjecutoraPrincipal       = p.EntidadEjecutoraPrincipal,
                EntidadEjecutoraParticipante    = p.EntidadEjecutoraParticipante ?? "",
                EstadoEjecucion                 = p.EstadoDeEjecucion,
                PublicacionesDerivadas          = string.Join("; ", p.PublicacionesDerivadas
                    .Select(pub => pub.UrlDoi ?? pub.Title)),
            })
            .ToListAsync(ct);

        var prci = await _context.Proyectos
            .OfType<ProyectoColabInternacional>()
            .OrderBy(p => p.CodigoProyecto)
            .Select(p => new ProyectoPRCIRowDto
            {
                CodigoProyecto                  = p.CodigoProyecto,
                TituloProyecto                  = p.Titulo,
                FuenteFinanciacion              = p.FuenteFinanciacion,
                JefeProyecto                    = p.JefeUsuario.UserName + " " + p.JefeUsuario.UserLastName1,
                CorreoJefeProyecto              = p.JefeUsuario.Email,
                TotalMiembros                   = p.NumeroMiembros,
                MiembrosUH                      = p.CantidadMiembrosUH,
                Estudiantes                     = p.CantidadEstudiantes,
                EstudiantesContratados          = p.CantidadEstudiantesContratados,
                Clasificacion                   = p.Clasificacion.Nombre,
                TributaFormacionDoctoral        = p.TributaFormacionDoctoral ? "Sí" : "No",
                TributaDesarrolloLocal          = p.TributaDesarrolloLocal ? "Sí" : "No",
                ConTerminosReferencia           = p.TerminosReferencia,
                ContribucionSectoresEstrategicos = p.ContribucionSectoresEstrategicos ?? "",
                ContribucionEjesEstrategicos    = p.ContribucionEjesEstrategicos ?? "",
                FechaInicio                     = p.FechaInicio.ToString("dd/MM/yyyy"),
                FechaCierre                     = p.FechaCierre.HasValue ? p.FechaCierre.Value.ToString("dd/MM/yyyy") : "",
                EntidadEjecutoraPrincipal       = p.EntidadEjecutoraPrincipal,
                EntidadEjecutoraParticipante    = p.EntidadEjecutoraParticipante ?? "",
                EstadoEjecucion                 = p.EstadoDeEjecucion,
                PublicacionesDerivadas          = string.Join("; ", p.PublicacionesDerivadas
                    .Select(pub => pub.UrlDoi ?? pub.Title)),
            })
            .ToListAsync(ct);

        var pnap = await _context.Proyectos
            .OfType<ProyectoPNAP>()
            .OrderBy(p => p.CodigoProyecto)
            .Select(static p => new ProyectoPNAPRowDto
            {
                CodigoProyecto                  = p.CodigoProyecto,
                TituloProyecto                  = p.Titulo,
                JefeProyecto                    = p.JefeUsuario.UserName + " " + p.JefeUsuario.UserLastName1,
                CorreoJefeProyecto              = p.JefeUsuario.Email,
                TotalMiembros                   = p.NumeroMiembros,
                MiembrosUH                      = p.CantidadMiembrosUH,
                Estudiantes                     = p.CantidadEstudiantes,
                EstudiantesContratados          = p.CantidadEstudiantesContratados,
                Clasificacion                   = p.Clasificacion.Nombre,
                TributaFormacionDoctoral        = p.TributaFormacionDoctoral ? "Sí" : "No",
                TributaDesarrolloLocal          = p.TributaDesarrolloLocal ? "Sí" : "No",
                FinanciamientoUH                = p.FinanciamientoUH,
                ContribucionSectoresEstrategicos = p.ContribucionSectoresEstrategicos ?? "",
                ContribucionEjesEstrategicos    = p.ContribucionEjesEstrategicos ?? "",
                FechaInicio                     = p.FechaInicio.ToString("dd/MM/yyyy"),
                FechaCierre                     = p.FechaCierre.HasValue ? p.FechaCierre.Value.ToString("dd/MM/yyyy") : "",
                EntidadEjecutoraPrincipal       = p.EntidadEjecutoraPrincipal,
                EntidadEjecutoraParticipante    = p.EntidadEjecutoraParticipante ?? "",
                EstadoEjecucion                 = p.EstadoDeEjecucion,
                PublicacionesDerivadas          = string.Join("; ", p.PublicacionesDerivadas
                    .Select(pub => pub.UrlDoi ?? pub.Title)),
            })
            .ToListAsync(ct);

        var nuevasAplicaciones = await _context.Proyectos
            .OfType<ProyectoEnRevision>()
            .OrderBy(p => p.Titulo)
            .Select(p => new ProyectoNuevasAplicacionesRowDto
            {
                TituloProyecto         = p.Titulo,
                JefeProyecto           = p.JefeUsuario.UserName + " " + p.JefeUsuario.UserLastName1,
                CorreoJefeProyecto     = p.JefeUsuario.Email,
                TipoProyecto           = p.Tipo,
                TotalMiembros          = p.NumeroMiembros,
                MiembrosUH             = p.CantidadMiembrosUH,
                Estudiantes            = p.CantidadEstudiantes,
                EstudiantesContratados = p.CantidadEstudiantesContratados,
                Clasificacion          = p.Clasificacion.Nombre,
                TributaFormacionDoctoral = p.TributaFormacionDoctoral ? "Sí" : "No",
                Situacion              = p.Situacion,
            })
            .ToListAsync(ct);

        // Cada clave debe coincidir exactamente con el Named Range definido en AnexoProyectos.xlsx
        return new Dictionary<string, object>
        {
            ["PE"]                  = pe,
            ["PAPN"]                = papn,
            ["PAPS"]                = paps,
            ["PAPT"]                = papt,
            ["PNE"]                 = pne,
            ["PDL"]                 = pdl,
            ["PRCI"]                = prci,
            ["PNAP"]                = pnap,
            ["NuevasAplicaciones"]  = nuevasAplicaciones,
        };
    }

    // ─── Helper para PAP filtrado por subtipo ──────────────────────────────
    private async Task<List<ProyectoPAPRowDto>> QueryPAP(TipoPAP tipo, CancellationToken ct)
        => await _context.Proyectos
            .OfType<ProyectoApoyoPrograma>()
            .Where(p => p.TipoPAP == tipo)
            .OrderBy(p => p.CodigoProyecto)
            .Select(p => new ProyectoPAPRowDto
            {
                CodigoProyecto                  = p.CodigoProyecto,
                TituloProyecto                  = p.Titulo,
                NombrePrograma                  = p.NombrePrograma,
                JefeProyecto                    = p.JefeUsuario.UserName + " " + p.JefeUsuario.UserLastName1,
                CorreoJefeProyecto              = p.JefeUsuario.Email,
                TotalMiembros                   = p.NumeroMiembros,
                MiembrosUH                      = p.CantidadMiembrosUH,
                Estudiantes                     = p.CantidadEstudiantes,
                EstudiantesContratados          = p.CantidadEstudiantesContratados,
                Clasificacion                   = p.Clasificacion.Nombre,
                TributaFormacionDoctoral        = p.TributaFormacionDoctoral ? "Sí" : "No",
                TributaDesarrolloLocal          = p.TributaDesarrolloLocal ? "Sí" : "No",
                ContribucionSectoresEstrategicos = p.ContribucionSectoresEstrategicos ?? "",
                ContribucionEjesEstrategicos    = p.ContribucionEjesEstrategicos ?? "",
                FechaInicio                     = p.FechaInicio.ToString("dd/MM/yyyy"),
                FechaCierre                     = p.FechaCierre.HasValue ? p.FechaCierre.Value.ToString("dd/MM/yyyy") : "",
                EntidadEjecutoraPrincipal       = p.EntidadEjecutoraPrincipal,
                EntidadEjecutoraParticipante    = p.EntidadEjecutoraParticipante ?? "",
                EstadoEjecucion                 = p.EstadoDeEjecucion,
                PublicacionesDerivadas          = string.Join("; ", p.PublicacionesDerivadas
                    .Select(pub => pub.UrlDoi ?? pub.Title)),
            })
            .ToListAsync(ct);
}

// ─── DTOs ──────────────────────────────────────────────────────────────────
// Propiedades públicas requeridas por ClosedXML.Report para acceso por reflexión.
// El nombre de cada propiedad coincide con item.Xxx en las expresiones de la plantilla.

/// <summary>Campos comunes a todos los tipos de proyecto en ejecución.</summary>
public abstract record ProyectoEnEjecucionRowDto
{
    public string CodigoProyecto                   { get; init; } = default!;
    public string TituloProyecto                   { get; init; } = default!;
    public string JefeProyecto                     { get; init; } = default!;
    public string CorreoJefeProyecto               { get; init; } = default!;
    public int    TotalMiembros                    { get; init; }
    public int    MiembrosUH                       { get; init; }
    public int    Estudiantes                      { get; init; }
    public int    EstudiantesContratados           { get; init; }
    public string Clasificacion                    { get; init; } = default!;
    public string TributaFormacionDoctoral         { get; init; } = default!;
    public string TributaDesarrolloLocal           { get; init; } = default!;
    public string ContribucionSectoresEstrategicos { get; init; } = "";
    public string ContribucionEjesEstrategicos     { get; init; } = "";
    public string FechaInicio                      { get; init; } = "";
    public string FechaCierre                      { get; init; } = "";
    // Compatibilidad con plantillas que esperan propiedades formateadas
    public string FechaInicioString => FechaInicio;
    public string FechaCierreString => FechaCierre;
    public string EntidadEjecutoraPrincipal        { get; init; } = default!;
    public string EntidadEjecutoraParticipante     { get; init; } = "";
    public string EstadoEjecucion                  { get; init; } = default!;
    public string PublicacionesDerivadas           { get; init; } = "";
}

public record ProyectoPERowDto : ProyectoEnEjecucionRowDto
{
    public string Empresa { get; init; } = default!;
}

public record ProyectoPAPRowDto : ProyectoEnEjecucionRowDto
{
    public string NombrePrograma { get; init; } = default!;
}

public record ProyectoPNERowDto : ProyectoEnEjecucionRowDto
{
    public string EntidadNoEmpresarial { get; init; } = default!;
}

public record ProyectoPDLRowDto : ProyectoEnEjecucionRowDto
{
    public string Municipio { get; init; } = default!;
}

public record ProyectoPRCIRowDto : ProyectoEnEjecucionRowDto
{
    public string FuenteFinanciacion  { get; init; } = default!;
    public string ConTerminosReferencia { get; init; } = default!;
}

public record ProyectoPNAPRowDto : ProyectoEnEjecucionRowDto
{
    public string FinanciamientoUH { get; init; } = default!;
}

public record ProyectoNuevasAplicacionesRowDto //Proyectos en revision
{
    public string TituloProyecto           { get; init; } = default!;
    public string JefeProyecto             { get; init; } = default!;
    public string CorreoJefeProyecto       { get; init; } = default!;
    public string TipoProyecto             { get; init; } = default!;
    public int    TotalMiembros            { get; init; }
    public int    MiembrosUH               { get; init; }
    public int    Estudiantes              { get; init; }
    public int    EstudiantesContratados   { get; init; }
    public string Clasificacion            { get; init; } = default!;
    public string TributaFormacionDoctoral { get; init; } = default!;
    public string Situacion                { get; init; } = default!;
}
