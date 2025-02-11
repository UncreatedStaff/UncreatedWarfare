using Microsoft.Extensions.Configuration;
using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Kits.Items;

public static class KitItemUtility
{
    /// <summary>
    /// Create a <see cref="KitItemModel"/> from an <see cref="IItem"/> instance.
    /// </summary>
    public static void CreateKitItemModel(IItem item, KitItemModel model)
    {
        if (item is IKitItem kitItem)
        {
            if (kitItem.KitId != 0 && model.KitId == 0)
                model.KitId = kitItem.KitId;
            if (kitItem.PrimaryKey != 0 && model.Id == 0)
                model.Id = kitItem.PrimaryKey;
        }

        if (item is IClothingItem clothing)
        {
            model.X = null;
            model.Y = null;
            model.Page = null;
            model.Rotation = null;
            model.ClothingSlot = clothing.ClothingType;
        }
        else
        {
            IPageItem page = (IPageItem)item;
            model.ClothingSlot = null;
            model.X = page.X;
            model.Y = page.Y;
            model.Page = page.Page;
            model.Rotation = page.Rotation;
        }

        if (item is IConcreteItem concrete)
        {
            model.Redirect = null;
            model.RedirectVariant = null;
            model.Item = new UnturnedAssetReference(concrete.Item);
            model.Metadata = concrete.State;
            model.Amount = concrete.Amount;
        }
        else
        {
            IRedirectedItem redirected = (IRedirectedItem)item;
            model.Item = null;
            model.Metadata = null;
            model.Amount = null;
            model.Redirect = redirected.Item;
            model.RedirectVariant = redirected.Variant;
        }
    }

    /// <summary>
    /// Create a kit item from an <see cref="IItem"/> instance.
    /// </summary>
    public static IKitItem CreateKitItem(IItem item, uint pk, uint kitId)
    {
        if (item is IKitItem kitItem && kitItem.KitId == kitId && kitItem.PrimaryKey == pk)
            return kitItem;

        if (item is IClothingItem clothing)
        {
            if (item is IConcreteItem concrete)
            {
                return new ConcreteClothingKitItem(pk,
                    kitId,
                    clothing.ClothingType,
                    concrete.Item.Cast<ItemAsset, ItemClothingAsset>(),
                    concrete.State,
                    concrete.Amount,
                    concrete.Quality
                );
            }

            IRedirectedItem redirected = (IRedirectedItem)item;
            return new RedirectedClothingKitItem(pk,
                kitId,
                clothing.ClothingType,
                redirected.Item,
                redirected.Variant
            );
        }
        else
        {
            IPageItem page = (IPageItem)item;
            if (item is IConcreteItem concrete)
            {
                return new ConcretePageKitItem(pk,
                    kitId,
                    page.X,
                    page.Y,
                    page.Page,
                    page.Rotation,
                    concrete.Item,
                    concrete.State,
                    concrete.Amount,
                    concrete.Quality
                );
            }

            IRedirectedItem redirected = (IRedirectedItem)item;
            return new RedirectedPageKitItem(pk,
                kitId,
                page.X,
                page.Y,
                page.Page,
                page.Rotation,
                redirected.Item,
                redirected.Variant
            );
        }
    }

    /// <summary>
    /// Read an item from a configuration file.
    /// </summary>
    public static IItem? ReadItem(IConfiguration section)
    {
        ClothingType clothing = section.GetValue("Clothing", (ClothingType)255);
        bool isClothing = EnumUtility.ValidateValidField(clothing);

        byte x = 0, y = 0, rot = 0;
        Page page = Page.Primary;
        if (!isClothing)
        {
            x = section.GetValue<byte>("X");
            y = section.GetValue<byte>("Y");
            page = section.GetValue("Page", (Page)255);
            rot = section.GetValue<byte>("Rotation");

            if (page == (Page)255)
                return null;
        }

        RedirectType redirect = section.GetValue("Redirect", RedirectType.None);
        string? redirectVariant = null;
        bool isConcrete = redirect == RedirectType.None;
        IAssetLink<ItemAsset>? item = null;
        byte quality = 0, amount = 0;
        byte[]? state = null;
        if (isConcrete)
        {
            item = section.GetAssetLink<ItemAsset>("Item");
            quality = section.GetValue<byte>("Quality", 100);
            amount = section.GetValue<byte>("Amount", 255);
            string? state64 = section["State"];
            if (!string.IsNullOrWhiteSpace(state64))
            {
                try
                {
                    state = Convert.FromBase64String(state64);
                }
                catch (FormatException)
                {
                    state = null;
                }
            }
        }
        else
        {
            redirectVariant = section["Variant"];
            if (string.IsNullOrWhiteSpace(redirectVariant))
                redirectVariant = null;
        }

        if (isConcrete && !item!.isValid)
            return null;

        if (isClothing)
        {
            return isConcrete
                ? new ConcreteClothingItem(clothing, item!.Cast<ItemAsset, ItemClothingAsset>(), state, amount, quality)
                : new RedirectedClothingItem(clothing, redirect, redirectVariant);
        }

        return isConcrete
            ? new ConcretePageItem(x, y, page, rot, item!, state, amount, quality)
            : new RedirectedPageItem(x, y, page, rot, redirect, redirectVariant);
    }

    /// <summary>
    /// Checks if a slot can be binded as a hotkey. Valid slots are 3-9 and 0.
    /// </summary>
    /// <param name="slot">The slot number, as in which key you would press to activate the hotkey.</param>
    public static bool ValidSlot(byte slot)
    {
        return slot == 0 || slot > PlayerInventory.SLOTS && slot < 10;
    }

    /// <summary>
    /// Get the hotkey index of a slot number.
    /// </summary>
    /// <param name="slot">The slot number, as in which key you would press to activate the hotkey.</param>
    /// <returns>An index 0-7.</returns>
    public static byte GetHotkeyIndex(byte slot)
    {
        if (!ValidSlot(slot))
        {
            return byte.MaxValue;
        }

        // 0 should be counted as slot 10, nelson removes the first two from hotkeys because slots.
        return slot == 0 ? (byte)(9 - PlayerInventory.SLOTS) : (byte)(slot - PlayerInventory.SLOTS - 1);
    }

    /// <summary>
    /// Checks if an item with the given <paramref name="asset"/> in the given <paramref name="page"/> can have a hotkey binded to it.
    /// </summary>
    public static bool CanBindHotkeyTo(ItemAsset asset, Page page)
    {
        return (byte)page >= PlayerInventory.SLOTS && asset.canPlayerEquip && asset.slot.canEquipInPage((byte)page);
    }
}