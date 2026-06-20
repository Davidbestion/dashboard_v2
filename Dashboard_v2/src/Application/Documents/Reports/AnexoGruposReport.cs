using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Entities;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Application.Documents.Reports;

/// <summary>
/// Reporte: Anexo de Grupos de Investigación.
/// URL generada: GET /api/Documents/anexo-grupos
/// Plantilla:    Infrastructure/Templates/AnexoGrupos.xlsx
///
/// Solo incluye los grupos cuya área coincide con el área del usuario que solicita el reporte.
///
/// Columnas rellenadas automáticamente: Nombre, Total integrantes, Dr, MSc, Lic,
/// PT, PAUX, PASIST, INST, IT, IAUX, IAGRG, ASP., Adiestrados, Área temática,
/// Líneas de investigación.
///
/// Columnas que el usuario debe completar (quedan vacías en el Excel generado):
/// Áreas UH de miembros, Técnicos/Especialistas, Estudiantes 1ro-2do, Estudiantes
/// 3ro-4to, Proyectos de investigación.
/// </summary>
public sealed class AnexoGruposReport : IDocumentReport
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public AnexoGruposReport(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public string ReportName   => "anexo-grupos";
    public string TemplateName => "AnexoGrupos";

    public IReadOnlyCollection<string> AllowedRoles =>
        [nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Grupo_de_investigacion), nameof(RolesEnum.Vicedecano_de_investigacion)];

    public async Task<IReadOnlyDictionary<string, object>> GatherVariablesAsync(IReadOnlyDictionary<string, string>? parameters, CancellationToken ct)
    {
        var requestingAreaId = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == _currentUser.Id)
            .Select(u => u.AreaId)
            .FirstOrDefaultAsync(ct);

        var rows = await _context.GruposDeInvestigacion
            .Where(g => requestingAreaId != null && g.AreaId == requestingAreaId)
            .OrderBy(g => g.Nombre)
            .Select(g => new AnexoGruposRowDto
            {
                Nombre           = g.Nombre,
                TotalIntegrantes = g.Usuarios.Count,

                // Categoría científica
                CantDoctores    = g.Usuarios.Count(u => u.ScientificCategory == ScientificCategory.Doctor),
                CantMasters     = g.Usuarios.Count(u => u.ScientificCategory == ScientificCategory.Master),
                CantLicenciados = g.Usuarios.Count(u => u.ScientificCategory == ScientificCategory.Licenciado),

                // Categoría docente
                CantPT     = g.Usuarios.Count(u => u.TeachingCategory == TeachingCategory.Titular),
                CantPAUX   = g.Usuarios.Count(u => u.TeachingCategory == TeachingCategory.Auxiliar),
                CantPASIST = g.Usuarios.Count(u => u.TeachingCategory == TeachingCategory.Asistente),
                CantINST   = g.Usuarios.Count(u => u.TeachingCategory == TeachingCategory.Instructor),

                // Categoría investigativa
                CantIT    = g.Usuarios.Count(u => u.InvestigationCategory == InvestigationCategory.Titular),
                CantIAUX  = g.Usuarios.Count(u => u.InvestigationCategory == InvestigationCategory.Auxiliar),
                CantIAGRG = g.Usuarios.Count(u => u.InvestigationCategory == InvestigationCategory.Agregado),
                CantASP   = g.Usuarios.Count(u => u.InvestigationCategory == InvestigationCategory.Asociado),

                // Adiestrados
                CantAdiestrados = g.Usuarios.Count(u => u.IsTrained),

                // Descriptivos del grupo
                AreaTematica          = g.Area.Nombre,
                LineasDeInvestigacion = string.Join(", ", g.LineasDeInvestigacion.Select(l => l.Nombre))
            })
            .ToListAsync(ct);

        // La clave "Grupos" debe coincidir exactamente con el Named Range en AnexoGrupos.xlsx
        return new Dictionary<string, object> { ["Grupos"] = rows };
    }
}

/// <summary>
/// Proyección plana de datos de un grupo de investigación para el Anexo.
/// Las propiedades deben ser públicas para que ClosedXML.Report pueda
/// acceder a ellas por reflexión desde las expresiones {{item.Xxx}} de la plantilla.
/// </summary>
public record AnexoGruposRowDto
{
    public string Nombre           { get; init; } = default!;
    public int TotalIntegrantes    { get; init; }
    public int CantDoctores        { get; init; }
    public int CantMasters         { get; init; }
    public int CantLicenciados     { get; init; }
    public int CantPT              { get; init; }
    public int CantPAUX            { get; init; }
    public int CantPASIST          { get; init; }
    public int CantINST            { get; init; }
    public int CantIT              { get; init; }
    public int CantIAUX            { get; init; }
    public int CantIAGRG           { get; init; }
    public int CantASP             { get; init; }
    public int CantAdiestrados     { get; init; }
    public string AreaTematica          { get; init; } = default!;
    public string LineasDeInvestigacion { get; init; } = string.Empty;
}
