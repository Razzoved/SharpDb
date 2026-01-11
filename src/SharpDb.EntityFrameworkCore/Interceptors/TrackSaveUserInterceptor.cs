using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SharpDb.Entities;
using SharpDb.EntityFrameworkCore.Entities;
using SharpDb.Services;

namespace SharpDb.EntityFrameworkCore.Interceptors;

/// <summary>
/// Intercepts save operations to automatically set user tracking properties on entities
/// implementing at least one of the <see cref="ITrackUserC"/>, <see cref="ITrackUserU"/>,
/// or <see cref="ITrackUserD"/> interfaces.
/// </summary>
/// <remarks>
/// This interceptor modifies the state of entities implementing user tracking interfaces during save operations.
/// When an entity is added, its <see cref="ITrackUserC.CreatedByUser"/> property is set to the current user.
/// When an entity is modified, its <see cref="ITrackUserU.UpdatedByUser"/> property is updated to the current user.
/// When an entity is deleted, its <see cref="ITrackUserD.DeletedByUser"/> property is set to the current user.
/// </remarks>
/// <param name="userService">Service used to obtain the current user.</param>
public sealed class TrackSaveUserInterceptor(IUserService userService) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null && eventData.Context.ChangeTracker.HasChanges() && userService.GetCurrentUser() is { } user)
        {
            // Handle inserted objects
            foreach (var entry in eventData.Context.ChangeTracker.Entries<ITrackUserC>())
            {
                if (entry.State == EntityState.Added)
                {
                    if (entry.Entity.CreatedByUser is null)
                    {
                        entry.Entity.CreatedByUser = user;
                    }
                }
            }
            // Handle updated objects
            foreach (var entry in eventData.Context.ChangeTracker.Entries<ITrackUserU>())
            {
                if (entry.State == EntityState.Modified)
                {
                    if (entry.Entity.UpdatedByUser is null)
                    {
                        entry.Entity.UpdatedByUser = user;
                    }
                }
            }
            // Handle deleted objects
            foreach (var entry in eventData.Context.ChangeTracker.Entries<ITrackUserD>())
            {
                if (entry.State == EntityState.Deleted || entry is { State: EntityState.Modified, Entity.IsDeleted: true })
                {
                    if (entry.Entity.DeletedByUser is null)
                    {
                        entry.Entity.DeletedByUser = user;
                    }
                }
            }
        }
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(SavingChanges(eventData, result));
    }
}
