using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Models.Localization;

namespace Uncreated.Warfare;
public interface ILanguageDataStore
{
    Task Initialize(CancellationToken token = default);
    void WriteWait();
    void WriteRelease();
    Task<LanguageInfo?> GetInfo(string name, bool exactOnly, bool allowCache, CancellationToken token = default);
    Task<LanguageInfo?> GetInfo(uint key, bool allowCache, CancellationToken token = default);
    Task WriteWaitAsync(CancellationToken token = default);
    Task AddOrUpdateInfo(LanguageInfo info, CancellationToken token = default);
    Task UpdateLanguagePreferences(LanguagePreferences preferences, CancellationToken token = default);
    Task<LanguagePreferences> GetLanguagePreferences(ulong steam64, CancellationToken token = default);
    Task GetLanguages(IList<LanguageInfo> outputList, CancellationToken token = default);
}
public interface ICachableLanguageDataStore : ILanguageDataStore
{
    IReadOnlyList<LanguageInfo> Languages { get; }
    LanguageInfo? GetInfoCached(string name, bool exactOnly = true);
    LanguageInfo? GetInfoCached(uint key);
    Task ReloadCache(CancellationToken token = default);
}

public abstract class MySqlLanguageDataStore<TDbContext> : ICachableLanguageDataStore where TDbContext : ILanguageDbContext, new()
{
    private List<LanguageInfo>? _langs;
    private Dictionary<string, LanguageInfo>? _codes;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    public IReadOnlyList<LanguageInfo> Languages { get; private set; }
    public Task Initialize(CancellationToken token = default) => Task.CompletedTask;
    public Task WriteWaitAsync(CancellationToken token = default) => _semaphore.WaitAsync(token);
    public void WriteWait() => _semaphore.Wait();
    public void WriteRelease() => _semaphore.Release();
    public LanguageInfo? GetInfoCached(string name, bool exactOnly = true)
    {
        lock (this)
        {
            if (_codes != null && _codes.TryGetValue(name, out LanguageInfo info))
                return info;
        }

        _semaphore.Wait();
        try
        {
            if (_langs == null)
                return null;

            LanguageInfo lang = _langs.Find(x => F.RoughlyEquals(x.Code, name));
            if (lang != null || exactOnly)
                return lang;

            lang = _langs.Find(x => F.RoughlyEquals(x.DisplayName, name));
            if (lang != null)
                return lang;

            string[] words = name.Split(F.SpaceSplit);
            lang = _langs.Find(x => words.All(l => x.Aliases.Any(x => F.RoughlyEquals(l, x.Alias))));
            if (lang != null)
                return lang;

            lang = _langs.Find(x => x.DisplayName.IndexOf(name, StringComparison.InvariantCultureIgnoreCase) != -1);
            if (lang != null)
                return lang;

            lang = _langs.Find(x => words.All(l => x.DisplayName.IndexOf(l, StringComparison.InvariantCultureIgnoreCase) != -1));
            if (lang != null)
                return lang;

            lang = _langs.Find(x => x.NativeName != null && x.NativeName.IndexOf(name, StringComparison.InvariantCultureIgnoreCase) != -1);
            if (lang != null)
                return lang;

            lang = _langs.Find(x => x.NativeName != null && words.All(l => x.NativeName.IndexOf(l, StringComparison.InvariantCultureIgnoreCase) != -1));
            if (lang != null)
                return lang;

            lang = _langs.Find(x => words.All(l => x.Aliases.Any(x => x.Alias.IndexOf(l, StringComparison.InvariantCultureIgnoreCase) != -1)));
            return lang;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    public LanguageInfo? GetInfoCached(uint key)
    {
        _semaphore.Wait();
        try
        {
            return _langs?.Find(x => x.Key == key);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    public async Task<LanguageInfo?> GetInfo(string name, bool exactOnly, bool allowCache, CancellationToken token = default)
    {
        if (_langs == null || !allowCache)
            await ReloadCache(token).ConfigureAwait(false);

        return GetInfoCached(name, exactOnly);
    }
    public async Task<LanguageInfo?> GetInfo(uint key, bool allowCache, CancellationToken token = default)
    {
        if (_langs == null || !allowCache)
            await ReloadCache(token).ConfigureAwait(false);

        return GetInfoCached(key);
    }
    public async Task AddOrUpdateInfo(LanguageInfo info, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await using ILanguageDbContext dbContext = new TDbContext();
            if (info.Key == 0)
                await dbContext.Languages.AddAsync(info, token);
            else
                dbContext.Update(info);
            await dbContext.SaveChangesAsync(token).ConfigureAwait(false);

            lock (this)
            {
                _codes ??= new Dictionary<string, LanguageInfo>(16);
                _langs ??= new List<LanguageInfo>(16);
                _codes[info.Code] = info;
                if (!_langs.Contains(info))
                {
                    int index = _langs.FindIndex(x => x.Code.Equals(info.Code, StringComparison.OrdinalIgnoreCase));
                    if (index == -1)
                        _langs.Add(info);
                    else
                        _langs[index] = info;
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
    public async Task UpdateLanguagePreferences(LanguagePreferences preferences, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await using ILanguageDbContext dbContext = new TDbContext();
            if (!await dbContext.LanguagePreferences.AnyAsync(x => x.Steam64 == preferences.Steam64, token).ConfigureAwait(false))
                await dbContext.LanguagePreferences.AddAsync(preferences, token);
            else
                dbContext.Update(preferences);
            await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    private IQueryable<LanguageInfo> Include(IQueryable<LanguageInfo> set)
    {
        return set
            .Include(x => x.Aliases)
            .Include(x => x.Contributors)
            .Include(x => x.SupportedCultures);
    }
    public async Task<LanguagePreferences> GetLanguagePreferences(ulong steam64, CancellationToken token = default)
    {
        if (_langs == null)
            await ReloadCache(token).ConfigureAwait(false);

        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await using ILanguageDbContext dbContext = new TDbContext();
            LanguagePreferences? pref = await dbContext.LanguagePreferences.FirstOrDefaultAsync(x => x.Steam64 == steam64, token).ConfigureAwait(false);
            return pref ?? new LanguagePreferences
            {
                Steam64 = steam64
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }
    public async Task ReloadCache(CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            List<LanguageInfo> newList = _langs ?? new List<LanguageInfo>();
            await GetLanguages(newList, token).ConfigureAwait(false);
            _langs = newList;
            Languages = newList.AsReadOnly();
            lock (this)
            {
                if (_codes != null)
                    _codes.Clear();
                else _codes = new Dictionary<string, LanguageInfo>(newList.Count, StringComparer.OrdinalIgnoreCase);
                for (int i = _langs.Count - 1; i >= 0; --i)
                    _codes[_langs[i].Code] = _langs[i];
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
    public async Task GetLanguages(IList<LanguageInfo> outputList, CancellationToken token = default)
    {
        await using ILanguageDbContext dbContext = new TDbContext();
        List<LanguageInfo> info = await Include(dbContext.Languages).ToListAsync(token).ConfigureAwait(false);
        if (outputList is List<LanguageInfo> list)
            list.AddRange(info);
        else
        {
            foreach (LanguageInfo info2 in info)
                outputList.Add(info2);
        }
    }
}

public sealed class WarfareMySqlLanguageDataStore : MySqlLanguageDataStore<WarfareDbContext>
{
    public void Subscribe()
    {
        EventDispatcher.PlayerPendingAsync += OnPlayerPending;
    }
    public void Unsubscribe()
    {
        EventDispatcher.PlayerPendingAsync -= OnPlayerPending;
    }
    private async Task OnPlayerPending(PlayerPending e, CancellationToken token)
    {
        e.AsyncData.LanguagePreferences = await GetLanguagePreferences(e.Steam64, token).ConfigureAwait(false);
    }
}