using SDG.NetPak;
using SDG.NetTransport;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Util.Region;

namespace Uncreated.Warfare.Util;

/// <summary>
/// Helper functions for dropped items and inventory pages.
/// </summary>
public static class ItemUtility
{
    internal static event ItemDestroyed? OnItemDestroyed;
    internal delegate void ItemDestroyed(in ItemInfo item, bool despawned, bool pickedUp, CSteamID pickUpPlayer);

    /// <summary>
    /// Enumerate items along the grid instead of the order they were added.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public static ItemPageIterator EnumerateAlongGrid(Items items, bool reverse = false)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        ThreadUtil.assertIsGameThread();

        return new ItemPageIterator(items, reverse);
    }

    /// <summary>
    /// Enumerate through all dropped items around the center of the level.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static DroppedItemIterator EnumerateDroppedItems()
    {
        ThreadUtil.assertIsGameThread();

        return new DroppedItemIterator();
    }

    /// <summary>
    /// Enumerate through all dropped items around the center of the level.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static DroppedItemIterator EnumerateDroppedItems(Vector3 center)
    {
        ThreadUtil.assertIsGameThread();

        if (!Regions.tryGetCoordinate(center, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        return new DroppedItemIterator(x, y);
    }

    /// <summary>
    /// Enumerate through all dropped items around the center of the level.
    /// </summary>
    /// <remarks>The square enumerated will have a size of <c><paramref name="maxRegionDistance"/> * 2 + 1</c> regions.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static DroppedItemIterator EnumerateDroppedItems(Vector3 center, byte maxRegionDistance)
    {
        ThreadUtil.assertIsGameThread();

        if (!Regions.tryGetCoordinate(center, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        return new DroppedItemIterator(x, y, maxRegionDistance);
    }

    /// <summary>
    /// Enumerate through all dropped items around the given <paramref name="region"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static DroppedItemIterator EnumerateDroppedItems(RegionCoord region)
    {
        ThreadUtil.assertIsGameThread();

        return new DroppedItemIterator(region.x, region.y);
    }

    /// <summary>
    /// Enumerate through all dropped items around the region <paramref name="x"/>, <paramref name="y"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static DroppedItemIterator EnumerateDroppedItems(byte x, byte y)
    {
        ThreadUtil.assertIsGameThread();

        return new DroppedItemIterator(x, y);
    }

    /// <summary>
    /// Enumerate through all dropped items around the given <paramref name="region"/>.
    /// </summary>
    /// <remarks>The square enumerated will have a size of <c><paramref name="maxRegionDistance"/> * 2 + 1</c> regions.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static DroppedItemIterator EnumerateDroppedItems(RegionCoord region, byte maxRegionDistance)
    {
        ThreadUtil.assertIsGameThread();

        return new DroppedItemIterator(region.x, region.y, maxRegionDistance);
    }

    /// <summary>
    /// Enumerate through all dropped items around the region <paramref name="x"/>, <paramref name="y"/>.
    /// </summary>
    /// <remarks>The square enumerated will have a size of <c><paramref name="maxRegionDistance"/> * 2 + 1</c> regions.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static DroppedItemIterator EnumerateDroppedItems(byte x, byte y, byte maxRegionDistance)
    {
        ThreadUtil.assertIsGameThread();

        return new DroppedItemIterator(x, y, maxRegionDistance);
    }

    /// <summary>
    /// Check if there is at least one item in the player's inventory that has the given <paramref name="asset"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public static bool HasItem(Player player, IAssetLink<ItemAsset> asset)
    {
        return CountItems(player, asset, 1) > 0;
    }

    /// <summary>
    /// Check if there is at least one item in the player's inventory that matches a predicate.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public static bool HasItem(Player player, Predicate<ItemJar> itemSelector)
    {
        return CountItems(player, itemSelector, 1) > 0;
    }

    /// <summary>
    /// Count the number of items in the player's inventory that have the given <paramref name="asset"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public static int CountItems(Player player, IAssetLink<ItemAsset> asset, int max = -1)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        ThreadUtil.assertIsGameThread();

        PlayerInventory inv = player.inventory;

        int totalItemCount = 0;

        int pageCt = PlayerInventory.PAGES - PlayerInventory.STORAGE;
        for (byte page = 0; page < pageCt; ++page)
        {
            int ct = inv.getItemCount(page);
            for (byte i = 0; i < ct; ++i)
            {
                ItemJar jar = inv.getItem(page, i);

                if (!(jar.item.id != 0 && asset.MatchId(jar.item.id) || asset.MatchAsset(jar.GetAsset())))
                    continue;

                ++totalItemCount;
                if (max >= 0 && totalItemCount >= max)
                {
                    return totalItemCount;
                }
            }
        }

        return totalItemCount;
    }

    /// <summary>
    /// Count the number of items in the player's inventory that match a predicate.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public static int CountItems(Player player, Predicate<ItemJar> itemSelector, int max = -1)
    {
        if (itemSelector == null)
            throw new ArgumentNullException(nameof(itemSelector));

        ThreadUtil.assertIsGameThread();

        PlayerInventory inv = player.inventory;

        int totalItemCount = 0;

        int pageCt = PlayerInventory.PAGES - PlayerInventory.STORAGE;
        for (byte page = 0; page < pageCt; ++page)
        {
            int ct = inv.getItemCount(page);
            for (byte i = 0; i < ct; ++i)
            {
                if (!itemSelector(inv.getItem(page, i)))
                    continue;

                ++totalItemCount;
                if (max >= 0 && totalItemCount >= max)
                {
                    return totalItemCount;
                }
            }
        }

        return totalItemCount;
    }

    /// <summary>
    /// Destroy all items on the map.
    /// </summary>
    /// <param name="despawned">If the item should be considered as having despawned, instead of destroyed.</param>
    /// <returns>Number of items destroyed.</returns>
    public static int DestroyAllDroppedItems(bool despawned)
    {
        int ct = 0;
        foreach (ItemInfo item in EnumerateDroppedItems())
        {
            ++ct;
            OnItemDestroyed?.Invoke(in item, despawned, false, CSteamID.Nil);
        }

        ItemManager.askClearAllItems();
        return ct;
    }

    /// <summary>
    /// Destroy the number of items in the given <paramref name="radius"/> matching an <paramref name="asset"/>.
    /// </summary>
    /// <param name="pickUpPlayer">The player that is picking up the items, if any.</param>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    /// <returns>Number of items destroyed.</returns>
    public static int DestroyDroppedItemsInRange(Vector3 position, float radius, IAssetLink<ItemAsset> asset, bool playTakeItemSound, int max = -1, bool horizontalDistanceOnly = false, CSteamID pickUpPlayer = default)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        ThreadUtil.assertIsGameThread();

        float sqrRadius = radius * radius;
        int totalItemsFound = 0;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            ItemRegion region = ItemManager.regions[coord.x, coord.y];
            for (int i = 0; i < region.items.Count; ++i)
            {
                ItemData item = region.items[i];
                Vector3 pos = item.point;

                float sqrDist = MathUtility.SquaredDistance(in position, in pos, horizontalDistanceOnly);

                if (sqrDist > sqrRadius || !asset.MatchId(item.item.id))
                    continue;

                ++totalItemsFound;
                RemoveDroppedItemUnsafe(coord.x, coord.y, i, false, pickUpPlayer, playTakeItemSound);
                if (max >= 0 && totalItemsFound >= max)
                {
                    return totalItemsFound;
                }
            }
        }

        return totalItemsFound;
    }

    /// <summary>
    /// Destroy the number of items in the given <paramref name="radius"/> matching a <paramref name="itemSelector"/>.
    /// </summary>
    /// <param name="pickUpPlayer">The player that is picking up the items, if any.</param>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    /// <returns>Number of items destroyed.</returns>
    public static int DestroyDroppedItemsInRange(Vector3 position, float radius, Predicate<ItemData> itemSelector, bool playTakeItemSound, int max = -1, bool horizontalDistanceOnly = false, CSteamID pickUpPlayer = default)
    {
        if (itemSelector == null)
            throw new ArgumentNullException(nameof(itemSelector));

        ThreadUtil.assertIsGameThread();

        float sqrRadius = radius * radius;
        int totalItemsFound = 0;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            ItemRegion region = ItemManager.regions[coord.x, coord.y];
            for (int i = 0; i < region.items.Count; ++i)
            {
                ItemData item = region.items[i];
                Vector3 pos = item.point;

                float sqrDist = MathUtility.SquaredDistance(in position, in pos, horizontalDistanceOnly);

                if (sqrDist > sqrRadius || !itemSelector(item))
                    continue;

                ++totalItemsFound;
                RemoveDroppedItemUnsafe(coord.x, coord.y, i, false, pickUpPlayer, playTakeItemSound);
                if (max >= 0 && totalItemsFound >= max)
                {
                    return totalItemsFound;
                }
            }
        }

        return totalItemsFound;
    }

    /// <summary>
    /// Destroy the number of items in the given <paramref name="radius"/> matching a <paramref name="itemSelector"/>.
    /// </summary>
    /// <param name="pickUpPlayer">The player that is picking up the items, if any.</param>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    /// <returns>Number of items destroyed.</returns>
    public static int DestroyDroppedItemsInRange(Vector3 position, float radius, bool playTakeItemSound, int max = -1, bool horizontalDistanceOnly = false, CSteamID pickUpPlayer = default)
    {
        ThreadUtil.assertIsGameThread();

        float sqrRadius = radius * radius;
        int totalItemsFound = 0;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            ItemRegion region = ItemManager.regions[coord.x, coord.y];
            for (int i = 0; i < region.items.Count; ++i)
            {
                ItemData item = region.items[i];
                Vector3 pos = item.point;

                float sqrDist = MathUtility.SquaredDistance(in position, in pos, horizontalDistanceOnly);

                if (sqrDist > sqrRadius)
                    continue;

                ++totalItemsFound;
                RemoveDroppedItemUnsafe(coord.x, coord.y, i, false, pickUpPlayer, playTakeItemSound);
                if (max >= 0 && totalItemsFound >= max)
                {
                    return totalItemsFound;
                }
            }
        }

        return totalItemsFound;
    }

    /// <summary>
    /// Property clean up and replicate destroying (taking) a dropped item.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public static bool DestroyDroppedItem(ItemData item, bool despawned, bool playTakeItemSound = false)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        ThreadUtil.assertIsGameThread();

        ItemInfo itemInfo = FindItem(item.instanceID, item.point);

        if (!itemInfo.HasValue)
            return false;

        RegionCoord region = itemInfo.Coord;
        RemoveDroppedItemUnsafe(region.x, region.y, itemInfo.Index, despawned, CSteamID.Nil, playTakeItemSound);
        return true;
    }

    /// <summary>
    /// Property clean up and replicate destroying (taking) a dropped item that was picked up by a player.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public static bool DestroyDroppedItem(ItemData item, bool despawned, WarfarePlayer pickUpPlayer, bool playTakeItemSound = false)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        ThreadUtil.assertIsGameThread();

        ItemInfo itemInfo = FindItem(item.instanceID, item.point);

        if (!itemInfo.HasValue)
            return false;

        RegionCoord region = itemInfo.Coord;
        RemoveDroppedItemUnsafe(region.x, region.y, itemInfo.Index, despawned, despawned ? CSteamID.Nil : pickUpPlayer.Steam64, playTakeItemSound);
        return true;
    }

    /// <summary>
    /// Property clean up and replicate destroying (taking) a dropped item.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public static void DestroyDroppedItem(byte x, byte y, int index, bool despawned, bool playTakeItemSound = false)
    {
        ThreadUtil.assertIsGameThread();

        if (!Regions.checkSafe(x, y))
            throw new ArgumentOutOfRangeException("(x, y)", "X and/or Y coordinate invalid.");

        if (index >= ItemManager.regions[x, y].items.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "No item with the given index.");

        RemoveDroppedItemUnsafe(x, y, index, despawned, CSteamID.Nil, playTakeItemSound);
    }

    /// <summary>
    /// Property clean up and replicate destroying (taking) a dropped item that was picked up by a player.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public static void DestroyDroppedItem(byte x, byte y, int index, bool despawned, WarfarePlayer pickUpPlayer, bool playTakeItemSound)
    {
        ThreadUtil.assertIsGameThread();

        if (!Regions.checkSafe(x, y))
            throw new ArgumentOutOfRangeException("(x, y)", "X and/or Y coordinate invalid.");

        if (index >= ItemManager.regions[x, y].items.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "No item with the given index.");

        RemoveDroppedItemUnsafe(x, y, index, despawned, despawned ? CSteamID.Nil : pickUpPlayer.Steam64, playTakeItemSound);
    }

    internal static void RemoveDroppedItemUnsafe(byte x, byte y, int index, bool despawned, CSteamID pickUpPlayer, bool playTakeItemSound)
    {
        ItemRegion region = ItemManager.regions[x, y];
        ItemData item = region.items[index];

        Data.SendDestroyItem.Invoke(ENetReliability.Reliable, Regions.GatherRemoteClientConnections(x, y, ItemManager.ITEM_REGIONS), x, y, item.instanceID, playTakeItemSound);

        region.items.RemoveAt(index);

        ItemInfo itemInfo = new ItemInfo(item, index, new RegionCoord(x, y));
        OnItemDestroyed?.Invoke(itemInfo, despawned, !despawned && pickUpPlayer.GetEAccountType() == EAccountType.k_EAccountTypeIndividual, despawned ? CSteamID.Nil : pickUpPlayer);
    }

    internal static void InvokeOnItemDestroyed(in ItemInfo item, bool despawned, bool pickedUp, CSteamID pickUpPlayer)
    {
        OnItemDestroyed?.Invoke(item, despawned, pickedUp, pickUpPlayer);
    }

    /// <summary>
    /// Get an array of kit item abstractions from a player's inventory and clothing.
    /// </summary>
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
                        items.Add(new AssetRedirectPageKitItem(0u, jar.x, jar.y, jar.rot, (Page)page, type, variant));
                    else items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference(asset.GUID), jar.x, jar.y, jar.rot, (Page)page, jar.item.amount, jar.item.state));
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
                    items.Add(new AssetRedirectClothingKitItem(0u, type, ClothingType.Shirt, variant));
                else
                    items.Add(new SpecificClothingKitItem(0u, new UnturnedAssetReference(playerClothes.shirtAsset.GUID), ClothingType.Shirt, playerClothes.shirtState));
            }
            if (playerClothes.pantsAsset != null)
            {
                if (findAssetRedirects && playerFaction != null && (type = TeamManager.GetClothingRedirect(playerClothes.pantsAsset.GUID, out string? variant, playerFaction)) != RedirectType.None)
                    items.Add(new AssetRedirectClothingKitItem(0u, type, ClothingType.Pants, variant));
                else
                    items.Add(new SpecificClothingKitItem(0u, new UnturnedAssetReference(playerClothes.pantsAsset.GUID), ClothingType.Pants, playerClothes.pantsState));
            }
            if (playerClothes.vestAsset != null)
            {
                if (findAssetRedirects && playerFaction != null && (type = TeamManager.GetClothingRedirect(playerClothes.vestAsset.GUID, out string? variant, playerFaction)) != RedirectType.None)
                    items.Add(new AssetRedirectClothingKitItem(0u, type, ClothingType.Vest, variant));
                else
                    items.Add(new SpecificClothingKitItem(0u, new UnturnedAssetReference(playerClothes.vestAsset.GUID), ClothingType.Vest, playerClothes.vestState));
            }
            if (playerClothes.hatAsset != null)
            {
                if (findAssetRedirects && playerFaction != null && (type = TeamManager.GetClothingRedirect(playerClothes.hatAsset.GUID, out string? variant, playerFaction)) != RedirectType.None)
                    items.Add(new AssetRedirectClothingKitItem(0u, type, ClothingType.Hat, variant));
                else
                    items.Add(new SpecificClothingKitItem(0u, new UnturnedAssetReference(playerClothes.hatAsset.GUID), ClothingType.Hat, playerClothes.hatState));
            }
            if (playerClothes.maskAsset != null)
            {
                if (findAssetRedirects && playerFaction != null && (type = TeamManager.GetClothingRedirect(playerClothes.maskAsset.GUID, out string? variant, playerFaction)) != RedirectType.None)
                    items.Add(new AssetRedirectClothingKitItem(0u, type, ClothingType.Mask, variant));
                else
                    items.Add(new SpecificClothingKitItem(0u, new UnturnedAssetReference(playerClothes.maskAsset.GUID), ClothingType.Mask, playerClothes.maskState));
            }
            if (playerClothes.backpackAsset != null)
            {
                if (findAssetRedirects && playerFaction != null && (type = TeamManager.GetClothingRedirect(playerClothes.backpackAsset.GUID, out string? variant, playerFaction)) != RedirectType.None)
                    items.Add(new AssetRedirectClothingKitItem(0u, type, ClothingType.Backpack, variant));
                else
                    items.Add(new SpecificClothingKitItem(0u, new UnturnedAssetReference(playerClothes.backpackAsset.GUID), ClothingType.Backpack, playerClothes.backpackState));
            }
            if (playerClothes.glassesAsset != null)
            {
                if (findAssetRedirects && playerFaction != null && (type = TeamManager.GetClothingRedirect(playerClothes.glassesAsset.GUID, out string? variant, playerFaction)) != RedirectType.None)
                    items.Add(new AssetRedirectClothingKitItem(0u, type, ClothingType.Glasses, variant));
                else
                    items.Add(new SpecificClothingKitItem(0u, new UnturnedAssetReference(playerClothes.glassesAsset.GUID), ClothingType.Glasses, playerClothes.glassesState));
            }
        }

        return items.ToArray();
    }

    /// <summary>
    /// Clear the playery's inventory and update their third-person model for other players.
    /// </summary>
    public static void ClearInventoryAndSlots(WarfarePlayer player, bool clothes = true)
    {
        ClearInventory(player, clothes);
        UpdateSlots(player);
    }

    /// <summary>
    /// Update the player's third-person model for other players.
    /// </summary>
    public static void UpdateSlots(WarfarePlayer player)
    {
        ThreadUtil.assertIsGameThread();
        if (!player.IsOnline)
            return;
        // removes the primaries/secondaries from the third person model
        player.UnturnedPlayer.equipment.sendSlot(0);
        player.UnturnedPlayer.equipment.sendSlot(1);
    }
    
    /// <summary>
    /// Effeciently remove all items and clothes from a player's inventory.
    /// </summary>
    public static void ClearInventory(WarfarePlayer player, bool clothes = true)
    {
        ThreadUtil.assertIsGameThread();
        if (!player.IsOnline)
            return;
        player.ItemTransformations.Clear();
        player.ItemDropTransformations.Clear();
        Player nativePlayer = player.UnturnedPlayer;
        if (Data.UseFastKits)
        {
            // clears the inventory quickly
            nativePlayer.equipment.dequip();

            Items[] inv = nativePlayer.inventory.items;

            // clear slots
            while (inv[0].getItemCount() > 0)
                inv[0].removeItem(0);

            while (inv[1].getItemCount() > 0)
                inv[1].removeItem(0);

            byte maxPage = (byte)(PlayerInventory.PAGES - 2);

            for (byte i = PlayerInventory.SLOTS; i < maxPage; ++i)
            {
                byte c = inv[i].getItemCount();
                for (byte it = 0; it < c; ++it)
                    player.SendItemRemove(i, inv[i].items[it]);
            }

            Items pg = inv[PlayerInventory.SLOTS];
            pg.clear();

            bool[,] itemMask = Data.GetItemsSlots(pg);
            for (int x = 0; x < pg.width; ++x)
            {
                for (int y = 0; y < pg.height; ++y)
                {
                    itemMask[x, y] = false;
                }
            }

            for (int i = PlayerInventory.SLOTS + 1; i < maxPage; ++i)
            {
                inv[i].clear();
            }

            if (clothes)
            {
                byte[] blank = Array.Empty<byte>();
                NetId id = nativePlayer.clothing.GetNetId();
                if (nativePlayer.clothing.shirt != 0)
                    Data.SendWearShirt!.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), Guid.Empty, 100, blank, false);
                if (nativePlayer.clothing.pants != 0)
                    Data.SendWearPants!.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), Guid.Empty, 100, blank, false);
                if (nativePlayer.clothing.hat != 0)
                    Data.SendWearHat!.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), Guid.Empty, 100, blank, false);
                if (nativePlayer.clothing.backpack != 0)
                    Data.SendWearBackpack!.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), Guid.Empty, 100, blank, false);
                if (nativePlayer.clothing.vest != 0)
                    Data.SendWearVest!.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), Guid.Empty, 100, blank, false);
                if (nativePlayer.clothing.mask != 0)
                    Data.SendWearMask!.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), Guid.Empty, 100, blank, false);
                if (nativePlayer.clothing.glasses != 0)
                    Data.SendWearGlasses!.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), Guid.Empty, 100, blank, false);
            }
        }
        else
        {
            for (byte page = 0; page < PlayerInventory.PAGES - 2; page++)
            {
                byte count = nativePlayer.inventory.getItemCount(page);

                for (byte index = 0; index < count; index++)
                {
                    nativePlayer.inventory.removeItem(page, 0);
                }
            }

            if (clothes)
            {
                byte[] blank = Array.Empty<byte>();
                nativePlayer.clothing.askWearBackpack(0, 0, blank, true);
                nativePlayer.inventory.removeItem(2, 0);

                nativePlayer.clothing.askWearGlasses(0, 0, blank, true);
                nativePlayer.inventory.removeItem(2, 0);

                nativePlayer.clothing.askWearHat(0, 0, blank, true);
                nativePlayer.inventory.removeItem(2, 0);

                nativePlayer.clothing.askWearPants(0, 0, blank, true);
                nativePlayer.inventory.removeItem(2, 0);

                nativePlayer.clothing.askWearMask(0, 0, blank, true);
                nativePlayer.inventory.removeItem(2, 0);

                nativePlayer.clothing.askWearShirt(0, 0, blank, true);
                nativePlayer.inventory.removeItem(2, 0);

                nativePlayer.clothing.askWearVest(0, 0, blank, true);
                nativePlayer.inventory.removeItem(2, 0);

                byte handcount = nativePlayer.inventory.getItemCount(2);
                for (byte i = 0; i < handcount; i++)
                {
                    nativePlayer.inventory.removeItem(2, 0);
                }
            }
        }
    }
    
    /// <summary>
    /// Replace a player's inventory with the given kit item abstractions.
    /// </summary>
    public static void GiveItems(WarfarePlayer player, IKitItem[] items, bool clear)
    {
        ThreadUtil.assertIsGameThread();
        if (!player.IsOnline)
            return;

        if (clear)
            ClearInventory(player, true);

        FactionInfo? faction = TeamManager.GetFactionSafe(player.GetTeam());
        Player nativePlayer = player.UnturnedPlayer;

        if (Data.UseFastKits)
        {
            NetId id = nativePlayer.clothing.GetNetId();
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

                if ((flag & (1 << (int)clothingJar.Type)) != 0)
                    continue;
                
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

                if (inv == null)
                    continue;

                inv.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), asset.GUID, 100, state, !hasPlayedEffect);
                hasPlayedEffect = true;
            }

            byte[] blank = Array.Empty<byte>();
            for (int i = 0; i < 7; ++i)
            {
                if (((flag >> i) & 1) == 1)
                    continue;

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

            Items[] p = nativePlayer.inventory.items;

            bool oldOwnerHasInventoryValue = Data.GetOwnerHasInventory(nativePlayer.inventory);
            if (oldOwnerHasInventoryValue)
            {
                Data.SetOwnerHasInventory(nativePlayer.inventory, false);
            }

            List<(Item, IPageKitItem)>? toAddLater = null;
            for (int i = 0; i < items.Length; ++i)
            {
                IKitItem item = items[i];
                if (item is not IPageKitItem jar)
                    continue;

                ItemAsset? asset = item.GetItem(null, faction, out byte amt, out byte[] state);
                if ((int)jar.Page < PlayerInventory.PAGES - 2 && asset != null)
                {
                    Items page = p[(int)jar.Page];
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
                        if (jar2 == null || !IsOverlapping(jar.X, jar.Y, asset.size_x, asset.size_y, jar2.x, jar2.y, jar2.size_x, jar2.size_y, jar.Rotation, jar2.rot))
                            continue;

                        L.LogWarning("Overlapping item in " + jar.Page + " defined: " + item + ".");
                        L.Log("[GIVEITEMS] Removing " + (jar2.GetAsset().itemName) + " (" + jar2.x + ", " + jar2.y + " @ " + jar2.rot + "), in place of duplicate.");
                        page.removeItem((byte)j--);
                        (toAddLater ??= new List<(Item, IPageKitItem)>(2)).Add((jar2.item, jar));
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
                    if (!nativePlayer.inventory.tryAddItemAuto(item, false, false, false, !hasPlayedEffect))
                    {
                        ItemManager.dropItem(item, player.Position, !hasPlayedEffect, true, false);
                    }

                    if (!hasPlayedEffect)
                        hasPlayedEffect = true;
                }
            }

            if (oldOwnerHasInventoryValue)
            {
                Data.SetOwnerHasInventory(nativePlayer.inventory, true);
            }

            SendPages(player);
        }
        else
        {
            foreach (IKitItem item in items)
            {
                if (item is not IClothingKitItem clothing)
                    continue;

                ItemAsset? asset = item.GetItem(null, faction, out byte amt, out byte[] state);
                if (asset is null)
                {
                    L.LogWarning("Unknown asset: " + clothing + ".");
                    return;
                }
                if (clothing.Type == ClothingType.Shirt)
                {
                    if (asset is ItemShirtAsset shirt)
                        nativePlayer.clothing.askWearShirt(shirt, 100, state, true);
                    else goto error;
                }
                else if (clothing.Type == ClothingType.Pants)
                {
                    if (asset is ItemPantsAsset pants)
                        nativePlayer.clothing.askWearPants(pants, 100, state, true);
                    else goto error;
                }
                else if (clothing.Type == ClothingType.Vest)
                {
                    if (asset is ItemVestAsset vest)
                        nativePlayer.clothing.askWearVest(vest, 100, state, true);
                    else goto error;
                }
                else if (clothing.Type == ClothingType.Hat)
                {
                    if (asset is ItemHatAsset hat)
                        nativePlayer.clothing.askWearHat(hat, 100, state, true);
                    else goto error;
                }
                else if (clothing.Type == ClothingType.Mask)
                {
                    if (asset is ItemMaskAsset mask)
                        nativePlayer.clothing.askWearMask(mask, 100, state, true);
                    else goto error;
                }
                else if (clothing.Type == ClothingType.Backpack)
                {
                    if (asset is ItemBackpackAsset backpack)
                        nativePlayer.clothing.askWearBackpack(backpack, 100, state, true);
                    else goto error;
                }
                else if (clothing.Type == ClothingType.Glasses)
                {
                    if (asset is ItemGlassesAsset glasses)
                        nativePlayer.clothing.askWearGlasses(glasses, 100, state, true);
                    else goto error;
                }
                else
                    goto error;

                continue;

                error:
                L.LogWarning("Invalid or mismatched clothing type: " + clothing + ".");
                Item uitem = new Item(asset.id, amt, 100, state);
                if (!nativePlayer.inventory.tryAddItem(uitem, true))
                {
                    ItemManager.dropItem(uitem, player.Position, false, true, true);
                }
            }
            foreach (IKitItem item in items)
            {
                if (item is IClothingKitItem)
                    continue;

                ItemAsset? asset = item.GetItem(null, faction, out byte amt, out byte[] state);
                if (asset is null)
                {
                    L.LogWarning("Unknown asset: " + item + ".");
                    return;
                }
                Item uitem = new Item(asset.id, amt, 100, state);

                if (item is IPageKitItem jar && nativePlayer.inventory.tryAddItem(uitem, jar.X, jar.Y, (byte)jar.Page, jar.Rotation))
                {
                    continue;
                }

                if (!nativePlayer.inventory.tryAddItem(uitem, true))
                {
                    ItemManager.dropItem(uitem, player.Position, false, true, true);
                }
            }
        }
    }

    /// <summary>
    /// Manually send the entire page via RPC to save on bandwidth.
    /// </summary>
    public static void SendPages(WarfarePlayer player)
    {
        ThreadUtil.assertIsGameThread();
        if (!player.IsOnline)
            return;
        Items[] il = player.UnturnedPlayer.inventory.items;
        Data.SendInventory!.Invoke(player.UnturnedPlayer.inventory.GetNetId(), ENetReliability.Reliable, player.Connection,
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

    /// <summary>
    /// Get the <see cref="ItemJar"/> that's actively held by the player.
    /// </summary>
    public static ItemJar? GetHeldItem(WarfarePlayer player, out Page page)
    {
        ThreadUtil.assertIsGameThread();
        if (player.IsOnline)
        {
            PlayerEquipment eq = player.UnturnedPlayer.equipment;
            if (eq.asset != null)
            {
                byte pg = eq.equippedPage;
                page = (Page)pg;
                return eq.player.inventory.getItem(pg, eq.player.inventory.getIndex(pg, eq.equipped_x, eq.equipped_y));
            }
        }

        page = (Page)byte.MaxValue;
        return null;
    }

    /// <summary>
    /// Check if two items are overlapping.
    /// </summary>
    public static bool IsOverlapping(IPageKitItem jar1, IPageKitItem jar2, ItemAsset asset1, ItemAsset asset2)
    {
        return jar1.Page == jar2.Page &&
               (jar1.Page is Page.Primary or Page.Secondary
                || IsOverlapping(jar1.X, jar1.Y, asset1.size_x, asset1.size_y, jar2.X, jar2.Y, asset2.size_x, asset2.size_y, jar1.Rotation, jar2.Rotation)
               );
    }

    /// <summary>
    /// Check if two items are overlapping.
    /// </summary>
    public static bool IsOverlapping(IPageKitItem jar1, ItemAsset asset1, byte x, byte y, byte sizeX, byte sizeY, byte rotation) =>
        IsOverlapping(jar1.X, jar1.Y, asset1.size_x, asset1.size_y, x, y, sizeX, sizeY, jar1.Rotation, rotation);

    /// <summary>
    /// Check if two items are overlapping.
    /// </summary>
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

    /// <summary>
    /// Check if an item is out of bounds of the page.
    /// </summary>
    public static bool IsOutOfBounds(Items page, ItemJar jar) => IsOutOfBounds(page.width, page.height, jar.x, jar.y, jar.size_x, jar.size_y, jar.rot);

    /// <summary>
    /// Check if an item is out of bounds of the page.
    /// </summary>
    public static bool IsOutOfBounds(Items page, byte posX, byte posY, byte sizeX, byte sizeY, byte rotation) =>
        IsOutOfBounds(page.width, page.height, posX, posY, sizeX, sizeY, rotation);

    /// <summary>
    /// Check if an item is out of bounds of the page.
    /// </summary>
    public static bool IsOutOfBounds(byte pageSizeX, byte pageSizeY, byte posX, byte posY, byte sizeX, byte sizeY, byte rotation)
    {
        if ((rotation % 2) == 1)
            (sizeX, sizeY) = (sizeY, sizeX);

        return posX + sizeX > pageSizeX || posY + sizeY > pageSizeY;
    }

    /// <summary>
    /// Find a item by it's instance ID.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static ItemInfo FindItem(uint instanceId)
    {
        return FindItem(instanceId, (byte)(Regions.WORLD_SIZE / 2), (byte)(Regions.WORLD_SIZE / 2));
    }

    /// <summary>
    /// Find a item by it's instance ID, with help from a position to prevent having to search every region.
    /// </summary>
    /// <remarks>All regions will be searched if it's not found near the expected position.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static ItemInfo FindItem(uint instanceId, Vector3 expectedPosition)
    {
        return Regions.tryGetCoordinate(expectedPosition, out byte x, out byte y)
            ? FindItem(instanceId, x, y)
            : FindItem(instanceId, (byte)(Regions.WORLD_SIZE / 2), (byte)(Regions.WORLD_SIZE / 2));
    }

    /// <summary>
    /// Find a item by it's instance ID, with help from an expected region to prevent having to search every region.
    /// </summary>
    /// <remarks>All regions will be searched if it's not found in the expected region.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static ItemInfo FindItem(uint instanceId, byte expectedRegionX, byte expectedRegionY)
    {
        ThreadUtil.assertIsGameThread();

        SurroundingRegionsIterator iterator = RegionUtility.EnumerateRegions(expectedRegionX, expectedRegionY);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            ItemRegion region = ItemManager.regions[coord.x, coord.y];
            for (int i = 0; i < region.items.Count; ++i)
            {
                if (region.items[i].instanceID == instanceId)
                    return new ItemInfo(region.items[i], i, coord);
            }
        }

        return new ItemInfo(null, -1, new RegionCoord(expectedRegionX, expectedRegionY));
    }

    /// <summary>
    /// Find a item by it's instance ID, with help from an expected region to prevent having to search every region.
    /// </summary>
    /// <remarks>All regions will be searched if it's not found in the expected region.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static ItemInfo FindItem(uint instanceId, IAssetLink<ItemAsset> expectedAsset, Vector3 expectedPosition)
    {
        ThreadUtil.assertIsGameThread();

        ItemInfo foundByPosition = default;

        SurroundingRegionsIterator iterator = RegionUtility.EnumerateRegions(expectedPosition);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            ItemRegion region = ItemManager.regions[coord.x, coord.y];
            for (int i = 0; i < region.items.Count; ++i)
            {
                ItemData item = region.items[i];

                if (item.instanceID == instanceId && expectedAsset.MatchId(item.item.id))
                    return new ItemInfo(item, i, coord);

                Vector3 pos = item.point;
                if (!pos.IsNearlyEqual(expectedPosition, 0.1f) || !expectedAsset.MatchId(item.item.id))
                    continue;

                // if not found or the one found is farther from the expected point than this one
                if (foundByPosition.Item == null || (foundByPosition.Item.point - expectedPosition).sqrMagnitude > (pos - expectedPosition).sqrMagnitude)
                {
                    foundByPosition = new ItemInfo(item, i, coord);
                }
            }
        }

        if (foundByPosition.Item != null)
        {
            return foundByPosition;
        }

        if (!Regions.tryGetCoordinate(expectedPosition, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        return new ItemInfo(null, -1, new RegionCoord(x, y));
    }

    /// <summary>
    /// Check for a nearby item with the given <paramref name="asset"/> to <paramref name="position"/> within the given <paramref name="radius"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static bool IsItemInRange(Vector3 position, float radius, IAssetLink<ItemAsset> asset, bool horizontalDistanceOnly = false)
    {
        return GetClosestItemInRange(position, radius, asset, horizontalDistanceOnly).Item != null;
    }

    /// <summary>
    /// Check for a nearby item matching a predicate to <paramref name="position"/> within the given <paramref name="radius"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static bool IsItemInRange(Vector3 position, float radius, Predicate<ItemData> itemSelector, bool horizontalDistanceOnly = false)
    {
        return GetClosestItemWhere(position, radius, itemSelector, horizontalDistanceOnly).Item != null;
    }

    /// <summary>
    /// Check for a nearby item to <paramref name="position"/> within the given <paramref name="radius"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static bool IsItemInRange(Vector3 position, float radius, bool horizontalDistanceOnly = false)
    {
        return GetClosestItemInRange(position, radius, horizontalDistanceOnly).Item != null;
    }

    /// <summary>
    /// Find the closest item with the given <paramref name="asset"/> to <paramref name="position"/> within the given <paramref name="radius"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static ItemInfo GetClosestItemInRange(Vector3 position, float radius, IAssetLink<ItemAsset> asset, bool horizontalDistanceOnly = false)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        ThreadUtil.assertIsGameThread();

        float closestSqrDist = 0f;
        float sqrRadius = radius * radius;
        ItemInfo closest = default;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            ItemRegion region = ItemManager.regions[coord.x, coord.y];
            for (int i = 0; i < region.items.Count; ++i)
            {
                ItemData item = region.items[i];
                Vector3 pos = item.point;

                float sqrDist = MathUtility.SquaredDistance(in position, in pos, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || sqrDist > sqrRadius || !asset.MatchId(item.item.id))
                    continue;

                closest = new ItemInfo(item, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest item with the given <paramref name="asset"/> to <paramref name="position"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static ItemInfo GetClosestItem(Vector3 position, IAssetLink<ItemAsset> asset, bool horizontalDistanceOnly = false)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        ThreadUtil.assertIsGameThread();

        float closestSqrDist = 0f;
        ItemInfo closest = default;
        if (!Regions.tryGetCoordinate(position, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator(x, y);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            ItemRegion region = ItemManager.regions[coord.x, coord.y];
            for (int i = 0; i < region.items.Count; ++i)
            {
                ItemData item = region.items[i];
                Vector3 pos = item.point;

                float sqrDist = MathUtility.SquaredDistance(in position, in pos, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || !asset.MatchId(item.item.id))
                    continue;

                closest = new ItemInfo(item, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest item to <paramref name="position"/> within the given <paramref name="radius"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static ItemInfo GetClosestItemInRange(Vector3 position, float radius, bool horizontalDistanceOnly = false)
    {
        ThreadUtil.assertIsGameThread();

        float closestSqrDist = 0f;
        float sqrRadius = radius * radius;
        ItemInfo closest = default;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            ItemRegion region = ItemManager.regions[coord.x, coord.y];
            for (int i = 0; i < region.items.Count; ++i)
            {
                ItemData item = region.items[i];
                Vector3 pos = item.point;

                float sqrDist = MathUtility.SquaredDistance(in position, in pos, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || sqrDist > sqrRadius)
                    continue;

                closest = new ItemInfo(item, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest item to <paramref name="position"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static ItemInfo GetClosestItem(Vector3 position, bool horizontalDistanceOnly = false)
    {
        ThreadUtil.assertIsGameThread();

        float closestSqrDist = 0f;
        ItemInfo closest = default;
        if (!Regions.tryGetCoordinate(position, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator(x, y);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            ItemRegion region = ItemManager.regions[coord.x, coord.y];
            for (int i = 0; i < region.items.Count; ++i)
            {
                ItemData item = region.items[i];
                Vector3 pos = item.point;

                float sqrDist = MathUtility.SquaredDistance(in position, in pos, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist)
                    continue;

                closest = new ItemInfo(item, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest item matching a predicate to <paramref name="position"/> within a given <paramref name="radius"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static ItemInfo GetClosestItemWhere(Vector3 position, float radius, Predicate<ItemData> itemSelector, bool horizontalDistanceOnly = false)
    {
        if (itemSelector == null)
            throw new ArgumentNullException(nameof(itemSelector));

        ThreadUtil.assertIsGameThread();

        float closestSqrDist = 0f;
        float sqrRadius = radius * radius;
        ItemInfo closest = default;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            ItemRegion region = ItemManager.regions[coord.x, coord.y];
            for (int i = 0; i < region.items.Count; ++i)
            {
                ItemData item = region.items[i];
                Vector3 pos = item.point;

                float sqrDist = MathUtility.SquaredDistance(in position, in pos, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || sqrDist > sqrRadius || !itemSelector(item))
                    continue;

                closest = new ItemInfo(item, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest item matching a predicate to <paramref name="position"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static ItemInfo GetClosestItemWhere(Vector3 position, Predicate<ItemData> itemSelector, bool horizontalDistanceOnly = false)
    {
        if (itemSelector == null)
            throw new ArgumentNullException(nameof(itemSelector));

        ThreadUtil.assertIsGameThread();

        float closestSqrDist = 0f;
        ItemInfo closest = default;
        if (!Regions.tryGetCoordinate(position, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator(x, y);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            ItemRegion region = ItemManager.regions[coord.x, coord.y];
            for (int i = 0; i < region.items.Count; ++i)
            {
                ItemData item = region.items[i];
                Vector3 pos = item.point;

                float sqrDist = MathUtility.SquaredDistance(in position, in pos, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || !itemSelector(item))
                    continue;

                closest = new ItemInfo(item, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Count the number of items in the given <paramref name="radius"/> matching a predicate.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static int CountItemsWhere(Vector3 position, float radius, Predicate<ItemData> itemSelector, int max = -1, bool horizontalDistanceOnly = false)
    {
        if (itemSelector == null)
            throw new ArgumentNullException(nameof(itemSelector));

        ThreadUtil.assertIsGameThread();

        float sqrRadius = radius * radius;
        int totalItemsFound = 0;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            ItemRegion region = ItemManager.regions[coord.x, coord.y];
            for (int i = 0; i < region.items.Count; ++i)
            {
                ItemData item = region.items[i];
                Vector3 pos = item.point;

                float sqrDist = MathUtility.SquaredDistance(in position, in pos, horizontalDistanceOnly);

                if (sqrDist > sqrRadius || !itemSelector(item))
                    continue;

                ++totalItemsFound;
                if (max >= 0 && totalItemsFound >= max)
                {
                    return totalItemsFound;
                }
            }
        }

        return totalItemsFound;
    }

    /// <summary>
    /// Count the number of items in the given radius matching a predicate.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static int CountItemsWhere(Predicate<ItemData> itemSelector, int max = -1)
    {
        if (itemSelector == null)
            throw new ArgumentNullException(nameof(itemSelector));

        ThreadUtil.assertIsGameThread();

        int totalItemsFound = 0;
        SurroundingRegionsIterator iterator = RegionUtility.EnumerateRegions();
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            ItemRegion region = ItemManager.regions[coord.x, coord.y];
            for (int i = 0; i < region.items.Count; ++i)
            {
                ItemData item = region.items[i];
                if (!itemSelector(item))
                    continue;

                ++totalItemsFound;
                if (max >= 0 && totalItemsFound >= max)
                {
                    return totalItemsFound;
                }
            }
        }

        return totalItemsFound;
    }

    /// <summary>
    /// Count the number of items in the given <paramref name="radius"/> matching an <paramref name="asset"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static int CountItemsInRange(Vector3 position, float radius, IAssetLink<ItemAsset> asset, int max = -1, bool horizontalDistanceOnly = false)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        ThreadUtil.assertIsGameThread();

        float sqrRadius = radius * radius;
        int totalItemsFound = 0;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            ItemRegion region = ItemManager.regions[coord.x, coord.y];
            for (int i = 0; i < region.items.Count; ++i)
            {
                ItemData item = region.items[i];
                Vector3 pos = item.point;

                float sqrDist = MathUtility.SquaredDistance(in position, in pos, horizontalDistanceOnly);

                if (sqrDist > sqrRadius || !asset.MatchId(item.item.id))
                    continue;

                ++totalItemsFound;
                if (max >= 0 && totalItemsFound >= max)
                {
                    return totalItemsFound;
                }
            }
        }

        return totalItemsFound;
    }

    /// <summary>
    /// Count the number of items in the given radius matching an <paramref name="asset"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static int CountItems(IAssetLink<ItemAsset> asset, int max = -1)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        ThreadUtil.assertIsGameThread();

        int totalItemsFound = 0;
        SurroundingRegionsIterator iterator = RegionUtility.EnumerateRegions();
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            ItemRegion region = ItemManager.regions[coord.x, coord.y];
            for (int i = 0; i < region.items.Count; ++i)
            {
                ItemData item = region.items[i];
                if (!asset.MatchId(item.item.id))
                    continue;

                ++totalItemsFound;
                if (max >= 0 && totalItemsFound >= max)
                {
                    return totalItemsFound;
                }
            }
        }

        return totalItemsFound;
    }

    /// <summary>
    /// Count the number of items in the given radius.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static int CountItemsInRange(Vector3 position, float radius, int max = -1, bool horizontalDistanceOnly = false)
    {
        ThreadUtil.assertIsGameThread();

        float sqrRadius = radius * radius;
        int totalItemsFound = 0;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            ItemRegion region = ItemManager.regions[coord.x, coord.y];
            for (int i = 0; i < region.items.Count; ++i)
            {
                ItemData item = region.items[i];
                Vector3 pos = item.point;

                float sqrDist = MathUtility.SquaredDistance(in position, in pos, horizontalDistanceOnly);

                if (sqrDist > sqrRadius)
                    continue;

                ++totalItemsFound;
                if (max >= 0 && totalItemsFound >= max)
                {
                    return totalItemsFound;
                }
            }
        }

        return totalItemsFound;
    }
}

/// <summary>
/// Stores return information about an item including it's region information.
/// </summary>
/// <remarks>Only valid for one frame, shouldn't be stored for longer than that.</remarks>
public readonly struct ItemInfo
{
#nullable disable
    public ItemData Item { get; }
#nullable restore
    public bool HasValue => Item != null;

    /// <summary>
    /// Coordinates of the region the item is in, if it's not on a vehicle.
    /// </summary>
    public RegionCoord Coord { get; }

    /// <summary>
    /// Index of the item in it's region's item list.
    /// </summary>
    public int Index { get; }

    public ItemInfo(ItemData? item, int index, RegionCoord coord)
    {
        Item = item;
        Coord = coord;
        Index = index;
    }

    [Pure]
    public ItemRegion GetRegion()
    {
        if (Item == null)
            throw new NullReferenceException("This info doesn't store a valid ItemData instance.");

        RegionCoord regionCoord = Coord;
        return ItemManager.regions[regionCoord.x, regionCoord.y];
    }
}