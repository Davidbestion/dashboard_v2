using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.ProductosComercializados;

/// <summary>Commercialized product entry with creator list for display.</summary>
public record ProductoDto(
    string Id,
    string Titulo,
    string TipoProductoComercializadoId,
    string TipoProductoComercializadoNombre,
    string InstitutionId,
    string InstitutionNombre,
    List<string> Creadores,
    List<CreatorDto> CreadoresDetalle);

/// <summary>Request to register a new commercialized product with optional co-creator resolution.</summary>
public record CreateProductoBody(
    string Titulo,
    string TipoProductoComercializadoId,
    string InstitutionId,
    List<string>? AdditionalAuthorIds = null,
    List<string>? AdditionalAuthorNames = null,
    List<string>? AdditionalUserIds = null);

/// <summary>Request to update an existing commercialized product and its co-creator list.</summary>
public record UpdateProductoBody(
    string Titulo,
    string TipoProductoComercializadoId,
    string InstitutionId,
    List<string>? AdditionalAuthorIds = null,
    List<string>? AdditionalAuthorNames = null,
    List<string>? AdditionalUserIds = null);
