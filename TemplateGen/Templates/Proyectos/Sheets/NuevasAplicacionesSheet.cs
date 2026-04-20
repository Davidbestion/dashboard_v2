using TemplateGen.Core.Base;
using ClosedXML.Excel;

namespace TemplateGen.Templates;

public class NuevasAplicacionesSheet : ProyectosSheetBase
{
    public NuevasAplicacionesSheet() : base(
        name: "NuevasAplicaciones",
        title: "Nuevas Aplicaciones",
        headers: new[] 
        {
            "Título del proyecto",
            "Nombres y apellidos del Jefe de Proyecto",
            "Correo electrónico del Jefe de Proyecto",
            "Tipo de Proyecto",
            "Total de miembros del proyectos",
            "Cantidad de miembros del proyectos que pertenecen a la UH",
            "Cantidad de estudiantes  del proyecto",
            "Cantidad de estudiantes contratados",
            "Clasificación del Proyecto (Básica, Aplicada, Experimental, Innovación)",
            "Tributa a la Formación Doctoral (cantidad de estudiantes de doctorado)",
            "Situación (aprobado e inicia este año, pendiente de aprobación, rechazado)",
        }
    )
    {

    }
    public override IEnumerable<(int Col, string Expression)> TemplateCells => new[]
    {
        (1,  "{{item.TituloProyecto}}"),
        (2,  "{{item.JefeProyecto}}"),
        (3,  "{{item.CorreoJefeProyecto}}"),
        (4,  "{{item.TipoProyecto}}"),
        (5,  "{{item.TotalMiembros}}"),
        (6,  "{{item.MiembrosUH}}"),
        (7,  "{{item.Estudiantes}}"),
        (8,  "{{item.EstudiantesContratados}}"),
        (9,  "{{item.Clasificacion}}"),
        (10,  "{{item.TributaFormacionDoctoral}}"),
        (11,  "{{item.Situacion}}"),
    };

    protected override void PostGenerate(IXLWorksheet ws)
    {
        for (int c = 1; c <= 11; c++) ws.Column(c).Width = 20;

        ws.SheetView.FreezeRows(4);
    }
}