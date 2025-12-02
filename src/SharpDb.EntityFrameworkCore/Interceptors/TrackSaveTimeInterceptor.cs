using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SharpDb.EntityFrameworkCore.Entities;
using SharpDb.Services;

namespace SharpDb.EntityFrameworkCore.Interceptors;

/// <summary>
/// Intercepts save operations to automatically set audit timestamps on entities implementing <see cref="ITrackSaveTime"/>.
/// </summary>
/// <remarks>
/// This interceptor modifies the state of entities implementing <see cref="ITrackSaveTime"/> during save operations.
/// When an entity is added, its <see cref="ITrackSaveTime.CreatedAt"/> property is set to the current date and time.
/// When an entity is modified, its <see cref="ITrackSaveTime.UpdatedAt"/> property is updated to the current date and time.
/// This ensures consistent tracking of creation and update times across the data model.
/// </remarks>
/// <param name="dateTimeService">The service used to obtain the current date and time for marking timestamps.</param>

public sealed class TrackSaveTimeInterceptor(IDateTimeService dateTimeService) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            foreach (var entry in eventData.Context.ChangeTracker.Entries<ITrackSaveTime>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = dateTimeService.Now.DateTime;
                        break;
                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = dateTimeService.Now.DateTime;
                        break;
                    default:
                        break;
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
