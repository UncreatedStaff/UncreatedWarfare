using SDG.Unturned;
using System;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Kits.Items;
public class TemplateKeyPageItem : IPageKitItem, ITemplateKeyKitItem
{
    public uint PrimaryKey { get; set; }
    public string TemplateKey { get; }
    public byte X { get; }
    public byte Y { get; }
    public byte Rotation { get; }
    public Page Page { get; }
    public TemplateKeyPageItem(uint primaryKey, string templateKey, byte x, byte y, byte rotation, Page page)
    {
        PrimaryKey = primaryKey;
        TemplateKey = templateKey;
        X = x;
        Y = y;
        Rotation = rotation;
        Page = page;
    }

    public TemplateKeyPageItem(TemplateKeyPageItem copy)
    {
        PrimaryKey = 0;
        TemplateKey = copy.TemplateKey;
        X = copy.X;
        Y = copy.Y;
        Rotation = copy.Rotation;
        Page = copy.Page;
    }

    public int CompareTo(object obj)
    {
        if (obj is IPageKitItem jar)
        {
            if (jar is not ITemplateKeyKitItem r)
                return 1;
            return Page != jar.Page ? Page.CompareTo(jar.Page) : TemplateKey.CompareTo(r.TemplateKey);
        }
        if (obj is IClothingKitItem)
            return 1;
        
        return -1;
    }

    public object Clone() => new TemplateKeyPageItem(this);
    public bool Equals(IKitItem? other) => other is TemplateKeyPageItem c &&
                                           string.Equals(TemplateKey, c.TemplateKey, StringComparison.InvariantCultureIgnoreCase) &&
                                           c.X == X && c.Y == Y && c.Rotation == Rotation && c.Page == Page;
    public override bool Equals(object obj) => Equals(obj as TemplateKeyPageItem);
    public override int GetHashCode() => HashCode.Combine(TemplateKey, X, Y, Rotation, Page);

    public KitItemModel CreateModel(Kit kit)
    {
        return new KitItemModel
        {
            Id = PrimaryKey,
            Page = Page,
            X = X,
            Y = Y,
            Rotation = Rotation,
            RedirectVariant = TemplateKey,
            Redirect = null,
            Kit = kit,
            KitId = kit.PrimaryKey
        };
    }

    public ItemAsset? GetItem(Kit? kit, FactionInfo? targetTeam, out byte amount, out byte[] state)
    {
        throw new NotImplementedException();
    }

    public void WriteToModel(KitItemModel model)
    {
        model.Page = Page;
        model.X = X;
        model.Y = Y;
        model.Rotation = Rotation;
        model.RedirectVariant = RedirectVariant;
        model.Redirect = null;
    }
    public override string ToString() => $"TemplateKeyPageItem:           \"{RedirectVariant}\", Pos: {X}, {Y}, Page: {Page}, Rot: {Rotation}";
}
