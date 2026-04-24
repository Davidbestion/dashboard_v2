using ClosedXML.Excel;

namespace TemplateGen.Templates;

/// <summary>
/// Utilidades de estilo y construcción compartidas entre las hojas del anexo
/// de publicaciones.
/// </summary>
internal static class PublicationSheetHelper
{
    /// <summary>
    /// Aplica formato de texto envolvente y alineación vertical centrada a una celda.
    /// </summary>
    /// <param name="cell">Celda a formatear.</param>
    /// <param name="bold">Indica si el texto debe mostrarse en negrita.</param>
    /// <param name="center">Indica si el contenido debe centrarse horizontalmente.</param>
    public static void ApplyTextStyle(IXLCell cell, bool bold = false, bool center = false)
    {
        cell.Style.Font.Bold = bold;
        cell.Style.Alignment.WrapText = true;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        cell.Style.Alignment.Horizontal = center
            ? XLAlignmentHorizontalValues.Center
            : XLAlignmentHorizontalValues.Left;
    }

    /// <summary>
    /// Aplica borde fino completo a una celda.
    /// </summary>
    /// <param name="cell">Celda a la que se le añaden los bordes.</param>
    public static void ApplyThinBorder(IXLCell cell)
    {
        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        cell.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }

    /// <summary>
    /// Escribe una fila de cabecera con estilo tabular consistente.
    /// </summary>
    /// <param name="ws">Hoja de trabajo destino.</param>
    /// <param name="row">Fila donde se escribirá la cabecera.</param>
    /// <param name="headers">Textos de las columnas.</param>
    public static void WriteHeaderRow(IXLWorksheet ws, int row, IReadOnlyList<string> headers)
    {
        for (int i = 0; i < headers.Count; i++)
        {
            var cell = ws.Cell(row, i + 1);
            cell.Value = headers[i];
            ApplyTextStyle(cell, bold: true, center: true);
            ApplyThinBorder(cell);
        }
    }

    /// <summary>
    /// Crea una fila plantilla de ClosedXML.Report y su fila de servicio,
    /// devolviendo además el rango nombrado que ambas conforman.
    /// </summary>
    /// <param name="ws">Hoja donde se insertará el rango.</param>
    /// <param name="rangeName">Nombre del rango para ClosedXML.Report.</param>
    /// <param name="dataRow">Fila que contiene las expresiones template.</param>
    /// <param name="lastColumn">Última columna incluida en el rango.</param>
    /// <param name="templateCells">Expresiones por columna de la fila plantilla.</param>
    public static void WriteTemplateRange(
        IXLWorksheet ws,
        string rangeName,
        int dataRow,
        int lastColumn,
        IReadOnlyList<(int Col, string Expression)> templateCells)
    {
        foreach (var (col, expression) in templateCells)
        {
            ws.Cell(dataRow, col).Value = expression;
        }

        for (int col = 1; col <= lastColumn; col++)
        {
            var templateCell = ws.Cell(dataRow, col);
            if (templateCell.IsEmpty())
            {
                templateCell.Value = string.Empty;
            }

            ApplyTextStyle(templateCell, center: col == 1);
            ApplyThinBorder(templateCell);

            var serviceCell = ws.Cell(dataRow + 1, col);
            serviceCell.Value = string.Empty;
            ApplyThinBorder(serviceCell);
        }

        var range = ws.Range(dataRow, 1, dataRow + 1, lastColumn);
        ws.Workbook.DefinedNames.Add(rangeName, range);
    }

    /// <summary>
    /// Escribe una fila estática opcionalmente fusionada a lo ancho de varias columnas.
    /// </summary>
    /// <param name="ws">Hoja destino.</param>
    /// <param name="row">Fila donde se ubicará el texto.</param>
    /// <param name="fromCol">Columna inicial.</param>
    /// <param name="toCol">Columna final.</param>
    /// <param name="text">Contenido textual a escribir.</param>
    /// <param name="bold">Indica si el texto irá en negrita.</param>
    public static void WriteMergedTextRow(
        IXLWorksheet ws,
        int row,
        int fromCol,
        int toCol,
        string text,
        bool bold = false)
    {
        var cell = ws.Cell(row, fromCol);
        cell.Value = text;
        ApplyTextStyle(cell, bold: bold);

        if (toCol > fromCol)
        {
            ws.Range(row, fromCol, row, toCol).Merge();
        }
    }
}

/// <summary>
/// Define una fila de texto fijo dentro de una hoja del anexo.
/// </summary>
/// <param name="Row">Fila donde se ubicará el texto.</param>
/// <param name="FromCol">Columna inicial.</param>
/// <param name="ToCol">Columna final para una posible fusión.</param>
/// <param name="Text">Texto mostrado en la hoja.</param>
public sealed record PublicationStaticRow(int Row, int FromCol, int ToCol, string Text);
