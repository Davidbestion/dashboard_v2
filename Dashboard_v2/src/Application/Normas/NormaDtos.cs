using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.Normas;

public record NormaDto(string Id, string Titulo, int? TipoNormaId, string? TipoNormaNombre, string InstitutionId, string InstitutionNombre, List<string> Creadores, List<CreatorDto> CreadoresDetalle);

public record CreateNormaBody(
    string Titulo,
    int? TipoNormaId,
    string InstitutionId,
    List<string>? AdditionalAuthorIds = null,
    List<string>? AdditionalAuthorNames = null,
    List<string>? AdditionalUserIds = null);

public record UpdateNormaBody(
    string Titulo,
    int? TipoNormaId,
    string InstitutionId,
    List<string>? AdditionalAuthorIds = null,
    List<string>? AdditionalAuthorNames = null,
    List<string>? AdditionalUserIds = null);
