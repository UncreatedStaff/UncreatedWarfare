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
    private readonly UCSemaphore _semaphore = new UCSemaphore(0, 1);
    public FobRecord Record { get; private set; }
    public IReadOnlyDictionary<IFOBItem, ulong> Items { get; private set; }
    public CancellationTokenSource CancelTokenSource { get; } = new CancellationTokenSource();
    public FobRecordTracker(FobRecord record)
    {
        Record = record;
        Items = new ReadOnlyDictionary<IFOBItem, ulong>(_itemPrimaryKeys);
    }
    public void Dispose()
    {
        Task task = Task.Run(() => _semaphore.WaitAsync(CancellationToken.None));
        UCWarfare.SpinWaitUntil(() => task.IsCompleted);
        CancelTokenSource.Dispose();
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
    public async Task Create(CancellationToken token = default)
    {
        if (_primaryKey != 0)
            throw new InvalidOperationException("Only run create once.");

        try
        {
            using CombinedTokenSources tokens = token.CombineTokensIfNeeded(CancelTokenSource.Token);

            await using IStatsDbContext dbContext = new TDbContext();

            dbContext.FobRecords.Add(Record);
            await dbContext.SaveChangesAsync(token);

            _primaryKey = Record.Id;
        }
        finally
        {
            _semaphore.Release();
        }
        
    }
    public async Task Update(Action<FobRecord> update, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            if (_primaryKey == 0)
                throw new InvalidOperationException("Run create before running Update.");

            await using IStatsDbContext dbContext = new TDbContext();

            Record = await dbContext.FobRecords.FirstAsync(x => x.Id == _primaryKey, token);

            await UCWarfare.ToUpdate(token);
            update(Record);

            dbContext.Update(Record);

            await dbContext.SaveChangesAsync(token);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    public async Task Create(IFOBItem item, FobItemRecord itemRecord, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            if (_itemPrimaryKeys.TryGetValue(item, out ulong pk) && pk != 0)
                throw new InvalidOperationException("Only run create once per item.");

            using CombinedTokenSources tokens = token.CombineTokensIfNeeded(CancelTokenSource.Token);

            await using IStatsDbContext dbContext = new TDbContext();

            dbContext.FobItemRecords.Add(itemRecord);
            await dbContext.SaveChangesAsync(token);
            _itemPrimaryKeys[item] = itemRecord.Id;
            item.RecordId = itemRecord.Id;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    public static async Task Update(ulong primaryKey, Action<FobItemRecord> update, CancellationToken token = default)
    {
        await using IStatsDbContext dbContext = new TDbContext();

        FobItemRecord item = await dbContext.FobItemRecords.FirstAsync(x => x.Id == primaryKey, token);

        await UCWarfare.ToUpdate(token);
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

        await dbContext.SaveChangesAsync(token);
    }
    public async Task Update(IFOBItem fobItem, Action<FobItemRecord> update, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            if (!_itemPrimaryKeys.TryGetValue(fobItem, out ulong pk) || pk == 0)
                throw new InvalidOperationException("Run create before running Update on this item.");

            await Update(pk, update, token);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
