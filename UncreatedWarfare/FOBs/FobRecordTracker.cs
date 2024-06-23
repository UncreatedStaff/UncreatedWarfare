using Cysharp.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Models.Stats.Records;

namespace Uncreated.Warfare.FOBs;
public class FobRecordTracker<TDbContext> : IDisposable where TDbContext : IStatsDbContext, new()
{
    private readonly Dictionary<IFOBItem, ulong> _itemPrimaryKeys = new Dictionary<IFOBItem, ulong>();
    private ulong _primaryKey;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0, 1);
    public FobRecord Record { get; private set; }
    public IReadOnlyDictionary<IFOBItem, ulong> Items { get; private set; }
    public FobRecordTracker(FobRecord record)
    {
        Record = record;
        Items = new ReadOnlyDictionary<IFOBItem, ulong>(_itemPrimaryKeys);
    }
    public void Dispose()
    {
        // allow time for objects to be destroyed first.
        UCWarfare.RunTask(async () =>
        {
            await UniTask.NextFrame(PlayerLoopTiming.PostLateUpdate);
            await UniTask.SwitchToThreadPool();
            await _semaphore.WaitAsync(CancellationToken.None);
        }, ctx: $"Finishing FOB record updates ({Record.FobName}, {Record.Id}).");
    }

    public Task WaitAsync(CancellationToken token = default) => _semaphore.WaitAsync(token);
    public void Release() => _semaphore.Release();
    public async ValueTask<ulong> GetRecordId(CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            return _primaryKey;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    public async Task Create()
    {
        if (_primaryKey != 0)
            throw new InvalidOperationException("Only run create once.");

        try
        {
            await using IStatsDbContext dbContext = new TDbContext();

            dbContext.FobRecords.Add(Record);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            _primaryKey = Record.Id;
        }
        finally
        {
            _semaphore.Release();
        }
        
    }
    public async Task Update(Action<FobRecord> update)
    {
        await _semaphore.WaitAsync(CancellationToken.None);
        try
        {
            if (_primaryKey == 0)
                throw new InvalidOperationException("Run create before running Update.");

            await using IStatsDbContext dbContext = new TDbContext();

            Record = await dbContext.FobRecords.FirstAsync(x => x.Id == _primaryKey, CancellationToken.None);

            await UCWarfare.ToUpdate(CancellationToken.None);
            update(Record);

            dbContext.Update(Record);

            await dbContext.SaveChangesAsync(CancellationToken.None);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    public async Task Create(IFOBItem item, FobItemRecord itemRecord)
    {
        await _semaphore.WaitAsync(CancellationToken.None);
        try
        {
            if (_itemPrimaryKeys.TryGetValue(item, out ulong pk) && pk != 0)
                throw new InvalidOperationException("Only run create once per item.");

            await using IStatsDbContext dbContext = new TDbContext();

            dbContext.FobItemRecords.Add(itemRecord);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            _itemPrimaryKeys[item] = itemRecord.Id;
            item.RecordId = itemRecord.Id;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    public static async Task Update(ulong primaryKey, Action<FobItemRecord> update)
    {
        await using IStatsDbContext dbContext = new TDbContext();

        FobItemRecord item = await dbContext.FobItemRecords.FirstAsync(x => x.Id == primaryKey, CancellationToken.None);

        await UCWarfare.ToUpdate(CancellationToken.None);
        update(item);

        dbContext.Update(item);
        if (item.Builders != null)
        {
            foreach (FobItemBuilderRecord r in item.Builders)
            {
                r.FobItem = item;
                dbContext.Add(r);
            }
        }

        await dbContext.SaveChangesAsync(CancellationToken.None);
    }
    public async Task Update(IFOBItem fobItem, Action<FobItemRecord> update)
    {
        await _semaphore.WaitAsync(CancellationToken.None);
        try
        {
            if (!_itemPrimaryKeys.TryGetValue(fobItem, out ulong pk) || pk == 0)
                throw new InvalidOperationException("Run create before running Update on this item.");

            await Update(pk, update);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
