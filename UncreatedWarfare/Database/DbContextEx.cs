using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Uncreated.Warfare.Database.Abstractions;

namespace Uncreated.Warfare.Database;
public static class DbContextEx
{
    public static void AttachAndMarkModified<TEntity>(this IDbContext dbContext, TEntity entity) where TEntity : class
    {
        try
        {
            dbContext.Attach(entity).State = EntityState.Modified;
            return;
        }
        catch (InvalidOperationException)
        {
        }
        EntityEntry<TEntity> entry = dbContext.Entry(entity);
        if (entry.State == EntityState.Added)
        {
            entry.State = EntityState.Modified;
            return;
        }


        entry.State = EntityState.Modified;
    }
}
