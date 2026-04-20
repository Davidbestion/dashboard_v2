using ClosedXML.Excel;

namespace TemplateGen.Templates;

/// <summary>
/// Genera la plantilla AnexoProyectos.xlsx para el Anexo X de Proyectos de Investigación.
/// </summary>
public static class AnexoProyectos
{
    private const string OutputPath =
    "../Dashboard_v2/src/Infrastructure/Templates/AnexoProyectos.xlsx";
    public static void Generate()
    {
        using var wb = new XLWorkbook();

        // Hoja de los PAPN
        var wsPAPN = wb.Worksheets.Add("PAPN");
        wsPAPN.Cell(1, 1).Value = "PAPN";
        wsPAPN.Range(1, 1, 1, 21).Merge();
        var titleStyle = wsPAPN.Cell(1, 1).Style;
        titleStyle.Font.Bold = true;
        titleStyle.Font.FontSize = 14;
        titleStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        titleStyle.Fill.BackgroundColor = XLColor.FromHtml("#1F3864");
        titleStyle.Font.FontColor = XLColor.White;

        var headers = new[]
        {
            "Código del proyecto",
            "Título del proyecto",
            "Nombre del Programa",
            "Nombres y apellidos del Jefe de Proyecto",
            "Correo electrónico del Jefe de Proyecto",
            "Total de miembros del proyectos",
            "Cantidad de miembros del proyectos que pertenecen a la UH",
            "Cantidad de estudiantes  del proyecto",
            "Cantidad de estudiantes contratados",
            "Clasificación del Proyecto (Básica, Aplicada, Experimental, Innovación)",
            "Tributa a la Formación Doctoral (cantidad de estudiantes de doctorado)",
            "Tributa al Desarrollo Local",
            "Contribución a Sectores estratégicos (Turismo; Producción de alimentos; Industria biotecnológica y famacéutica; Servicios profesionales en el exterior; Construcciones; Sector electroenergético, industria ligera, agroindustria azucarera, telcomunicaciones e informática,logistica redes hidráulitas y sanitarias, Otros servicios )",
            "Contribución a Ejes estratégicos (I.Gobierno eficaz y socialista e integración social; IITransformación productiva e inserción internacional; III. Infraestructura; IV.Potencial humano, Ciencia, tecnología e innovación; V.Recursos naturales y medio ambiente; VI.Desarrollo humano, justicia y equidad)",
            "Fecha inicio",
            "Fecha cierre",
            "Entidad Ejecutora Principal",
            "Entidad Ejecutora Participante",
            "Estado de ejecución del proyecto (normal, atrasado, cancelado, termina con este infome, se informa por primera vez)",
            "Publicaciones derivadas del proyecto (DOI / URL)",
        };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = wsPAPN.Cell(4, i + 1);
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
        wsPAPN.Row(4).Height = 120; 

        // ─── Fila 5: Expresiones ClosedXML.Report ─────────────────────────
        var templateCells = new (int Col, string Expression)[]
        {
            (1,  "{{item.CodigoProyecto}}"),
            (2,  "{{item.TituloProyecto}}"),
            (3,  "{{item.NombrePrograma}}"),
            (4,  "{{item.JefeProyecto}}"),
            (5,  "{{item.CorreoJefeProyecto}}"),
            (6,  "{{item.TotalMiembros}}"),
            (7,  "{{item.MiembrosUH}}"),
            (8,  "{{item.Estudiantes}}"),
            (9,  "{{item.EstudiantesContratados}}"),
            (10,  "{{item.Clasificacion}}"),
            (11,  "{{item.TributaFormacionDoctoral}}"),
            (12,  "{{item.TributaDesarrolloLocal}}"),
            (13,  "{{item.ContribucionSectoresEstrategicos}}"),
            (14,  "{{item.ContribucionEjesEstrategicos}}"),
            (15,  "{{item.FechaInicio:dd/MM/yyyy}}"),
            (16,  "{{item.FechaCierre:dd/MM/yyyy}}"),
            (17,  "{{item.EntidadEjecutoraPrincipal}}"),
            (18,  "{{item.EntidadEjecutoraParticipante}}"),
            (19,  "{{item.EstadoEjecucion}}"),
            (20,  "{{item.PublicacionesDerivadas}}"),

        };

        foreach (var (col, expr) in templateCells)
        {
            var cell = wsPAPN.Cell(5, col);
            cell.Value = expr;
            var s = cell.Style;
            s.Border.OutsideBorder = XLBorderStyleValues.Thin;
            s.Border.InsideBorder = XLBorderStyleValues.Thin;
            if (col >= 2 && col <= 18)
                s.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        for (int col = 1; col <= headers.Length; col++)
        {
            var s = wsPAPN.Cell(5, col).Style;
            s.Border.OutsideBorder = XLBorderStyleValues.Thin;
            s.Border.InsideBorder = XLBorderStyleValues.Thin;
        }

        // ─── Fila 6: Fila de servicio (requerida por ClosedXML.Report) ────
        wsPAPN.Cell(6, 1).Value = "";

        for (int col = 1; col <= headers.Length; col++)
        {
            wsPAPN.Column(col).Width = 35;
        }
        // ─── Named Range "Grupos" ──────────────────────────────────────────
        var rangoPAPN = wsPAPN.Range(5, 1, 6, headers.Length);
        wb.DefinedNames.Add("PAPN", rangoPAPN);




        // Hoja de los PAPS
        var wsPAPS = wb.Worksheets.Add("PAPS");

        // Aquí puedes agregar la lógica para llenar la plantilla con datos

        var rangoPAPS = wsPAPS.Range(5, 1, 6, 15);
        wb.DefinedNames.Add("PAPS", rangoPAPS);

        // Hoja de los PAPT
        var wsPAPT = wb.Worksheets.Add("PAPT");

        // Aquí puedes agregar la lógica para llenar la plantilla con datos

        var rangoPAPT = wsPAPT.Range(5, 1, 6, 15);
        wb.DefinedNames.Add("PAPT", rangoPAPT);

        // Hoja de los PE
        var wsPE = wb.Worksheets.Add("PE");

        // Aquí puedes agregar la lógica para llenar la plantilla con datos

        var rangoPE = wsPE.Range(5, 1, 6, 15);
        wb.DefinedNames.Add("PE", rangoPE);

        // Hoja de los PNE
        var wsPNE = wb.Worksheets.Add("PNE");

        // Aquí puedes agregar la lógica para llenar la plantilla con datos

        var rangoPNE = wsPNE.Range(5, 1, 6, 15);
        wb.DefinedNames.Add("PNE", rangoPNE);

        // Hoja de los PDL
        var wsPDL = wb.Worksheets.Add("PDL");

        // Aquí puedes agregar la lógica para llenar la plantilla con datos

        var rangoPDL = wsPDL.Range(5, 1, 6, 15);
        wb.DefinedNames.Add("PDL", rangoPDL);

        // Hoja de los PRCI
        var wsPRCI = wb.Worksheets.Add("PRCI");

        // Aquí puedes agregar la lógica para llenar la plantilla con datos

        var rangoPRCI = wsPRCI.Range(5, 1, 6, 15);
        wb.DefinedNames.Add("PRCI", rangoPRCI);

        //Hoja de los PNAP
        var wsPNAP = wb.Worksheets.Add("PNAP");

        // Aquí puedes agregar la lógica para llenar la plantilla con datos

        var rangoPNAP = wsPNAP.Range(5, 1, 6, 15);
        wb.DefinedNames.Add("PNAP", rangoPNAP);

        //Hoja de los Proyectos en Revision
        var wsRevision = wb.Worksheets.Add("Nuevas Aplicaciones");
        
        // Aquí puedes agregar la lógica para llenar la plantilla con datos

        var rangoRevision = wsRevision.Range(5, 1, 6, 15);
        wb.DefinedNames.Add("Nuevas Aplicaciones", rangoRevision);

        wb.SaveAs(OutputPath);
    }
}