using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Uncreated.Framework.UI;

namespace Uncreated.Warfare.Configuration;

/// <summary>
/// Home for storing asset GUIDs, etc.
/// </summary>
/// <remarks>Use <see cref="AssetLink.GetAssetLink(IConfiguration,string)"/> or <see cref="ConfigurationBinder.GetValue{T}(IConfiguration,string)"/> with <see cref="IAssetLink{TAsset}"/> to get assets.</remarks>
public class AssetConfiguration : IConfiguration, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly IDisposable _reloadToken;
    private readonly ConcurrentDictionary<string, IAssetContainer?> _cache = new ConcurrentDictionary<string, IAssetContainer?>(StringComparer.Ordinal);

    public string FilePath { get; }
    public IConfiguration UnderlyingConfiguration { get; }


    public event Action<IConfiguration>? OnChange;
    public AssetConfiguration(WarfareModule module)
    {
        FilePath = Path.Combine(module.HomeDirectory, "Assets.yml");

        ConfigurationBuilder builder = new ConfigurationBuilder();
        ConfigurationHelper.AddSourceWithMapOverride(builder, module.FileProvider, FilePath);
        _configuration = builder.Build();

        UnderlyingConfiguration = _configuration;

        _reloadToken = ChangeToken.OnChange(
            _configuration.GetReloadToken,
            static me =>
            {
                me._cache.Clear();
                UniTask.Create(me.InvokeChange);
            },
            this);
    }

    private async UniTask InvokeChange()
    {
        await UniTask.SwitchToMainThread();
        OnChange?.Invoke(this);
    }

    public string? this[string key] { get => _configuration[key]; set => _configuration[key] = value; }
    public IConfigurationSection GetSection(string key) => _configuration.GetSection(key);
    public IEnumerable<IConfigurationSection> GetChildren() => _configuration.GetChildren();
    public IChangeToken GetReloadToken() => _configuration.GetReloadToken();
    public void Dispose()
    {
        _reloadToken.Dispose();
        if (_configuration is IDisposable disp)
            disp.Dispose();
    }

    internal bool TryGetAssetLinkCached<TAsset>(string key, [MaybeNullWhen(false)] out IAssetLink<TAsset> link) where TAsset : Asset
    {
        IAssetContainer? container = _cache.GetOrAdd(
            key,
            static (key, me) => me.GetValue<IAssetLink<TAsset>>(key),
            this
        );

        if (container is IAssetLink<TAsset> l)
        {
            link = l;
            return true;
        }

        if (container == null)
        {
            link = null;
            return false;
        }

        container = _cache.AddOrUpdate(
            key,
            static (key, me) => me.GetValue<IAssetLink<TAsset>>(key),
            static (key, _, me) => me.GetValue<IAssetLink<TAsset>>(key),
            this
        );

        link = (container as IAssetLink<TAsset>)!;
        return link != null;
    }
}