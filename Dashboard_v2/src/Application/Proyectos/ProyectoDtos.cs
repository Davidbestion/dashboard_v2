using Dashboard_v2.Domain.Enums;

namespace Dashboard_v2.Application.Proyectos;

/// <summary>Resumen para el listado general (todos los tipos mezclados, solo campos de display).</summary>
public record ProyectoResumenDto
{
    public string Id { get; init; } = default!;
    public string Titulo { get; init; } = default!;
    /// <summary>ID del usuario jefe (FK).</summary>
    public string JefeId { get; init; } = default!;
    /// <summary>Nombre completo del jefe, derivado del usuario.</summary>
    public string Jefe { get; init; } = default!;
    public string CorreoJefe { get; init; } = default!;
    public int NumeroMiembros { get; init; }
    public string ClasificacionId { get; init; } = default!;
    public string ClasificacionNombre { get; init; } = default!;
    public string AreaId { get; init; } = default!;
    public string AreaNombre { get; init; } = default!;
    /// <summary>Slug de URL del tipo: "en-revision", "empresariales", etc. Usado por el frontend para construir rutas.</summary>
    public string Tipo { get; init; } = default!;
    // Campos extra útiles para el resumen
    public string? CodigoProyecto { get; init; }
    public string? EstadoDeEjecucion { get; init; }
    public string? Situacion { get; init; }
    /// <summary>DOI/URLs de las publicaciones derivadas del proyecto.</summary>
    public List<string> PublicacionesDerivadas { get; init; } = [];
}

/// <summary>Campos comunes a todos los proyectos.</summary>
public abstract record ProyectoBaseDto
{
    public string Id { get; init; } = default!;
    public string Titulo { get; init; } = default!;
    /// <summary>ID del usuario jefe (FK a Users).</summary>
    public string JefeId { get; init; } = default!;
    /// <summary>Nombre completo del jefe, construido a partir de UserName + apellidos.</summary>
    public string Jefe { get; init; } = default!;
    public string CorreoJefe { get; init; } = default!;
    public int NumeroMiembros { get; init; }
    public int CantidadMiembrosUH { get; init; }
    public int CantidadEstudiantes { get; init; }
    public int CantidadEstudiantesContratados { get; init; }
    public bool TributaFormacionDoctoral { get; init; }
    public string ClasificacionId { get; init; } = default!;
    public string ClasificacionNombre { get; init; } = default!;
    public string AreaId { get; init; } = default!;
    public string AreaNombre { get; init; } = default!;
    /// <summary>DOI/URLs de las publicaciones derivadas del proyecto.</summary>
    public List<string> PublicacionesDerivadas { get; init; } = [];
}
public abstract record ProyectoEnEjecucionBaseDto : ProyectoBaseDto
{
    public DateOnly FechaInicio { get; init; }
    public DateOnly? FechaCierre { get; init; }
    public string EstadoDeEjecucion { get; init; } = default!;
    public string CodigoProyecto { get; init; } = default!;
    public string EntidadEjecutoraPrincipal { get; init; } = default!;
    public string? EntidadEjecutoraParticipante { get; init; }
    public string? ContribucionSectoresEstrategicos { get; init; }
    public string? ContribucionEjesEstrategicos { get; init; }
    /// <summary>Para ProyectoDesarrolloLocal siempre es <c>true</c>.</summary>
    public bool TributaDesarrolloLocal { get; init; }
}

public record ProyectoEnRevisionDto : ProyectoBaseDto
{
    public string Situacion { get; init; } = default!;
    public string Tipo { get; init; } = default!;
}

public record ProyectoEmpresarialDto : ProyectoEnEjecucionBaseDto
{
    public string Empresa { get; init; } = default!;
}

public record ProyectoApoyoProgramaDto : ProyectoEnEjecucionBaseDto
{
    public string NombrePrograma { get; init; } = default!;
    public TipoPAP TipoPAP { get; init; }
}

public record ProyectoDesarrolloLocalDto : ProyectoEnEjecucionBaseDto
{
    public string Municipio { get; init; } = default!;
}

public record ProyectoNoEmpresarialDto : ProyectoEnEjecucionBaseDto
{
    public string EntidadNoEmpresarial { get; init; } = default!;
}

public record ProyectoColabInternacionalDto : ProyectoEnEjecucionBaseDto
{
    public string FuenteFinanciacion { get; init; } = default!;
    public string TerminosReferencia { get; init; } = default!;
}

public record ProyectoPNAPDto : ProyectoEnEjecucionBaseDto
{
    public string FinanciamientoUH { get; init; } = default!;
}

/// <summary>
/// Resumen mínimo de publicación usado por los endpoints de vinculación entre proyectos y publicaciones.
/// </summary>
public record ProyectoPublicacionDto
{
    public string Id { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string? UrlDoi { get; init; }
}

/// <summary>
/// Par mínimo Id/Título para el selector de proyectos en el formulario de publicaciones.
/// </summary>
public record ProyectoCatalogoDto(string Id, string Titulo);
