using System.Globalization;
using System.Text;

namespace Dashboard_v2.Domain.Common;

/// <summary>
/// Utility for normalizing text by removing diacritics (accents) and converting to lowercase.
/// Used to generate search keys for accent-tolerant lookup.
/// </summary>
public static class TextNormalizer
{
    /// <summary>
    /// Returns a lowercase, diacritic-free version of the input.
    /// NFD-decomposes to separate base chars from combining marks, removes non-spacing marks,
    /// then NFC-recomposes. Returns empty string for null or whitespace input.
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
