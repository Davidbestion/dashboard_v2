using Dashboard_v2.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Web.Endpoints;

public class Normas : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("", GetNormas)
            .RequireAuthorization()
            .WithName("GetNormas")
            .Produces<List<NormaDto>>(200);

        groupBuilder.MapGet("mis", GetMisNormas)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Superuser)))
            .WithName("GetMisNormas")
            .Produces<List<NormaDto>>(200);

        groupBuilder.MapPost("", CreateNorma)
            .RequireAuthorization(p => p.RequireRole("Superuser"))
            .WithName("CreateNorma")
            .Produces(201)
            .ProducesProblem(400);

        groupBuilder.MapPut("{id}", UpdateNorma)
            .RequireAuthorization(p => p.RequireRole("Superuser"))
            .WithName("UpdateNorma")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);

        groupBuilder.MapDelete("{id}", DeleteNorma)
            .RequireAuthorization(p => p.RequireRole("Superuser"))
            .WithName("DeleteNorma")
            .Produces(200)
            .ProducesProblem(404);
    }

    private async Task<IResult> GetNormas(IApplicationDbContext db)
    {
        var list = await db.Normas
            .Include(n => n.Institution)
            .Include(n => n.Creadores).ThenInclude(c => c.User)
            .Select(n => new NormaDto(
                n.Id, n.Titulo, n.Tipo, n.InstitutionId, n.Institution.Nombre,
                n.Creadores.Select(c => c.User.UserName + " " + c.User.UserLastName1).ToList()))
            .ToListAsync();
        return Results.Ok(list);
    }

    private static async Task<IResult> GetMisNormas(IApplicationDbContext db, IUser currentUser)
    {
        var userId = currentUser.Id;
        var list = await db.UserNormas
            .Where(un => un.UserId == userId)
            .Include(un => un.Norma).ThenInclude(n => n.Institution)
            .Include(un => un.Norma).ThenInclude(n => n.Creadores).ThenInclude(c => c.User)
            .Select(un => new NormaDto(
                un.Norma.Id, un.Norma.Titulo, un.Norma.Tipo,
                un.Norma.InstitutionId, un.Norma.Institution.Nombre,
                un.Norma.Creadores.Select(c => c.User.UserName + " " + c.User.UserLastName1).ToList()))
            .ToListAsync();
        return Results.Ok(list);
    }

    private async Task<IResult> CreateNorma(IApplicationDbContext db, CreateNormaBody body)
    {
        var norma = new Dashboard_v2.Domain.Entities.Norma
        {
            Titulo = body.Titulo,
            Tipo = body.Tipo,
            InstitutionId = body.InstitutionId
        };

        db.Normas.Add(norma);
        await db.SaveChangesAsync(CancellationToken.None);

        return Results.Created($"/api/Normas/{norma.Id}", new { id = norma.Id });
    }

    private async Task<IResult> UpdateNorma(IApplicationDbContext db, string id, UpdateNormaBody body)
    {
        var norma = await db.Normas.FindAsync(new object[] { id }, CancellationToken.None);
        if (norma == null)
            return Results.NotFound(new { errors = new[] { "Norma no encontrada." } });

        norma.Titulo = body.Titulo;
        norma.Tipo = body.Tipo;
        norma.InstitutionId = body.InstitutionId;

        await db.SaveChangesAsync(CancellationToken.None);
        return Results.Ok(new { message = "Norma actualizada." });
    }

    private async Task<IResult> DeleteNorma(IApplicationDbContext db, string id)
    {
        var norma = await db.Normas.FindAsync(new object[] { id }, CancellationToken.None);
        if (norma == null)
            return Results.NotFound(new { errors = new[] { "Norma no encontrada." } });

        db.Normas.Remove(norma);
        await db.SaveChangesAsync(CancellationToken.None);
        return Results.Ok(new { message = "Norma eliminada." });
    }
}

public record NormaDto(string Id, string Titulo, string Tipo, string InstitutionId, string InstitutionNombre, List<string> Creadores);
public record CreateNormaBody(string Titulo, string Tipo, string InstitutionId);
public record UpdateNormaBody(string Titulo, string Tipo, string InstitutionId);
