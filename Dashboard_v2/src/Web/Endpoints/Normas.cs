using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Application.Normas;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Web.Endpoints;

public class Normas : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("", GetNormas)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser), nameof(RolesEnum.Vicedecano_de_investigacion)))
            .WithName("GetNormas")
            .Produces<List<NormaDto>>(200);

        groupBuilder.MapGet("mis", GetMisNormas)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("GetMisNormas")
            .Produces<List<NormaDto>>(200);

        groupBuilder.MapPost("", CreateNorma)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("CreateNorma")
            .Produces(201)
            .ProducesProblem(400);

        groupBuilder.MapPut("{id}", UpdateNorma)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("UpdateNorma")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(403)
            .ProducesProblem(404);

        groupBuilder.MapDelete("{id}", DeleteNorma)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("DeleteNorma")
            .Produces(200)
            .ProducesProblem(403)
            .ProducesProblem(404);
    }

    private static async Task<IResult> GetNormas(INormaService service, CancellationToken ct)
        => Results.Ok(await service.GetAllAsync(ct));

    private static async Task<IResult> GetMisNormas(INormaService service, CancellationToken ct)
        => Results.Ok(await service.GetMisAsync(ct));

    private static async Task<IResult> CreateNorma(INormaService service, CreateNormaBody body, CancellationToken ct)
    {
        var (result, id) = await service.CreateAsync(body, ct);
        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Created($"/api/Normas/{id}", new { id });
    }

    private static async Task<IResult> UpdateNorma(INormaService service, string id, UpdateNormaBody body, CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, body, ct);
        return ToUpdateOrDeleteResult(result, "Norma actualizada.");
    }

    private static async Task<IResult> DeleteNorma(INormaService service, string id, CancellationToken ct)
    {
        var result = await service.DeleteAsync(id, ct);
        return ToUpdateOrDeleteResult(result, "Norma eliminada.");
    }

    private static IResult ToUpdateOrDeleteResult(Result result, string successMessage)
    {
        if (result.Succeeded)
            return Results.Ok(new { message = successMessage });
        if (HasError(result, "Norma no encontrada."))
            return Results.NotFound(new { errors = result.Errors });
        if (HasError(result, "No tiene permisos sobre esta norma."))
            return Results.Forbid();

        return Results.BadRequest(new { errors = result.Errors });
    }

    private static bool HasError(Result result, string error)
        => result.Errors.Contains(error, StringComparer.Ordinal);
}
