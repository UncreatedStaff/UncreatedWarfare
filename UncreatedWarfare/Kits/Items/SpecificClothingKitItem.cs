﻿using SDG.Unturned;
using System;
using Uncreated.Framework;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Kits.Items;
public class SpecificClothingKitItem : IClothingKitItem, ISpecificKitItem
{
    public uint PrimaryKey { get; set; }
    public ClothingType Type { get; }
    public UnturnedAssetReference Item { get; }
    public byte[] State { get; }
    public SpecificClothingKitItem(uint primaryKey, UnturnedAssetReference item, ClothingType type, byte[] state)
    {
        PrimaryKey = primaryKey;
        Item = item;
        Type = type;
        State = state;
    }
    public SpecificClothingKitItem(SpecificClothingKitItem copy)
    {
        PrimaryKey = 0;
        Type = copy.Type;
        Item = copy.Item;
        State = copy.State;
    }
    public object Clone() => new SpecificClothingKitItem(this);
    public int CompareTo(object obj)
    {
        if (obj is IClothingKitItem cjar)
        {
            if (Type != cjar.Type)
                return Type.CompareTo(cjar.Type);
            return 0;
        }
        if (obj is IPageKitItem jar)
        {
            return jar.Page is Page.Primary or Page.Secondary ? 1 : -1;
        }

        return -1;
    }
    public bool Equals(IKitItem? other) => other is SpecificClothingKitItem c && c.Type == Type && c.Item == Item && c.State.CompareBytes(State);
    public override bool Equals(object obj) => Equals(obj as IKitItem);
    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Item, State.Length);
    }
    public ItemAsset? GetItem(Kit? kit, FactionInfo? targetTeam, out byte amount, out byte[] state)
    {
        if (!UCWarfare.IsLoaded) throw new SingletonUnloadedException(typeof(UCWarfare));

        amount = 1;

        if (Item.TryGetAsset(out ItemAsset item))
        {
            state = State is null ? item.getState(EItemOrigin.ADMIN) : Util.CloneBytes(State);
            return item;
        }

        state = Array.Empty<byte>();
        return null;
    }
    public KitItemModel CreateModel(Kit kit)
    {
        return new KitItemModel
        {
            Id = PrimaryKey,
            Kit = kit,
            KitId = kit.PrimaryKey,
            ClothingSlot = Type,
            Item = Item,
            Metadata = State
        };
    }
    public void WriteToModel(KitItemModel model)
    {
        model.ClothingSlot = Type;
        model.Item = Item;
        model.Metadata = State;
    }
    public override string ToString() => $"SpecificClothingKitItem:       {Item}, Type: {Type}, State: byte[{State?.Length ?? 0}]";
}
