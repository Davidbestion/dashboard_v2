using ClosedXML.Excel;

namespace TemplateGen.Templates;

/// <summary>
/// Utilidades compartidas para construir la hoja del anexo de eventos.
/// Centraliza estilo, bordes y creación de rangos dinámicos para mantener
/// consistencia visual y reducir duplicación en la plantilla.
/// </summary>
internal static class EventosSheetHelper
{
    /// <summary>
    /// Aplica el estilo base de texto a una celda.
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
    /// Aplica un borde fino completo a la celda dada.
    /// </summary>
    /// <param name="cell">Celda a la que se le aplicarán bordes.</param>
    public static void ApplyThinBorder(IXLCell cell)
    {
        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        cell.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }

    /// <summary>
    /// Escribe una fila de texto fija, fusionando columnas cuando sea necesario.
    /// </summary>
    /// <param name="ws">Hoja destino.</param>
    /// <param name="row">Fila donde se escribirá el contenido.</param>
    /// <param name="fromCol">Columna inicial.</param>
    /// <param name="toCol">Columna final de la fusión.</param>
    /// <param name="text">Texto a escribir.</param>
    /// <param name="bold">Indica si el contenido debe mostrarse en negrita.</param>
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

    /// <summary>
    /// Escribe una fila de cabeceras con estilo tabular consistente.
    /// </summary>
    /// <param name="ws">Hoja destino.</param>
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
    /// Crea una fila plantilla de ClosedXML.Report y su fila de servicio
    /// asociada, registrando además el rango nombrado correspondiente.
    /// </summary>
    /// <param name="ws">Hoja donde se creará el rango.</param>
    /// <param name="rangeName">Nombre del rango dinámico.</param>
    /// <param name="dataRow">Fila donde se escribirán las expresiones template.</param>
    /// <param name="lastColumn">Última columna incluida en el rango.</param>
    /// <param name="templateCells">Expresiones a insertar por columna.</param>
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

            ApplyTextStyle(templateCell, center: col > 1 && lastColumn <= 4);
            ApplyThinBorder(templateCell);

            var serviceCell = ws.Cell(dataRow + 1, col);
            serviceCell.Value = string.Empty;
            ApplyThinBorder(serviceCell);
        }

        ws.Workbook.DefinedNames.Add(rangeName, ws.Range(dataRow, 1, dataRow + 1, lastColumn));
    }
}
