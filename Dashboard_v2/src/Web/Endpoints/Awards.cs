using Dashboard_v2.Application.Awards;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

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
            .RequireAuthorization(p => p.RequireRole("Profesor", "Superuser"))
            .WithName("GetMyAwards")
            .Produces<List<AwardWithGrantingsDto>>(200);

        // GET /api/Awards/catalogo — catálogo reutilizable de premios
        groupBuilder.MapGet("catalogo", GetCatalog)
            .RequireAuthorization(p => p.RequireRole("Profesor", "Superuser"))
            .WithName("GetAwardCatalog")
            .Produces<List<AwardCatalogDto>>(200);

        // GET /api/Awards/todas — todas los premios (Superuser)
        groupBuilder.MapGet("todas", GetAllAwards)
            .RequireAuthorization(p => p.RequireRole("Superuser"))
            .WithName("GetAllAwards")
            .Produces<List<AwardWithGrantingsDto>>(200);

        // GET /api/Awards/area — premios del área del Vicedecano
        groupBuilder.MapGet("area", GetAreaAwards)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Vicedecano_de_investigacion)))
            .WithName("GetAreaAwards")
            .Produces<List<AwardWithGrantingsDto>>(200);

        // POST /api/Awards
        groupBuilder.MapPost("", CreateAward)
            .RequireAuthorization(p => p.RequireRole("Profesor", "Superuser"))
            .WithName("CreateAward")
            .Produces(201)
            .ProducesProblem(400);

        // PUT /api/Awards/{id}
        groupBuilder.MapPut("{id}", UpdateAward)
            .RequireAuthorization(p => p.RequireRole("Profesor", "Superuser"))
            .WithName("UpdateAward")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);

        // DELETE /api/Awards/{id}
        groupBuilder.MapDelete("{id}", DeleteAward)
            .RequireAuthorization(p => p.RequireRole("Profesor", "Superuser"))
            .WithName("DeleteAward")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);
    }
    private async Task<IResult> GetMyAwards(IAwardService service)
    {
        var awards = await service.GetMyAwardsAsync();
        return Results.Ok(awards);
    }

    private async Task<IResult> GetCatalog(IAwardService service)
    {
        var awards = await service.GetCatalogAsync();
        return Results.Ok(awards);
    }

    private async Task<IResult> GetAllAwards(IAwardService service)
    {
        var awards = await service.GetAllAwardsAsync();
        return Results.Ok(awards);
    }

    private async Task<IResult> GetAreaAwards(IAwardService service)
    {
        var awards = await service.GetAreaAwardsAsync();
        return Results.Ok(awards);
    }

    private async Task<IResult> CreateAward(IAwardService service, CreateAwardRequest body)
    {
        var (result, id) = await service.CreateAsync(body);

        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Created($"/api/Awards/{id}", new { id });
    }

    private async Task<IResult> UpdateAward(IAwardService service, int id, UpdateAwardRequest body)
    {
        var result = await service.UpdateAsync(id, body);

        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Ok(new { message = "Premio actualizado." });
    }

    private async Task<IResult> DeleteAward(IAwardService service, int id)
    {
        var result = await service.DeleteAsync(id);

        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Ok(new { message = "Premio eliminado." });
    }
}
