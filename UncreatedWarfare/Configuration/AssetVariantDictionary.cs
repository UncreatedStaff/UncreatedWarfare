using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Uncreated.Warfare.Configuration;
public class AssetVariantDictionary<TAsset> : Dictionary<string, JsonAssetReference<TAsset>>, ICloneable where TAsset : Asset
{
    public JsonAssetReference<TAsset>? Default
    {
        get => TryGetValue(string.Empty, out JsonAssetReference<TAsset> asset) ? asset : null;
        set { if (value is not null) this[string.Empty] = value; }
    }

    public AssetVariantDictionary()
        : this(0) { }
    public AssetVariantDictionary(int capacity)
        : base(capacity, StringComparer.InvariantCultureIgnoreCase) { }
    public AssetVariantDictionary(IDictionary<string, JsonAssetReference<TAsset>> dictionary)
        : base(dictionary, StringComparer.InvariantCultureIgnoreCase) { }


    public JsonAssetReference<TAsset> ResolveRandom()
    {
        int randomValue = UCWarfare.IsLoaded ? UnityEngine.Random.Range(0, Count) : new Random().Next(0, Count);
        return Values.ElementAt(randomValue);
    }
    public JsonAssetReference<TAsset> Resolve(string? variant = null)
    {
        variant ??= string.Empty;
        if (TryGetValue(variant, out JsonAssetReference<TAsset> asset))
            return asset;

        if (variant.Length > 0 && TryGetValue(string.Empty, out asset))
            return asset;

        return null!;
    }
    object ICloneable.Clone() => Clone();
    public AssetVariantDictionary<TAsset> Clone()
    {
        return new AssetVariantDictionary<TAsset>(this);
    }
}
