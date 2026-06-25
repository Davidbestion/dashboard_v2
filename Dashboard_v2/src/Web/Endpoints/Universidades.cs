using Dashboard_v2.Application.Universidades;
using Dashboard_v2.Web.Infrastructure;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Web.Endpoints;

/// <summary>
/// API endpoints for university management.
/// </summary>
public class Universidades : EndpointGroupBase
{
    /// <summary>Registers the Universidades route group with CRUD endpoints. All operations require Superuser.</summary>
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("", GetUniversidades)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser)))
            .WithName("GetUniversidades")
            .Produces<List<UniversidadDto>>(200);

        groupBuilder.MapPost("", CreateUniversidad)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser)))
            .WithName("CreateUniversidad")
            .Produces(201)
            .ProducesProblem(400);

        groupBuilder.MapPut("{id}", UpdateUniversidad)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser)))
            .WithName("UpdateUniversidad")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);

        groupBuilder.MapDelete("{id}", DeleteUniversidad)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser)))
            .WithName("DeleteUniversidad")
            .Produces(200)
            .ProducesProblem(404);
    }

    private async Task<IResult> GetUniversidades(IUniversidadService svc)
    {
        var list = await svc.GetAllAsync();
        return Results.Ok(list);
    }

    private async Task<IResult> CreateUniversidad(IUniversidadService svc, CreateUniversidadBody body)
    {
        var (result, id) = await svc.CreateAsync(body.Nombre);

        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Created($"/api/Universidades/{id}", new { id });
    }

    private async Task<IResult> UpdateUniversidad(IUniversidadService svc, string id, UpdateUniversidadBody body)
    {
        var result = await svc.UpdateAsync(id, body.Nombre);

        if (!result.Succeeded)
            return result.Errors.Contains("Universidad no encontrada.")
                ? Results.NotFound(new { errors = result.Errors })
                : Results.BadRequest(new { errors = result.Errors });

        return Results.Ok(new { message = "Universidad actualizada." });
    }

    private async Task<IResult> DeleteUniversidad(IUniversidadService svc, string id)
    {
        var result = await svc.DeleteAsync(id);

        if (!result.Succeeded)
            return Results.NotFound(new { errors = result.Errors });

        return Results.Ok(new { message = "Universidad eliminada." });
    }
}

public record CreateUniversidadBody(string Nombre);
public record UpdateUniversidadBody(string Nombre);
