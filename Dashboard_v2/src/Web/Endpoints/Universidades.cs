using Dashboard_v2.Application.Universidades;
using Dashboard_v2.Application.Universidades.Commands.CreateUniversidad;
using Dashboard_v2.Application.Universidades.Commands.DeleteUniversidad;
using Dashboard_v2.Application.Universidades.Commands.UpdateUniversidad;
using Dashboard_v2.Application.Universidades.Queries.GetUniversidades;
using Dashboard_v2.Web.Infrastructure;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Web.Endpoints;

/// <summary>
/// CRUD de Universidades bajo /api/Universidades. Solo Superuser.
/// </summary>
public class Universidades : EndpointGroupBase
{
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

    private async Task<IResult> GetUniversidades(ISender sender)
    {
        var list = await sender.Send(new GetUniversidadesQuery());
        return Results.Ok(list);
    }

    private async Task<IResult> CreateUniversidad(ISender sender, CreateUniversidadBody body)
    {
        var (result, id) = await sender.Send(new CreateUniversidadCommand { Nombre = body.Nombre });

        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Created($"/api/Universidades/{id}", new { id });
    }

    private async Task<IResult> UpdateUniversidad(ISender sender, string id, UpdateUniversidadBody body)
    {
        var result = await sender.Send(new UpdateUniversidadCommand { Id = id, Nombre = body.Nombre });

        if (!result.Succeeded)
            return result.Errors.Contains("Universidad no encontrada.")
                ? Results.NotFound(new { errors = result.Errors })
                : Results.BadRequest(new { errors = result.Errors });

        return Results.Ok(new { message = "Universidad actualizada." });
    }

    private async Task<IResult> DeleteUniversidad(ISender sender, string id)
    {
        var result = await sender.Send(new DeleteUniversidadCommand(id));

        if (!result.Succeeded)
            return Results.NotFound(new { errors = result.Errors });

        return Results.Ok(new { message = "Universidad eliminada." });
    }
}

public record CreateUniversidadBody(string Nombre);
public record UpdateUniversidadBody(string Nombre);
