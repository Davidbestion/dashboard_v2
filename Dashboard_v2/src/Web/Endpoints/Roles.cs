using Dashboard_v2.Application.Roles;
using Dashboard_v2.Web.Infrastructure;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Web.Endpoints;

/// <summary>
/// API endpoints for role catalog retrieval.
/// </summary>
public class Roles : EndpointGroupBase
{
    /// <summary>Registers the Roles route group with the role listing endpoint.</summary>
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("", GetRoles)
            .RequireAuthorization(policy => policy.RequireRole(nameof(RolesEnum.Superuser)))
            .WithName("GetRoles")
            .Produces<List<RoleDto>>(200);
    }

    private async Task<IResult> GetRoles(IRoleService service)
    {
        var roles = await service.GetAssignableRolesAsync();
        return Results.Ok(roles);
    }
}
