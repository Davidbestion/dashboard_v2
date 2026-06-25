namespace Dashboard_v2.Application.Common.Interfaces;

/// <summary>
/// Contract for generating institutional Excel reports from domain data.
/// Implementations receive a template stream and variable dictionary and return rendered .xlsx bytes.
/// </summary>
public interface ICustomDocumentRenderer
{
    /// <summary>Identifier for the template this renderer handles (without file extension).</summary>
    string TemplateName { get; }

    /// <summary>
    /// Renders the given template stream with the supplied variables and returns the resulting .xlsx bytes.
    /// </summary>
    /// <param name="templateStream">Stream of the .xlsx template file.</param>
    /// <param name="variables">Named range variables to inject into the template.</param>
    byte[] Render(Stream templateStream, IReadOnlyDictionary<string, object> variables);
}
