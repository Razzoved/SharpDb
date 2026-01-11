using Microsoft.EntityFrameworkCore;

namespace SharpDb.EntityFrameworkCore.Repositories;

/// <summary>
/// Base implementation of <see cref="IRepository{TEntity}"/> for Entity Framework Core.
/// Use this as a base class for custom repositories. By default, all operations throw
/// an exception, so you need to override the methods you want to support.
/// </summary>
/// <typeparam name="TEntity">Target entity type</typeparam>
public abstract class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    private DbSet<TEntity>? _set;

    protected Repository(DbContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        Context = context;
    }

    protected DbContext Context { get; }
    protected DbSet<TEntity> Set => _set ??= Context.Set<TEntity>();

    protected bool IsAddEnabled { private get; init; } = true;
    protected bool IsUpdateEnabled { private get; init; } = true;
    protected bool IsDeleteEnabled { private get; init; } = true;

    public void Attach(TEntity entity)
    {
        if (!IsUpdateEnabled)
        {
            string message = string.Format(Resources.Text_Error_Repository_AttachNotSupported, GetType().Name, typeof(TEntity).Name);
            throw new NotSupportedException(message);
        }

        var entry = Set.Entry(entity);
        if (entry.State == EntityState.Detached)
            entry.State = EntityState.Unchanged;
    }

    public void Detach(TEntity entity)
    {
        var entry = Set.Entry(entity);
        if (entry.State != EntityState.Detached)
            entry.State = EntityState.Detached;
    }

    public void Add(TEntity entity, bool cascade = true)
    {
        if (!IsAddEnabled)
        {
            string message = string.Format(Resources.Text_Error_Repository_AddNotSupported, GetType().Name, typeof(TEntity).Name);
            throw new NotSupportedException(message);
        }

        if (cascade)
        {
            Set.Add(entity);
            return;
        }

        Set.Entry(entity).State = EntityState.Added;
    }

    public void Update(TEntity entity, bool cascade = true)
    {
        if (!IsUpdateEnabled)
        {
            string message = string.Format(Resources.Text_Error_Repository_UpdateNotSupported, GetType().Name, typeof(TEntity).Name);
            throw new NotSupportedException(message);
        }

        if (cascade)
        {
            Set.Update(entity);
            return;
        }

        var entry = Set.Entry(entity);
        if (!entry.IsKeySet)
        {
            var key = entry.Metadata.FindPrimaryKey();
            if (key is not null)
            {
                if (key.Properties.All(p => p.ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd))
                {
                    entry.State = EntityState.Added;
                    return;
                }
            }
        }
        entry.State = EntityState.Modified;
    }

    public void Delete(TEntity entity, bool cascade = true)
    {
        if (!IsDeleteEnabled)
        {
            string message = string.Format(Resources.Text_Error_Repository_DeleteNotSupported, GetType().Name, typeof(TEntity).Name);
            throw new NotSupportedException(message);
        }

        if (cascade)
        {
            Set.Remove(entity);
            return;
        }

        var entry = Set.Entry(entity);
        if (!entry.IsKeySet)
        {
            var key = entry.Metadata.FindPrimaryKey();
            if (key is not null)
            {
                entry.State = EntityState.Detached;
                return;
            }
        }
        entry.State = EntityState.Deleted;
    }
}
