using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.Registros;

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
