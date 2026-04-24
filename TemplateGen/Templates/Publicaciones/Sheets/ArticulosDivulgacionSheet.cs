using ClosedXML.Excel;
using TemplateGen.Core.Interfaces;

namespace TemplateGen.Templates;

/// <summary>
/// Hoja para artículos de divulgación y publicaciones en medios de prensa o sitios web.
/// </summary>
public sealed class ArticulosDivulgacionSheet : ISheetTemplate
{
    /// <summary>
    /// Nombre visible de la hoja.
    /// </summary>
    public string Name => "articulos de divulgacion";

    /// <summary>
    /// No se usa en esta implementación.
    /// </summary>
    public string Title => string.Empty;

    /// <summary>
    /// Cabeceras de la tabla principal.
    /// </summary>
    public string[] Headers => ["No", "Título", "Datos de la publicación", "Relación de autoría", string.Empty, string.Empty];

    /// <summary>
    /// Nombre del rango dinámico de la hoja.
    /// </summary>
    public string RangeName => "ArticulosDivulgacion";

    /// <summary>
    /// Fila de datos del rango dinámico.
    /// </summary>
    public int StartRow => 6;

    /// <summary>
    /// Columna inicial del rango.
    /// </summary>
    public int StartCol => 1;

    /// <summary>
    /// Fila de servicio del rango dinámico.
    /// </summary>
    public int EndRowOffset => 1;

    /// <summary>
    /// Expresiones template que rellenarán el rango dinámico.
    /// </summary>
    public IEnumerable<(int Col, string Expression)> TemplateCells =>
    [
        (1, "{{item.No}}"),
        (2, "{{item.Titulo}}"),
        (3, "{{item.DatosPublicacion}}"),
        (4, "{{item.RelacionAutoria}}"),
    ];

    /// <summary>
    /// Construye la hoja completa de artículos de divulgación.
    /// </summary>
    /// <param name="workbook">Libro destino.</param>
    public void Generate(IXLWorkbook workbook)
    {
        var ws = workbook.Worksheets.Add(Name);

        ApplyLayout(ws);
        PublicationSheetHelper.WriteMergedTextRow(ws, 3, 1, 4, "Publicaciones en sitios webs, medios de prensa, repositorios, boletines, etc.");
        PublicationSheetHelper.WriteHeaderRow(ws, 5, Headers);
        PublicationSheetHelper.WriteTemplateRange(ws, RangeName, 6, 6, TemplateCells.ToArray());
    }

    /// <summary>
    /// Ajusta anchos de columnas.
    /// </summary>
    /// <param name="ws">Hoja destino.</param>
    private static void ApplyLayout(IXLWorksheet ws)
    {
        ws.Column(1).Width = 8;
        ws.Column(2).Width = 24;
        ws.Column(3).Width = 25.125;
        ws.Column(4).Width = 30.625;
        ws.Column(5).Width = 12;
        ws.Column(6).Width = 12;
    }
}
