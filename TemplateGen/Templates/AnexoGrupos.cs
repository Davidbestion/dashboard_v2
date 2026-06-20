using ClosedXML.Excel;

namespace TemplateGen.Templates;

/// <summary>
/// Genera la plantilla AnexoGrupos.xlsx para el Anexo 10 de Grupos de Investigación.
/// </summary>
public static class AnexoGrupos
{
    private const string OutputPath =
        "../Dashboard_v2/src/Infrastructure/Templates/AnexoGrupos.xlsx";

    public static void Generate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Grupos de Investigación");

        // ─── Fila 1: Título general ────────────────────────────────────────
        ws.Cell(1, 1).Value = "GRUPOS DE INVESTIGACIÓN";
        ws.Range(1, 1, 1, 21).Merge();
        var titleStyle = ws.Cell(1, 1).Style;
        titleStyle.Font.Bold = true;
        titleStyle.Font.FontSize = 14;
        titleStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        titleStyle.Fill.BackgroundColor = XLColor.FromHtml("#1F3864");
        titleStyle.Font.FontColor = XLColor.White;

        // ─── Filas 2–3: Notas al pie ───────────────────────────────────────
        ws.Cell(2, 23).Value = "* código y título del proyecto";
        ws.Cell(3, 23).Value = "** si el grupo es ejecutor principal";
        ws.Cell(2, 23).Style.Font.Italic = true;
        ws.Cell(3, 23).Style.Font.Italic = true;

        // ─── Fila 4: Cabeceras ─────────────────────────────────────────────
        var headers = new[]
        {
            "Nombre del Grupo de Investigación",       // 1
            "Total de Integrantes",                    // 2
            "Áreas de la UH de sus Miembros",          // 3
            "Dr",                                      // 4
            "MSc",                                     // 5
            "Lic",                                     // 6
            "PT",                                      // 7
            "PAUX",                                    // 8
            "PASIST",                                  // 9
            "INST",                                    // 10
            "IT",                                      // 11
            "IAUX",                                    // 12
            "IAGRG",                                   // 13
            "ASP.",                                    // 14
            "Adiestrados",                             // 15
            "Técnicos / Especialistas",                // 16
            "No Estudiantes 1ro-2do",                  // 17
            "No Estudiantes 3ro-4to",                  // 18
            "Área Temática UH",                        // 19
            "Línea de Investigación",                  // 20
            "Proyectos de Investigación del Grupo *",  // 21
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(4, i + 1);
            cell.Value = headers[i];
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

        // ─── Fila 5: Expresiones ClosedXML.Report ─────────────────────────
        var templateCells = new (int Col, string Expression)[]
        {
            (1,  "{{item.Nombre}}"),
            (2,  "{{item.TotalIntegrantes}}"),
            // (3, "{{item.AreasUH}}"), — vacío (usuario)
            (4,  "{{item.CantDoctores}}"),
            (5,  "{{item.CantMasters}}"),
            (6,  "{{item.CantLicenciados}}"),
            (7,  "{{item.CantPT}}"),
            (8,  "{{item.CantPAUX}}"),
            (9,  "{{item.CantPASIST}}"),
            (10, "{{item.CantINST}}"),
            (11, "{{item.CantIT}}"),
            (12, "{{item.CantIAUX}}"),
            (13, "{{item.CantIAGRG}}"),
            (14, "{{item.CantASP}}"),
            (15, "{{item.CantAdiestrados}}"),
            // (16, "{{item.CantTecnicos}}"), — vacío (usuario)
            // (17, "{{item.CantNoEstudiantes12}}"), — vacío (usuario)
            // (18, "{{item.CantNoEstudiantes34}}"), — vacío (usuario)
            (19, "{{item.AreaTematica}}"),
            (20, "{{item.LineasDeInvestigacion}}"),
            // (21, "{{item.Proyectos}}"), — vacío (usuario)
        };

        foreach (var (col, expr) in templateCells)
        {
            var cell = ws.Cell(5, col);
            cell.Value = expr;
            var s = cell.Style;
            s.Border.OutsideBorder = XLBorderStyleValues.Thin;
            s.Border.InsideBorder = XLBorderStyleValues.Thin;
            if (col >= 2 && col <= 18)
                s.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        for (int col = 1; col <= 21; col++)
        {
            var s = ws.Cell(5, col).Style;
            s.Border.OutsideBorder = XLBorderStyleValues.Thin;
            s.Border.InsideBorder = XLBorderStyleValues.Thin;
        }

        // ─── Fila 6: Fila de servicio (requerida por ClosedXML.Report) ────
        ws.Cell(6, 1).Value = "";

        // ─── Named Range "Grupos" ──────────────────────────────────────────
        var range = ws.Range(5, 1, 6, 21);
        wb.DefinedNames.Add("Grupos", range);

        // ─── Anchos de columna ─────────────────────────────────────────────
        ws.Column(1).Width = 35;
        ws.Column(2).Width = 12;
        ws.Column(3).Width = 20;
        for (int c = 4; c <= 16; c++) ws.Column(c).Width = 8;
        ws.Column(17).Width = 14;
        ws.Column(18).Width = 14;
        ws.Column(19).Width = 22;
        ws.Column(20).Width = 30;
        ws.Column(21).Width = 30;

        ws.SheetView.FreezeRows(4);

        wb.SaveAs(OutputPath);
        Console.WriteLine($"  → {OutputPath}");
    }
}
