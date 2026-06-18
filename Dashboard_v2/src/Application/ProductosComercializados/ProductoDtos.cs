using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.ProductosComercializados;

public record ProductoDto(
    string Id,
    string Titulo,
    string TipoProductoComercializadoId,
    string TipoProductoComercializadoNombre,
    string InstitutionId,
    string InstitutionNombre,
    List<string> Creadores,
    List<CreatorDto> CreadoresDetalle);

public record CreateProductoBody(
    string Titulo,
    string TipoProductoComercializadoId,
    string InstitutionId,
    List<string>? AdditionalAuthorIds = null,
    List<string>? AdditionalAuthorNames = null,
    List<string>? AdditionalUserIds = null);

public record UpdateProductoBody(
    string Titulo,
    string TipoProductoComercializadoId,
    string InstitutionId,
    List<string>? AdditionalAuthorIds = null,
    List<string>? AdditionalAuthorNames = null,
    List<string>? AdditionalUserIds = null);
