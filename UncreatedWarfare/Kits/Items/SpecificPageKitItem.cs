using System;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Kits.Items;
public class SpecificPageKitItem : ISpecificPageKitItem
{
    public uint PrimaryKey { get; set; }
    public UnturnedAssetReference Item { get; }
    public byte[] State { get; }
    public byte X { get; }
    public byte Y { get; }
    public byte Rotation { get; }
    public Page Page { get; }
    public byte Amount { get; }
    public SpecificPageKitItem(uint key, UnturnedAssetReference item, byte x, byte y, byte rotation, Page page, byte amount, byte[] state)
    {
        PrimaryKey = key;
        Item = item;
        X = x;
        Y = y;
        Rotation = rotation;
        Page = page;
        Amount = amount;
        State = state;
    }
    public SpecificPageKitItem(SpecificPageKitItem copy)
    {
        PrimaryKey = 0;
        Item = copy.Item;
        X = copy.X;
        Y = copy.Y;
        Rotation = copy.Rotation;
        Page = copy.Page;
        Amount = copy.Amount;
        State = copy.State.CloneBytes();
    }
    public object Clone() => new SpecificPageKitItem(this);
    public int CompareTo(object obj)
    {
        if (obj is IPageKitItem jar)
        {
            if (jar is not ISpecificKitItem)
                return 1;
            return Page != jar.Page ? Page.CompareTo(jar.Page) : jar.Y == Y ? X.CompareTo(jar.X) : Y.CompareTo(jar.Y);
        }
        if (obj is IClothingKitItem)
        {
            return Page is Page.Primary or Page.Secondary ? -1 : 1;
        }

        return -1;
    }
    public override bool Equals(object obj) => Equals(obj as SpecificPageKitItem);
    public bool Equals(IKitItem? other) => other is SpecificPageKitItem c && c.X == X && c.Y == Y && c.Page == Page && c.Amount == Amount && c.Item == Item && c.Rotation == Rotation && CollectionUtility.CompareBytes(c.State, State);
    public override int GetHashCode()
    {
        return HashCode.Combine(Item, X, Y, Rotation, Page, Amount, State.Length);
    }
    public ItemAsset? GetItem(Kit? kit, FactionInfo? targetTeam, out byte amount, out byte[] state)
    {
        if (!Provider.isInitialized) throw new InvalidOperationException("Not loaded.");
        if (Item.TryGetAsset(out ItemAsset item))
        {
            amount = Amount < 1 ? item.amount : Amount;
            state = State is null ? item.getState(EItemOrigin.ADMIN) : State.CloneBytes();
            return item;
        }

        state = Array.Empty<byte>();
        amount = default;
        return null;
    }
    public KitItemModel CreateModel(Kit kit)
    {
        return new KitItemModel
        {
            Id = PrimaryKey,
            Kit = kit,
            KitId = kit.PrimaryKey,
            Amount = Amount,
            X = X,
            Y = Y,
            Rotation = Rotation,
            Item = Item,
            Metadata = State,
            Page = Page
        };
    }
    public void WriteToModel(KitItemModel model)
    {
        model.Amount = Amount;
        model.X = X;
        model.Y = Y;
        model.Rotation = Rotation;
        model.Item = Item;
        model.Metadata = State;
        model.Page = Page;
    }
    public override string ToString() => $"SpecificPageKitItem:           {Item}, Pos: {X}, {Y}, Page: {Page}, Rot: {Rotation}, Amount: {Amount}, State: byte[{State?.Length ?? 0}]";
}
