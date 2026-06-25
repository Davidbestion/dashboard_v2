namespace Dashboard_v2.Domain.Entities;

/// <summary>Teaching rank of a faculty member.</summary>
public enum TeachingCategory
{
    /// <summary>No teaching category assigned.</summary>
    None = 0,
    /// <summary>Full professor (Profesor Titular).</summary>
    Titular = 1,
    /// <summary>Associate professor (Profesor Auxiliar).</summary>
    Auxiliar = 2,
    /// <summary>Assistant professor (Profesor Asistente).</summary>
    Asistente = 3,
    /// <summary>Teaching assistant / instructor in training.</summary>
    Instructor = 4,
}

/// <summary>Display name mappings for TeachingCategory (Spanish locale). Used for presentation only.</summary>
public static class TeachingCategoryExtensions
{
    public static string ToDisplayString(this TeachingCategory category)
    {
        return category switch
        {
            TeachingCategory.None => "Sin categoría docente",
            TeachingCategory.Titular => "Profesor Titular",
            TeachingCategory.Auxiliar => "Profesor Auxiliar",
            TeachingCategory.Asistente => "Profesor Asistente",
            TeachingCategory.Instructor => "Instructor",
            _ => "Desconocida"
        };
    }
}