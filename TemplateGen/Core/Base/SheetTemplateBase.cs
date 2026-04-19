using TemplateGen.Core.Interfaces;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TemplateGen.Core.Base;

public abstract class SheetTemplateBase : ISheetTemplate
{
    public abstract string Name { get; }
    public abstract string Title { get; }
    public abstract string[] Headers { get; }
    public abstract string RangeName { get; }
    public virtual int StartRow => 5;
    public virtual int StartCol => 1;
    public virtual int EndRowOffset => 1;  // una fila extra para la fila de servicio

    public virtual IEnumerable<(int Col, string Expression)> TemplateCells => 
        Enumerable.Empty<(int, string)>();

    public void Generate(IXLWorkbook workbook)
    {
        var ws = workbook.Worksheets.Add(Name);
        ApplyTitle(ws);
        ApplyHeaders(ws);
        ApplyTemplateCells(ws);
        ApplyNamedRange(ws);
        PostGenerate(ws); // Hook para ajustes específicos (anchos, congelar paneles, notas, etc.)
    }

    protected virtual void ApplyTitle(IXLWorksheet ws)
    {
        ws.Cell(1, StartCol).Value = Title;
        // Merge across header columns (respect StartCol offset)
        ws.Range(1, StartCol, 1, StartCol + Headers.Length - 1).Merge();
        var style = ws.Cell(1, StartCol).Style;
        style.Font.Bold = true;
        style.Font.FontSize = 14;
        style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        style.Fill.BackgroundColor = XLColor.FromHtml("#1F3864");
        style.Font.FontColor = XLColor.White;
    }

    protected virtual void ApplyHeaders(IXLWorksheet ws)
    {
        for (int i = 0; i < Headers.Length; i++)
        {
            var cell = ws.Cell(4, StartCol + i);
            cell.Value = Headers[i];
            var s = cell.Style;
            s.Font.Bold = true;
            s.Alignment.WrapText = true;
            s.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            s.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            s.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");
            s.Border.OutsideBorder = XLBorderStyleValues.Thin;
            s.Border.InsideBorder = XLBorderStyleValues.Thin;
        }
        ws.Row(4).Height = 50;
    }

    protected virtual void ApplyTemplateCells(IXLWorksheet ws)
    {
        foreach (var (col, expr) in TemplateCells)
        {
            var cell = ws.Cell(StartRow, col);
            cell.Value = expr;
            var s = cell.Style;
            s.Border.OutsideBorder = XLBorderStyleValues.Thin;
            s.Border.InsideBorder = XLBorderStyleValues.Thin;
            // Opcional: centrar numéricos en el rango de headers
            var endCol = Headers.Length;
            if (col >= StartCol && col <= endCol)
                s.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
        // Asegurar bordes en toda la fila de datos
        for (int col = StartCol; col <= Headers.Length; col++)
        {
            var cell = ws.Cell(StartRow, col);
            if (cell.IsEmpty())
                cell.Value = ""; // mantener celda vacía con bordes
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        }
        // Fila de servicio (necesaria para ClosedXML.Report)
        ws.Cell(StartRow + 1, StartCol).Value = "";
    }

    protected virtual void ApplyNamedRange(IXLWorksheet ws)
    {
        var range = ws.Range(StartRow, StartCol, StartRow + EndRowOffset, Headers.Length);
        // Use DefinedNames for consistency with older code
        ws.Workbook.DefinedNames.Add(RangeName, range);
    }

    // Hook para personalizaciones adicionales
    protected virtual void PostGenerate(IXLWorksheet ws) { }
}