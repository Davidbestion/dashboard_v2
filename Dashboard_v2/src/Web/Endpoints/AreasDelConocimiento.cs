using Dashboard_v2.Application.AreasDelConocimiento;
using Dashboard_v2.Web.Infrastructure;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Web.Endpoints;

/// <summary>
/// CRUD de Áreas del Conocimiento bajo /api/AreasDelConocimiento. Solo Superuser.
/// </summary>
public class AreasDelConocimiento : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("", GetAreasDelConocimiento)
            .RequireAuthorization()
            .WithName("GetAreasDelConocimiento")
            .Produces<List<AreaDelConocimientoDto>>(200);

        groupBuilder.MapPost("", CreateAreaDelConocimiento)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser)))
            .WithName("CreateAreaDelConocimiento")
            .Produces(201)
            .ProducesProblem(400);

        groupBuilder.MapPut("{id}", UpdateAreaDelConocimiento)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser)))
            .WithName("UpdateAreaDelConocimiento")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);

        groupBuilder.MapDelete("{id}", DeleteAreaDelConocimiento)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser)))
            .WithName("DeleteAreaDelConocimiento")
            .Produces(200)
            .ProducesProblem(404);
    }

    private async Task<IResult> GetAreasDelConocimiento(IAreaDelConocimientoService svc)
    {
        var list = await svc.GetAllAsync();
        return Results.Ok(list);
    }

    private async Task<IResult> CreateAreaDelConocimiento(IAreaDelConocimientoService svc, CreateAreaDelConocimientoRequest body)
    {
        var (result, id) = await svc.CreateAsync(body);
        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Created($"/api/AreasDelConocimiento/{id}", new { id });
    }

    private async Task<IResult> UpdateAreaDelConocimiento(IAreaDelConocimientoService svc, string id, UpdateAreaDelConocimientoRequest body)
    {
        var result = await svc.UpdateAsync(id, body);
        if (!result.Succeeded)
            return result.Errors.Contains("Área del conocimiento no encontrada.")
                ? Results.NotFound(new { errors = result.Errors })
                : Results.BadRequest(new { errors = result.Errors });

        return Results.Ok(new { message = "Área del conocimiento actualizada." });
    }

    private async Task<IResult> DeleteAreaDelConocimiento(IAreaDelConocimientoService svc, string id)
    {
        var result = await svc.DeleteAsync(id);

        if (!result.Succeeded)
            return Results.NotFound(new { errors = result.Errors });

        return Results.Ok(new { message = "Área del conocimiento eliminada." });
    }
}

// Request body types now use Application/AreasDelConocimiento request records
