using System.Collections.Generic;

namespace Dashboard_v2.Application.Publications;

/// <summary>
/// Result of attempting to resolve which bibliographic database(s) a journal
/// (identified by ISSN) is covered by, together with the derived Group and
/// optional cuartil information.
/// </summary>
public class PublicationDatabaseMatchDto
{
    /// <summary>
    /// ISSNs returned by CrossRef for this journal. Populated even when no
    /// database provider found a match, so the client can display them.
    /// </summary>
    public List<string> Issns { get; set; } = [];

    /// <summary>
    /// Primary database name determined for the journal, e.g. "Scopus", "DOAJ",
    /// "SciELO", "MEDLINE". Null when no match found.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// Determined group according to local policy (1..4). Null when unknown.
    /// </summary>
    public int? Group { get; set; }

    /// <summary>
    /// When available, cuartil value such as "Q1".."Q4" (from SJR or similar).
    /// </summary>
    public string? Cuartil { get; set; }

    /// <summary>
    /// Source that provided the match (e.g. "LocalCsv:scopus.csv" or "DOAJ API").
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Confidence score (0..1) for heuristic ranking of multiple matches.
    /// </summary>
    public double Confidence { get; set; } = 0.0;

    /// <summary>
    /// True when the journal is indexed in both ESCI (Group 2) and a main WoS index
    /// (SCIE/SSCI/AHCI, Group 1) and the correct group depends on the publication date
    /// relative to the journal's promotion. The client should let the user choose.
    /// </summary>
    public bool AmbiguousGroup { get; set; } = false;

    /// <summary>
    /// Approximate date when the journal first gained a main WoS index (promotion date),
    /// derived from the name of the change file where the transition was detected.
    /// Null when the promotion date could not be determined from the available files.
    /// Only populated when <see cref="AmbiguousGroup"/> is true.
    /// </summary>
    public DateOnly? PromotionDate { get; set; }

    /// <summary>
    /// Optional human-readable message explaining why the resolution was partial or
    /// unsuccessful (e.g. no ISSNs found for a proceedings article).
    /// </summary>
    public string? Message { get; set; }
}
