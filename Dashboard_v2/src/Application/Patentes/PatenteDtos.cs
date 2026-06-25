using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.Patentes;

/// <summary>Patent summary with creator list for display in lists and project detail views.</summary>
public record PatenteDto(string Id, string Titulo, string NumeroSolicitudConcesion, bool EsNacional, List<string> Creadores, List<CreatorDto> CreadoresDetalle);

/// <summary>Minimal project reference for the patent-project link view.</summary>
public record ProyectoPatenteDto(string ProyectoId, string ProyectoTitulo);

/// <summary>Request to register a new patent with optional co-creator resolution.</summary>
public record CreatePatenteBody(
    string Titulo,
    string NumeroSolicitudConcesion,
    bool EsNacional,
    List<string>? AdditionalAuthorIds = null,
    List<string>? AdditionalAuthorNames = null,
    List<string>? AdditionalUserIds = null);

/// <summary>Request to update an existing patent's data and co-creator list.</summary>
public record UpdatePatenteBody(
    string Titulo,
    string NumeroSolicitudConcesion,
    bool EsNacional,
    List<string>? AdditionalAuthorIds = null,
    List<string>? AdditionalAuthorNames = null,
    List<string>? AdditionalUserIds = null);
