using Dashboard_v2.Application.Events;
// using MediatR commands/queries replaced by IEventService
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Web.Endpoints;

/// <summary>
/// API endpoints for recording event oral presentations.
/// </summary>
public class Presentations : EndpointGroupBase
{
    /// <summary>Registers the Presentations route group with all presentation endpoints.</summary>
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("", GetMyPresentations)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("GetMyPresentations")
            .Produces<List<PresentationDto>>(200);

        groupBuilder.MapGet("all", GetAllPresentations)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser)))
            .WithName("GetAllPresentations")
            .Produces<List<PresentationDto>>(200);

        groupBuilder.MapGet("area", GetAreaPresentations)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Vicedecano_de_investigacion)))
            .WithName("GetAreaPresentations")
            .Produces<List<PresentationDto>>(200);

        groupBuilder.MapPost("", CreatePresentation)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("CreatePresentation")
            .Produces(201)
            .ProducesProblem(400);

        groupBuilder.MapPut("{id}", UpdatePresentation)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("UpdatePresentation")
            .Produces(200)
            .ProducesProblem(400);

        groupBuilder.MapDelete("{id}", DeletePresentation)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("DeletePresentation")
            .Produces(200)
            .ProducesProblem(400);
    }
    private async Task<IResult> GetMyPresentations(IPresentationService service)
        => Results.Ok(await service.GetMyPresentationsAsync());

    private async Task<IResult> GetAllPresentations(IPresentationService service)
        => Results.Ok(await service.GetAllPresentationsAsync());

    private async Task<IResult> GetAreaPresentations(IPresentationService service)
        => Results.Ok(await service.GetAreaPresentationsAsync());

    private async Task<IResult> CreatePresentation(IPresentationService service, CreatePresentationRequest body)
    {
        var (result, id) = await service.CreatePresentationAsync(body);

        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Created($"/api/Presentations/{id}", new { id });
    }

    private async Task<IResult> UpdatePresentation(IPresentationService service, int id, UpdatePresentationRequest body)
    {
        var result = await service.UpdatePresentationAsync(id, body);

        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Ok(new { message = "Presentación actualizada." });
    }

    private async Task<IResult> DeletePresentation(IPresentationService service, int id)
    {
        var result = await service.DeletePresentationAsync(id);

        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Ok(new { message = "Presentación eliminada." });
    }
}
