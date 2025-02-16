using System;
using System.Globalization;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Models.Assets;
public readonly struct UnturnedAssetReference
{
    public Guid Guid { get; }
    public ushort Id { get; }

    public string? GetFriendlyName()
    {
        // multi-threaded access may become a problem here but assets aren't updated during play so it should be okay.
        if (Provider.isInitialized && !SDG.Unturned.Assets.isLoading)
            return GetAsset<Asset>()?.FriendlyName;

        return null;
    }


    public UnturnedAssetReference(IAssetContainer? assetContainer)
    {
        if (assetContainer == null)
        {
            return;
        }

        Guid = assetContainer.Guid;
        Id = Guid == Guid.Empty ? assetContainer.Id : (ushort)0;
    }
    public UnturnedAssetReference(Asset? asset)
    {
        if (asset == null)
        {
            return;
        }

        Guid = asset.GUID;
        Id = Guid == Guid.Empty ? asset.id : (ushort)0;
    }
    public UnturnedAssetReference(Guid guid)
    {
        Guid = guid;
        Id = 0;
    }
    public UnturnedAssetReference(string guid)
    {
        this = Parse(guid);
    }
    public UnturnedAssetReference(ushort id)
    {
        Id = id;
        Guid = Guid.Empty;
    }
    public UnturnedAssetReference()
    {
        Id = 0;
        Guid = Guid.Empty;
    }

    public bool TryGetAsset<TAsset>(out TAsset asset) where TAsset : Asset
    {
        asset = GetAsset<TAsset>()!;
        return asset != null;
    }
    public TAsset? GetAsset<TAsset>() where TAsset : Asset
    {
        if (Id != 0)
        {
            EAssetType type = AssetUtility.GetAssetCategory<TAsset>();
            if (type == EAssetType.NONE)
                return null;

            return SDG.Unturned.Assets.find(type, Id) as TAsset;
        }

        return SDG.Unturned.Assets.find<TAsset>(Guid);
    }
    public IAssetLink<TAsset> GetAssetLink<TAsset>() where TAsset : Asset
    {
        return Guid != Guid.Empty ? AssetLink.Create<TAsset>(Guid) : AssetLink.Create<TAsset>(Id);
    }
    public static UnturnedAssetReference FromAssetLink<TAsset>(IAssetLink<TAsset>? assetReference) where TAsset : Asset
    {
        return assetReference != null ? assetReference.Guid != Guid.Empty ? new UnturnedAssetReference(assetReference.Guid) : (assetReference.Id != 0 ? new UnturnedAssetReference(assetReference.Id) : default) : default;
    }
    public static bool operator !=(UnturnedAssetReference left, UnturnedAssetReference right) => !(left == right);
    public static bool operator ==(UnturnedAssetReference left, UnturnedAssetReference right)
    {
        return left.Guid == right.Guid && left.Id == right.Id;
    }

    public bool Equals(Asset asset)
    {
        if (asset == null)
        {
            return Id == 0 && Guid == Guid.Empty;
        }

        if (Guid != Guid.Empty && asset.GUID == Guid)
            return true;

        if (Id != 0 && asset.id == Id)
            return true;

        return false;
    }

    public bool Equals(UnturnedAssetReference other) => other.Guid == Guid && other.Id == Id;
    public bool Equals(Guid guid) => Guid != Guid.Empty && guid == Guid;
    public bool Equals(ushort id) => Id != 0 && id == Id;
    public override bool Equals(object? obj) => obj switch
    {
        Guid g => g == Guid,
        ushort i => i == Id,
        UnturnedAssetReference r => r.Guid == Guid && r.Id == Id,
        IAssetLink<Asset> l => l.Guid == Guid && l.Id == Id,
        _ => false
    };

    public override int GetHashCode() => Guid != Guid.Empty ? Guid.GetHashCode() : Id.GetHashCode();
    public static UnturnedAssetReference Parse(string text) => Parse(text.AsSpan());
    public static UnturnedAssetReference Parse(ReadOnlySpan<char> text)
    {
        if (TryParse(text, out UnturnedAssetReference result))
            return result;

        throw new FormatException("Failed to parse UnturnedAssetReference. Expected a UInt16 ID or Guid.");
    }

    public static bool TryParse(string text, out UnturnedAssetReference result) => TryParse(text.AsSpan(), out result);
    public static bool TryParse(ReadOnlySpan<char> text, out UnturnedAssetReference result)
    {
        int index = text.IndexOf('\0');
        if (index != -1)
        {
            text = text[..index];
        }

        if (text.IsWhiteSpace())
        {
            result = default;
            return true;
        }

        if (ushort.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out ushort id))
        {
            result = new UnturnedAssetReference(id);
            return true;
        }

        if (Guid.TryParse(text, out Guid guid))
        {
            result = new UnturnedAssetReference(guid);
            return true;
        }

        result = default;
        return false;
    }
    public override string ToString()
    {
        return Id != 0 ? Id.ToString(CultureInfo.InvariantCulture) : Guid.ToString("N", CultureInfo.InvariantCulture);
    }
}
