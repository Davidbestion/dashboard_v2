using ClosedXML.Report;
using Dashboard_v2.Application.Common.Interfaces;

namespace Dashboard_v2.Infrastructure.Services;

/// <summary>
/// Implementación del renderizador de documentos Excel usando ClosedXML.Report.
/// Carga la plantilla .xlsx embebida, inyecta las variables en los Named Ranges
/// y devuelve el archivo generado como bytes.
///
/// Esta clase NO contiene lógica de negocio ni conoce la estructura de ningún
/// reporte concreto. Cada reporte define sus propias variables en su clase
/// <see cref="IDocumentReport"/> correspondiente.
/// </summary>
public sealed class DocumentRenderer : IDocumentRenderer
{
    // TODO(david): 'anexo-eventos' no es estable con este renderer genérico basado en
    // ClosedXML.Report porque su hoja mezcla varias tablas dinámicas, merges y bloques fijos.
    // Opciones para arreglarlo:
    // 1. Sacar 'anexo-eventos' de este renderer y generar el workbook manualmente.
    // 2. Mantener el .xlsx solo como referencia visual y rellenarlo celda por celda.
    // 3. Reestructurar el anexo en varias hojas si el formato final lo permite.
    // 4. Rediseñar la plantilla para evitar desplazamientos automáticos de filas.
    // Prefijo del recurso embebido: [AssemblyName].[ruta relativa con puntos]
    private const string ResourcePrefix = "Dashboard_v2.Infrastructure.Templates.";

    /// <inheritdoc />
    public byte[] Render(string templateName, IReadOnlyDictionary<string, object> variables)
    {
        using var templateStream = LoadEmbeddedTemplate(templateName);
        var template = new XLTemplate(templateStream);

        foreach (var (name, value) in variables)
            template.AddVariable(name, value);

        template.Generate();

        using var output = new MemoryStream();
        template.SaveAs(output);
        return output.ToArray();
    }

    private static Stream LoadEmbeddedTemplate(string templateName)
    {
        var resourceName = $"{ResourcePrefix}{templateName}.xlsx";
        var assembly = typeof(DocumentRenderer).Assembly;
        return assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Plantilla embebida '{resourceName}' no encontrada. " +
                $"Verifica que Infrastructure/Templates/{templateName}.xlsx exista " +
                "y esté marcada como EmbeddedResource en Infrastructure.csproj.");
    }
}
