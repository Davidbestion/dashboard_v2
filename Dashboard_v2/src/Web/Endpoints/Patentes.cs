using Dashboard_v2.Application.Patentes;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Web.Endpoints;

/// <summary>
/// API endpoints for patent management.
/// </summary>
public class Patentes : EndpointGroupBase
{
    /// <summary>Registers the Patentes route group with CRUD and project link/unlink endpoints.</summary>
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("", GetPatentes)
            .RequireAuthorization()
            .WithName("GetPatentes")
            .Produces<List<PatenteDto>>(200);

        groupBuilder.MapGet("mis", GetMisPatentes)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Superuser)))
            .WithName("GetMisPatentes")
            .Produces<List<PatenteDto>>(200);

        groupBuilder.MapGet("{id}/proyectos", GetProyectosDePatente)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Superuser)))
            .WithName("GetProyectosDePatente")
            .Produces<List<ProyectoPatenteDto>>(200)
            .ProducesProblem(404);

        groupBuilder.MapPost("{id}/proyectos/{proyectoId}", LinkProyectoAPatente)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Superuser)))
            .WithName("LinkProyectoAPatente")
            .Produces(204)
            .ProducesProblem(400)
            .ProducesProblem(403)
            .ProducesProblem(404);

        groupBuilder.MapDelete("{id}/proyectos/{proyectoId}", UnlinkProyectoDePatente)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Superuser)))
            .WithName("UnlinkProyectoDePatente")
            .Produces(204)
            .ProducesProblem(403)
            .ProducesProblem(404);

        groupBuilder.MapPost("", CreatePatente)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("CreatePatente")
            .Produces(201)
            .ProducesProblem(400);

        groupBuilder.MapPut("{id}", UpdatePatente)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("UpdatePatente")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(403)
            .ProducesProblem(404);

        groupBuilder.MapDelete("{id}", DeletePatente)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("DeletePatente")
            .Produces(200)
            .ProducesProblem(403)
            .ProducesProblem(404);
    }

    private static async Task<IResult> GetPatentes(IPatenteService service, CancellationToken ct)
        => Results.Ok(await service.GetAllAsync(ct));

    private static async Task<IResult> GetMisPatentes(IPatenteService service, CancellationToken ct)
        => Results.Ok(await service.GetMisAsync(ct));

    private static async Task<IResult> GetProyectosDePatente(IPatenteService service, string id, CancellationToken ct)
    {
        var (found, proyectos) = await service.GetProyectosDeAsync(id, ct);
        if (!found)
            return Results.NotFound(new { errors = new[] { "Patente no encontrada." } });

        return Results.Ok(proyectos);
    }

    private static async Task<IResult> LinkProyectoAPatente(IPatenteService service, string id, string proyectoId, CancellationToken ct)
    {
        var result = await service.LinkProyectoAsync(id, proyectoId, ct);
        return ToLinkResult(result);
    }

    private static async Task<IResult> UnlinkProyectoDePatente(IPatenteService service, string id, string proyectoId, CancellationToken ct)
    {
        var result = await service.UnlinkProyectoAsync(id, proyectoId, ct);
        return ToLinkResult(result, noContentOnSuccess: true);
    }

    private static async Task<IResult> CreatePatente(IPatenteService service, CreatePatenteBody body, CancellationToken ct)
    {
        var (result, id) = await service.CreateAsync(body, ct);
        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Created($"/api/Patentes/{id}", new { id });
    }

    private static async Task<IResult> UpdatePatente(IPatenteService service, string id, UpdatePatenteBody body, CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, body, ct);
        return ToUpdateOrDeleteResult(result, "Patente actualizada.");
    }

    private static async Task<IResult> DeletePatente(IPatenteService service, string id, CancellationToken ct)
    {
        var result = await service.DeleteAsync(id, ct);
        return ToUpdateOrDeleteResult(result, "Patente eliminada.");
    }

    private static IResult ToLinkResult(Dashboard_v2.Application.Common.Models.Result result, bool noContentOnSuccess = true)
    {
        if (result.Succeeded)
            return Results.NoContent();
        if (HasError(result, "Patente no encontrada.") || HasError(result, "Proyecto no encontrado.") || HasError(result, "Vinculo no encontrado."))
            return Results.NotFound(new { errors = result.Errors });
        if (HasError(result, "No tiene permisos sobre esta patente."))
            return Results.Forbid();

        return Results.BadRequest(new { errors = result.Errors });
    }

    private static IResult ToUpdateOrDeleteResult(Dashboard_v2.Application.Common.Models.Result result, string successMessage)
    {
        if (result.Succeeded)
            return Results.Ok(new { message = successMessage });
        if (HasError(result, "Patente no encontrada."))
            return Results.NotFound(new { errors = result.Errors });
        if (HasError(result, "No tiene permisos sobre esta patente."))
            return Results.Forbid();

        return Results.BadRequest(new { errors = result.Errors });
    }

    private static bool HasError(Dashboard_v2.Application.Common.Models.Result result, string error)
        => result.Errors.Contains(error, StringComparer.Ordinal);
}
