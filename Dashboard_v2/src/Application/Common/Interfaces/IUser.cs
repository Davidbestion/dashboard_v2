namespace Dashboard_v2.Application.Common.Interfaces;

/// <summary>
/// Exposes the identity of the currently authenticated user (extracted from HTTP context claims).
/// </summary>
public interface IUser
{
    /// <summary>Unique identifier of the current user. Null when the request is anonymous.</summary>
    string? Id { get; }

    /// <summary>Roles assigned to the current user for this session. Null when unauthenticated.</summary>
    List<string>? Roles { get; }
}
