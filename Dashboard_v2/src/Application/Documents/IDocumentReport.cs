namespace Dashboard_v2.Application.Documents;

/// <summary>
/// Contrato que debe implementar cada reporte de documento Excel del sistema.
///
/// ─── CÓMO AGREGAR UN NUEVO REPORTE ───────────────────────────────────────────
///
///  1. Diseña la plantilla .xlsx:
///     - Abre Excel y crea el archivo con el formato deseado (título, cabeceras,
///       colores, logos, múltiples hojas, etc.).
///     - Para cada colección de datos que quieras volcar en una hoja/tabla, define
///       un Named Range en Excel (Fórmulas → Administrador de nombres).
///       El rango debe abarcar DOS filas:
///         · Fila de datos   → celdas con expresiones  {{item.NombrePropiedad}}
///         · Fila de servicio → celdas vacías (ClosedXML.Report la elimina al generar)
///     - El nombre del Named Range debe coincidir con la clave que uses en
///       <see cref="GatherVariablesAsync"/> (ej. Named Range "Grupos" → key "Grupos").
///
///  2. Guarda la plantilla en  Infrastructure/Templates/NombrePlantilla.xlsx
///     El archivo se embebe automáticamente como recurso (la regla  *.xlsx  en
///     Infrastructure.csproj lo cubre).
///
///  3. Crea la clase del reporte en  Application/Documents/Reports/:
///     <code>
///     public sealed class MiNuevoReport : IDocumentReport
///     {
///         public string ReportName   => "mi-nuevo-reporte";   // URL: GET /api/Documents/mi-nuevo-reporte
///         public string TemplateName => "NombrePlantilla";    // sin extensión .xlsx
///
///         public async Task&lt;IReadOnlyDictionary&lt;string, object&gt;&gt; GatherVariablesAsync(CancellationToken ct)
///         {
///             var datos = await _context.MiEntidad.Select(...).ToListAsync(ct);
///             return new Dictionary&lt;string, object&gt;
///             {
///                 ["MiRango1"] = datos,           // Named Range "MiRango1" en la plantilla
///                 ["MiRango2"] = otraColeccion,   // Named Range "MiRango2" en otra hoja
///             };
///         }
///     }
///     </code>
///
///  4. Registra el reporte en  Infrastructure/DependencyInjection.cs:
///     <code>builder.Services.AddScoped&lt;IDocumentReport, MiNuevoReport&gt;();</code>
///
///  Ya está. El endpoint genérico  GET /api/Documents/{reportName}  lo encontrará
///  automáticamente por el nombre.
/// ─────────────────────────────────────────────────────────────────────────────
/// </summary>
public interface IDocumentReport
{
    /// <summary>
    /// Identificador del reporte. Se usa como segmento en la URL:
    /// GET /api/Documents/{ReportName}
    /// Usa kebab-case, ej. "anexo-grupos".
    /// </summary>
    string ReportName { get; }

    /// <summary>
    /// Nombre del archivo de plantilla (sin extensión) en Infrastructure/Templates/.
    /// Ej. "AnexoGrupos" → Infrastructure/Templates/AnexoGrupos.xlsx
    /// </summary>
    string TemplateName { get; }

    /// <summary>
    /// Consulta la base de datos y devuelve las variables que se inyectarán en la plantilla.
    /// Cada clave del diccionario debe coincidir exactamente con un Named Range
    /// definido en la plantilla .xlsx.
    /// </summary>
    /// <param name="parameters">
    /// Parámetros de filtrado adicionales enviados por el cliente (ej. "from", "to").
    /// Los reportes que no los necesiten pueden ignorarlos.
    /// </param>
    /// <param name="ct">Token de cancelación.</param>
    Task<IReadOnlyDictionary<string, object>> GatherVariablesAsync(
        IReadOnlyDictionary<string, string>? parameters,
        CancellationToken ct);
}
