using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.Normas;

/// <summary>Technical standard (norma) with creator list for display.</summary>
public record NormaDto(string Id, string Titulo, int? TipoNormaId, string? TipoNormaNombre, string InstitutionId, string InstitutionNombre, List<string> Creadores, List<CreatorDto> CreadoresDetalle);

/// <summary>Request to register a new technical standard with optional co-creator resolution.</summary>
public record CreateNormaBody(
    string Titulo,
    int? TipoNormaId,
    string InstitutionId,
    List<string>? AdditionalAuthorIds = null,
    List<string>? AdditionalAuthorNames = null,
    List<string>? AdditionalUserIds = null);

/// <summary>Request to update an existing technical standard and its co-creator list.</summary>
public record UpdateNormaBody(
    string Titulo,
    int? TipoNormaId,
    string InstitutionId,
    List<string>? AdditionalAuthorIds = null,
    List<string>? AdditionalAuthorNames = null,
    List<string>? AdditionalUserIds = null);
