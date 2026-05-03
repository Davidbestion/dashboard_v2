using System.Linq;
using System.Text;

namespace Dashboard_v2.Application.Publications;

public static class CrossRefToPublicationMapper
{
    public static string BuildPublicationData(PublicationCrossRefDto dto)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(dto.ContainerTitle))
            sb.AppendLine($"Container: {dto.ContainerTitle}");

        if (dto.Issns?.Any() == true)
            sb.AppendLine($"ISSN: {string.Join(", ", dto.Issns)}");

        if (dto.Isbns?.Any() == true)
            sb.AppendLine($"ISBN: {string.Join(", ", dto.Isbns)}");

        if (!string.IsNullOrWhiteSpace(dto.Volume))
            sb.AppendLine($"Volume: {dto.Volume}");

        if (!string.IsNullOrWhiteSpace(dto.Issue))
            sb.AppendLine($"Issue: {dto.Issue}");

        if (!string.IsNullOrWhiteSpace(dto.Page))
            sb.AppendLine($"Pages: {dto.Page}");

        if (!string.IsNullOrWhiteSpace(dto.Publisher))
            sb.AppendLine($"Publisher: {dto.Publisher}");

        if (!string.IsNullOrWhiteSpace(dto.Published))
            sb.AppendLine($"Published: {dto.Published}");

        if (dto.Authors?.Any() == true)
            sb.AppendLine($"Authors: {string.Join("; ", dto.Authors)}");

        return sb.ToString().TrimEnd();
    }

    public static Dashboard_v2.Domain.Enums.PublicationType? MapCrossRefTypeToPublicationType(string? crossrefType)
    {
        if (string.IsNullOrWhiteSpace(crossrefType))
            return null;

        return crossrefType switch
        {
            "journal-article" => Dashboard_v2.Domain.Enums.PublicationType.Diario,
            "book" => Dashboard_v2.Domain.Enums.PublicationType.Libro,
            "monograph" => Dashboard_v2.Domain.Enums.PublicationType.Monografía,
            "book-chapter" => Dashboard_v2.Domain.Enums.PublicationType.Capítulo,
            _ => null
        };
    }
}
