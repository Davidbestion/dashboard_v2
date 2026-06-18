using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Domain.Enums;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Application.Documents.Reports;

/// <summary>
/// Reporte del anexo de publicaciones científicas.
/// URL generada: GET /api/Documents/anexo-publicaciones
/// Plantilla:    Infrastructure/Templates/AnexoPublicaciones.xlsx
///
/// La primera hoja es informativa y no se rellena automáticamente.
/// El resto de las hojas sí reciben datos dinámicos clasificados por:
/// G1, G2, G3, G4, libros, monografías, capítulos de libro y artículos de divulgación.
/// </summary>
public sealed class AnexoPublicacionesReport : IDocumentReport
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    /// <summary>
    /// Inicializa el reporte con acceso al contexto de aplicación y al usuario actual.
    /// </summary>
    /// <param name="context">Contexto de base de datos usado para consultar publicaciones.</param>
    /// <param name="currentUser">Identidad del usuario que solicita el reporte.</param>
    public AnexoPublicacionesReport(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Identificador público del reporte consumido por el endpoint genérico.
    /// </summary>
    public string ReportName => "anexo-publicaciones";

    /// <summary>
    /// Nombre base de la plantilla embebida sin extensión.
    /// </summary>
    public string TemplateName => "AnexoPublicaciones";

    public IReadOnlyCollection<string> AllowedRoles =>
        [nameof(RolesEnum.Superuser), nameof(RolesEnum.Vicedecano_de_investigacion)];

    /// <summary>
    /// Reúne y clasifica todas las publicaciones necesarias para el anexo.
    /// </summary>
    /// <param name="parameters">
    /// Parámetros opcionales de filtrado:
    /// <list type="bullet">
    ///   <item><c>from</c> – fecha inicio en formato <c>YYYY-MM-DD</c> (inclusive).</item>
    ///   <item><c>to</c>   – fecha fin en formato <c>YYYY-MM-DD</c> (inclusive).</item>
    /// </list>
    /// Si se omite alguno de los dos extremos, ese límite no se aplica.
    /// </param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Variables cuyo nombre coincide con los rangos definidos en la plantilla.</returns>
    public async Task<IReadOnlyDictionary<string, object>> GatherVariablesAsync(
        IReadOnlyDictionary<string, string>? parameters,
        CancellationToken ct)
    {
        var fromRaw = parameters is not null && parameters.TryGetValue("from", out var f) ? f : null;
        var toRaw   = parameters is not null && parameters.TryGetValue("to",   out var t) ? t : null;

        // Los límites se reciben en formato YYYY-MM. Se truncan a 7 chars por seguridad.
        var from = string.IsNullOrWhiteSpace(fromRaw) ? null : fromRaw[..Math.Min(fromRaw.Length, 7)];
        var to   = string.IsNullOrWhiteSpace(toRaw)   ? null : toRaw[..Math.Min(toRaw.Length, 7)];

        var requestingAreaId = await _context.GetUserAreaIdAsync(_currentUser.Id, ct);

        var publications = await _context.Publications
            .AsNoTracking()
            .Include(p => p.AuthorPublications)
                .ThenInclude(ap => ap.Author)
            .Include(p => p.JournalPublication)
                .ThenInclude(jp => jp!.JournalGroup1Publication)
            .Include(p => p.JournalPublication)
                .ThenInclude(jp => jp!.BaseDeDatos)
            .Include(p => p.IndexedPublication)
            .Where(p => requestingAreaId != null &&
                p.AuthorPublications.Any(ap =>
                    ap.Author.UserId != null &&
                    ap.Author.User!.AreaId == requestingAreaId))
            .OrderBy(p => p.Title)
            .ToListAsync(ct);

        // Filtro de rango de fechas en memoria a granularidad de mes (YYYY-MM).
        // Las publicaciones con fecha solo de año se expanden al mes mínimo (01) para
        // el límite inferior y al mes máximo (12) para el superior, evitando falsos
        // positivos cuando el rango no abarca el año completo.
        var filtered = publications
            .Where(p =>
            {
                var d = p.PublishedDate;
                // Expande a YYYY-MM: año solo → YYYY-01 para from, YYYY-12 para to.
                var dFrom = d.Length == 4 ? d + "-01" : d[..Math.Min(d.Length, 7)];
                var dTo   = d.Length == 4 ? d + "-12" : d[..Math.Min(d.Length, 7)];
                var fromOk = from == null || string.Compare(dFrom, from, StringComparison.Ordinal) >= 0;
                var toOk   = to   == null || string.Compare(dTo,   to,   StringComparison.Ordinal) <= 0;
                return fromOk && toOk;
            })
            .ToList();

        var journalPublications = filtered
            .Where(p => p.PublicationType == PublicationType.Diario && p.JournalPublication is not null)
            .OrderBy(p => p.Title)
            .ToList();

        var indexedPublications = filtered
            .Where(p => p.PublicationType != PublicationType.Diario && p.IndexedPublication is not null)
            .OrderBy(p => p.Title)
            .ToList();

        // Materializar sublistas para calcular conteos y reutilizarlas en el diccionario.
        var g1           = journalPublications.Where(p => p.JournalPublication!.Group == 1).ToList();
        var g2           = journalPublications.Where(p => p.JournalPublication!.Group == 2).ToList();
        var g3           = journalPublications.Where(p => p.JournalPublication!.Group == 3).ToList();
        var g4           = journalPublications.Where(p => p.JournalPublication!.Group == 4).ToList();
        var libros       = indexedPublications.Where(p => p.PublicationType == PublicationType.Libro).ToList();
        var monografias  = indexedPublications.Where(p => p.PublicationType == PublicationType.Monografía).ToList();
        var capitulos    = indexedPublications.Where(p => p.PublicationType == PublicationType.Capítulo).ToList();
        var artDiv       = indexedPublications.Where(p => p.PublicationType == PublicationType.Artículo_de_Divulgación).ToList();

        return new Dictionary<string, object>
        {
            // ── Conteos para la primera hoja (columna "Publicados") ────────────
            ["G1Count"]                   = g1.Count,
            ["G2Count"]                   = g2.Count,
            ["G3Count"]                   = g3.Count,
            ["G4Count"]                   = g4.Count,
            ["CapitulosCount"]            = capitulos.Count,
            ["LibrosMonografiasCount"]    = libros.Count + monografias.Count,
            ["ArticulosDivulgacionCount"] = artDiv.Count,

            // ── Datos de cada hoja ─────────────────────────────────────────────
            ["G1"] = g1
                .Select((p, index) => new PublicacionG1RowDto
                {
                    No = index + 1,
                    Titulo = p.Title,
                    DatosPublicacion = BuildPublicationDetails(p),
                    RelacionAutoria = BuildAuthorsSummary(p),
                    BaseDeDatos = p.JournalPublication!.BaseDeDatos?.Nombre ?? string.Empty,
                    Cuartil = p.JournalPublication.JournalGroup1Publication?.Cuartil ?? string.Empty,
                })
                .ToList(),
            ["G2"] = g2.Select((p, index) => BuildJournalRow(p, index)).ToList(),
            ["G3"] = g3.Select((p, index) => BuildJournalRow(p, index)).ToList(),
            ["G4"] = g4.Select((p, index) => BuildJournalRow(p, index)).ToList(),
            ["Libros"]      = libros.Select((p, index) => BuildIndexedRow(p, index)).ToList(),
            ["Monografias"] = monografias.Select((p, index) => BuildIndexedRow(p, index)).ToList(),
            ["Capitulos"]   = capitulos.Select((p, index) => BuildIndexedRow(p, index)).ToList(),
            ["ArticulosDivulgacion"] = artDiv
                .Select((p, index) => new PublicacionDivulgacionRowDto
                {
                    No = index + 1,
                    Titulo = p.Title,
                    DatosPublicacion = BuildPublicationDetails(p),
                    RelacionAutoria = BuildAuthorsSummary(p),
                })
                .ToList(),
        };
    }

    /// <summary>
    /// Construye una fila para los grupos G2-G4, donde no existe columna de cuartil.
    /// </summary>
    /// <param name="publication">Publicación de revista a proyectar.</param>
    /// <param name="index">Posición cero basada dentro del grupo.</param>
    /// <returns>Fila lista para ClosedXML.Report.</returns>
    private static PublicacionJournalRowDto BuildJournalRow(Publication publication, int index)
    {
        return new PublicacionJournalRowDto
        {
            No = index + 1,
            Titulo = publication.Title,
            DatosPublicacion = BuildPublicationDetails(publication),
            RelacionAutoria = BuildAuthorsSummary(publication),
            BaseDeDatos = publication.JournalPublication!.BaseDeDatos?.Nombre ?? string.Empty,
        };
    }

    /// <summary>
    /// Construye una fila para libros, monografías y capítulos de libro.
    /// </summary>
    /// <param name="publication">Publicación indexada a proyectar.</param>
    /// <param name="index">Posición cero basada dentro de la categoría.</param>
    /// <returns>Fila lista para ClosedXML.Report.</returns>
    private static PublicacionIndexadaRowDto BuildIndexedRow(Publication publication, int index)
    {
        return new PublicacionIndexadaRowDto
        {
            No = index + 1,
            Indexacion = publication.IndexedPublication?.Index?.ToString() ?? string.Empty,
            Titulo = publication.Title,
            DatosEditorial = publication.PublicationData,
            RelacionAutoria = BuildAuthorsSummary(publication),
        };
    }

    /// <summary>
    /// Construye el texto de autores mostrado en el anexo.
    /// Como el modelo actual no persiste el orden de autoría, se usa orden alfabético
    /// para garantizar un resultado estable y reproducible.
    /// </summary>
    /// <param name="publication">Publicación cuyo listado de autores se formateará.</param>
    /// <returns>Cadena con los autores separados por coma.</returns>
    private static string BuildAuthorsSummary(Publication publication)
    {
        return string.Join(", ", publication.AuthorPublications
            .Where(ap => ap.Author is not null)
            .Select(ap => ap.Author.Name)
            .OrderBy(name => name));
    }

    /// <summary>
    /// Construye el bloque descriptivo de la publicación combinando el texto base
    /// almacenado en el sistema con el DOI o URL directa cuando exista.
    /// </summary>
    /// <param name="publication">Publicación a describir.</param>
    /// <returns>Texto consolidado de datos bibliográficos.</returns>
    private static string BuildPublicationDetails(Publication publication)
    {
        if (string.IsNullOrWhiteSpace(publication.UrlDoi))
        {
            return publication.PublicationData;
        }

        return $"{publication.PublicationData} DOI/URL: {publication.UrlDoi}";
    }
}

/// <summary>
/// Fila para publicaciones seriadas G2-G4.
/// </summary>
public record PublicacionJournalRowDto
{
    /// <summary>
    /// Numeración consecutiva dentro del grupo.
    /// </summary>
    public int No { get; init; }

    /// <summary>
    /// Título de la publicación.
    /// </summary>
    public string Titulo { get; init; } = string.Empty;

    /// <summary>
    /// Datos bibliográficos consolidados.
    /// </summary>
    public string DatosPublicacion { get; init; } = string.Empty;

    /// <summary>
    /// Relación de autoría mostrada en el anexo.
    /// </summary>
    public string RelacionAutoria { get; init; } = string.Empty;

    /// <summary>
    /// Base de datos o índice de la revista.
    /// </summary>
    public string BaseDeDatos { get; init; } = string.Empty;
}

/// <summary>
/// Fila para publicaciones seriadas G1, que además incorpora cuartil.
/// </summary>
public sealed record PublicacionG1RowDto : PublicacionJournalRowDto
{
    /// <summary>
    /// Cuartil asociado a la publicación del grupo 1.
    /// </summary>
    public string Cuartil { get; init; } = string.Empty;
}

/// <summary>
/// Fila para libros, monografías y capítulos de libro.
/// </summary>
public sealed record PublicacionIndexadaRowDto
{
    /// <summary>
    /// Numeración consecutiva dentro de la categoría.
    /// </summary>
    public int No { get; init; }

    /// <summary>
    /// Texto de indexación o referencia editorial.
    /// </summary>
    public string Indexacion { get; init; } = string.Empty;

    /// <summary>
    /// Título de la publicación.
    /// </summary>
    public string Titulo { get; init; } = string.Empty;

    /// <summary>
    /// Datos editoriales consolidados.
    /// </summary>
    public string DatosEditorial { get; init; } = string.Empty;

    /// <summary>
    /// Relación de autoría mostrada en el anexo.
    /// </summary>
    public string RelacionAutoria { get; init; } = string.Empty;
}

/// <summary>
/// Fila para artículos de divulgación.
/// </summary>
public sealed record PublicacionDivulgacionRowDto
{
    /// <summary>
    /// Numeración consecutiva dentro de la categoría.
    /// </summary>
    public int No { get; init; }

    /// <summary>
    /// Título de la publicación.
    /// </summary>
    public string Titulo { get; init; } = string.Empty;

    /// <summary>
    /// Datos bibliográficos consolidados.
    /// </summary>
    public string DatosPublicacion { get; init; } = string.Empty;

    /// <summary>
    /// Relación de autoría mostrada en el anexo.
    /// </summary>
    public string RelacionAutoria { get; init; } = string.Empty;
}
