namespace Dashboard_v2.Domain.Enums;

/// <summary>Classification of an academic publication (e.g. journal article, book chapter, conference paper).</summary>
public enum PublicationType
{
    /// <summary>Article published in a peer-reviewed scientific journal.</summary>
    Artículo_en_Revista_Científica = 0,
    /// <summary>Full academic or scientific book.</summary>
    Libro = 1,
    /// <summary>Monograph: in-depth study on a single topic.</summary>
    Monografía = 2,
    /// <summary>Chapter or section within a collective book.</summary>
    Capítulo = 3,
    /// <summary>Popular-science or outreach article aimed at a general audience.</summary>
    Artículo_de_Divulgación = 4
}
