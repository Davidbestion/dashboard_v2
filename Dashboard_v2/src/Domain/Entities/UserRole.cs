using Dashboard_v2.Domain.Enums;

namespace Dashboard_v2.Domain.Entities;

/// <summary>Junction entity: assigns a role to a user. A user may have multiple roles.</summary>
public class UserRole
{
    public string UserId { get; set; } = default!;
    public Roles Role { get; set; }

    // Navegación
    public User User { get; set; } = default!;
}
