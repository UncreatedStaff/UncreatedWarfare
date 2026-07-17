using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using Uncreated.Framework.UI;

namespace Uncreated.Warfare.Configuration;

/// <summary>
/// Home for storing asset GUIDs used in gameplay features. Can be overridden for specific maps using map overrides (ex. Assets.Yellowknife.yml).
/// </summary>
/// <remarks>Use <see cref="AssetLink.GetAssetLink(IConfiguration,string)"/> or <see cref="ConfigurationBinder.GetValue{T}(IConfiguration,string)"/> with <see cref="IAssetLink{TAsset}"/> to get assets.</remarks>
public class AssetConfiguration : BaseAlternateConfigurationFile
{
    private readonly ConcurrentDictionary<string, IAssetContainer?> _cache = new ConcurrentDictionary<string, IAssetContainer?>(StringComparer.Ordinal);

    public AssetConfiguration(IServiceProvider serviceProvider)
        : base(serviceProvider, "Assets.yml")
    {

    }

    protected override void HandleChange()
    {
        _cache.Clear();
    }

    internal bool TryGetAssetLinkCached<TAsset>(string key, [NotNullWhen(true)] out IAssetLink<TAsset>? link) where TAsset : Asset
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