using ClosedXML.Excel;
using TemplateGen.Core.Interfaces;

namespace TemplateGen.Templates;

/// <summary>
/// Hoja compuesta original del anexo 3 (mantenida como referencia visual).
/// Reemplazada por hojas individuales por sección en <see cref="AnexoEventosTemplate"/>.
/// </summary>
public sealed class EventosCientificosSheet : ISheetTemplate
{
    /// <summary>
    /// Nombre visible de la hoja.
    /// </summary>
    public string Name => "Eventos y actividades";

    /// <summary>
    /// No se utiliza en esta hoja compuesta.
    /// </summary>
    public string Title => string.Empty;

    /// <summary>
    /// No se utiliza en esta hoja compuesta.
    /// </summary>
    public string[] Headers => [];

    /// <summary>
    /// No se utiliza en esta hoja compuesta porque define varios rangos.
    /// </summary>
    public string RangeName => string.Empty;

    /// <summary>
    /// No se utiliza en esta hoja compuesta.
    /// </summary>
    public int StartRow => 0;

    /// <summary>
    /// No se utiliza en esta hoja compuesta.
    /// </summary>
    public int StartCol => 1;

    /// <summary>
    /// No se utiliza en esta hoja compuesta.
    /// </summary>
    public int EndRowOffset => 0;

    /// <summary>
    /// No se utiliza en esta hoja compuesta.
    /// </summary>
    public IEnumerable<(int Col, string Expression)> TemplateCells => [];

    /// <summary>
    /// Construye la hoja completa del anexo 3.
    /// </summary>
    /// <param name="workbook">Libro destino.</param>
    public void Generate(IXLWorkbook workbook)
    {
        var ws = workbook.Worksheets.Add(Name);

        ApplyLayout(ws);
        WriteTitle(ws);
        WriteInternationalEventsSection(ws);
        WriteNationalEventsSection(ws);
        WriteCoSponsoredEventsSection(ws);
        WriteUhActivitiesSection(ws);
        WritePresentationsSummarySection(ws);
        WriteKeynoteSection(ws);
        WritePresentationDetailsSection(ws);
        WriteNotes(ws);
    }

    /// <summary>
    /// Ajusta columnas y propiedades generales de la hoja.
    /// </summary>
    /// <param name="ws">Hoja a configurar.</param>
    private static void ApplyLayout(IXLWorksheet ws)
    {
        ws.Column(1).Width = 43;
        ws.Column(2).Width = 34;
        ws.Column(3).Width = 18;
        ws.Column(4).Width = 20;
    }

    /// <summary>
    /// Escribe el título principal del anexo.
    /// </summary>
    /// <param name="ws">Hoja destino.</param>
    private static void WriteTitle(IXLWorksheet ws)
    {
        EventosSheetHelper.WriteMergedTextRow(
            ws,
            1,
            1,
            4,
            "Anexo 3.  EVENTOS Y ACTIVIDADES CIENTIFICAS BALANCE 2025",
            bold: true);

        ws.Row(1).Height = 22.5;
    }

    /// <summary>
    /// Construye la tabla de eventos internacionales.
    /// </summary>
    /// <param name="ws">Hoja destino.</param>
    private static void WriteInternationalEventsSection(IXLWorksheet ws)
    {
        EventosSheetHelper.WriteMergedTextRow(
            ws,
            3,
            1,
            4,
            "1.  Eventos cientificos internacionales (en el extranjero o en Cuba) en los que profesores o investigadores del area han tenido cualquier tipo de participacion:",
            bold: true);

        EventosSheetHelper.WriteHeaderRow(
            ws,
            5,
            ["Nombre del evento internacional", "Pais, si fue en el extranjero", "En Cuba"]);

        EventosSheetHelper.WriteTemplateRange(
            ws,
            "EventosInternacionales",
            6,
            3,
            [
                (1, "{{item.NombreEventoInternacional}}"),
                (2, "{{item.PaisSiFueEnElExtranjero}}"),
                (3, "{{item.EnCuba}}"),
            ]);
    }

    /// <summary>
    /// Construye la tabla de eventos nacionales.
    /// </summary>
    /// <param name="ws">Hoja destino.</param>
    private static void WriteNationalEventsSection(IXLWorksheet ws)
    {
        EventosSheetHelper.WriteMergedTextRow(
            ws,
            12,
            1,
            4,
            "2. Eventos cientificos nacionales (en Cuba) en los que profesores o investigadores del area han tenido cualquier tipo de participacion:",
            bold: true);

        EventosSheetHelper.WriteHeaderRow(
            ws,
            14,
            ["Nombre del evento nacional en Cuba", "Institucion que lo organizo"]);

        EventosSheetHelper.WriteTemplateRange(
            ws,
            "EventosNacionales",
            15,
            2,
            [
                (1, "{{item.NombreEventoNacional}}"),
                (2, "{{item.InstitucionQueLoOrganizo}}"),
            ]);
    }

    /// <summary>
    /// Construye la tabla de eventos coauspiciados por el area.
    /// El rango se conserva para que el anexo mantenga el formato oficial,
    /// aunque hoy su llenado sigue siendo manual.
    /// </summary>
    /// <param name="ws">Hoja destino.</param>
    private static void WriteCoSponsoredEventsSection(IXLWorksheet ws)
    {
        EventosSheetHelper.WriteMergedTextRow(
            ws,
            20,
            1,
            4,
            "3. Eventos cientificos coauspiciados por el area:",
            bold: true);

        EventosSheetHelper.WriteHeaderRow(
            ws,
            22,
            ["Evento coauspiciado", "Institucion externa a la UH responsable del evento", "Internacional", "Nacional"]);

        EventosSheetHelper.WriteTemplateRange(
            ws,
            "EventosCoauspiciados",
            23,
            4,
            [
                (1, "{{item.EventoCoauspiciado}}"),
                (2, "{{item.InstitucionExternaResponsable}}"),
                (3, "{{item.Internacional}}"),
                (4, "{{item.Nacional}}"),
            ]);
    }

    /// <summary>
    /// Construye la tabla de actividades cientificas organizadas por el area en la UH.
    /// El rango se conserva para que el anexo mantenga el formato oficial,
    /// aunque hoy su llenado sigue siendo manual.
    /// </summary>
    /// <param name="ws">Hoja destino.</param>
    private static void WriteUhActivitiesSection(IXLWorksheet ws)
    {
        EventosSheetHelper.WriteMergedTextRow(
            ws,
            29,
            1,
            4,
            "4. Actividades cientificas organizadas por el area en la UH:",
            bold: true);

        EventosSheetHelper.WriteHeaderRow(ws, 31, ["Actividad cientifica"]);

        EventosSheetHelper.WriteTemplateRange(
            ws,
            "ActividadesCientificasUH",
            32,
            1,
            [
                (1, "{{item.ActividadCientifica}}"),
            ]);
    }

    /// <summary>
    /// Construye la tabla-resumen de ponencias presentadas.
    /// Solo la columna de cantidades base se rellena automaticamente. El renglón
    /// de actividades en la UH queda en cero porque el modelo actual no conserva
    /// esa clasificacion, y las otras dos columnas siguen sin poder inferirse.
    /// </summary>
    /// <param name="ws">Hoja destino.</param>
    private static void WritePresentationsSummarySection(IXLWorksheet ws)
    {
        EventosSheetHelper.WriteMergedTextRow(
            ws,
            37,
            1,
            4,
            "5. Ponencias presentadas:",
            bold: true);

        EventosSheetHelper.WriteHeaderRow(
            ws,
            39,
            ["", "Cantidad de ponencias", "Cantidad donde 1er autor es del area", "Cantidad con autores de otras areas de la UH"]);

        var labels = new Dictionary<int, string>
        {
            [40] = "En eventos internacionales en extranjero",
            [41] = "En eventos internacionales en Cuba",
            [42] = "En eventos nacionales en Cuba",
            [43] = "En actividades cientificas celebradas en la UH",
            [44] = "TOTAL",
        };

        var valueExpressions = new Dictionary<int, string>
        {
            [40] = "{{PonenciasInternacionalesExtranjero}}",
            [41] = "{{PonenciasInternacionalesCuba}}",
            [42] = "{{PonenciasNacionalesCuba}}",
            [43] = "{{PonenciasActividadesUH}}",
            [44] = "{{PonenciasTotal}}",
        };

        foreach (var (row, label) in labels)
        {
            ws.Cell(row, 1).Value = label;
            EventosSheetHelper.ApplyTextStyle(ws.Cell(row, 1), bold: row == 44);

            ws.Cell(row, 2).Value = valueExpressions[row];
            EventosSheetHelper.ApplyTextStyle(ws.Cell(row, 2), bold: row == 44, center: true);

            for (int col = 1; col <= 4; col++)
            {
                EventosSheetHelper.ApplyThinBorder(ws.Cell(row, col));
            }
        }
    }

    /// <summary>
    /// Construye la tabla de conferencias magistrales.
    /// Se deja vacia porque el dominio actual no diferencia ese tipo de presentacion.
    /// </summary>
    /// <param name="ws">Hoja destino.</param>
    private static void WriteKeynoteSection(IXLWorksheet ws)
    {
        EventosSheetHelper.WriteMergedTextRow(
            ws,
            47,
            1,
            4,
            "6. Conferencias magistrales impartidas:",
            bold: true);

        EventosSheetHelper.WriteHeaderRow(ws, 49, ["", "Cantidad"]);

        var labels = new Dictionary<int, string>
        {
            [50] = "En eventos internacionales en extranjero",
            [51] = "En eventos internacionales en Cuba",
            [52] = "En eventos nacionales en Cuba",
            [53] = "En actividades cientificas celebradas en la UH",
            [54] = "En instituciones extranjeras",
            [55] = "En instituciones cubanas",
            [56] = "TOTAL",
        };

        foreach (var (row, label) in labels)
        {
            ws.Cell(row, 1).Value = label;
            EventosSheetHelper.ApplyTextStyle(ws.Cell(row, 1), bold: row == 56);

            for (int col = 1; col <= 2; col++)
            {
                EventosSheetHelper.ApplyThinBorder(ws.Cell(row, col));
            }
        }
    }

    /// <summary>
    /// Construye la tabla detallada de ponencias.
    /// </summary>
    /// <param name="ws">Hoja destino.</param>
    private static void WritePresentationDetailsSection(IXLWorksheet ws)
    {
        EventosSheetHelper.WriteMergedTextRow(
            ws,
            58,
            1,
            4,
            "7. Datos de ponencias presentadas en eventos y actividades cientificas.",
            bold: true);

        EventosSheetHelper.WriteHeaderRow(
            ws,
            59,
            ["Nombre de la ponencia", "Nombre de autores", "Nombre del evento o actividad cientifica", "Pais de celebracion"]);

        EventosSheetHelper.WriteTemplateRange(
            ws,
            "DatosPonencias",
            60,
            4,
            [
                (1, "{{item.NombrePonencia}}"),
                (2, "{{item.NombreAutores}}"),
                (3, "{{item.NombreEventoOActividadCientifica}}"),
                (4, "{{item.PaisDeCelebracion}}"),
            ]);
    }

    /// <summary>
    /// Escribe las notas finales incluidas en el modelo de referencia.
    /// </summary>
    /// <param name="ws">Hoja destino.</param>
    private static void WriteNotes(IXLWorksheet ws)
    {
        ws.Cell(67, 1).Value = "NOTAS:";
        EventosSheetHelper.ApplyTextStyle(ws.Cell(67, 1), bold: true);

        ws.Cell(68, 1).Value = "- El anexo se entrega en Word en el mismo modelo del anexo.";
        ws.Cell(69, 1).Value = "- Informar solo lo que se pide en cada punto.";
        ws.Cell(70, 1).Value = "- Cualquier duda comunicarse con Manuel Alvarez (DCT) por WhatsApp 53026650.";
        ws.Cell(71, 1).Value = "- Los apartados 3 y 4 se mantienen para llenado manual: el sistema no modela eventos coauspiciados ni actividades cientificas internas en la UH.";

        EventosSheetHelper.ApplyTextStyle(ws.Cell(68, 1));
        EventosSheetHelper.ApplyTextStyle(ws.Cell(69, 1));
        EventosSheetHelper.ApplyTextStyle(ws.Cell(70, 1));
        EventosSheetHelper.ApplyTextStyle(ws.Cell(71, 1));
    }
}
