using Dashboard_v2.Domain.Enums;

namespace Dashboard_v2.Application.Proyectos;

/// <summary>Contrato común para operaciones de escritura sobre proyectos.</summary>
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
    IList<string> ParticipantesIds { get; }
}

/// <summary>Contrato común para proyectos en ejecución.</summary>
public interface IProyectoEnEjecucionUpsertRequest : IProyectoUpsertRequest
{
    DateOnly FechaInicio { get; }
    DateOnly? FechaCierre { get; }
    string CodigoProyecto { get; }
    bool TributaDesarrolloLocal { get; }
    IList<int> EstadosDeEjecucionIds { get; }
    IList<string> EntidadesEjecutorasPrincipalesIds { get; }
    IList<string> EntidadesEjecutorasParticipantesIds { get; }
    IList<int> SectoresEstrategicosIds { get; }
    IList<int> EjesEstrategicosIds { get; }
}

/// <summary>Base reusable para requests de creación o actualización de proyectos.</summary>
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
    public IList<string> ParticipantesIds { get; init; } = [];
}

/// <summary>Base reusable para requests de proyectos en ejecución.</summary>
public abstract record ProyectoEnEjecucionUpsertRequestBase
    : ProyectoUpsertRequestBase, IProyectoEnEjecucionUpsertRequest
{
    public DateOnly FechaInicio { get; init; }
    public DateOnly? FechaCierre { get; init; }
    public string CodigoProyecto { get; init; } = default!;
    public bool TributaDesarrolloLocal { get; init; }
    public IList<int> EstadosDeEjecucionIds { get; init; } = [];
    public IList<string> EntidadesEjecutorasPrincipalesIds { get; init; } = [];
    public IList<string> EntidadesEjecutorasParticipantesIds { get; init; } = [];
    public IList<int> SectoresEstrategicosIds { get; init; } = [];
    public IList<int> EjesEstrategicosIds { get; init; } = [];
}

/// <summary>Request de alta o modificación para proyectos en revisión.</summary>
public sealed record ProyectoEnRevisionUpsertRequest : ProyectoUpsertRequestBase
{
    public IList<int> SituacionesIds { get; init; } = [];
    public string Tipo { get; init; } = default!;
}

/// <summary>Request de alta o modificación para proyectos empresariales.</summary>
public sealed record ProyectoEmpresarialUpsertRequest : ProyectoEnEjecucionUpsertRequestBase
{
    public IList<string> EmpresasIds { get; init; } = [];
}

/// <summary>Request de alta o modificación para proyectos de apoyo a programa.</summary>
public sealed record ProyectoApoyoProgramaUpsertRequest : ProyectoEnEjecucionUpsertRequestBase
{
    public IList<int> ProgramasIds { get; init; } = [];
    public TipoPAP TipoPAP { get; init; }
}

/// <summary>Request de alta o modificación para proyectos de desarrollo local.</summary>
public sealed record ProyectoDesarrolloLocalUpsertRequest : ProyectoEnEjecucionUpsertRequestBase
{
    public int MunicipioId { get; init; }
}

/// <summary>Request de alta o modificación para proyectos no empresariales.</summary>
public sealed record ProyectoNoEmpresarialUpsertRequest : ProyectoEnEjecucionUpsertRequestBase
{
    public IList<string> EntidadesIds { get; init; } = [];
}

/// <summary>Request de alta o modificación para proyectos de colaboración internacional.</summary>
public sealed record ProyectoColabInternacionalUpsertRequest : ProyectoEnEjecucionUpsertRequestBase
{
    public IList<int> FuentesFinanciacionIds { get; init; } = [];
    public string TerminosReferencia { get; init; } = default!;
}

/// <summary>Request de alta o modificación para proyectos PNAP.</summary>
public sealed record ProyectoPNAPUpsertRequest : ProyectoEnEjecucionUpsertRequestBase
{
    public IList<int> FuentesFinanciacionIds { get; init; } = [];
}

/// <summary>Request para asignar el conjunto de participantes de un proyecto.</summary>
public sealed record SetParticipantesRequest(IList<string> ParticipantesIds);
