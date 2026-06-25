using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Application.Registros;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Web.Endpoints;

/// <summary>
/// API endpoints for registered works management.
/// </summary>
public class Registros : EndpointGroupBase
{
    /// <summary>Registers the Registros route group with CRUD endpoints.</summary>
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("", GetRegistros)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser), nameof(RolesEnum.Vicedecano_de_investigacion)))
            .WithName("GetRegistros")
            .Produces<List<RegistroDto>>(200);

        groupBuilder.MapGet("mis", GetMisRegistros)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("GetMisRegistros")
            .Produces<List<RegistroDto>>(200);

        groupBuilder.MapPost("", CreateRegistro)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("CreateRegistro")
            .Produces(201)
            .ProducesProblem(400);

        groupBuilder.MapPut("{id}", UpdateRegistro)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("UpdateRegistro")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(403)
            .ProducesProblem(404);

        groupBuilder.MapDelete("{id}", DeleteRegistro)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("DeleteRegistro")
            .Produces(200)
            .ProducesProblem(403)
            .ProducesProblem(404);
    }

    private static async Task<IResult> GetRegistros(IRegistroService service, CancellationToken ct)
        => Results.Ok(await service.GetAllAsync(ct));

    private static async Task<IResult> GetMisRegistros(IRegistroService service, CancellationToken ct)
        => Results.Ok(await service.GetMisAsync(ct));

    private static async Task<IResult> CreateRegistro(IRegistroService service, CreateRegistroBody body, CancellationToken ct)
    {
        var (result, id) = await service.CreateAsync(body, ct);
        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Created($"/api/Registros/{id}", new { id });
    }

    private static async Task<IResult> UpdateRegistro(IRegistroService service, string id, UpdateRegistroBody body, CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, body, ct);
        return ToUpdateOrDeleteResult(result, "Registro actualizado.");
    }

    private static async Task<IResult> DeleteRegistro(IRegistroService service, string id, CancellationToken ct)
    {
        var result = await service.DeleteAsync(id, ct);
        return ToUpdateOrDeleteResult(result, "Registro eliminado.");
    }

    private static IResult ToUpdateOrDeleteResult(Result result, string successMessage)
    {
        if (result.Succeeded)
            return Results.Ok(new { message = successMessage });
        if (HasError(result, "Registro no encontrado."))
            return Results.NotFound(new { errors = result.Errors });
        if (HasError(result, "No tiene permisos sobre este registro."))
            return Results.Forbid();

        return Results.BadRequest(new { errors = result.Errors });
    }

    private static bool HasError(Result result, string error)
        => result.Errors.Contains(error, StringComparer.Ordinal);
}
