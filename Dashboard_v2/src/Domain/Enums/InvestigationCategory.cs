namespace Dashboard_v2.Domain.Entities;

/// <summary>Research professorship category assigned by the institution.</summary>
public enum InvestigationCategory
{
    /// <summary>No investigation category assigned.</summary>
    None = 0,
    /// <summary>Senior researcher (Investigador Titular).</summary>
    Titular = 1,
    /// <summary>Associate researcher (Investigador Auxiliar).</summary>
    Auxiliar = 2,
    /// <summary>Aggregate researcher (Investigador Agregado).</summary>
    Agregado = 3,
    /// <summary>Junior/affiliated researcher (Investigador Asociado).</summary>
    Asociado = 4,
}

/// <summary>Display name mappings for InvestigationCategory (Spanish locale). Used for presentation only.</summary>
public static class InvestigationCategoryExtensions
{
    public static string ToDisplayString(this InvestigationCategory category)
    {
        return category switch
        {
            InvestigationCategory.None => "Sin categoría de investigación",
            InvestigationCategory.Titular => "Investigador Titular",
            InvestigationCategory.Auxiliar => "Investigador Auxiliar",
            InvestigationCategory.Agregado => "Investigador Agregado",
            InvestigationCategory.Asociado => "Investigador Asociado",
            _ => "Desconocida"
        };
    }
}