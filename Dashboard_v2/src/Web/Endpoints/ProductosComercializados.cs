using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Application.ProductosComercializados;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Web.Endpoints;

public class ProductosComercializados : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("", GetAll)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser), nameof(RolesEnum.Vicedecano_de_investigacion)))
            .WithName("GetProductosComercializados")
            .Produces<List<ProductoDto>>(200);

        groupBuilder.MapGet("mis", GetMis)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("GetMisProductosComercializados")
            .Produces<List<ProductoDto>>(200);

        groupBuilder.MapPost("", Create)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("CreateProductoComercializado")
            .Produces(201)
            .ProducesProblem(400);

        groupBuilder.MapPut("{id}", Update)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("UpdateProductoComercializado")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(403)
            .ProducesProblem(404);

        groupBuilder.MapDelete("{id}", Delete)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("DeleteProductoComercializado")
            .Produces(200)
            .ProducesProblem(403)
            .ProducesProblem(404);
    }

    private static async Task<IResult> GetAll(IProductoComercializadoService service, CancellationToken ct)
        => Results.Ok(await service.GetAllAsync(ct));

    private static async Task<IResult> GetMis(IProductoComercializadoService service, CancellationToken ct)
        => Results.Ok(await service.GetMisAsync(ct));

    private static async Task<IResult> Create(IProductoComercializadoService service, CreateProductoBody body, CancellationToken ct)
    {
        var (result, id) = await service.CreateAsync(body, ct);
        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Created($"/api/ProductosComercializados/{id}", new { id });
    }

    private static async Task<IResult> Update(IProductoComercializadoService service, string id, UpdateProductoBody body, CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, body, ct);
        return ToUpdateOrDeleteResult(result, "Producto actualizado.");
    }

    private static async Task<IResult> Delete(IProductoComercializadoService service, string id, CancellationToken ct)
    {
        var result = await service.DeleteAsync(id, ct);
        return ToUpdateOrDeleteResult(result, "Producto eliminado.");
    }

    private static IResult ToUpdateOrDeleteResult(Result result, string successMessage)
    {
        if (result.Succeeded)
            return Results.Ok(new { message = successMessage });
        if (HasError(result, "Producto no encontrado."))
            return Results.NotFound(new { errors = result.Errors });
        if (HasError(result, "No tiene permisos sobre este producto."))
            return Results.Forbid();

        return Results.BadRequest(new { errors = result.Errors });
    }

    private static bool HasError(Result result, string error)
        => result.Errors.Contains(error, StringComparer.Ordinal);
}
