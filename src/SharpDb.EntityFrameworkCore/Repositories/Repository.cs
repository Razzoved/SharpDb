using Microsoft.EntityFrameworkCore;

namespace SharpDb.EntityFrameworkCore.Repositories;

public abstract class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    private readonly Lazy<DbSet<TEntity>> _set;

    protected Repository(DbContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        Context = context;
        _set = new Lazy<DbSet<TEntity>>(() => Context.Set<TEntity>());
    }

    protected DbContext Context { get; }
    protected DbSet<TEntity> Set => _set.Value;

    public virtual void Add(TEntity entity, bool cascade = true)
        => throw new NotSupportedException(string.Format(Resources.Text_Error_Repository_AddNotSupported, GetType().Name, typeof(TEntity).Name));

    public virtual void Update(TEntity entity, bool cascade = true)
        => throw new NotSupportedException(string.Format(Resources.Text_Error_Repository_UpdateNotSupported, GetType().Name, typeof(TEntity).Name));

    public virtual void Delete(TEntity entity, bool cascade = true)
        => throw new NotSupportedException(string.Format(Resources.Text_Error_Repository_DeleteNotSupported, GetType().Name, typeof(TEntity).Name));
}
