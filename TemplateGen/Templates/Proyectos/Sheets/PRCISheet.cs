using TemplateGen.Core.Base;
using ClosedXML.Excel;

namespace TemplateGen.Templates;

public class PRCISheet : ProyectosSheetBase
{
    public PRCISheet() : base(
        name: "PRCI",
        title: "Proyectos PRCI",
        headers: new[] 
        {
            "Código del proyecto",
            "Título del proyecto",
            "Fuente de financiación (País, Organismo, Agencia, etc.)",
            "Nombres y apellidos del Jefe de Proyecto",
            "Correo electrónico del Jefe de Proyecto",
            "Total de miembros del proyectos",
            "Cantidad de miembros del proyectos que pertenecen a la UH",
            "Cantidad de estudiantes  del proyecto",
            "Cantidad de estudiantes contratados",
            "Clasificación del Proyecto (Básica, Aplicada, Experimental, Innovación)",
            "Tributa a la Formación Doctoral (cantidad de estudiantes de doctorado)",
            "Tributa al Desarrollo Local",
            "Con Términos de referencia",
            "Contribución a Sectores estratégicos (Turismo; Producción de alimentos; Industria biotecnológica y famacéutica; Servicios profesionales en el exterior; Construcciones; Sector electroenergético, industria ligera, agroindustria azucarera, telcomunicaciones e informática,logistica redes hidráulitas y sanitarias, Otros servicios )",
            "Contribución a Ejes estratégicos (I.Gobierno eficaz y socialista e integración social; IITransformación productiva e inserción internacional; III. Infraestructura; IV.Potencial humano, Ciencia, tecnología e innovación; V.Recursos naturales y medio ambiente; VI.Desarrollo humano, justicia y equidad)",
            "Fecha inicio",
            "Fecha cierre",
            "Entidad Ejecutora Principal",
            "Entidad Ejecutora Participante",
            "Estado de ejecución del proyecto (normal, atrasado, cancelado, termina con este infome, se informa por primera vez)",
            "Publicaciones derivadas del proyecto (DOI / URL)",
        }
    )
    {

    }
    public override IEnumerable<(int Col, string Expression)> TemplateCells => new[]
    {
        (1,  "{{item.CodigoProyecto}}"),
        (2,  "{{item.TituloProyecto}}"),
        (3,  "{{item.FuenteFinanciacion}}"),
        (4,  "{{item.JefeProyecto}}"),
        (5,  "{{item.CorreoJefeProyecto}}"),
        (6,  "{{item.TotalMiembros}}"),
        (7,  "{{item.MiembrosUH}}"),
        (8,  "{{item.Estudiantes}}"),
        (9,  "{{item.EstudiantesContratados}}"),
        (10,  "{{item.Clasificacion}}"),
        (11,  "{{item.TributaFormacionDoctoral}}"),
        (12,  "{{item.TributaDesarrolloLocal}}"),
        (13,  "{{item.ConTerminosReferencia}}"),
        (14,  "{{item.ContribucionSectoresEstrategicos}}"),
        (15,  "{{item.ContribucionEjesEstrategicos}}"),
        (16,  "{{item.FechaInicio}}"),
        (17,  "{{item.FechaCierre}}"),
        (18,  "{{item.EntidadEjecutoraPrincipal}}"),
        (19,  "{{item.EntidadEjecutoraParticipante}}"),
        (20,  "{{item.EstadoEjecucion}}"),
        (21,  "{{item.PublicacionesDerivadas}}"),
    };

    protected override void PostGenerate(IXLWorksheet ws)
    {
        for (int c = 1; c <= 21; c++) ws.Column(c).Width = 20;

        ws.SheetView.FreezeRows(4);
    }
}