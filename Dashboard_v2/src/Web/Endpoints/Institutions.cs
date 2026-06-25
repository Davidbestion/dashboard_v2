using Dashboard_v2.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Web.Endpoints;

/// <summary>
/// API endpoints for external institution management.
/// </summary>
public class Institutions : EndpointGroupBase
{
    /// <summary>Registers the Institutions route group with list and create endpoints.</summary>
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("", GetAll)
            .RequireAuthorization()
            .WithName("GetInstitutions")
            .Produces<List<InstitutionDto>>(200);
        
        groupBuilder.MapPost("", Create)
            .RequireAuthorization()
            .WithName("CreateInstitution")
            .Produces(201)
            .ProducesProblem(400);
    }

    private async Task<IResult> GetAll(IApplicationDbContext db)
    {
        var list = await db.Institutions
            .Select(i => new InstitutionDto(i.Id, i.Nombre))
            .ToListAsync();

        return Results.Ok(list);
    }

    private async Task<IResult> Create(IApplicationDbContext db, CreateInstitutionBody body)
    {
        if (string.IsNullOrWhiteSpace(body.Nombre))
            return Results.BadRequest(new { errors = new[] { "Nombre requerido." } });

        var inst = new Dashboard_v2.Domain.Entities.Institution
        {
            Nombre = body.Nombre.Trim()
        };

        db.Institutions.Add(inst);
        await db.SaveChangesAsync(CancellationToken.None);

        return Results.Created($"/api/Institutions/{inst.Id}", new InstitutionDto(inst.Id, inst.Nombre));
    }
}

public record InstitutionDto(string Id, string Nombre);
public record CreateInstitutionBody(string Nombre);
