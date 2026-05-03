namespace Dashboard_v2.Application.Publications;

/// <summary>
/// Maps bibliographic database names to the project's internal group classification (1–4).
/// This logic lives in the Application layer so it can be shared by any resolver or
/// provider without coupling business rules to Infrastructure.
/// </summary>
public static class PublicationGroupMapper
{
    /// <summary>
    /// Returns the group (1–4) for a given database name.
    /// <list type="bullet">
    ///   <item><term>1</term><description>Scopus, Web of Science (SCIE/SSCI/AHCI), Scimago.</description></item>
    ///   <item><term>2</term><description>SciELO, MEDLINE, Emerging Sources, Compendex, INSPEC, BIOSIS, CAB.</description></item>
    ///   <item><term>3</term><description>DOAJ, Latindex, Redalyc, LILACS, CLASE, ICYT, IME, Periódica.</description></item>
    ///   <item><term>4</term><description>Everything else (local/institutional databases).</description></item>
    /// </list>
    /// </summary>
    public static int MapToGroup(string? databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName)) return 4;
        var d = databaseName.ToLowerInvariant();

        if (d.Contains("scopus") || d.Contains("scimago") ||
            d.Contains("web of science") || d.Contains("scie") ||
            d.Contains("ssci") || d.Contains("ahci"))
            return 1;

        if (d.Contains("scielo") || d.Contains("medline") ||
            d.Contains("emerging") || d.Contains("chemical") ||
            d.Contains("biosis") || d.Contains("compendex") ||
            d.Contains("cab") || d.Contains("inspec"))
            return 2;

        if (d.Contains("doaj") || d.Contains("lilacs") ||
            d.Contains("redalyc") || d.Contains("latindex") ||
            d.Contains("ime") || d.Contains("icyt") ||
            d.Contains("periodica") || d.Contains("clase"))
            return 3;

        return 4;
    }

    /// <summary>Canonicalizes an ISSN to 8 uppercase digits without hyphen.</summary>
    public static string NormalizeIssn(string? issn)
    {
        if (string.IsNullOrWhiteSpace(issn)) return string.Empty;
        return issn.Trim().Replace("-", "").ToUpperInvariant();
    }
}
