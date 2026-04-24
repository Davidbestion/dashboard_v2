using ClosedXML.Excel;
using TemplateGen.Core.Interfaces;

namespace TemplateGen.Templates;

/// <summary>
/// Primera hoja del anexo de publicaciones.
/// Es informativa y resume instrucciones y métricas, por lo que no contiene
/// ningún rango dinámico para ClosedXML.Report.
/// </summary>
public sealed class ListadoPublicacionesSheet : ISheetTemplate
{
    /// <summary>
    /// Nombre de la hoja.
    /// </summary>
    public string Name => "Listado de las publicaciones";

    /// <summary>
    /// No se utiliza en esta hoja informativa.
    /// </summary>
    public string Title => string.Empty;

    /// <summary>
    /// No se utiliza en esta hoja informativa.
    /// </summary>
    public string[] Headers => [];

    /// <summary>
    /// No se utiliza en esta hoja informativa.
    /// </summary>
    public string RangeName => string.Empty;

    /// <summary>
    /// No se utiliza en esta hoja informativa.
    /// </summary>
    public int StartRow => 0;

    /// <summary>
    /// No se utiliza en esta hoja informativa.
    /// </summary>
    public int StartCol => 1;

    /// <summary>
    /// No se utiliza en esta hoja informativa.
    /// </summary>
    public int EndRowOffset => 0;

    /// <summary>
    /// No se utiliza en esta hoja informativa.
    /// </summary>
    public IEnumerable<(int Col, string Expression)> TemplateCells => [];

    /// <summary>
    /// Construye la hoja de resumen e instrucciones copiando la información
    /// textual del anexo de referencia sin añadir relleno automático.
    /// </summary>
    /// <param name="workbook">Libro destino.</param>
    public void Generate(IXLWorkbook workbook)
    {
        var ws = workbook.Worksheets.Add(Name);

        ApplyLayout(ws);
        WriteIntroSection(ws);
        WriteSummaryTable(ws);
        WriteGuidanceSection(ws);
    }

    /// <summary>
    /// Ajusta columnas y configura la hoja para lectura cómoda.
    /// </summary>
    /// <param name="ws">Hoja a configurar.</param>
    private static void ApplyLayout(IXLWorksheet ws)
    {
        ws.Column(1).Width = 57.875;
        ws.Column(2).Width = 14.5;
        ws.Column(3).Width = 14.5;
        ws.Column(4).Width = 14.125;
        ws.Column(5).Width = 14;
        ws.Column(6).Width = 14;
    }

    /// <summary>
    /// Escribe el bloque inicial descriptivo del anexo.
    /// </summary>
    /// <param name="ws">Hoja destino.</param>
    private static void WriteIntroSection(IXLWorksheet ws)
    {
        ws.Cell(2, 1).Value = "Anexo 2. Publicaciones.";
        PublicationSheetHelper.ApplyTextStyle(ws.Cell(2, 1), bold: true);

        PublicationSheetHelper.WriteMergedTextRow(
            ws,
            3,
            1,
            6,
            "Chequeo anual de la productividad científica mediante publicaciones (artículos originales, artículos de revisión, artículos de divulgación científico-técnica, ponencias en memorias de eventos, libros, monografías, capítulos de libros y otros documentos que comuniquen los resultados de investigación de las áreas).");
        PublicationSheetHelper.WriteMergedTextRow(
            ws,
            4,
            1,
            6,
            "Deben compilar la información relacionada con esta esfera que corresponda a los documentos publicados durante el pasado año que no fueron reportados al balance del 2024 más todos los que han sido publicado de enero a octubre de 2024.");
        PublicationSheetHelper.WriteMergedTextRow(
            ws,
            5,
            1,
            6,
            "Para este corte no deben reportar los documentos aceptados para publicación que no han terminado aún el proceso editorial, estos deberán reportalos, una vez publicados y si ya se cerró toda la información para este balance, queda para el próximo.");

        ws.Row(3).Height = 76.5;
        ws.Row(4).Height = 49.5;
        ws.Row(5).Height = 60.75;
    }

    /// <summary>
    /// Reproduce la tabla resumen de la primera hoja, dejándola vacía para
    /// completado manual tal como solicitó el usuario.
    /// </summary>
    /// <param name="ws">Hoja destino.</param>
    private static void WriteSummaryTable(IXLWorksheet ws)
    {
        var headers = new[]
        {
            "Descripción",
            "Compromiso",
            "Publicados",
            "Total y porcentaje de cumplimiento.",
        };

        PublicationSheetHelper.WriteHeaderRow(ws, 6, headers);
        ws.Row(6).Height = 48;

        var descriptions = new Dictionary<int, string>
        {
            [7] = "grupo 1",
            [8] = "grupo 2",
            [9] = "grupo 3",
            [10] = "grupo 4",
            [11] = "capitulo de libros",
            [12] = "libros y monografias",
            [13] = "Cantidad de publicaciones científicas en colaboración con autores extranjeros.",
            [14] = "Cantidad de publicaciones científicas en colaboración donde el autor para correspondencia es de la UH.",
            [15] = "Cantidad de artículos de revista de divulgación.",
            [16] = "Cantidad de artículos en medios de prensa (digitales o impresos) y blogs que publican sobre resultados de la actividad científica",
            [17] = "Cantidad de publicaciones en coautoría con el sector productivo y de los servicios.",
            [18] = "Cantidad de ponencias de eventos nacionales e internacionales publicadas con ISSN o ISBN",
        };

        foreach (var (row, text) in descriptions)
        {
            ws.Cell(row, 1).Value = text;
            PublicationSheetHelper.ApplyTextStyle(ws.Cell(row, 1));

            for (int col = 1; col <= 4; col++)
            {
                PublicationSheetHelper.ApplyThinBorder(ws.Cell(row, col));
            }
        }

        ws.Row(16).Height = 32.25;
        ws.Row(18).Height = 99.75;
    }

    /// <summary>
    /// Escribe las instrucciones extendidas incluidas en la hoja de referencia.
    /// </summary>
    /// <param name="ws">Hoja destino.</param>
    private static void WriteGuidanceSection(IXLWorksheet ws)
    {
        var notes = new Dictionary<int, string>
        {
            [23] = "Sobre el reporte de artículos en publicaciones seriadas (revistas)",
            [24] = "Información que no puede faltar (deben aparecer todos estos datos):",
            [25] = "1.     Título del artículo",
            [26] = "2.     Año de publicación",
            [27] = "3.     Autores",
            [28] = "-         En el orden de aparición, Formato: Primer apellido-segundo apellido, Inicial del nombre, si el autor tiene nombre compuesto entre ambas iniciales se ubica un punto.",
            [29] = "-         Subrayar al autor de correspondencia.",
            [30] = "-         Los autores de la UH se resaltan en negritas. Después de cada nombre entre paréntesis indicar la abreviatura del área universitaria a la que pertenece el autor (Ej, fbio: Facultad de biología), puede ser el dominio de cada área en el correo electrónico institucional.",
            [31] = "4.     Nombre completo de la revista y datos de la misma, NO USAR ABREVIATURAS (volumen, número, páginas, etc)",
            [32] = "5.     ISSN electrónico (eISSN) e impreso (Print), si procede, reportar los dos.",
            [33] = "6.     URL DOI si procede o URL que redireccionen al sitio de la publicación si no tiene DOI, es importante que sea el link directo a la publicación y no la dirección del sitio Web de la revista, del Research Gate del investigador o de repositorios, etc.",
            [34] = "7.     Organizar los listados en orden alfabético en orden creciente (A a la Z)",
            [36] = "Ejemplo 1: Artículo con DOI",
            [37] = "Analysis of body condition indices reveals different ecotypes of the Antillean manatee (2024) Castelblanco-Martínez, D.N, …., Álvarez-Alemán, A (cim).  Scientific Reports, 11(1), art. no. 19451. DOI URL: https://doi.org/10.1038/s41598-021-98890-0 Poner acá el (los) ISSN entre paréntesis (eISSN: 2045-2322).",
            [39] = "Ejemplo 2: Artículo sin DOI",
            [40] = "Calidad biofarmacéutica de tabletas de atenolol 100 mg (2024) Pérez-Naranjo, A, …, Fernández-Cervera, M (ifal), Pérez-Ricardo, D (ifal). Revista Cubana de Farmacia, 54(3), art. no. e553. URL: http://www.revfarmacia.sld.cu/index.php/far/article/view/553 (print ISSN: 0034-7515, eISSN: 0034-7515).",
            [42] = "Sobre el reporte de libros capítulos y monografías",
            [43] = "1. Título del libro, capítulo o monografía. En el caso de los capítulos en necesario reflejar el título del libro donde aparece publicado.",
            [44] = "2. Año de publicación",
            [45] = "3. Autores, En el orden de aparición, Formato: Primer apellido-Segundo apellido, Inicial del nombre, si el autor tiene nombre compuesto entre ambas iniciales se ubica un punto. Los autores de la UH se resaltan en negritas.",
            [46] = "3. Nombre completo de la Editorial (NO USAR ABREVIATURAS).",
            [47] = "4. Datos de la publicación: Ciudad y país de publicación, año.",
            [48] = "5. ISBN electrónico e impreso si procede.",
            [49] = "6. URL DOI si procede o URL del libro, capítulo o monografía si no tiene DOI (Es importante que sea el link directo a la publicación y no la dirección del sitio Web de la revista).",
            [51] = "Ejemplo 3: Capitulo de libro",
            [52] = "Accumulation of benzoic acid derivatives in sugarcane tissues as an active defense against Xanthomonasalbilineans Capítulo en el libro: Sugarcane: Production, Properties and Uses (2024) de Armas- Urquiza, R (fbio) Editorial Nova Science Publishers, Inc. New York, USA ISBN:978-1-53618-417-4 DOI URL o URL: https://novapublishers.com/shop/sugarcane-production-properties-and-uses/",
            [54] = "Nota: Los libros y monografías se reportan como:",
            [55] = "Sugarcane: Production, Properties and Uses (2024) de Armas- Urquiza, R (fbio) Editorial Nova Science Publishers, Inc. New York, USA ISBN:978-1-53618-417-4 DOI URL o URL: https://novapublishers.com/shop/sugarcane-production-properties-and-uses/",
            [57] = "Las ponencias publicadas en Memorias de Eventos se reportan del mismo modo que los artículos, no debe faltar la siguiente información:",
            [58] = "1.     Título del artículo, ponencia o conferencia",
            [59] = "2.     Año de publicación entre paréntesis",
            [60] = "3.     Autor(es): apellido(s) e inicial del nombre.",
            [61] = "4.     Colocar la expresión En seguida del nombre del editor: inicial del nombre y apellido(s), seguido de punto",
            [62] = "5.     Coloca la expresión Ed. después del nombre del editor seguido de coma",
            [63] = "6.     Nombre del congreso, simposio, reunión, jornada en letra cursiva (con la inicial del nombre en mayúsculas)",
            [64] = "7.     Páginas donde aparece publicada la contribución entre paréntesis, seguido de un punto",
            [65] = "8.     Ciudad y/ país seguido de dos puntos",
            [66] = "9.     Editorial y punto final",
            [67] = "10.  ISBN electrónico e impreso si procede",
            [68] = "11.  URL DOI si procede o URL del libro, capítulo o monografía si no tiene DOI (Es importante que sea el link directo a la publicación y no la dirección del sitio Web de la revista)",
            [70] = "Ejemplo 5: Ponencias en memorias",
            [71] = "No se habla de Bruno y otras canciones de Disney que han roto todos los records: el efecto animado en la industria musical (2024) Jhon, E En: L.M. Miranda (ed.) Caja de pandora. Una mirada analítica a la industria musical moderna. (pp. 60-75). Harper Collins. https://doi.org/123456789",
            [73] = "Ejemplo 6: Artículos de prensa, blogs, etc,",
            [74] = "Son de ladera: apuesta musical para una vida sin violencia en Cali (Diciembre 3, 2018) Hurtado, S. y Zúñiga, V.. El Giro: periodismo reflexivo. URL: https://periodicoelgiro.com/ciudad/son-de-ladera-apuesta-musical-para-una-vida-sin-violencia-en-cali/",
        };

        foreach (var (row, text) in notes)
        {
            ws.Cell(row, 1).Value = text;
            PublicationSheetHelper.ApplyTextStyle(ws.Cell(row, 1), bold: row is 23 or 36 or 39 or 42 or 51 or 54 or 57 or 70 or 73);
        }
    }
}
