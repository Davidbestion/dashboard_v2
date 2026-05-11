using TemplateGen.Core.Base;
using ClosedXML.Excel;

namespace TemplateGen.Templates;

/// <summary>
/// Clase base para todas las hojas del anexo de red universitaria.
/// Añade la fila 2 con el nombre de la red (sustituido en tiempo de generación).
/// </summary>
public abstract class UniversitariaSheetBase : SheetTemplateBase
{
    protected override void PostGenerate(IXLWorksheet ws)
    {
        // Fila 2: nombre de la red (variable escalar ClosedXML.Report)
        int lastCol = StartCol + Headers.Length - 1;
        ws.Cell(2, StartCol).Value = "Red: {{NombreRed}}";
        ws.Range(2, StartCol, 2, lastCol).Merge();
        var s = ws.Cell(2, StartCol).Style;
        s.Font.Bold = true;
        s.Font.FontSize = 12;
        s.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        PostGenerateSheet(ws);
    }

    // Hook adicional para personalizaciones específicas de cada hoja
    protected virtual void PostGenerateSheet(IXLWorksheet ws) { }
}

/// <summary>
/// Hoja 1 del archivo universitario: áreas de la UH y áreas externas que participan.
/// </summary>
public class AreasParticipantesSheet : UniversitariaSheetBase
{
    public override string Name => "Áreas Participantes";
    public override string Title => "ÁREAS PARTICIPANTES";
    public override string RangeName => "AreasParticipantes";

    public override string[] Headers => new[]
    {
        "Áreas que participan de la UH",
        "Áreas Externas que Participan",
    };

    public override IEnumerable<(int Col, string Expression)> TemplateCells => new[]
    {
        (1, "{{item.AreaUH}}"),
        (2, "{{item.AreaExterna}}"),
    };

    protected override void PostGenerateSheet(IXLWorksheet ws)
    {
        ws.Column(1).Width = 40;
        ws.Column(2).Width = 40;
        ws.SheetView.FreezeRows(4);
    }
}
