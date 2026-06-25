using Dashboard_v2.Application.Areas;
using Dashboard_v2.Web.Infrastructure;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Web.Endpoints;

/// <summary>
/// API endpoints for academic area management.
/// </summary>
public class Areas : EndpointGroupBase
{
    /// <summary>Registers the Areas route group with CRUD endpoints. Write operations require Superuser.</summary>
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("", GetAreas)
            .RequireAuthorization()
            .WithName("GetAreas")
            .Produces<List<AreaDto>>(200);

        groupBuilder.MapPost("", CreateArea)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser)))
            .WithName("CreateArea")
            .Produces(201)
            .ProducesProblem(400);

        groupBuilder.MapPut("{id}", UpdateArea)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser)))
            .WithName("UpdateArea")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);

        groupBuilder.MapDelete("{id}", DeleteArea)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser)))
            .WithName("DeleteArea")
            .Produces(200)
            .ProducesProblem(404);
    }

    private async Task<IResult> GetAreas(IAreaService svc)
    {
        var list = await svc.GetAllAsync();
        return Results.Ok(list);
    }

    private async Task<IResult> CreateArea(IAreaService svc, CreateAreaRequest body)
    {
        var (result, id) = await svc.CreateAsync(body);

        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Created($"/api/Areas/{id}", new { id });
    }

    private async Task<IResult> UpdateArea(IAreaService svc, string id, UpdateAreaRequest body)
    {
        var result = await svc.UpdateAsync(id, body);

        if (!result.Succeeded)
            return result.Errors.Contains("Área no encontrada.")
                ? Results.NotFound(new { errors = result.Errors })
                : Results.BadRequest(new { errors = result.Errors });

        return Results.Ok(new { message = "Área actualizada." });
    }

    private async Task<IResult> DeleteArea(IAreaService svc, string id)
    {
        var result = await svc.DeleteAsync(id);

        if (!result.Succeeded)
            return Results.NotFound(new { errors = result.Errors });

        return Results.Ok(new { message = "Área eliminada." });
    }
}

// Request DTOs for create/update are defined in Application/Areas/AreaRequests.cs
