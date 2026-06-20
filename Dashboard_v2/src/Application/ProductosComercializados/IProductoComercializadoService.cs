using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.ProductosComercializados;

/// <summary>Servicio de gestión de productos comercializados: listados con filtro por área y CRUD con verificación de autoría.</summary>
public interface IProductoComercializadoService
{
    /// <summary>Listado general; aplica filtro por área cuando el usuario actual es Vicedecano.</summary>
    Task<List<ProductoDto>> GetAllAsync(CancellationToken ct = default);

    Task<List<ProductoDto>> GetMisAsync(CancellationToken ct = default);

    Task<(Result Result, string? Id)> CreateAsync(CreateProductoBody body, CancellationToken ct = default);

    Task<Result> UpdateAsync(string id, UpdateProductoBody body, CancellationToken ct = default);

    Task<Result> DeleteAsync(string id, CancellationToken ct = default);
}
