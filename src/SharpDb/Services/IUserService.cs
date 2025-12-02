using SharpDb.Entities;

namespace SharpDb.Services;

/// <summary>
/// Interface for a service that provides information about the current user.
/// </summary>
public interface IUserService
{
    IUser? GetCurrentUser();
    object? GetCurrentUserID();
    string? GetCurrentUserDisplayName();
}
