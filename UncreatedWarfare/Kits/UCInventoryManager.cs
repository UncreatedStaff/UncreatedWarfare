using HarmonyLib;
using SDG.NetPak;
using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Reflection;
using Uncreated.Framework;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Kits;

public static class UCInventoryManager
{
    public static void OnLoad()
    {
        try
        {
            Harmony.Patches.Patcher.Patch(
                typeof(PlayerEquipment).GetMethod("InitializePlayer", BindingFlags.Instance | BindingFlags.NonPublic),
                prefix: new HarmonyMethod(
                    typeof(UCInventoryManager).GetMethod("GlassesFix", BindingFlags.Static | BindingFlags.NonPublic)));
        }
        catch (Exception ex)
        {
            L.LogError("Error patching PlayerEquipment.InitializePlayer()");
            L.LogError(ex);
        }
    }
    public static IKitItem[] ItemsFromInventory(UCPlayer player, bool addClothes = true, bool addItems = true, bool findAssetRedirects = false)
    {
        ThreadUtil.assertIsGameThread();
        if (!addItems && !addClothes)
            return Array.Empty<IKitItem>();
        List<IKitItem> items = new List<IKitItem>(32);
        RedirectType type;
        if (addItems)
        {
            Items[] ia = player.Player.inventory.items;
            for (byte page = 0; page < PlayerInventory.STORAGE; ++page)
            {
                Items it = ia[page];
                byte ct = it.getItemCount();
                for (byte index = 0; index < ct; ++index)
                {
                    ItemJar jar = it.items[index];
                    ItemAsset asset = jar.GetAsset();
                    if (asset == null)
                        continue;
                    if (findAssetRedirects && (type = TeamManager.GetItemRedirect(asset.GUID)) != RedirectType.None)
                        items.Add(new AssetRedirectItem(type, jar.x, jar.y, jar.rot, (Page)page));
                    else items.Add(new PageItem(asset.GUID, jar.x, jar.y, jar.rot, jar.item.state, jar.item.amount, (Page)page));
                }
            }
        }
        if (addClothes)
        {
            PlayerClothing playerClothes = player.Player.clothing;
            if (playerClothes.shirtAsset != null)
            {
                if (findAssetRedirects && (type = TeamManager.GetClothingRedirect(playerClothes.shirtAsset.GUID)) != RedirectType.None)
                    items.Add(new AssetRedirectClothing(type, ClothingType.Shirt));
                else
                    items.Add(new ClothingItem(playerClothes.shirtAsset.GUID, ClothingType.Shirt, playerClothes.shirtState));
            }
            if (playerClothes.pantsAsset != null)
            {
                if (findAssetRedirects && (type = TeamManager.GetClothingRedirect(playerClothes.pantsAsset.GUID)) != RedirectType.None)
                    items.Add(new AssetRedirectClothing(type, ClothingType.Pants));
                else
                    items.Add(new ClothingItem(playerClothes.pantsAsset.GUID, ClothingType.Pants, playerClothes.pantsState));
            }
            if (playerClothes.vestAsset != null)
            {
                if (findAssetRedirects && (type = TeamManager.GetClothingRedirect(playerClothes.vestAsset.GUID)) != RedirectType.None)
                    items.Add(new AssetRedirectClothing(type, ClothingType.Vest));
                else
                    items.Add(new ClothingItem(playerClothes.vestAsset.GUID, ClothingType.Vest, playerClothes.vestState));
            }
            if (playerClothes.hatAsset != null)
            {
                if (findAssetRedirects && (type = TeamManager.GetClothingRedirect(playerClothes.hatAsset.GUID)) != RedirectType.None)
                    items.Add(new AssetRedirectClothing(type, ClothingType.Hat));
                else
                    items.Add(new ClothingItem(playerClothes.hatAsset.GUID, ClothingType.Hat, playerClothes.hatState));
            }
            if (playerClothes.maskAsset != null)
            {
                if (findAssetRedirects && (type = TeamManager.GetClothingRedirect(playerClothes.maskAsset.GUID)) != RedirectType.None)
                    items.Add(new AssetRedirectClothing(type, ClothingType.Mask));
                else
                    items.Add(new ClothingItem(playerClothes.maskAsset.GUID, ClothingType.Mask, playerClothes.maskState));
            }
            if (playerClothes.backpackAsset != null)
            {
                if (findAssetRedirects && (type = TeamManager.GetClothingRedirect(playerClothes.backpackAsset.GUID)) != RedirectType.None)
                    items.Add(new AssetRedirectClothing(type, ClothingType.Backpack));
                else
                    items.Add(new ClothingItem(playerClothes.backpackAsset.GUID, ClothingType.Backpack, playerClothes.backpackState));
            }
            if (playerClothes.glassesAsset != null)
            {
                if (findAssetRedirects && (type = TeamManager.GetClothingRedirect(playerClothes.glassesAsset.GUID)) != RedirectType.None)
                    items.Add(new AssetRedirectClothing(type, ClothingType.Glasses));
                else
                    items.Add(new ClothingItem(playerClothes.glassesAsset.GUID, ClothingType.Glasses, playerClothes.glassesState));
            }
        }

        return items.ToArray();
    }

    public static void ClearInventory(UCPlayer player, bool clothes = true)
    {
        ThreadUtil.assertIsGameThread();
        if (Data.UseFastKits)
        {
            // clears the inventory quickly
            NetId id = player.Player.inventory.GetNetId();
            player.Player.equipment.dequip();

            ITransportConnection tc = player.Connection;
            Items[] inv = player.Player.inventory.items;
            inv[0].removeItem(0);
            inv[1].removeItem(0);
            
            byte m = (byte)(PlayerInventory.PAGES - 2);
            for (byte i = PlayerInventory.SLOTS; i < m; ++i)
            {
                byte c = inv[i].getItemCount();
                for (byte it = 0; it < c; ++it)
                    player.SendItemRemove(i, inv[i].items[it]);
            }
            Items pg = inv[PlayerInventory.SLOTS];
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
                    Data.SendWearShirt!.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.EnumerateClients_Remote(), Guid.Empty, 100, blank, false);
                if (player.Player.clothing.pants != 0)
                    Data.SendWearPants!.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.EnumerateClients_Remote(), Guid.Empty, 100, blank, false);
                if (player.Player.clothing.hat != 0)
                    Data.SendWearHat!.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.EnumerateClients_Remote(), Guid.Empty, 100, blank, false);
                if (player.Player.clothing.backpack != 0)
                    Data.SendWearBackpack!.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.EnumerateClients_Remote(), Guid.Empty, 100, blank, false);
                if (player.Player.clothing.vest != 0)
                    Data.SendWearVest!.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.EnumerateClients_Remote(), Guid.Empty, 100, blank, false);
                if (player.Player.clothing.mask != 0)
                    Data.SendWearMask!.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.EnumerateClients_Remote(), Guid.Empty, 100, blank, false);
                if (player.Player.clothing.glasses != 0)
                    Data.SendWearGlasses!.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.EnumerateClients_Remote(), Guid.Empty, 100, blank, false);
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
    public static void LoadClothes(UCPlayer player, List<ClothingItem> clothes)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        player.Player.equipment.dequip();
        ulong team = player.GetTeam();
        byte[] blank = Array.Empty<byte>();
        NetId id = player.Player.clothing.GetNetId();
        byte flag = 0;
        bool hasPlayedEffect = false;
        for (int i = 0; i < clothes.Count; ++i)
        {
            ClothingItem clothing = clothes[i];
            flag |= (byte)(1 << (int)clothing.Type);
            ClientInstanceMethod<Guid, byte, byte[], bool>? inv =
                clothing.Type switch
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
                inv.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.EnumerateClients_Remote(), TeamManager.CheckClothingAssetRedirect(clothing.Item, team), 100, blank, !hasPlayedEffect);
                hasPlayedEffect = true;
            }
        }
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
                })?.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.EnumerateClients_Remote(), Guid.Empty, 100, blank, false);
            }
        }
    }
    public static void SendPages(UCPlayer player)
    {
        Items[] il = player.Player.inventory.items;
        Data.SendInventory!.Invoke(player.Player.inventory.GetNetId(), ENetReliability.Reliable, player.Connection,
            writer =>
            {
                for (int i = 0; i < PlayerInventory.PAGES - 2; ++i)
                {
                    Items i2 = il[i];
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

    private static void GlassesFix(PlayerEquipment __instance)
    {
        ItemGlassesAsset? ga = __instance.player.clothing.glassesAsset;
        if (ga != null && ga.vision != ELightingVision.NONE &&
            (__instance.player.clothing.glassesState == null
            || __instance.player.clothing.glassesState.Length < 1))
        {
            __instance.player.clothing.glassesState = new byte[1];
            __instance.ReceiveToggleVision();
        }
    }
}
