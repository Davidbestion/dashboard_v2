using Dashboard_v2.Domain.Enums;

namespace Dashboard_v2.Application.Proyectos;

/// <summary>DTO mínimo para un nomenclador con Id entero.</summary>
public record NomencladorDto(int Id, string Nombre);

/// <summary>Referencia mínima a un usuario participante en un proyecto.</summary>
public record UserRefDto(string Id, string NombreCompleto, string Email);

/// <summary>DTO mínimo para una referencia a institución (Id = Guid como string).</summary>
public record InstitutionRefDto(string Id, string Nombre);

/// <summary>Resumen para el listado general (todos los tipos mezclados, solo campos de display).</summary>
public record ProyectoResumenDto
{
    public string Id { get; init; } = default!;
    public string Titulo { get; init; } = default!;
    public string JefeId { get; init; } = default!;
    public string Jefe { get; init; } = default!;
    public string CorreoJefe { get; init; } = default!;
    public int NumeroMiembros { get; init; }
    public string ClasificacionId { get; init; } = default!;
    public string ClasificacionNombre { get; init; } = default!;
    public List<UserRefDto> Participantes { get; init; } = [];
    /// <summary>Slug de URL del tipo: "en-revision", "empresariales", etc.</summary>
    public string Tipo { get; init; } = default!;
    public string? CodigoProyecto { get; init; }
    public List<string> EstadosDeEjecucion { get; init; } = [];
    public List<string> Situaciones { get; init; } = [];
    public List<string> PublicacionesDerivadas { get; init; } = [];
}

/// <summary>Campos comunes a todos los proyectos.</summary>
public abstract record ProyectoBaseDto
{
    public string Id { get; init; } = default!;
    public string Titulo { get; init; } = default!;
    public string JefeId { get; init; } = default!;
    public string Jefe { get; init; } = default!;
    public string CorreoJefe { get; init; } = default!;
    public int NumeroMiembros { get; init; }
    public int CantidadMiembrosUH { get; init; }
    public int CantidadEstudiantes { get; init; }
    public int CantidadEstudiantesContratados { get; init; }
    public bool TributaFormacionDoctoral { get; init; }
    public string ClasificacionId { get; init; } = default!;
    public string ClasificacionNombre { get; init; } = default!;
    public List<UserRefDto> Participantes { get; init; } = [];
    public List<string> PublicacionesDerivadas { get; init; } = [];
}

public abstract record ProyectoEnEjecucionBaseDto : ProyectoBaseDto
{
    public DateOnly FechaInicio { get; init; }
    public DateOnly? FechaCierre { get; init; }
    public string CodigoProyecto { get; init; } = default!;
    public bool TributaDesarrolloLocal { get; init; }
    public List<NomencladorDto> EstadosDeEjecucion { get; init; } = [];
    public List<InstitutionRefDto> EntidadesEjecutorasPrincipales { get; init; } = [];
    public List<InstitutionRefDto> EntidadesEjecutorasParticipantes { get; init; } = [];
    public List<NomencladorDto> SectoresEstrategicos { get; init; } = [];
    public List<NomencladorDto> EjesEstrategicos { get; init; } = [];
}

public record ProyectoEnRevisionDto : ProyectoBaseDto
{
    public List<NomencladorDto> Situaciones { get; init; } = [];
    public string Tipo { get; init; } = default!;
}

public record ProyectoEmpresarialDto : ProyectoEnEjecucionBaseDto
{
    public List<InstitutionRefDto> Empresas { get; init; } = [];
}

public record ProyectoApoyoProgramaDto : ProyectoEnEjecucionBaseDto
{
    public List<NomencladorDto> Programas { get; init; } = [];
    public TipoPAP TipoPAP { get; init; }
}

public record ProyectoDesarrolloLocalDto : ProyectoEnEjecucionBaseDto
{
    public int MunicipioId { get; init; }
    public string MunicipioNombre { get; init; } = default!;
    public string? ProvinciaNombre { get; init; }
}

public record ProyectoNoEmpresarialDto : ProyectoEnEjecucionBaseDto
{
    public List<InstitutionRefDto> Entidades { get; init; } = [];
}

public record ProyectoColabInternacionalDto : ProyectoEnEjecucionBaseDto
{
    public List<NomencladorDto> FuentesFinanciacion { get; init; } = [];
    public string TerminosReferencia { get; init; } = default!;
}

public record ProyectoPNAPDto : ProyectoEnEjecucionBaseDto
{
    public List<NomencladorDto> FuentesFinanciacion { get; init; } = [];
}

/// <summary>Resumen mínimo de publicación usado por los endpoints de vinculación.</summary>
public record ProyectoPublicacionDto
{
    public string Id { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string? UrlDoi { get; init; }
}

/// <summary>Par mínimo Id/Título para el selector de proyectos en formularios de publicaciones.</summary>
public record ProyectoCatalogoDto(string Id, string Titulo);

public record ProyectoPatenteResumenDto(
    string PatenteId,
    string Titulo,
    string NumeroSolicitudConcesion,
    bool EsNacional,
    string? Creador,
    List<string> Creadores);
