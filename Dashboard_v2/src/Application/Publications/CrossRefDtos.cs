using System.Collections.Generic;

namespace Dashboard_v2.Application.Publications;

/// <summary>
/// Publication metadata fetched from the CrossRef API for import assistance.
/// </summary>
public class PublicationCrossRefDto
{
    public string? Doi { get; set; }
    public string? Url { get; set; }
    public string? Title { get; set; }
    public string? PublicationData { get; set; }
    public int? SuggestedPublicationType { get; set; }
    public List<string> Authors { get; set; } = new();
    public string? ContainerTitle { get; set; }
    public List<string> Issns { get; set; } = new();
    public List<string> Isbns { get; set; } = new();
    public string? Volume { get; set; }
    public string? Issue { get; set; }
    public string? Page { get; set; }
    public string? Published { get; set; }
    public string? Publisher { get; set; }
    public string? Type { get; set; }
}
