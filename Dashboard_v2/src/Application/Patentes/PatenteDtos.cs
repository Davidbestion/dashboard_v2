using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.Patentes;

public record PatenteDto(string Id, string Titulo, string NumeroSolicitudConcesion, bool EsNacional, List<string> Creadores, List<CreatorDto> CreadoresDetalle);

public record ProyectoPatenteDto(string ProyectoId, string ProyectoTitulo);

public record CreatePatenteBody(
    string Titulo,
    string NumeroSolicitudConcesion,
    bool EsNacional,
    List<string>? AdditionalAuthorIds = null,
    List<string>? AdditionalAuthorNames = null,
    List<string>? AdditionalUserIds = null);

public record UpdatePatenteBody(
    string Titulo,
    string NumeroSolicitudConcesion,
    bool EsNacional,
    List<string>? AdditionalAuthorIds = null,
    List<string>? AdditionalAuthorNames = null,
    List<string>? AdditionalUserIds = null);
