using Dashboard_v2.Application.Roles;
using Dashboard_v2.Web.Infrastructure;

namespace Dashboard_v2.Web.Endpoints;

public class Roles : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("", GetRoles)
            .RequireAuthorization(policy => policy.RequireRole("Superuser"))
            .WithName("GetRoles")
            .Produces<List<RoleDto>>(200);
    }

    private async Task<IResult> GetRoles(IRoleService service)
    {
        var roles = await service.GetAssignableRolesAsync();
        return Results.Ok(roles);
    }
}
