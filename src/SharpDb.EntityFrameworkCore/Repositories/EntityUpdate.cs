using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace SharpDb.EntityFrameworkCore.Repositories;

public static class EntityUpdate
{
    public static EntityEntry<TEntity> Execute<TEntity>(DbContext context, TEntity entity, bool cascade) where TEntity : class
    {
        if (cascade)
        {
            return context.Update(entity);
        }

        var entry = context.Entry(entity);
        if (!entry.IsKeySet)
        {
            var key = entry.Metadata.FindPrimaryKey();
            if (key is not null)
            {
                if (key.Properties.All(p => p.ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd))
                {
                    entry.State = EntityState.Added;
                    return entry;
                }
            }
        }
        entry.State = EntityState.Modified;
        return entry;
    }
}
