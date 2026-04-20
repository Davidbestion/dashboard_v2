using TemplateGen.Core.Base;
using ClosedXML.Excel;

namespace TemplateGen.Templates;

public class GruposSheet : SheetTemplateBase
{
    public override string Name => "Grupos de Investigación";
    public override string Title => "GRUPOS DE INVESTIGACIÓN";
    public override string RangeName => "Grupos";

    public override string[] Headers => new[]
    {
        "Nombre del Grupo de Investigación",
        "Total de Integrantes",
        "Áreas de la UH de sus Miembros",
        "Dr", "MSc", "Lic", "PT", "PAUX", "PASIST", "INST", "IT", "IAUX", "IAGRG", "ASP.",
        "Adiestrados",
        "Técnicos / Especialistas",
        "No Estudiantes 1ro-2do",
        "No Estudiantes 3ro-4to",
        "Área Temática UH",
        "Línea de Investigación",
        "Proyectos de Investigación del Grupo *"
    };

    public override IEnumerable<(int Col, string Expression)> TemplateCells => new[]
    {
        (1,  "{{item.Nombre}}"),
        (2,  "{{item.TotalIntegrantes}}"),
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
        (19, "{{item.AreaTematica}}"),
        (20, "{{item.LineasDeInvestigacion}}")
    };

    protected override void PostGenerate(IXLWorksheet ws)
    {
        // Notas al pie
        ws.Cell(2, 23).Value = "* código y título del proyecto";
        ws.Cell(3, 23).Value = "** si el grupo es ejecutor principal";
        ws.Cell(2, 23).Style.Font.Italic = true;
        ws.Cell(3, 23).Style.Font.Italic = true;

        // Anchos de columna
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
    }
}