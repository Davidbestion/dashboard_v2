using ClosedXML.Excel;
using TemplateGen.Core.Interfaces;

namespace TemplateGen.Templates;

/// <summary>
/// Hoja que contiene tres tablas dinámicas en la misma hoja:
/// libros, monografías y capítulos de libros.
/// </summary>
public sealed class LibrosMonografiasCapitulosSheet : ISheetTemplate
{
    /// <summary>
    /// Nombre visible de la hoja.
    /// </summary>
    public string Name => " LIBROS, monografias y capitulo";

    /// <summary>
    /// No se usa en esta hoja compuesta.
    /// </summary>
    public string Title => string.Empty;

    /// <summary>
    /// No se usa en esta hoja compuesta.
    /// </summary>
    public string[] Headers => [];

    /// <summary>
    /// No se usa en esta hoja compuesta porque la hoja define varios rangos.
    /// </summary>
    public string RangeName => string.Empty;

    /// <summary>
    /// No se usa en esta hoja compuesta.
    /// </summary>
    public int StartRow => 0;

    /// <summary>
    /// No se usa en esta hoja compuesta.
    /// </summary>
    public int StartCol => 1;

    /// <summary>
    /// No se usa en esta hoja compuesta.
    /// </summary>
    public int EndRowOffset => 0;

    /// <summary>
    /// No se usa en esta hoja compuesta.
    /// </summary>
    public IEnumerable<(int Col, string Expression)> TemplateCells => [];

    /// <summary>
    /// Construye la hoja incluyendo el texto clasificatorio y las tres tablas
    /// dinámicas del anexo.
    /// </summary>
    /// <param name="workbook">Libro destino.</param>
    public void Generate(IXLWorkbook workbook)
    {
        var ws = workbook.Worksheets.Add(Name);

        ApplyLayout(ws);
        WriteIndexedTable(ws, 1,  2,  3,  "Libros",     "libros");
        WriteIndexedTable(ws, 6,  7,  8,  "Monografias", "monografias");
        WriteIndexedTable(ws, 11, 12, 13, "Capitulos",   "capitulo de libros");
    }

    /// <summary>
    /// Ajusta anchos de columnas de la hoja.
    /// </summary>
    /// <param name="ws">Hoja destino.</param>
    private static void ApplyLayout(IXLWorksheet ws)
    {
        ws.Column(1).Width = 7.375;
        ws.Column(2).Width = 14.875;
        ws.Column(3).Width = 31;
        ws.Column(4).Width = 26.25;
        ws.Column(5).Width = 30.75;
    }

    /// <summary>
    /// Inserta una de las tres tablas dinámicas indexadas de la hoja.
    /// </summary>
    /// <param name="ws">Hoja destino.</param>
    /// <param name="titleRow">Fila del rótulo de la sección.</param>
    /// <param name="headerRow">Fila de cabeceras.</param>
    /// <param name="dataRow">Fila plantilla.</param>
    /// <param name="rangeName">Nombre del rango dinámico.</param>
    /// <param name="sectionTitle">Texto visible de la sección.</param>
    private static void WriteIndexedTable(
        IXLWorksheet ws,
        int titleRow,
        int headerRow,
        int dataRow,
        string rangeName,
        string sectionTitle)
    {
        PublicationSheetHelper.WriteMergedTextRow(ws, titleRow, 1, 5, sectionTitle, bold: true);
        PublicationSheetHelper.ApplyThinBorder(ws.Cell(titleRow, 1));
        ws.Range(titleRow, 1, titleRow, 5).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        PublicationSheetHelper.WriteHeaderRow(
            ws,
            headerRow,
            ["No", "indexacion", "titulo", "datos de la editorial", "relacion de autoria"]);

        PublicationSheetHelper.WriteTemplateRange(
            ws,
            rangeName,
            dataRow,
            5,
            [
                (1, "{{item.No}}"),
                (2, "{{item.Indexacion}}"),
                (3, "{{item.Titulo}}"),
                (4, "{{item.DatosEditorial}}"),
                (5, "{{item.RelacionAutoria}}"),
            ]);
    }
}
