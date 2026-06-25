namespace Dashboard_v2.Domain.Entities;

/// <summary>Academic scientific degree held by a user (None, Licenciado, Master, Doctor).</summary>
public enum ScientificCategory
{
    /// <summary>No scientific degree.</summary>
    None = 0,
    /// <summary>Bachelor/Licenciado degree.</summary>
    Licenciado = 1,
    /// <summary>Master's degree.</summary>
    Master = 2,
    /// <summary>PhD/Doctorate.</summary>
    Doctor = 3,
    //Other = 4
}

/// <summary>Display name mappings for ScientificCategory (Spanish locale). Used for presentation only.</summary>
public static class ScientificCategoryExtensions
{
    public static string ToDisplayString(this ScientificCategory category)
    {
        return category switch
        {
            ScientificCategory.None => "Sin categoría científica",
            ScientificCategory.Licenciado => "Licenciado",
            ScientificCategory.Master => "Master",
            ScientificCategory.Doctor => "Doctor",
            //ScientificCategory.Other => "Otro",
            _ => "Desconocido"
        };
    }
}