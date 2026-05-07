using Dashboard_v2.Domain.Enums;

namespace Dashboard_v2.Application.Proyectos;

/// <summary>
/// Contrato común para operaciones de escritura sobre proyectos.
/// </summary>
public interface IProyectoUpsertRequest
{
    string Titulo { get; }
    string JefeId { get; }
    int NumeroMiembros { get; }
    int CantidadMiembrosUH { get; }
    int CantidadEstudiantes { get; }
    int CantidadEstudiantesContratados { get; }
    bool TributaFormacionDoctoral { get; }
    string ClasificacionId { get; }
    string AreaId { get; }
}

/// <summary>
/// Contrato común para proyectos en ejecución.
/// </summary>
public interface IProyectoEnEjecucionUpsertRequest : IProyectoUpsertRequest
{
    DateOnly FechaInicio { get; }
    DateOnly? FechaCierre { get; }
    string EstadoDeEjecucion { get; }
    string CodigoProyecto { get; }
    string EntidadEjecutoraPrincipal { get; }
    string? EntidadEjecutoraParticipante { get; }
    string? ContribucionSectoresEstrategicos { get; }
    string? ContribucionEjesEstrategicos { get; }
    bool TributaDesarrolloLocal { get; }
}

/// <summary>
/// Base reusable para requests de creación o actualización de proyectos.
/// </summary>
public abstract record ProyectoUpsertRequestBase : IProyectoUpsertRequest
{
    public string Titulo { get; init; } = default!;
    public string JefeId { get; init; } = default!;
    public int NumeroMiembros { get; init; }
    public int CantidadMiembrosUH { get; init; }
    public int CantidadEstudiantes { get; init; }
    public int CantidadEstudiantesContratados { get; init; }
    public bool TributaFormacionDoctoral { get; init; }
    public string ClasificacionId { get; init; } = default!;
    public string AreaId { get; init; } = default!;
}

/// <summary>
/// Base reusable para requests de proyectos en ejecución.
/// </summary>
public abstract record ProyectoEnEjecucionUpsertRequestBase
    : ProyectoUpsertRequestBase, IProyectoEnEjecucionUpsertRequest
{
    public DateOnly FechaInicio { get; init; }
    public DateOnly? FechaCierre { get; init; }
    public string EstadoDeEjecucion { get; init; } = default!;
    public string CodigoProyecto { get; init; } = default!;
    public string EntidadEjecutoraPrincipal { get; init; } = default!;
    public string? EntidadEjecutoraParticipante { get; init; }
    public string? ContribucionSectoresEstrategicos { get; init; }
    public string? ContribucionEjesEstrategicos { get; init; }
    public bool TributaDesarrolloLocal { get; init; }
}

/// <summary>
/// Request de alta o modificación para proyectos en revisión.
/// </summary>
public sealed record ProyectoEnRevisionUpsertRequest : ProyectoUpsertRequestBase
{
    public string Situacion { get; init; } = default!;
    public string Tipo { get; init; } = default!;
}

/// <summary>
/// Request de alta o modificación para proyectos empresariales.
/// </summary>
public sealed record ProyectoEmpresarialUpsertRequest : ProyectoEnEjecucionUpsertRequestBase
{
    public string Empresa { get; init; } = default!;
}

/// <summary>
/// Request de alta o modificación para proyectos de apoyo a programa.
/// </summary>
public sealed record ProyectoApoyoProgramaUpsertRequest : ProyectoEnEjecucionUpsertRequestBase
{
    public string NombrePrograma { get; init; } = default!;
    public TipoPAP TipoPAP { get; init; }
}

/// <summary>
/// Request de alta o modificación para proyectos de desarrollo local.
/// </summary>
public sealed record ProyectoDesarrolloLocalUpsertRequest : ProyectoEnEjecucionUpsertRequestBase
{
    public string Municipio { get; init; } = default!;
}

/// <summary>
/// Request de alta o modificación para proyectos no empresariales.
/// </summary>
public sealed record ProyectoNoEmpresarialUpsertRequest : ProyectoEnEjecucionUpsertRequestBase
{
    public string EntidadNoEmpresarial { get; init; } = default!;
}

/// <summary>
/// Request de alta o modificación para proyectos de colaboración internacional.
/// </summary>
public sealed record ProyectoColabInternacionalUpsertRequest : ProyectoEnEjecucionUpsertRequestBase
{
    public string FuenteFinanciacion { get; init; } = default!;
    public string TerminosReferencia { get; init; } = default!;
}

/// <summary>
/// Request de alta o modificación para proyectos PNAP.
/// </summary>
public sealed record ProyectoPNAPUpsertRequest : ProyectoEnEjecucionUpsertRequestBase
{
    public string FinanciamientoUH { get; init; } = default!;
}
