using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.Registros;

/// <summary>Software/intellectual property registration with creator list for display.</summary>
public record RegistroDto(
    string Id,
    string Titulo,
    string NumeroCertificado,
    bool EsInformatico,
    int CountryId,
    string CountryName,
    string InstitutionId,
    string InstitutionNombre,
    int? EvidenceFileId,
    List<string> Creadores,
    List<CreatorDto> CreadoresDetalle);

/// <summary>Request to register a new software/IP registration with optional co-creator resolution.</summary>
public record CreateRegistroBody(
    string Titulo,
    string NumeroCertificado,
    bool EsInformatico,
    int CountryId,
    string InstitutionId,
    int? EvidenceFileId = null,
    List<string>? AdditionalAuthorIds = null,
    List<string>? AdditionalAuthorNames = null,
    List<string>? AdditionalUserIds = null);

/// <summary>Request to update an existing registration record and its co-creator list.</summary>
public record UpdateRegistroBody(
    string Titulo,
    string NumeroCertificado,
    bool EsInformatico,
    int CountryId,
    string InstitutionId,
    int? EvidenceFileId = null,
    List<string>? AdditionalAuthorIds = null,
    List<string>? AdditionalAuthorNames = null,
    List<string>? AdditionalUserIds = null);
