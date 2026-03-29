using Dashboard_v2.Application.Publications;
using Dashboard_v2.Application.Publications.Commands.CreatePublication;
using Dashboard_v2.Application.Publications.Commands.CreatePublicationType;
using Dashboard_v2.Application.Publications.Commands.DeletePublication;
using Dashboard_v2.Application.Publications.Commands.UpdatePublication;
using Dashboard_v2.Application.Publications.Queries.GetMyPublications;
using Dashboard_v2.Application.Publications.Queries.GetPublicationById;
using Dashboard_v2.Application.Publications.Queries.GetPublicationTypes;

namespace Dashboard_v2.Web.Endpoints;

/// <summary>
/// Endpoints de gestión de publicaciones académicas bajo /api/Publications.
/// Todos requieren el rol "Profesor".
/// </summary>
public class Publications : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        // GET /api/Publications/types — lista los tipos disponibles (para el selector)
        groupBuilder.MapGet("types", GetPublicationTypes)
            .RequireAuthorization(p => p.RequireRole("Profesor"))
            .WithName("GetPublicationTypes")
            .Produces<List<PublicationTypeDto>>(200);

        // POST /api/Publications/types — crea un nuevo tipo de publicación
        groupBuilder.MapPost("types", CreatePublicationType)
            .RequireAuthorization(p => p.RequireRole("Profesor"))
            .WithName("CreatePublicationType")
            .Produces<PublicationTypeDto>(201)
            .ProducesProblem(400);

        // GET /api/Publications — publicaciones del usuario autenticado
        groupBuilder.MapGet("", GetMyPublications)
            .RequireAuthorization(p => p.RequireRole("Profesor"))
            .WithName("GetMyPublications")
            .Produces<List<PublicationDto>>(200);

        // GET /api/Publications/{id}
        groupBuilder.MapGet("{id}", GetPublicationById)
            .RequireAuthorization(p => p.RequireRole("Profesor"))
            .WithName("GetPublicationById")
            .Produces<PublicationDto>(200)
            .ProducesProblem(404);

        // POST /api/Publications
        groupBuilder.MapPost("", CreatePublication)
            .RequireAuthorization(p => p.RequireRole("Profesor"))
            .WithName("CreatePublication")
            .Produces(201)
            .ProducesProblem(400);

        // PUT /api/Publications/{id}
        groupBuilder.MapPut("{id}", UpdatePublication)
            .RequireAuthorization(p => p.RequireRole("Profesor"))
            .WithName("UpdatePublication")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);

        // DELETE /api/Publications/{id}
        groupBuilder.MapDelete("{id}", DeletePublication)
            .RequireAuthorization(p => p.RequireRole("Profesor"))
            .WithName("DeletePublication")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);
    }

    private async Task<IResult> GetPublicationTypes(ISender sender)
    {
        var types = await sender.Send(new GetPublicationTypesQuery());
        return Results.Ok(types);
    }

    private async Task<IResult> CreatePublicationType(ISender sender, CreatePublicationTypeBody body)
    {
        var (result, type) = await sender.Send(new CreatePublicationTypeCommand { Name = body.Name });
        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });
        return Results.Created($"/api/Publications/types/{type!.Id}", type);
    }

    private async Task<IResult> GetMyPublications(ISender sender)
    {
        var publications = await sender.Send(new GetMyPublicationsQuery());
        return Results.Ok(publications);
    }

    private async Task<IResult> GetPublicationById(ISender sender, string id)
    {
        var publication = await sender.Send(new GetPublicationByIdQuery(id));
        return publication is null ? Results.NotFound() : Results.Ok(publication);
    }

    private async Task<IResult> CreatePublication(ISender sender, CreatePublicationCommand command)
    {
        var (result, id) = await sender.Send(command);

        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Created($"/api/Publications/{id}", new { id });
    }

    private async Task<IResult> UpdatePublication(ISender sender, string id, UpdatePublicationBody body)
    {
        var result = await sender.Send(new UpdatePublicationCommand
        {
            Id = id,
            Title = body.Title,
            PublicationData = body.PublicationData,
            PublicationTypeId = body.PublicationTypeId,
            UrlDoi = body.UrlDoi
        });

        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Ok(new { message = "Publicación actualizada." });
    }

    private async Task<IResult> DeletePublication(ISender sender, string id)
    {
        var result = await sender.Send(new DeletePublicationCommand(id));

        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Ok(new { message = "Publicación eliminada." });
    }
}

/// <summary>Cuerpo de la petición PUT para actualizar una publicación.</summary>
public record UpdatePublicationBody(string Title, string PublicationData, string PublicationTypeId, string? UrlDoi);

/// <summary>Cuerpo de la petición POST para crear un tipo de publicación.</summary>
public record CreatePublicationTypeBody(string Name);
