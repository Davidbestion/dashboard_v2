using ClosedXML.Excel;
using TemplateGen.Core.Interfaces;

namespace TemplateGen.Templates;

/// <summary>
/// Hoja reutilizable para los grupos G1-G4 de publicaciones seriadas.
/// </summary>
public sealed class GrupoRevistasSheet : ISheetTemplate
{
    private readonly IReadOnlyList<PublicationStaticRow> _staticRows;
    private readonly IReadOnlyDictionary<int, double> _columnWidths;
    private readonly bool _hasQuartileColumn;
    private readonly int _headerRow;
    private readonly int _dataRow;

    /// <summary>
    /// Inicializa una hoja de grupo de revistas.
    /// </summary>
    /// <param name="name">Nombre visible de la hoja.</param>
    /// <param name="rangeName">Nombre del rango dinámico.</param>
    /// <param name="headerRow">Fila donde se dibuja la cabecera de la tabla.</param>
    /// <param name="dataRow">Fila de expresiones template.</param>
    /// <param name="hasQuartileColumn">Indica si la tabla debe incluir columna de cuartil.</param>
    /// <param name="staticRows">Filas de texto fijo situadas por encima de la tabla.</param>
    /// <param name="columnWidths">Anchos de columna por índice.</param>
    public GrupoRevistasSheet(
        string name,
        string rangeName,
        int headerRow,
        int dataRow,
        bool hasQuartileColumn,
        IReadOnlyList<PublicationStaticRow> staticRows,
        IReadOnlyDictionary<int, double> columnWidths)
    {
        Name = name;
        RangeName = rangeName;
        _headerRow = headerRow;
        _dataRow = dataRow;
        _hasQuartileColumn = hasQuartileColumn;
        _staticRows = staticRows;
        _columnWidths = columnWidths;
    }

    /// <summary>
    /// Nombre de la hoja.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// No se utiliza en esta implementación específica.
    /// </summary>
    public string Title => string.Empty;

    /// <summary>
    /// Cabeceras de la tabla principal.
    /// </summary>
    public string[] Headers => _hasQuartileColumn
        ? ["No", "Título", "Datos de la publicación", "Relación de autoría", "base de datos", "cuartil"]
        : ["No", "Título", "Datos de la publicación", "Relación de autoría", "base de datos"];

    /// <summary>
    /// Nombre del rango dinámico de la hoja.
    /// </summary>
    public string RangeName { get; }

    /// <summary>
    /// Fila de datos del rango dinámico.
    /// </summary>
    public int StartRow => _dataRow;

    /// <summary>
    /// Columna inicial del rango.
    /// </summary>
    public int StartCol => 1;

    /// <summary>
    /// El rango abarca la fila plantilla y la fila de servicio.
    /// </summary>
    public int EndRowOffset => 1;

    /// <summary>
    /// Expresiones template para la fila dinámica.
    /// </summary>
    public IEnumerable<(int Col, string Expression)> TemplateCells => _hasQuartileColumn
        ? [(1, "{{item.No}}"), (2, "{{item.Titulo}}"), (3, "{{item.DatosPublicacion}}"), (4, "{{item.RelacionAutoria}}"), (5, "{{item.BaseDeDatos}}"), (6, "{{item.Cuartil}}")]
        : [(1, "{{item.No}}"), (2, "{{item.Titulo}}"), (3, "{{item.DatosPublicacion}}"), (4, "{{item.RelacionAutoria}}"), (5, "{{item.BaseDeDatos}}")];

    /// <summary>
    /// Construye la hoja completa del grupo de revistas.
    /// </summary>
    /// <param name="workbook">Libro destino.</param>
    public void Generate(IXLWorkbook workbook)
    {
        var ws = workbook.Worksheets.Add(Name);

        WriteStaticRows(ws);
        PublicationSheetHelper.WriteHeaderRow(ws, _headerRow, Headers);
        PublicationSheetHelper.WriteTemplateRange(ws, RangeName, _dataRow, Headers.Length, TemplateCells.ToArray());
        ApplyColumnWidths(ws);
    }

    /// <summary>
    /// Escribe el bloque introductorio fijo de la hoja.
    /// </summary>
    /// <param name="ws">Hoja destino.</param>
    private void WriteStaticRows(IXLWorksheet ws)
    {
        foreach (var row in _staticRows)
        {
            PublicationSheetHelper.WriteMergedTextRow(ws, row.Row, row.FromCol, row.ToCol, row.Text);
        }
    }

    /// <summary>
    /// Ajusta los anchos de columnas según la referencia.
    /// </summary>
    /// <param name="ws">Hoja destino.</param>
    private void ApplyColumnWidths(IXLWorksheet ws)
    {
        foreach (var (col, width) in _columnWidths)
        {
            ws.Column(col).Width = width;
        }
    }
}
