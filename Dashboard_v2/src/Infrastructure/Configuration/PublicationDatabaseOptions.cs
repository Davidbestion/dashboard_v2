using System.Collections.Generic;

namespace Dashboard_v2.Infrastructure.Configuration;

/// <summary>
/// Configuration options for the local-CSV provider within the publication
/// database resolution subsystem.
/// </summary>
public class PublicationDatabaseOptions
{
    /// <summary>
    /// Paths to Scimago/Scopus CSV files (semicolon-delimited SJR export).
    /// Replaces the legacy <see cref="LocalMappingFiles"/> property.
    /// </summary>
    public List<string> ScimagoFiles { get; set; } = new();

    /// <summary>
    /// Legacy alias for <see cref="ScimagoFiles"/>. Used when ScimagoFiles is empty.
    /// </summary>
    public List<string> LocalMappingFiles { get; set; } = new();

    /// <summary>
    /// Directory containing Clarivate Web of Science Master Journal List change
    /// Excel files (.xlsx). All .xlsx files in this directory are loaded and
    /// processed in chronological order (by filename).
    /// Typically: "data/wos"
    /// </summary>
    public string? WosDirectory { get; set; }
}
