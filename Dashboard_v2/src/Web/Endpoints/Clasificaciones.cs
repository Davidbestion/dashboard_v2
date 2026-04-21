using Dashboard_v2.Application.Clasificaciones;
using Dashboard_v2.Web.Infrastructure;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Web.Endpoints;

public class Clasificaciones : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("", GetClasificaciones)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("GetClasificaciones")
            .Produces<List<ClasificacionDto>>(200);

        groupBuilder.MapPost("", CreateClasificacion)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser)))
            .WithName("CreateClasificacion")
            .Produces(201)
            .ProducesProblem(400);

        groupBuilder.MapPut("{id}", UpdateClasificacion)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser)))
            .WithName("UpdateClasificacion")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);

        groupBuilder.MapDelete("{id}", DeleteClasificacion)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser)))
            .WithName("DeleteClasificacion")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);
    }

    private async Task<IResult> GetClasificaciones(IClasificacionService svc)
    {
        var list = await svc.GetAllAsync();
        return Results.Ok(list);
    }

    private async Task<IResult> CreateClasificacion(IClasificacionService svc, CreateClasificacionRequest body)
    {
        var (result, id) = await svc.CreateAsync(body);
        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });
        return Results.Created($"/api/Clasificaciones/{id}", new { id });
    }

    private async Task<IResult> UpdateClasificacion(IClasificacionService svc, string id, UpdateClasificacionRequest body)
    {
        var result = await svc.UpdateAsync(id, body);
        if (!result.Succeeded)
        {
            if (result.Errors.Contains("Clasificación no encontrada."))
                return Results.NotFound(new { errors = result.Errors });
            return Results.BadRequest(new { errors = result.Errors });
        }
        return Results.Ok(new { message = "Clasificación actualizada." });
    }

    private async Task<IResult> DeleteClasificacion(IClasificacionService svc, string id)
    {
        var result = await svc.DeleteAsync(id);
        if (!result.Succeeded)
        {
            if (result.Errors.Contains("Clasificación no encontrada."))
                return Results.NotFound(new { errors = result.Errors });
            return Results.BadRequest(new { errors = result.Errors });
        }
        return Results.Ok(new { message = "Clasificación eliminada." });
    }
}

// Request body types now use Application/Clasificaciones request records
