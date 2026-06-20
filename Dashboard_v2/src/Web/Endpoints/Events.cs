using Dashboard_v2.Application.Events;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Web.Endpoints;

public class Events : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("", GetMyEvents)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser), nameof(RolesEnum.Vicedecano_de_investigacion)))
            .WithName("GetMyEvents")
            .Produces<List<EventDto>>(200);

        groupBuilder.MapGet("all", GetAllEvents)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser), nameof(RolesEnum.Vicedecano_de_investigacion)))
            .WithName("GetAllEvents")
            .Produces<List<EventDto>>(200);

        groupBuilder.MapGet("countries", GetCountries)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser), nameof(RolesEnum.Vicedecano_de_investigacion), nameof(RolesEnum.Jefe_de_Redes)))
            .WithName("GetCountries")
            .Produces<List<CountryDto>>(200);

        groupBuilder.MapPost("countries", CreateCountry)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Vicedecano_de_investigacion), nameof(RolesEnum.Jefe_de_Redes), nameof(RolesEnum.Superuser)))
            .WithName("CreateCountry")
            .Produces<CountryDto>(201)
            .ProducesProblem(400);

        groupBuilder.MapGet("types", GetEventTypes)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser), nameof(RolesEnum.Vicedecano_de_investigacion)))
            .WithName("GetEventTypes")
            .Produces<List<EventTypeDto>>(200);

        groupBuilder.MapPost("", CreateEvent)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Vicedecano_de_investigacion), nameof(RolesEnum.Superuser)))
            .WithName("CreateEvent")
            .Produces(201)
            .ProducesProblem(400);

        groupBuilder.MapPut("{id}", UpdateEvent)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Vicedecano_de_investigacion), nameof(RolesEnum.Superuser)))
            .WithName("UpdateEvent")
            .Produces(200)
            .ProducesProblem(400);

        groupBuilder.MapDelete("{id}", DeleteEvent)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Vicedecano_de_investigacion), nameof(RolesEnum.Superuser)))
            .WithName("DeleteEvent")
            .Produces(200)
            .ProducesProblem(400);
    }

    private async Task<IResult> GetMyEvents(IEventService service, HttpContext http)
    {
        if (http.User.IsInRole(nameof(RolesEnum.Vicedecano_de_investigacion)))
            return Results.Ok(await service.GetAreaEventsAsync());
        return Results.Ok(await service.GetMyEventsAsync());
    }

    private async Task<IResult> GetAllEvents(IEventService service, HttpContext http)
    {
        if (http.User.IsInRole(nameof(RolesEnum.Vicedecano_de_investigacion)))
            return Results.Ok(await service.GetAreaEventsAsync());
        return Results.Ok(await service.GetAllEventsAsync());
    }

    private async Task<IResult> GetCountries(IEventService service)
        => Results.Ok(await service.GetCountriesAsync());

    private async Task<IResult> CreateCountry(IEventService service, CreateCountryRequest body)
    {
        var (result, country) = await service.CreateCountryAsync(body);

        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Created($"/api/Events/countries/{country!.Id}", country);
    }

    private async Task<IResult> GetEventTypes(IEventService service)
        => Results.Ok(await service.GetEventTypesAsync());

    private async Task<IResult> CreateEvent(IEventService service, CreateEventRequest body)
    {
        var (result, id) = await service.CreateEventAsync(body);

        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Created($"/api/Events/{id}", new { id });
    }

    private async Task<IResult> UpdateEvent(IEventService service, int id, UpdateEventRequest body)
    {
        var result = await service.UpdateEventAsync(id, body);

        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Ok(new { message = "Evento actualizado." });
    }

    private async Task<IResult> DeleteEvent(IEventService service, int id)
    {
        var result = await service.DeleteEventAsync(id);

        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Ok(new { message = "Evento eliminado." });
    }
}

