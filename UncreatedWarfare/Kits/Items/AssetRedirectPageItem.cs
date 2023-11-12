using SDG.Unturned;
using System;
using Uncreated.SQL;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Kits.Items;
public class AssetRedirectPageKitItem : IAssetRedirectKitItem, IPageKitItem
{
    public PrimaryKey PrimaryKey { get; set; }
    public RedirectType RedirectType { get; }
    public string? RedirectVariant { get; }
    public byte X { get; }
    public byte Y { get; }
    public byte Rotation { get; }
    public Page Page { get; }

    public AssetRedirectPageKitItem(PrimaryKey key, byte x, byte y, byte rotation, Page page, RedirectType redirectType, string? redirectVariant)
    {
        PrimaryKey = key;
        X = x;
        Y = y;
        Rotation = rotation;
        Page = page;
        RedirectType = redirectType;
        RedirectVariant = redirectVariant;
    }
    public AssetRedirectPageKitItem(AssetRedirectPageKitItem item)
    {
        PrimaryKey = PrimaryKey.NotAssigned;
        RedirectType = item.RedirectType;
        RedirectVariant = item.RedirectVariant;
        X = item.X;
        Y = item.Y;
        Rotation = item.Rotation;
        Page = item.Page;
    }

    public int CompareTo(object obj)
    {
        if (obj is IPageKitItem jar)
        {
            if (jar is not IAssetRedirectKitItem r)
                return -1;
            return Page != jar.Page ? Page.CompareTo(jar.Page) : RedirectType.CompareTo(r.RedirectType);
        }
        if (obj is IClothingKitItem)
        {
            return Page is Page.Primary or Page.Secondary ? -1 : 1;
        }

        return -1;
    }

    public bool Equals(IKitItem? other) => other is AssetRedirectPageKitItem c &&
                                           c.RedirectType == RedirectType &&
                                           string.Equals(RedirectVariant, c.RedirectVariant, StringComparison.InvariantCultureIgnoreCase) &&
                                           c.X == X && c.Y == Y && c.Rotation == Rotation && c.Page == Page;
    public override bool Equals(object obj) => Equals(obj as AssetRedirectPageKitItem);
    public override int GetHashCode() => HashCode.Combine(RedirectType, RedirectVariant, X, Y, Rotation, Page);
    public object Clone() => new AssetRedirectPageKitItem(this);
    public ItemAsset? GetItem(Kit? kit, FactionInfo? targetTeam, out byte amount, out byte[] state)
    {
        if (!UCWarfare.IsLoaded) throw new SingletonUnloadedException(typeof(UCWarfare));
        return TeamManager.GetRedirectInfo(RedirectType, RedirectVariant ?? string.Empty, TeamManager.GetFactionInfo(kit?.Faction), targetTeam, out state, out amount);
    }
    public KitItemModel CreateModel(Kit kit)
    {
        return new KitItemModel
        {
            Id = PrimaryKey,
            Page = Page,
            X = X,
            Y = Y,
            Rotation = Rotation,
            RedirectVariant = RedirectVariant,
            Redirect = RedirectType,
            Kit = kit
        };
    }
    public void WriteToModel(KitItemModel model)
    {
        model.Page = Page;
        model.X = X;
        model.Y = Y;
        model.Rotation = Rotation;
        model.RedirectVariant = RedirectVariant;
        model.Redirect = RedirectType;
    }
    public override string ToString() => $"AssetRedirectPageKitItem:      {RedirectType}, Pos: {X}, {Y}, Page: {Page}, Rot: {Rotation}";
}
