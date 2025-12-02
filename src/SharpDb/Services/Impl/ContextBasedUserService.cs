using SharpDb.Entities;

namespace SharpDb.Services.Impl;

/// <summary>
/// Context based user service that retrieves the current user from the <see cref="UserContext"/>.
/// </summary>
public sealed class ContextBasedUserService : IUserService
{
    public IUser? GetCurrentUser() => UserContext.CurrentUser;
    public object? GetCurrentUserID() => GetCurrentUser()?.GetID();
    public string? GetCurrentUserDisplayName() => GetCurrentUser()?.GetDisplayName();

    /// <summary>
    /// Context that follows logical flow. Allows setting the current user for the scope of the context.
    /// </summary>
    public sealed class UserContext : IDisposable
    {
        private static readonly AsyncLocal<IUser?> s_currentUser = new();
        private readonly IUser? _savedUser;
        private bool _disposed;

        public UserContext(IUser? currentUser)
        {
            _savedUser = s_currentUser.Value;
            s_currentUser.Value = currentUser;
        }

        public static IUser? CurrentUser => s_currentUser.Value;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                s_currentUser.Value = _savedUser;
            }
        }
    }
}
