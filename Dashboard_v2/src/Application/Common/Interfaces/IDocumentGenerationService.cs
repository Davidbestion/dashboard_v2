namespace Dashboard_v2.Application.Common.Interfaces;

/// <summary>
/// Contrato de renderizado de documentos Excel.
/// Recibe el nombre de la plantilla y las variables calculadas por un <see cref="Documents.IDocumentReport"/>,
/// y devuelve el archivo .xlsx como bytes.
/// La implementación (ClosedXML.Report) vive en Infrastructure; Application no tiene
/// dependencia directa de ninguna librería de hojas de cálculo.
/// </summary>
public interface IDocumentRenderer
{
    /// <summary>
    /// Carga la plantilla embebida, inyecta las variables en los Named Ranges
    /// y devuelve el archivo .xlsx generado como array de bytes.
    /// </summary>
    /// <param name="templateName">Nombre del archivo de plantilla sin extensión (ej. "AnexoGrupos").</param>
    /// <param name="variables">
    /// Diccionario de variables a inyectar. Cada clave debe coincidir con un Named Range en la plantilla.
    /// </param>
    byte[] Render(string templateName, IReadOnlyDictionary<string, object> variables);
}
