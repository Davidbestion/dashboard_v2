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
        WriteClassificationText(ws);
        WriteIndexedTable(ws, 17, 18, 19, "Libros", "libros");
        WriteIndexedTable(ws, 23, 24, 25, "Monografias", "monografias");
        WriteIndexedTable(ws, 29, 30, 31, "Capitulos", "capitulo de libros");
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
    /// Escribe el bloque inicial de clasificación de libros y editoriales.
    /// </summary>
    /// <param name="ws">Hoja destino.</param>
    private static void WriteClassificationText(IXLWorksheet ws)
    {
        var rows = new Dictionary<int, string>
        {
            [2] = "clasificacion.",
            [3] = "GRUPO 1. Libros publicados por editoriales que están en el Web de la Ciencias",
            [4] = "▪ Book Citation Index (BkCI) desarrollado por Clarivate analytics, se lanzó por primera vez en 2011 e indexa más de 60.000 libros editorialmente seleccionados, a partir de 2005. Estas bases de datos se recogen, además de libros y monografías en colecciones o aislados, actas de congresos, tesis doctorales, disertaciones y libros de texto. Tiene una edición de Ciencias Sociales y Humanidades y una de ciencia que abarca física, química, ingeniería, informática y tecnología, medicina clínica, ciencias de la vida, agricultura y biología. http://wokinfo.com/products_tools/multidisciplinary/bookcitationindex",
            [5] = "GRUPO 2. Libros publicados por Editoriales de reconocido prestigio internacional",
            [6] = "▪",
            [7] = "SciELO Libros. La red realiza la publicación online de colecciones nacionales y temáticas de libros académicos con el fin de maximizar la visibilidad, accesibilidad, uso e impacto, de la investigación, los ensayos y los estudios que se han realizado. Los libros publicados se seleccionan según controles de calidad aplicados por un comité científico y los textos en formato digital se preparan por normas internacionales. http://books.scielo.org/es/introduccion/",
            [8] = "▪",
            [9] = "SPI (Scholarly Publishers Indicators in Humanities and Social Sciences) Es el resultado de un proyecto de investigación del CSIC para obtener indicadores de calidad para libros y editoriales de carácter científico en Humanidades y Ciencias Sociales. Muestra un ranking de editoriales basado en la opinión expertos en dichas áreas, de forma general para todas las áreas y especializado por disciplinas. http://ilia.cchs.csic.es/SPI/expanded_index.html",
            [10] = "▪",
            [11] = "Publishers Scholar Metrics. Es una herramienta elaborada por el grupo de investigación EC3 para medir el impacto de las editoriales de monografías científicas a partir del total de citas de los libros publicados indizados en Google",
            [12] = "Académico hasta 2012 en el ámbito de las Humanidades y de las Ciencias Sociales. http://www.publishers-scholarmetrics.info/",
            [13] = "GRUPO 3. Libros publicados por Editoriales con referencia nacional",
        };

        foreach (var (row, text) in rows)
        {
            ws.Cell(row, row == 2 ? 2 : 1).Value = text;
            PublicationSheetHelper.ApplyTextStyle(ws.Cell(row, row == 2 ? 2 : 1), bold: row is 3 or 5 or 13);
        }
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
