using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;

namespace Uncreated.Warfare.Singletons;
public abstract class CachedEntityFrameworkSingleton<TEntity> : BaseAsyncReloadSingleton where TEntity : class
{
    protected internal readonly UCSemaphore WriteSemaphore;
    protected internal readonly UCSemaphore UpdateSemaphore;
    private List<TEntity> _entities;
    public event Func<Task>? OnItemsRefreshed;
    public event Func<TEntity, Task>? OnItemAdded;
    public event Func<TEntity, Task>? OnItemUpdated;
    public event Func<TEntity, Task>? OnItemDeleted;
    public Type EntityType { get; set; }
    public override bool AwaitLoad => true;
    public IReadOnlyList<TEntity> Items { get; private set; }
    private protected IList<TEntity> List => _entities;
    protected abstract DbSet<TEntity> Set { get; }
    protected CachedEntityFrameworkSingleton(string? reloadKey) : base(reloadKey)
    {
        WriteSemaphore = new UCSemaphore(0, 1);
        UpdateSemaphore = new UCSemaphore(0, 1);
#if DEBUG
        WriteSemaphore.WaitCallback     += () => L.LogDebug($"Writer semaphore waiting       in {typeof(TEntity).Name} EF cache.");
        WriteSemaphore.ReleaseCallback  += n  => L.LogDebug($"Writer semaphore released x{n} in {typeof(TEntity).Name} EF cache.");
        UpdateSemaphore.WaitCallback    += () => L.LogDebug($"Update semaphore Waiting       in {typeof(TEntity).Name} EF cache.");
        UpdateSemaphore.ReleaseCallback += n  => L.LogDebug($"Update semaphore Released x{n} in {typeof(TEntity).Name} EF cache.");
#endif
        EntityType = typeof(TEntity);
        _entities = new List<TEntity>(0);
        Items = _entities.AsReadOnly();
    }

    public void WriteWait(CancellationToken token = default) => WriteSemaphore.Wait(token);
    public void WriteWait(TimeSpan timeout, CancellationToken token = default) => WriteSemaphore.Wait(timeout, token);
    public void WriteWait(int millisecondsTimeout, CancellationToken token = default) => WriteSemaphore.Wait(millisecondsTimeout, token);
    public Task<bool> WriteWaitAsync(CancellationToken token = default) => WriteSemaphore.WaitAsync(token);
    public Task<bool> WriteWaitAsync(TimeSpan timeout, CancellationToken token = default) => WriteSemaphore.WaitAsync(timeout, token);
    public Task<bool> WriteWaitAsync(int millisecondsTimeout, CancellationToken token = default) => WriteSemaphore.WaitAsync(millisecondsTimeout, token);
    
    public Task<bool> WaitAsync(CancellationToken token = default) => UpdateSemaphore.WaitAsync(token);
    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken token = default) => UpdateSemaphore.WaitAsync(timeout, token);
    public Task<bool> WaitAsync(int millisecondsTimeout, CancellationToken token = default) => UpdateSemaphore.WaitAsync(millisecondsTimeout, token);

    public void WriteRelease(int amt = 1) => WriteSemaphore.Release(amt);
    public void Release(int amt = 1) => UpdateSemaphore.Release(amt);
    public override async Task LoadAsync(CancellationToken token)
    {
        try
        {
            try
            {
                await PreLoad(token).ConfigureAwait(false);

                _entities = await OnInclude(Set).ToListAsync(token).ConfigureAwait(false);
                Items = _entities.AsReadOnly();
            }
            finally
            {
                WriteRelease();
            }

            await UCWarfare.ToUpdate(token);
            if (OnItemsRefreshed != null)
            {
                await OnItemsRefreshed();
                await UCWarfare.ToUpdate(token);
            }
            await PostLoad(token).ConfigureAwait(false);
        }
        finally
        {
            Release();
        }
    }
    public override async Task UnloadAsync(CancellationToken token)
    {
        await WaitAsync(token).ConfigureAwait(false);
        try
        {
            UCWarfare.ToUpdate(token);
            WriteWait(token);
            try
            {
                await PreUnload(token).ConfigureAwait(false);

                _entities.Clear();
            }
            finally
            {
                WriteRelease();
            }

            await UCWarfare.ToUpdate(token);
            await PostUnload(token).ConfigureAwait(false);
        }
        finally
        {
            Release();
        }
    }
    public override async Task ReloadAsync(CancellationToken token)
    {
        await WaitAsync(token).ConfigureAwait(false);
        try
        {
            UCWarfare.ToUpdate(token);
            WriteWait(token);
            try
            {
                await PreReload(token).ConfigureAwait(false);

                _entities = await OnInclude(Set).ToListAsync(token).ConfigureAwait(false);
                Items = _entities.AsReadOnly();
            }
            finally
            {
                WriteSemaphore.Release();
            }

            await UCWarfare.ToUpdate(token);
            if (OnItemsRefreshed != null)
            {
                await OnItemsRefreshed();
                await UCWarfare.ToUpdate(token);
            }
            await PostReload(token).ConfigureAwait(false);
        }
        finally
        {
            UpdateSemaphore.Release();
        }
    }

    public async Task Add(TEntity entity, bool save = true, CancellationToken token = default)
    {
        await WaitAsync(token).ConfigureAwait(false);
        try
        {
            await WriteSemaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (_entities.Contains(entity))
                    return;

                await Set.AddAsync(entity, token).ConfigureAwait(false);
                if (save)
                    await Data.DbContext.SaveChangesAsync(token).ConfigureAwait(false);
                _entities.Add(entity);
            }
            finally
            {
                WriteRelease();
            }

            if (OnItemAdded != null)
            {
                await UCWarfare.ToUpdate(token);
                await (OnItemAdded?.Invoke(entity) ?? Task.CompletedTask);
            }
        }
        finally
        {
            Release();
        }
    }
    public async Task AddRange(IEnumerable<TEntity> entities, bool save = true, CancellationToken token = default)
    {
        List<TEntity> list = new List<TEntity>(entities);

        await WaitAsync(token).ConfigureAwait(false);
        try
        {
            await WriteSemaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                list.RemoveAll(x => _entities.Contains(x));
                if (list.Count == 0)
                    return;

                await Set.AddRangeAsync(list.Where(x => !_entities.Contains(x)), token).ConfigureAwait(false);
                if (save)
                    await Data.DbContext.SaveChangesAsync(token).ConfigureAwait(false);
                _entities.AddRange(list);
            }
            finally
            {
                WriteRelease();
            }

            if (OnItemAdded != null)
            {
                await UCWarfare.ToUpdate(token);
                Task[] tasks = new Task[list.Count];
                for (int i = 0; i < list.Count; ++i)
                {
                    tasks[i] = OnItemAdded?.Invoke(list[i]) ?? Task.CompletedTask;
                }
                await Task.WhenAll(tasks);
            }
        }
        finally
        {
            Release();
        }
    }
    public async Task<TEntity?> Redownload(Expression<Func<TEntity, bool>> selector, CancellationToken token = default)
    {
        await WaitAsync(token).ConfigureAwait(false);
        try
        {
            return await RedownloadNoLock(selector, token).ConfigureAwait(false);
        }
        finally
        {
            Release();
        }
    }
    public async Task Update(TEntity entity, bool save = true, CancellationToken token = default)
    {
        await WaitAsync(token).ConfigureAwait(false);
        try
        {
            await WriteSemaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (!_entities.Contains(entity))
                    return;

                Set.Update(entity);
                if (save)
                    await Data.DbContext.SaveChangesAsync(token).ConfigureAwait(false);
            }
            finally
            {
                WriteRelease();
            }

            if (OnItemUpdated != null)
            {
                await UCWarfare.ToUpdate(token);
                await (OnItemUpdated?.Invoke(entity) ?? Task.CompletedTask);
            }
        }
        finally
        {
            Release();
        }
    }
    public async Task UpdateRange(IEnumerable<TEntity> entities, bool save = true, CancellationToken token = default)
    {
        List<TEntity> list = new List<TEntity>(entities);

        await WaitAsync(token).ConfigureAwait(false);
        try
        {
            await WriteSemaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                list.RemoveAll(x => !_entities.Contains(x));
                if (list.Count == 0)
                    return;

                Set.UpdateRange(list);
                if (save)
                    await Data.DbContext.SaveChangesAsync(token).ConfigureAwait(false);
            }
            finally
            {
                WriteRelease();
            }

            if (OnItemUpdated != null)
            {
                await UCWarfare.ToUpdate(token);
                Task[] tasks = new Task[list.Count];
                for (int i = 0; i < list.Count; ++i)
                {
                    tasks[i] = OnItemUpdated?.Invoke(list[i]) ?? Task.CompletedTask;
                }
                await Task.WhenAll(tasks);
            }
        }
        finally
        {
            Release();
        }
    }
    public async Task Remove(TEntity entity, bool save = true, CancellationToken token = default)
    {
        await WaitAsync(token).ConfigureAwait(false);
        try
        {
            await WriteSemaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (!_entities.Remove(entity))
                    return;

                Set.Remove(entity);
                if (save)
                    await Data.DbContext.SaveChangesAsync(token).ConfigureAwait(false);
            }
            finally
            {
                WriteRelease();
            }

            if (OnItemDeleted != null)
            {
                await UCWarfare.ToUpdate(token);
                await (OnItemDeleted?.Invoke(entity) ?? Task.CompletedTask);
            }
        }
        finally
        {
            Release();
        }
    }
    public async Task RemoveRange(IEnumerable<TEntity> entities, bool save = true, CancellationToken token = default)
    {
        List<TEntity> list = new List<TEntity>(entities);

        await WaitAsync(token).ConfigureAwait(false);
        try
        {
            await WriteSemaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                list.RemoveAll(x => !_entities.Contains(x));
                if (list.Count == 0)
                    return;

                for (int i = 0; i < list.Count; ++i)
                    _entities.Remove(list[i]);

                Set.RemoveRange(list);
                if (save)
                    await Data.DbContext.SaveChangesAsync(token).ConfigureAwait(false);
            }
            finally
            {
                WriteRelease();
            }

            if (OnItemDeleted != null)
            {
                await UCWarfare.ToUpdate(token);
                Task[] tasks = new Task[list.Count];
                for (int i = 0; i < list.Count; ++i)
                {
                    tasks[i] = OnItemDeleted?.Invoke(list[i]) ?? Task.CompletedTask;
                }
                await Task.WhenAll(tasks);
            }
        }
        finally
        {
            Release();
        }
    }

    public async Task AddNoLock(TEntity entity, bool save = true, CancellationToken token = default)
    {
        await WriteSemaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (_entities.Contains(entity))
                return;

            await Set.AddAsync(entity, token).ConfigureAwait(false);
            if (save)
                await Data.DbContext.SaveChangesAsync(token).ConfigureAwait(false);
            _entities.Add(entity);
        }
        finally
        {
            WriteRelease();
        }

        if (OnItemAdded != null)
        {
            await UCWarfare.ToUpdate(token);
            await (OnItemAdded?.Invoke(entity) ?? Task.CompletedTask);
        }
    }
    public async Task AddRangeNoLock(IEnumerable<TEntity> entities, bool save = true, CancellationToken token = default)
    {
        List<TEntity> list = new List<TEntity>(entities);
        
        await WriteSemaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            list.RemoveAll(x => _entities.Contains(x));
            if (list.Count == 0)
                return;

            await Set.AddRangeAsync(list.Where(x => !_entities.Contains(x)), token).ConfigureAwait(false);
            if (save)
                await Data.DbContext.SaveChangesAsync(token).ConfigureAwait(false);
            _entities.AddRange(list);
        }
        finally
        {
            WriteRelease();
        }

        if (OnItemAdded != null)
        {
            await UCWarfare.ToUpdate(token);
            Task[] tasks = new Task[list.Count];
            for (int i = 0; i < list.Count; ++i)
            {
                tasks[i] = OnItemAdded?.Invoke(list[i]) ?? Task.CompletedTask;
            }
            await Task.WhenAll(tasks);
        }
    }
    public async Task<TEntity?> RedownloadNoLock(Expression<Func<TEntity, bool>> selector, CancellationToken token = default)
    {
        TEntity? removed = null, updated = null, added = null;
        await WriteSemaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            Func<TEntity, bool> v = selector.Compile();
            int index = _entities.FindIndex(x => v(x));

            TEntity? entity = await OnInclude(Set).FirstOrDefaultAsync(selector, token).ConfigureAwait(false);
            if (entity == null)
            {
                if (index != -1)
                {
                    removed = _entities[index];
                    _entities.RemoveAt(index);
                }
            }
            else if (index == -1)
            {
                added = entity;
                _entities.Add(entity);
            }
            else
            {
                updated = entity;
                _entities[index] = entity;
            }
        }
        finally
        {
            WriteRelease();
        }

        if (updated != null)
        {
            if (OnItemUpdated != null)
            {
                await UCWarfare.ToUpdate(token);
                await (OnItemUpdated?.Invoke(updated) ?? Task.CompletedTask);
            }
        }
        else if (removed != null)
        {
            if (OnItemDeleted != null)
            {
                await UCWarfare.ToUpdate(token);
                await (OnItemDeleted?.Invoke(removed!) ?? Task.CompletedTask);
            }
        }
        else if (added != null)
        {
            if (OnItemAdded != null)
            {
                await UCWarfare.ToUpdate(token);
                await (OnItemAdded?.Invoke(added!) ?? Task.CompletedTask);
            }
        }

        return updated ?? added;
    }
    public async Task UpdateNoLock(TEntity entity, bool save = true, CancellationToken token = default)
    {
        await WriteSemaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (!_entities.Contains(entity))
                return;

            Set.Update(entity);
            if (save)
                await Data.DbContext.SaveChangesAsync(token).ConfigureAwait(false);
        }
        finally
        {
            WriteRelease();
        }

        if (OnItemUpdated != null)
        {
            await UCWarfare.ToUpdate(token);
            await (OnItemUpdated?.Invoke(entity) ?? Task.CompletedTask);
        }
    }
    public async Task UpdateRangeNoLock(IEnumerable<TEntity> entities, bool save = true, CancellationToken token = default)
    {
        List<TEntity> list = new List<TEntity>(entities);
        
        await WriteSemaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            list.RemoveAll(x => !_entities.Contains(x));
            if (list.Count == 0)
                return;

            Set.UpdateRange(list);
            if (save)
                await Data.DbContext.SaveChangesAsync(token).ConfigureAwait(false);
        }
        finally
        {
            WriteRelease();
        }

        if (OnItemUpdated != null)
        {
            await UCWarfare.ToUpdate(token);
            Task[] tasks = new Task[list.Count];
            for (int i = 0; i < list.Count; ++i)
            {
                tasks[i] = OnItemUpdated?.Invoke(list[i]) ?? Task.CompletedTask;
            }
            await Task.WhenAll(tasks);
        }
    }
    public async Task RemoveNoLock(TEntity entity, bool save = true, CancellationToken token = default)
    {
        await WriteSemaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (!_entities.Remove(entity))
                return;

            Set.Remove(entity);
            if (save)
                await Data.DbContext.SaveChangesAsync(token).ConfigureAwait(false);
        }
        finally
        {
            WriteRelease();
        }

        if (OnItemDeleted != null)
        {
            await UCWarfare.ToUpdate(token);
            await (OnItemDeleted?.Invoke(entity) ?? Task.CompletedTask);
        }
    }
    public async Task RemoveRangeNoLock(IEnumerable<TEntity> entities, bool save = true, CancellationToken token = default)
    {
        List<TEntity> list = new List<TEntity>(entities);
        
        await WriteSemaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            list.RemoveAll(x => !_entities.Contains(x));
            if (list.Count == 0)
                return;

            for (int i = 0; i < list.Count; ++i)
                _entities.Remove(list[i]);

            Set.RemoveRange(list);
            if (save)
                await Data.DbContext.SaveChangesAsync(token).ConfigureAwait(false);
        }
        finally
        {
            WriteRelease();
        }

        if (OnItemDeleted != null)
        {
            await UCWarfare.ToUpdate(token);
            Task[] tasks = new Task[list.Count];
            for (int i = 0; i < list.Count; ++i)
            {
                tasks[i] = OnItemDeleted?.Invoke(list[i]) ?? Task.CompletedTask;
            }
            await Task.WhenAll(tasks);
        }
    }

    protected virtual Task PreLoad(CancellationToken token) => Task.CompletedTask;
    protected virtual Task PostLoad(CancellationToken token) => Task.CompletedTask;
    protected virtual Task PreReload(CancellationToken token) => Task.CompletedTask;
    protected virtual Task PostReload(CancellationToken token) => Task.CompletedTask;
    protected virtual Task PreUnload(CancellationToken token) => Task.CompletedTask;
    protected virtual Task PostUnload(CancellationToken token) => Task.CompletedTask;
    protected virtual IQueryable<TEntity> OnInclude(IQueryable<TEntity> set)
    {
        return set;
    }
}
