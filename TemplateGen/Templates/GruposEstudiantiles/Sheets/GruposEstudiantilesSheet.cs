using ClosedXML.Excel;
using TemplateGen.Core.Base;

namespace TemplateGen.Templates;

/// <summary>
/// Hoja única del anexo de grupos científicos estudiantiles.
/// Replica la estructura del Excel de referencia: una tabla simple, sin fila
/// de título superior y con ocho columnas, dejando en blanco las columnas que
/// hoy no pueden completarse automáticamente desde el dominio.
/// </summary>
public sealed class GruposEstudiantilesSheet : SheetTemplateBase
{
    /// <summary>
    /// Nombre visible de la hoja dentro del libro Excel.
    /// </summary>
    public override string Name => "Grupos Estudiantiles";

    /// <summary>
    /// Título lógico de la hoja. No se renderiza porque esta plantilla sigue
    /// el formato del anexo de referencia, que comienza directamente en la tabla.
    /// </summary>
    public override string Title => "GRUPOS CIENTÍFICOS ESTUDIANTILES";

    /// <summary>
    /// Nombre del rango que ClosedXML.Report utilizará para expandir las filas.
    /// Debe coincidir con la clave enviada por el reporte de aplicación.
    /// </summary>
    public override string RangeName => "GruposEstudiantiles";

    /// <summary>
    /// Fila donde comienza el registro plantilla usado por ClosedXML.Report.
    /// </summary>
    // public override int StartRow => 3;

    /// <summary>
    /// Cabeceras del anexo, alineadas con el ejemplo provisto por el usuario.
    /// </summary>
    public override string[] Headers => new[]
    {
        "Nombre del Grupo Científico Estudiantil",
        "Total de Integrantes",
        "Áreas de la UH de sus Miembros",
        "No Estudiantes 1ro-2do",
        "No Estudiantes 3ro-4to",
        "Área Temática UH",
        "Línea de Investigación",
        "Proyectos de Investigación y/o Extensión Vinculados",
    };

    /// <summary>
    /// Expresiones de ClosedXML.Report para las columnas que sí puede poblar el sistema.
    /// Las restantes se dejan vacías para completado manual, igual que en el anexo
    /// de grupos de investigación ya existente.
    /// </summary>
    public override IEnumerable<(int Col, string Expression)> TemplateCells => new[]
    {
        (1, "{{item.Nombre}}"),
        (6, "{{item.AreaTematica}}"),
        (7, "{{item.LineasDeInvestigacion}}"),
    };

    /// <summary>
    /// Omite la fila de título para respetar la estructura del archivo de referencia,
    /// que inicia directamente con la cabecera de la tabla en la fila 2.
    /// </summary>
    // protected override void ApplyTitle(IXLWorksheet ws)
    // {
    // }

    /// <summary>
    /// Dibuja la fila de cabeceras con un estilo sencillo de borde y texto en negrita,
    /// replicando el formato del anexo de ejemplo.
    /// </summary>
    // protected override void ApplyHeaders(IXLWorksheet ws)
    // {
    //     for (int i = 0; i < Headers.Length; i++)
    //     {
    //         var cell = ws.Cell(2, StartCol + i);
    //         cell.Value = Headers[i];

    //         var style = cell.Style;
    //         style.Font.Bold = true;
    //         style.Alignment.WrapText = true;
    //         style.Border.OutsideBorder = XLBorderStyleValues.Thin;
    //         style.Border.InsideBorder = XLBorderStyleValues.Thin;
    //     }

    //     ws.Row(2).Height = 46.5;
    // }

    /// <summary>
    /// Coloca la fila plantilla y la fila de servicio requeridas por ClosedXML.Report,
    /// preservando bordes en todo el rango para que el archivo generado ya salga tabulado.
    /// </summary>
    // protected override void ApplyTemplateCells(IXLWorksheet ws)
    // {
    //     foreach (var (col, expr) in TemplateCells)
    //     {
    //         ws.Cell(StartRow, col).Value = expr;
    //     }

    //     for (int col = StartCol; col <= Headers.Length; col++)
    //     {
    //         var dataCell = ws.Cell(StartRow, col);
    //         if (dataCell.IsEmpty())
    //         {
    //             dataCell.Value = string.Empty;
    //         }

    //         dataCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
    //         dataCell.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

    //         var serviceCell = ws.Cell(StartRow + 1, col);
    //         serviceCell.Value = string.Empty;
    //         serviceCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
    //         serviceCell.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    //     }
    // }

    /// <summary>
    /// Ajusta anchos de columna para que la plantilla quede lista para usar sin
    /// depender de autoajustes manuales tras la generación.
    /// </summary>
    protected override void PostGenerate(IXLWorksheet ws)
    {
        ws.Column(1).Width = 35.57;
        ws.Column(2).Width = 9.57;
        ws.Column(3).Width = 11.43;
        ws.Column(4).Width = 8.43;
        ws.Column(5).Width = 8.43;
        ws.Column(6).Width = 17.14;
        ws.Column(7).Width = 13.57;
        ws.Column(8).Width = 23.43;
    }
}
