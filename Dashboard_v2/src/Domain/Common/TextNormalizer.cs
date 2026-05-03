using System.Globalization;
using System.Text;

namespace Dashboard_v2.Domain.Common;

/// <summary>
/// Normaliza cadenas de texto para búsquedas tolerantes a acentos y
/// diferencias de mayúsculas/minúsculas.
/// Ejemplo: "Damián" → "damian", "García López" → "garcia lopez"
/// </summary>
public static class TextNormalizer
{
    /// <summary>
    /// Devuelve la cadena en minúsculas y sin marcas diacríticas (tildes, diéresis, etc.).
    /// Apta para comparación en memoria y para almacenar como search key en la BD.
    /// </summary>
    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Descomponer en forma NFC → NFD para separar la letra base del diacrítico
        var normalized = input.Normalize(NormalizationForm.FormD);

        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat != UnicodeCategory.NonSpacingMark)
                sb.Append(char.ToLowerInvariant(c));
        }

        // Volver a NFC para que el string resultante sea canónico
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
