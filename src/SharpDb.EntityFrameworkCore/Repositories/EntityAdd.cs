using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace SharpDb.EntityFrameworkCore.Repositories;

public static class EntityAdd
{
    public static EntityEntry<TEntity> Execute<TEntity>(DbContext context, TEntity entity, bool cascade) where TEntity : class
    {
        EntityEntry<TEntity> entry;
        if (cascade)
        {
            entry = context.Add(entity);
        }
        else
        {
            entry = context.Entry(entity);
            entry.State = EntityState.Added;
        }
        return entry;
    }
}
