using System;
using System.Collections.Generic;
using Uncreated.Warfare.Models.Localization;

namespace Uncreated.Warfare.Translations.Languages;
public class NullLanguageDataStore : ICachableLanguageDataStore
{
    public Task Initialize(CancellationToken token = default) => Task.CompletedTask;

    public void WriteWait() { }

    public void WriteRelease() { }

    public Task<LanguageInfo?> GetInfo(string name, bool exactOnly, bool allowCache, CancellationToken token = default) => Task.FromResult<LanguageInfo?>(null);

    public Task<LanguageInfo?> GetInfo(uint key, bool allowCache, CancellationToken token = default) => Task.FromResult<LanguageInfo?>(null);

    public Task WriteWaitAsync(CancellationToken token = default)
    {
        return Task.CompletedTask;
    }

    public Task AddOrUpdateInfo(LanguageInfo info, CancellationToken token = default)
    {
        return Task.CompletedTask;
    }

    public Task UpdateLanguagePreferences(LanguagePreferences preferences, CancellationToken token = default)
    {
        return Task.CompletedTask;
    }

    public Task<LanguagePreferences> GetLanguagePreferences(ulong steam64, CancellationToken token = default) =>
        Task.FromResult(new LanguagePreferences { Steam64 = steam64 });

    public Task GetLanguages(IList<LanguageInfo> outputList, CancellationToken token = default)
    {
        return Task.CompletedTask;
    }

    public IReadOnlyList<LanguageInfo> Languages => Array.Empty<LanguageInfo>();

    public LanguageInfo? GetInfoCached(string name, bool exactOnly = true)
    {
        return null;
    }

    public LanguageInfo? GetInfoCached(uint key)
    {
        return null;
    }

    public Task ReloadCache(CancellationToken token = default)
    {
        return Task.CompletedTask;
    }
}
