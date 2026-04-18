using Dashboard_v2.Application.AreasDelConocimiento;
using Dashboard_v2.Application.AreasDelConocimiento.Commands.CreateAreaDelConocimiento;
using Dashboard_v2.Application.AreasDelConocimiento.Commands.DeleteAreaDelConocimiento;
using Dashboard_v2.Application.AreasDelConocimiento.Commands.UpdateAreaDelConocimiento;
using Dashboard_v2.Application.AreasDelConocimiento.Queries.GetAreasDelConocimiento;
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

    private async Task<IResult> GetAreasDelConocimiento(ISender sender)
    {
        var list = await sender.Send(new GetAreasDelConocimientoQuery());
        return Results.Ok(list);
    }

    private async Task<IResult> CreateAreaDelConocimiento(ISender sender, CreateAreaDelConocimientoBody body)
    {
        var (result, id) = await sender.Send(new CreateAreaDelConocimientoCommand
        {
            Nombre = body.Nombre,
            Descripcion = body.Descripcion,
            LineasDeInvestigacionIds = body.LineasDeInvestigacionIds ?? [],
        });

        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Created($"/api/AreasDelConocimiento/{id}", new { id });
    }

    private async Task<IResult> UpdateAreaDelConocimiento(ISender sender, string id, UpdateAreaDelConocimientoBody body)
    {
        var result = await sender.Send(new UpdateAreaDelConocimientoCommand
        {
            Id = id,
            Nombre = body.Nombre,
            Descripcion = body.Descripcion,
            LineasDeInvestigacionIds = body.LineasDeInvestigacionIds ?? [],
        });

        if (!result.Succeeded)
            return result.Errors.Contains("Área del conocimiento no encontrada.")
                ? Results.NotFound(new { errors = result.Errors })
                : Results.BadRequest(new { errors = result.Errors });

        return Results.Ok(new { message = "Área del conocimiento actualizada." });
    }

    private async Task<IResult> DeleteAreaDelConocimiento(ISender sender, string id)
    {
        var result = await sender.Send(new DeleteAreaDelConocimientoCommand(id));

        if (!result.Succeeded)
            return Results.NotFound(new { errors = result.Errors });

        return Results.Ok(new { message = "Área del conocimiento eliminada." });
    }
}

public record CreateAreaDelConocimientoBody(string Nombre, string? Descripcion, IList<string>? LineasDeInvestigacionIds);
public record UpdateAreaDelConocimientoBody(string Nombre, string? Descripcion, IList<string>? LineasDeInvestigacionIds);
