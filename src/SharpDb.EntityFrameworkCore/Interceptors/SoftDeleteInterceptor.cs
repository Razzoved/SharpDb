using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SharpDb.EntityFrameworkCore.Entities;
using SharpDb.Services;

namespace SharpDb.EntityFrameworkCore.Interceptors;

/// <summary>
/// Intercepts save operations to implement soft-delete behavior for entities that support the ISoftDelete interface.
/// Instead of physically removing entities, sets their Deleted property to the current date and time when a delete
/// operation is detected.
/// </summary>
/// <remarks>
/// This interceptor modifies the state of entities implementing ISoftDelete during save operations. When
/// an entity is deleted, its Deleted property is set to the current timestamp, and the entity's state is changed to
/// Modified to prevent physical deletion. If the Deleted property is cleared during an update, the interceptor resets
/// it to null. This approach enables soft deletion, allowing entities to be excluded from queries without being removed
/// from the database.
/// </remarks>
/// <param name="dateTimeService">The service used to obtain the current date and time for marking entities as deleted.</param>
public sealed class SoftDeleteInterceptor(IDateTimeService dateTimeService) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            foreach (var entry in eventData.Context.ChangeTracker.Entries<ISoftDelete>())
            {
                switch (entry.State)
                {
                    case EntityState.Modified:
                        if (entry.OriginalValues.GetValue<DateTime?>(nameof(ISoftDelete.DeletedAt)).HasValue
                            && !entry.CurrentValues.GetValue<DateTime?>(nameof(ISoftDelete.DeletedAt)).HasValue)
                        {
                            entry.Entity.DeletedAt = null;
                        }
                        break;
                    case EntityState.Deleted:
                        if (!entry.OriginalValues.GetValue<DateTime?>(nameof(ISoftDelete.DeletedAt)).HasValue)
                        {
                            entry.Entity.DeletedAt = dateTimeService.Now.DateTime;
                        }
                        break;
                    default:
                        break;
                }

                if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
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
