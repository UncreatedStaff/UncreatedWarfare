using SDG.NetPak;
using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.SQL;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Kits;

public static class UCInventoryManager
{
    public static IKitItem[] ItemsFromInventory(UCPlayer player, bool addClothes = true, bool addItems = true, bool findAssetRedirects = false)
    {
        ThreadUtil.assertIsGameThread();
        if (!addItems && !addClothes)
            return Array.Empty<IKitItem>();
        List<IKitItem> items = new List<IKitItem>(32);
        RedirectType type;
        if (addItems)
        {
            SDG.Unturned.Items[] ia = player.Player.inventory.items;
            for (byte page = 0; page < PlayerInventory.STORAGE; ++page)
            {
                SDG.Unturned.Items it = ia[page];
                byte ct = it.getItemCount();
                if (ct > 1 && page < PlayerInventory.SLOTS)
                {
                    L.LogWarning("More than one item detected in gun slot: " + (Page)page + ".");
                    ct = 1;
                }
                for (int index = ct - 1; index >= 0; --index)
                {
                    ItemJar jar = it.items[index];
                    ItemAsset asset = jar.GetAsset();
                    if (asset == null)
                        continue;
                    if (findAssetRedirects && (type = TeamManager.GetRedirectInfo(asset.GUID, out _, out string? variant, false)) != RedirectType.None)
                        items.Add(new AssetRedirectPageKitItem(PrimaryKey.NotAssigned, jar.x, jar.y, jar.rot, (Page)page, type, variant));
                    else items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference(asset.GUID), jar.x, jar.y, jar.rot, (Page)page, jar.item.amount, jar.item.state));
                }
            }
        }
        if (addClothes)
        {
            FactionInfo? playerFaction = TeamManager.GetFactionSafe(player.GetTeam());
            PlayerClothing playerClothes = player.Player.clothing;
            if (playerClothes.shirtAsset != null)
            {
                if (findAssetRedirects && playerFaction != null && (type = TeamManager.GetClothingRedirect(playerClothes.shirtAsset.GUID, out string? variant, playerFaction)) != RedirectType.None)
                    items.Add(new AssetRedirectClothingKitItem(PrimaryKey.NotAssigned, type, ClothingType.Shirt, variant));
                else
                    items.Add(new SpecificClothingKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference(playerClothes.shirtAsset.GUID), ClothingType.Shirt, playerClothes.shirtState));
            }
            if (playerClothes.pantsAsset != null)
            {
                if (findAssetRedirects && playerFaction != null && (type = TeamManager.GetClothingRedirect(playerClothes.pantsAsset.GUID, out string? variant, playerFaction)) != RedirectType.None)
                    items.Add(new AssetRedirectClothingKitItem(PrimaryKey.NotAssigned, type, ClothingType.Pants, variant));
                else
                    items.Add(new SpecificClothingKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference(playerClothes.pantsAsset.GUID), ClothingType.Pants, playerClothes.pantsState));
            }
            if (playerClothes.vestAsset != null)
            {
                if (findAssetRedirects && playerFaction != null && (type = TeamManager.GetClothingRedirect(playerClothes.vestAsset.GUID, out string? variant, playerFaction)) != RedirectType.None)
                    items.Add(new AssetRedirectClothingKitItem(PrimaryKey.NotAssigned, type, ClothingType.Vest, variant));
                else
                    items.Add(new SpecificClothingKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference(playerClothes.vestAsset.GUID), ClothingType.Vest, playerClothes.vestState));
            }
            if (playerClothes.hatAsset != null)
            {
                if (findAssetRedirects && playerFaction != null && (type = TeamManager.GetClothingRedirect(playerClothes.hatAsset.GUID, out string? variant, playerFaction)) != RedirectType.None)
                    items.Add(new AssetRedirectClothingKitItem(PrimaryKey.NotAssigned, type, ClothingType.Hat, variant));
                else
                    items.Add(new SpecificClothingKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference(playerClothes.hatAsset.GUID), ClothingType.Hat, playerClothes.hatState));
            }
            if (playerClothes.maskAsset != null)
            {
                if (findAssetRedirects && playerFaction != null && (type = TeamManager.GetClothingRedirect(playerClothes.maskAsset.GUID, out string? variant, playerFaction)) != RedirectType.None)
                    items.Add(new AssetRedirectClothingKitItem(PrimaryKey.NotAssigned, type, ClothingType.Mask, variant));
                else
                    items.Add(new SpecificClothingKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference(playerClothes.maskAsset.GUID), ClothingType.Mask, playerClothes.maskState));
            }
            if (playerClothes.backpackAsset != null)
            {
                if (findAssetRedirects && playerFaction != null && (type = TeamManager.GetClothingRedirect(playerClothes.backpackAsset.GUID, out string? variant, playerFaction)) != RedirectType.None)
                    items.Add(new AssetRedirectClothingKitItem(PrimaryKey.NotAssigned, type, ClothingType.Backpack, variant));
                else
                    items.Add(new SpecificClothingKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference(playerClothes.backpackAsset.GUID), ClothingType.Backpack, playerClothes.backpackState));
            }
            if (playerClothes.glassesAsset != null)
            {
                if (findAssetRedirects && playerFaction != null && (type = TeamManager.GetClothingRedirect(playerClothes.glassesAsset.GUID, out string? variant, playerFaction)) != RedirectType.None)
                    items.Add(new AssetRedirectClothingKitItem(PrimaryKey.NotAssigned, type, ClothingType.Glasses, variant));
                else
                    items.Add(new SpecificClothingKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference(playerClothes.glassesAsset.GUID), ClothingType.Glasses, playerClothes.glassesState));
            }
        }

        return items.ToArray();
    }
    public static void ClearInventoryAndSlots(UCPlayer player, bool clothes = true)
    {
        ClearInventory(player, clothes);
        UpdateSlots(player);
    }
    public static void UpdateSlots(UCPlayer player)
    {
        // removes the primaries/secondaries from the third person model
        player.Player.equipment.sendSlot(0);
        player.Player.equipment.sendSlot(1);
    }
    public static void ClearInventory(UCPlayer player, bool clothes = true)
    {
        ThreadUtil.assertIsGameThread();
        player.ItemTransformations.Clear();
        player.ItemDropTransformations.Clear();
        if (Data.UseFastKits)
        {
            // clears the inventory quickly
            NetId id = player.Player.inventory.GetNetId();
            player.Player.equipment.dequip();

            SDG.Unturned.Items[] inv = player.Player.inventory.items;
            while (inv[0].getItemCount() > 0)
                inv[0].removeItem(0);
            while (inv[1].getItemCount() > 0)
                inv[1].removeItem(0);
            
            byte m = (byte)(PlayerInventory.PAGES - 2);
            for (byte i = PlayerInventory.SLOTS; i < m; ++i)
            {
                byte c = inv[i].getItemCount();
                for (byte it = 0; it < c; ++it)
                    player.SendItemRemove(i, inv[i].items[it]);
            }
            SDG.Unturned.Items pg = inv[PlayerInventory.SLOTS];
            pg.clear();
            bool[,] b = Data.GetItemsSlots(pg);
            for (int x = 0; x < pg.width; ++x)
                for (int y = 0; y < pg.height; ++y)
                    b[x, y] = false;
            for (int i = PlayerInventory.SLOTS + 1; i < m; ++i)
            {
                inv[i].clear();
            }
            if (clothes)
            {
                byte[] blank = Array.Empty<byte>();
                id = player.Player.clothing.GetNetId();
                if (player.Player.clothing.shirt != 0)
                    Data.SendWearShirt!.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), Guid.Empty, 100, blank, false);
                if (player.Player.clothing.pants != 0)
                    Data.SendWearPants!.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), Guid.Empty, 100, blank, false);
                if (player.Player.clothing.hat != 0)
                    Data.SendWearHat!.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), Guid.Empty, 100, blank, false);
                if (player.Player.clothing.backpack != 0)
                    Data.SendWearBackpack!.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), Guid.Empty, 100, blank, false);
                if (player.Player.clothing.vest != 0)
                    Data.SendWearVest!.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), Guid.Empty, 100, blank, false);
                if (player.Player.clothing.mask != 0)
                    Data.SendWearMask!.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), Guid.Empty, 100, blank, false);
                if (player.Player.clothing.glasses != 0)
                    Data.SendWearGlasses!.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), Guid.Empty, 100, blank, false);
            }
        }
        else
        {
            for (byte page = 0; page < PlayerInventory.PAGES - 2; page++)
            {
                byte count = player.Player.inventory.getItemCount(page);

                for (byte index = 0; index < count; index++)
                {
                    player.Player.inventory.removeItem(page, 0);
                }
            }

            if (clothes)
            {
                byte[] blank = Array.Empty<byte>();
                player.Player.clothing.askWearBackpack(0, 0, blank, true);
                player.Player.inventory.removeItem(2, 0);

                player.Player.clothing.askWearGlasses(0, 0, blank, true);
                player.Player.inventory.removeItem(2, 0);

                player.Player.clothing.askWearHat(0, 0, blank, true);
                player.Player.inventory.removeItem(2, 0);

                player.Player.clothing.askWearPants(0, 0, blank, true);
                player.Player.inventory.removeItem(2, 0);

                player.Player.clothing.askWearMask(0, 0, blank, true);
                player.Player.inventory.removeItem(2, 0);

                player.Player.clothing.askWearShirt(0, 0, blank, true);
                player.Player.inventory.removeItem(2, 0);

                player.Player.clothing.askWearVest(0, 0, blank, true);
                player.Player.inventory.removeItem(2, 0);

                byte handcount = player.Player.inventory.getItemCount(2);
                for (byte i = 0; i < handcount; i++)
                {
                    player.Player.inventory.removeItem(2, 0);
                }
            }
        }
    }
    public static void GiveItems(UCPlayer player, IKitItem[] items, bool clear)
    {
        ThreadUtil.assertIsGameThread();

        if (clear)
            ClearInventory(player, true);

        FactionInfo? faction = TeamManager.GetFactionSafe(player.GetTeam());

        if (Data.UseFastKits)
        {
            NetId id = player.Player.clothing.GetNetId();
            byte flag = 0;
            bool hasPlayedEffect = false;
            for (int i = 0; i < items.Length; ++i)
            {
                IKitItem item = items[i];
                if (item is not IClothingKitItem clothingJar)
                    continue;
                ItemAsset? asset = item.GetItem(null, faction, out _, out byte[] state);
                if (asset == null || asset.type != clothingJar.Type.GetItemType())
                    continue;

                if ((flag & (1 << (int)clothingJar.Type)) == 0)
                {
                    flag |= (byte)(1 << (int)clothingJar.Type);
                    ClientInstanceMethod<Guid, byte, byte[], bool>? inv =
                        clothingJar.Type switch
                        {
                            ClothingType.Shirt => Data.SendWearShirt,
                            ClothingType.Pants => Data.SendWearPants,
                            ClothingType.Hat => Data.SendWearHat,
                            ClothingType.Backpack => Data.SendWearBackpack,
                            ClothingType.Vest => Data.SendWearVest,
                            ClothingType.Mask => Data.SendWearMask,
                            ClothingType.Glasses => Data.SendWearGlasses,
                            _ => null
                        };
                    if (inv != null)
                    {
                        inv.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), asset.GUID, 100, state, !hasPlayedEffect);
                        hasPlayedEffect = true;
                    }
                }
            }
            byte[] blank = Array.Empty<byte>();
            for (int i = 0; i < 7; ++i)
            {
                if (((flag >> i) & 1) != 1)
                {
                    ((ClothingType)i switch
                    {
                        ClothingType.Shirt => Data.SendWearShirt,
                        ClothingType.Pants => Data.SendWearPants,
                        ClothingType.Hat => Data.SendWearHat,
                        ClothingType.Backpack => Data.SendWearBackpack,
                        ClothingType.Vest => Data.SendWearVest,
                        ClothingType.Mask => Data.SendWearMask,
                        ClothingType.Glasses => Data.SendWearGlasses,
                        _ => null
                    })?.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), Guid.Empty, 100, blank, false);
                }
            }
            SDG.Unturned.Items[] p = player.Player.inventory.items;
            bool ohi = Data.GetOwnerHasInventory(player.Player.inventory);
            if (ohi)
                Data.SetOwnerHasInventory(player.Player.inventory, false);
            List<(Item, IPageKitItem)>? toAddLater = null;
            for (int i = 0; i < items.Length; ++i)
            {
                IKitItem item = items[i];
                if (item is not IPageKitItem jar)
                    continue;
                ItemAsset? asset = item.GetItem(null, faction, out byte amt, out byte[] state);

                if ((int)jar.Page < PlayerInventory.PAGES - 2 && asset != null)
                {
                    SDG.Unturned.Items page = p[(int)jar.Page];
                    Item itm = new Item(asset.id, amt, 100, state);
                    // ensures multiple items are not put in the slots (causing the ghost gun issue)
                    if (jar.Page is Page.Primary or Page.Secondary)
                    {
                        if (page.getItemCount() > 0)
                        {
                            L.LogWarning("Duplicate " + jar.Page.ToString().ToLowerInvariant() + " defined: " + item + ".");
                            L.Log("[GIVEITEMS] Removing " + (page.items[0].GetAsset().itemName) + " in place of duplicate.");
                            (toAddLater ??= new List<(Item, IPageKitItem)>(2)).Add((page.items[0].item, jar));
                            page.removeItem(0);
                        }
                    }
                    else if (IsOutOfBounds(page, jar.X, jar.Y, asset.size_x, asset.size_y, jar.Rotation))
                    {
                        L.LogWarning("Out of bounds item in " + jar.Page + " defined: " + item + ".");
                        (toAddLater ??= new List<(Item, IPageKitItem)>(2)).Add((itm, jar));
                        continue;
                    }

                    int ic2 = page.getItemCount();
                    for (int j = 0; j < ic2; ++j)
                    {
                        ItemJar? jar2 = page.getItem((byte)j);
                        if (jar2 != null && IsOverlapping(jar.X, jar.Y, asset.size_x, asset.size_y, jar2.x, jar2.y, jar2.size_x, jar2.size_y, jar.Rotation, jar2.rot))
                        {
                            L.LogWarning("Overlapping item in " + jar.Page + " defined: " + item + ".");
                            L.Log("[GIVEITEMS] Removing " + (jar2.GetAsset().itemName) + " (" + jar2.x + ", " + jar2.y + " @ " + jar2.rot + "), in place of duplicate.");
                            page.removeItem((byte)j--);
                            (toAddLater ??= new List<(Item, IPageKitItem)>(2)).Add((jar2.item, jar));
                        }
                    }

                    page.addItem(jar.X, jar.Y, jar.Rotation, itm);
                }
                else
                {
                    L.LogWarning("Unknown asset: " + (jar is ISpecificKitItem i2 ? i2.Item.ToString() : (jar is IAssetRedirectKitItem a2 ? a2.RedirectType.ToString() : jar.ToString()) + "."));
                }
            }

            // try to add removed items later
            if (toAddLater is { Count: > 0 })
            {
                for (int i = 0; i < toAddLater.Count; ++i)
                {
                    (Item item, _) = toAddLater[i];
                    L.LogWarning("Had to re-add item: " + item.GetAsset()?.itemName + ".");
                    if (!player.Player.inventory.tryAddItemAuto(item, false, false, false, !hasPlayedEffect))
                    {
                        ItemManager.dropItem(item, player.Position, !hasPlayedEffect, true, false);
                    }

                    if (!hasPlayedEffect)
                        hasPlayedEffect = true;
                }
            }
            if (ohi)
                Data.SetOwnerHasInventory(player.Player.inventory, true);
            SendPages(player);
        }
        else
        {
            foreach (IKitItem item in items)
            {
                if (item is IClothingKitItem clothing)
                {
                    ItemAsset? asset = item.GetItem(null, faction, out byte amt, out byte[] state);
                    if (asset is null)
                    {
                        L.LogWarning("Unknown asset: " + clothing + ".");
                        return;
                    }
                    if (clothing.Type == ClothingType.Shirt)
                    {
                        if (asset is ItemShirtAsset shirt)
                            player.Player.clothing.askWearShirt(shirt, 100, state, true);
                        else goto e;
                    }
                    else if (clothing.Type == ClothingType.Pants)
                    {
                        if (asset is ItemPantsAsset pants)
                            player.Player.clothing.askWearPants(pants, 100, state, true);
                        else goto e;
                    }
                    else if (clothing.Type == ClothingType.Vest)
                    {
                        if (asset is ItemVestAsset vest)
                            player.Player.clothing.askWearVest(vest, 100, state, true);
                        else goto e;
                    }
                    else if (clothing.Type == ClothingType.Hat)
                    {
                        if (asset is ItemHatAsset hat)
                            player.Player.clothing.askWearHat(hat, 100, state, true);
                        else goto e;
                    }
                    else if (clothing.Type == ClothingType.Mask)
                    {
                        if (asset is ItemMaskAsset mask)
                            player.Player.clothing.askWearMask(mask, 100, state, true);
                        else goto e;
                    }
                    else if (clothing.Type == ClothingType.Backpack)
                    {
                        if (asset is ItemBackpackAsset backpack)
                            player.Player.clothing.askWearBackpack(backpack, 100, state, true);
                        else goto e;
                    }
                    else if (clothing.Type == ClothingType.Glasses)
                    {
                        if (asset is ItemGlassesAsset glasses)
                            player.Player.clothing.askWearGlasses(glasses, 100, state, true);
                        else goto e;
                    }
                    else
                        goto e;

                    continue;
                    e:
                    L.LogWarning("Invalid or mismatched clothing type: " + clothing + ".");
                    Item uitem = new Item(asset.id, amt, 100, state);
                    if (!player.Player.inventory.tryAddItem(uitem, true))
                    {
                        ItemManager.dropItem(uitem, player.Position, false, true, true);
                    }
                }
            }
            foreach (IKitItem item in items)
            {
                if (item is not IClothingKitItem)
                {
                    ItemAsset? asset = item.GetItem(null, faction, out byte amt, out byte[] state);
                    if (asset is null)
                    {
                        L.LogWarning("Unknown asset: " + item + ".");
                        return;
                    }
                    Item uitem = new Item(asset.id, amt, 100, state);

                    if (item is not IPageKitItem jar || !player.Player.inventory.tryAddItem(uitem, jar.X, jar.Y, (byte)jar.Page, jar.Rotation))
                    {
                        if (!player.Player.inventory.tryAddItem(uitem, true))
                        {
                            ItemManager.dropItem(uitem, player.Position, false, true, true);
                        }
                    }
                }
            }
        }
    }
    public static void SendPages(UCPlayer player)
    {
        SDG.Unturned.Items[] il = player.Player.inventory.items;
        Data.SendInventory!.Invoke(player.Player.inventory.GetNetId(), ENetReliability.Reliable, player.Connection,
            writer =>
            {
                for (int i = 0; i < PlayerInventory.PAGES - 2; ++i)
                {
                    SDG.Unturned.Items i2 = il[i];
                    int ct = i2.getItemCount();
                    writer.WriteUInt8(i2.width);
                    writer.WriteUInt8(i2.height);
                    writer.WriteUInt8((byte)ct);
                    for (int j = 0; j < ct; ++j)
                    {
                        ItemJar jar = i2.items[j];
                        writer.WriteUInt8(jar.x);
                        writer.WriteUInt8(jar.y);
                        writer.WriteUInt8(jar.rot);
                        writer.WriteUInt16(jar.item.id);
                        writer.WriteUInt8(jar.item.amount);
                        writer.WriteUInt8(jar.item.quality);
                        writer.WriteUInt8((byte)jar.item.state.Length);
                        writer.WriteBytes(jar.item.state);
                    }
                }
            });
    }
    public static ItemJar? GetHeldItem(this UCPlayer player, out byte page)
    {
        if (player.IsOnline)
        {
            PlayerEquipment eq = player.Player.equipment;
            if (eq.asset != null)
            {
                page = eq.equippedPage;
                return eq.player.inventory.getItem(page, eq.player.inventory.getIndex(page, eq.equipped_x, eq.equipped_y));
            }
        }

        page = byte.MaxValue;
        return null;
    }
    public static bool IsOverlapping(IPageKitItem jar1, IPageKitItem jar2, ItemAsset asset1, ItemAsset asset2) =>
        jar1.Page == jar2.Page &&
        (jar1.Page is Page.Primary or Page.Secondary || 
         IsOverlapping(jar1.X, jar1.Y, asset1.size_x, asset1.size_y, jar2.X, jar2.Y, asset2.size_x, asset2.size_y, jar1.Rotation, jar2.Rotation));
    public static bool IsOverlapping(IPageKitItem jar1, ItemAsset asset1, byte x, byte y, byte sizeX, byte sizeY, byte rotation) =>
        IsOverlapping(jar1.X, jar1.Y, asset1.size_x, asset1.size_y, x, y, sizeX, sizeY, jar1.Rotation, rotation);
    public static bool IsOverlapping(byte posX1, byte posY1, byte sizeX1, byte sizeY1, byte posX2, byte posY2, byte sizeX2, byte sizeY2, byte rotation1, byte rotation2)
    {
        if (rotation1 % 2 == 1)
            (sizeX1, sizeY1) = (sizeY1, sizeX1);
        if (rotation2 % 2 == 1)
            (sizeX2, sizeY2) = (sizeY2, sizeX2);
        for (int x = posX1; x < posX1 + sizeX1; ++x)
        {
            for (int y = posY1; y < posY1 + sizeY1; ++y)
            {
                if (x >= posX2 && x < posX2 + sizeX2 && y >= posY2 && y < posY2 + sizeY2)
                    return true;
            }
        }

        return false;
    }

    public static bool IsOutOfBounds(SDG.Unturned.Items page, ItemJar jar) => IsOutOfBounds(page.width, page.height, jar.x, jar.y, jar.size_x, jar.size_y, jar.rot);
    public static bool IsOutOfBounds(SDG.Unturned.Items page, byte posX, byte posY, byte sizeX, byte sizeY, byte rotation) =>
        IsOutOfBounds(page.width, page.height, posX, posY, sizeX, sizeY, rotation);

    public static bool IsOutOfBounds(byte pageSizeX, byte pageSizeY, byte posX, byte posY, byte sizeX, byte sizeY, byte rotation)
    {
        if ((rotation % 2) == 1)
            (sizeX, sizeY) = (sizeY, sizeX);

        return posX + sizeX > pageSizeX || posY + sizeY > pageSizeY;
    }
    public static void RemoveNumberOfItemsFromStorage(InteractableStorage storage, ushort itemID, int amount)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        int counter = 0;

        for (byte i = (byte)(storage.items.getItemCount() - 1); i >= 0; i--)
        {
            if (storage.items.getItem(i).item.id == itemID)
            {
                counter++;
                storage.items.removeItem(i);

                if (counter == amount)
                    return;
            }
        }
    }
    [Obsolete]
    public static int CountItems(Player player, ushort itemID)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        int count = 0;

        for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
        {
            byte pageCount = player.inventory.getItemCount(page);

            for (byte index = 0; index < pageCount; index++)
            {
                if (player.inventory.getItem(page, index).item.id == itemID)
                {
                    count++;
                }
            }
        }

        return count;
    }
    public static int CountItems(Player player, Guid item)
    {
#pragma warning disable CS0612
        return Assets.find(item) is not ItemAsset asset ? 0 : CountItems(player, asset.id);
#pragma warning restore CS0612
    }
    public static void RemoveSingleItem(UCPlayer player, ushort itemID)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
        {
            byte pageCount = player.Player.inventory.getItemCount(page);

            for (byte index = 0; index < pageCount; index++)
            {
                if (player.Player.inventory.getItem(page, index).item.id == itemID)
                {
                    player.Player.inventory.removeItem(page, index);
                    return;
                }
            }
        }
    }
}
