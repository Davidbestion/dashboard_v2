using Dashboard_v2.Application.Awards;

namespace Dashboard_v2.Web.Endpoints;

/// <summary>
/// Endpoints de gestión de premios bajo /api/Awards.
/// Todos requieren el rol "Profesor".
/// </summary>
public class Awards : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        // GET /api/Awards — premios del usuario autenticado
        groupBuilder.MapGet("", GetMyAwards)
            .RequireAuthorization(p => p.RequireRole("Profesor"))
            .WithName("GetMyAwards")
            .Produces<List<AwardDto>>(200);

        // POST /api/Awards
        groupBuilder.MapPost("", CreateAward)
            .RequireAuthorization(p => p.RequireRole("Profesor"))
            .WithName("CreateAward")
            .Produces(201)
            .ProducesProblem(400);

        // PUT /api/Awards/{id}
        groupBuilder.MapPut("{id}", UpdateAward)
            .RequireAuthorization(p => p.RequireRole("Profesor"))
            .WithName("UpdateAward")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);

        // DELETE /api/Awards/{id}
        groupBuilder.MapDelete("{id}", DeleteAward)
            .RequireAuthorization(p => p.RequireRole("Profesor"))
            .WithName("DeleteAward")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);
    }
    private async Task<IResult> GetMyAwards(Dashboard_v2.Application.Awards.IAwardService service)
    {
        var awards = await service.GetMyAwardsAsync();
        return Results.Ok(awards);
    }

    private async Task<IResult> CreateAward(Dashboard_v2.Application.Awards.IAwardService service, CreateAwardRequest body)
    {
        var (result, id) = await service.CreateAsync(body);

        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Created($"/api/Awards/{id}", new { id });
    }

    private async Task<IResult> UpdateAward(Dashboard_v2.Application.Awards.IAwardService service, int id, UpdateAwardRequest body)
    {
        var result = await service.UpdateAsync(id, body);

        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Ok(new { message = "Premio actualizado." });
    }

    private async Task<IResult> DeleteAward(Dashboard_v2.Application.Awards.IAwardService service, int id)
    {
        var result = await service.DeleteAsync(id);

        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Ok(new { message = "Premio eliminado." });
    }
}

