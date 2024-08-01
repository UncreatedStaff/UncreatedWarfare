using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Configuration;

public class AssetVariantDictionary<TAsset> : Dictionary<string, IAssetLink<TAsset>>, ICloneable where TAsset : Asset
{
    public IAssetLink<TAsset>? Default
    {
        get => TryGetValue(string.Empty, out IAssetLink<TAsset> asset) ? asset : null;
        set { if (value is not null) this[string.Empty] = value; }
    }

    public AssetVariantDictionary()
        : this(0) { }
    public AssetVariantDictionary(int capacity)
        : base(capacity, StringComparer.InvariantCultureIgnoreCase) { }
    public AssetVariantDictionary(IDictionary<string, IAssetLink<TAsset>> dictionary)
        : base(dictionary, StringComparer.InvariantCultureIgnoreCase) { }


    public IAssetLink<TAsset> ResolveRandom()
    {
        return Values.ElementAt(RandomUtility.GetIndex((ICollection)Values));
    }
    public bool TryMatchVariant(Guid item, out string? variant)
    {
        foreach (KeyValuePair<string, IAssetLink<TAsset>> reference in this)
        {
            if (!reference.Value.TryGetGuid(out Guid guid) || item != guid)
                continue;

            variant = reference.Key;
            return true;
        }

        variant = null;
        return false;
    }
    public bool TryResolve(string? variant, out IAssetLink<TAsset> reference)
    {
        reference = Resolve(variant)!;
        return reference is not null;
    }
    public IAssetLink<TAsset>? Resolve(string? variant = null)
    {
        variant ??= string.Empty;
        if (TryGetValue(variant, out IAssetLink<TAsset> asset))
            return asset;

        if (variant.Length > 0 && TryGetValue(string.Empty, out asset))
            return asset;

        return Values.FirstOrDefault();
    }
    object ICloneable.Clone() => Clone();
    public AssetVariantDictionary<TAsset> Clone()
    {
        return new AssetVariantDictionary<TAsset>(this);
    }
}
