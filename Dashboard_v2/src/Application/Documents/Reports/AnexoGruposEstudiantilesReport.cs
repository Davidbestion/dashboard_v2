using Dashboard_v2.Application.Common.Interfaces;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Application.Documents.Reports;

/// <summary>
/// Reporte del anexo de grupos científicos estudiantiles.
/// URL generada: GET /api/Documents/anexo-grupos-estudiantiles
/// Plantilla:    Infrastructure/Templates/AnexoGruposEstudiantiles.xlsx
///
/// Solo incluye los grupos cuya área coincide con el área del usuario que solicita el reporte.
///
/// Columnas rellenadas automáticamente:
/// Nombre del grupo, Área temática UH, Línea de investigación.
///
/// Columnas que permanecen vacías para completado manual:
/// Total de integrantes, Áreas de la UH de sus miembros, No estudiantes 1ro-2do,
/// No estudiantes 3ro-4to y Proyectos de investigación y/o extensión vinculados.
/// Estas columnas no pueden derivarse hoy del modelo de dominio actual.
/// </summary>
public sealed class AnexoGruposEstudiantilesReport : IDocumentReport
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    /// <summary>
    /// Inicializa el reporte con acceso al contexto de aplicación y al usuario actual.
    /// </summary>
    /// <param name="context">Contexto usado para consultar los grupos estudiantiles.</param>
    /// <param name="currentUser">Identidad del usuario que solicita el reporte.</param>
    public AnexoGruposEstudiantilesReport(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Identificador público del reporte consumido por el endpoint genérico de documentos.
    /// </summary>
    public string ReportName => "anexo-grupos-estudiantiles";

    public IReadOnlyCollection<string> AllowedRoles =>
        [nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Grupo_de_investigacion), nameof(RolesEnum.Vicedecano_de_investigacion)];

    /// <summary>
    /// Nombre base de la plantilla embebida sin extensión.
    /// </summary>
    public string TemplateName => "AnexoGruposEstudiantiles";

    /// <summary>
    /// Proyecta los grupos estudiantiles a una estructura plana compatible con
    /// ClosedXML.Report. La clave del diccionario debe coincidir exactamente con
    /// el nombre del rango definido en la plantilla.
    /// </summary>
    /// <param name="ct">Token para cancelar la consulta.</param>
    /// <returns>Variables que se inyectarán en la plantilla Excel.</returns>
    public async Task<IReadOnlyDictionary<string, object>> GatherVariablesAsync(IReadOnlyDictionary<string, string>? parameters, CancellationToken ct)
    {
        var requestingAreaId = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == _currentUser.Id)
            .Select(u => u.AreaId)
            .FirstOrDefaultAsync(ct);

        var rows = await _context.GruposEstudiantiles
            .Where(g => requestingAreaId != null && g.AreaId == requestingAreaId)
            .OrderBy(g => g.Nombre)
            .Select(g => new AnexoGruposEstudiantilesRowDto
            {
                Nombre = g.Nombre,
                AreaTematica = g.Area.Nombre,
                LineasDeInvestigacion = string.Join(", ", g.LineasDeInvestigacion
                    .OrderBy(l => l.Nombre)
                    .Select(l => l.Nombre)),
            })
            .ToListAsync(ct);

        return new Dictionary<string, object>
        {
            ["GruposEstudiantiles"] = rows,
        };
    }
}

/// <summary>
/// Proyección plana usada por ClosedXML.Report para resolver las expresiones
/// {{item.Xxx}} declaradas en la plantilla del anexo estudiantil.
/// </summary>
public sealed record AnexoGruposEstudiantilesRowDto
{
    /// <summary>
    /// Nombre del grupo científico estudiantil.
    /// </summary>
    public string Nombre { get; init; } = string.Empty;

    /// <summary>
    /// Área temática asociada al grupo.
    /// </summary>
    public string AreaTematica { get; init; } = string.Empty;

    /// <summary>
    /// Listado textual de líneas de investigación asociadas al grupo.
    /// </summary>
    public string LineasDeInvestigacion { get; init; } = string.Empty;
}
