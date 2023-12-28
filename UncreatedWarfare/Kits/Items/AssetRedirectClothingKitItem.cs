using SDG.Unturned;
using System;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Kits.Items;

public class AssetRedirectClothingKitItem : IClothingKitItem, IAssetRedirectKitItem
{
    public uint PrimaryKey { get; set; }
    public RedirectType RedirectType { get; }
    public string? RedirectVariant { get; }
    public ClothingType Type { get; }
    public AssetRedirectClothingKitItem(uint key, RedirectType redirectType, ClothingType type, string? redirectVariant)
    {
        PrimaryKey = key;
        RedirectType = redirectType;
        Type = type;
        RedirectVariant = redirectVariant;
    }
    public AssetRedirectClothingKitItem(AssetRedirectClothingKitItem copy)
    {
        PrimaryKey = 0;
        RedirectType = copy.RedirectType;
        RedirectVariant = copy.RedirectVariant;
        Type = copy.Type;
    }
    public object Clone() => new AssetRedirectClothingKitItem(this);
    public int CompareTo(object obj)
    {
        if (obj is IClothingKitItem cjar)
        {
            if (Type != cjar.Type)
                return Type.CompareTo(cjar.Type);
        }
        if (obj is IPageKitItem jar)
        {
            return jar.Page is Page.Primary or Page.Secondary ? 1 : -1;
        }

        return -1;
    }
    public ItemAsset? GetItem(Kit? kit, FactionInfo? targetTeam, out byte amount, out byte[] state)
    {
        if (!UCWarfare.IsLoaded) throw new SingletonUnloadedException(typeof(UCWarfare));
        return TeamManager.GetRedirectInfo(RedirectType, RedirectVariant ?? string.Empty, kit?.FactionInfo, targetTeam, out state, out amount);
    }

    public KitItemModel CreateModel(Kit kit)
    {
        return new KitItemModel
        {
            Id = PrimaryKey,
            Kit = kit,
            KitId = kit.PrimaryKey,
            ClothingSlot = Type,
            RedirectVariant = RedirectVariant,
            Redirect = RedirectType
        };
    }
    public void WriteToModel(KitItemModel model)
    {
        model.ClothingSlot = Type;
        model.RedirectVariant = RedirectVariant;
        model.Redirect = RedirectType;
    }
    public bool Equals(IKitItem? other) => other is AssetRedirectClothingKitItem c && c.Type == Type && c.RedirectType == RedirectType;
    public override bool Equals(object obj) => Equals(obj as AssetRedirectClothingKitItem);
    public override int GetHashCode()
    {
        return HashCode.Combine(Type, RedirectType, RedirectVariant);
    }
    public override string ToString() => $"AssetRedirectClothingKitItem:  {RedirectType} ({RedirectVariant}), Type: {Type}";
}