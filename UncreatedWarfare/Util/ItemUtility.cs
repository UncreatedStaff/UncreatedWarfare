﻿using DanielWillett.ReflectionTools;
using SDG.NetPak;
using SDG.NetTransport;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Layouts.Teams;
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
    private static readonly ClientInstanceMethod<Guid, byte, byte[], bool>? SendWearShirt
        = ReflectionUtility.FindRpc<PlayerClothing, ClientInstanceMethod<Guid, byte, byte[], bool>>("SendWearShirt");
    private static readonly ClientInstanceMethod<Guid, byte, byte[], bool>? SendWearPants
        = ReflectionUtility.FindRpc<PlayerClothing, ClientInstanceMethod<Guid, byte, byte[], bool>>("SendWearPants");
    private static readonly ClientInstanceMethod<Guid, byte, byte[], bool>? SendWearHat
        = ReflectionUtility.FindRpc<PlayerClothing, ClientInstanceMethod<Guid, byte, byte[], bool>>("SendWearHat");
    private static readonly ClientInstanceMethod<Guid, byte, byte[], bool>? SendWearBackpack
        = ReflectionUtility.FindRpc<PlayerClothing, ClientInstanceMethod<Guid, byte, byte[], bool>>("SendWearBackpack");
    private static readonly ClientInstanceMethod<Guid, byte, byte[], bool>? SendWearVest
        = ReflectionUtility.FindRpc<PlayerClothing, ClientInstanceMethod<Guid, byte, byte[], bool>>("SendWearVest");
    private static readonly ClientInstanceMethod<Guid, byte, byte[], bool>? SendWearMask
        = ReflectionUtility.FindRpc<PlayerClothing, ClientInstanceMethod<Guid, byte, byte[], bool>>("SendWearMask");
    private static readonly ClientInstanceMethod<Guid, byte, byte[], bool>? SendWearGlasses
        = ReflectionUtility.FindRpc<PlayerClothing, ClientInstanceMethod<Guid, byte, byte[], bool>>("SendWearGlasses");

    private static readonly ClientInstanceMethod? SendInventory
        = ReflectionUtility.FindRpc<PlayerInventory, ClientInstanceMethod>("SendInventory");

    private static readonly InstanceGetter<Items, bool[,]>? GetItemsSlots
        = Accessor.GenerateInstanceGetter<Items, bool[,]>("slots", throwOnError: false);

    private static readonly InstanceSetter<PlayerInventory, bool>? SetOwnerHasInventory
        = Accessor.GenerateInstanceSetter<PlayerInventory, bool>("ownerHasInventory", throwOnError: false);
    private static readonly InstanceGetter<PlayerInventory, bool>? GetOwnerHasInventory
        = Accessor.GenerateInstanceGetter<PlayerInventory, bool>("ownerHasInventory", throwOnError: false);

    private static readonly Action<PlayerInventory, byte, ItemJar>? SendItemRemove
        = Accessor.GenerateInstanceCaller<PlayerInventory, Action<PlayerInventory, byte, ItemJar>>("sendItemRemove", throwOnError: false, allowUnsafeTypeBinding: true);

    internal static event ItemDestroyed? OnItemDestroyed;
    internal delegate void ItemDestroyed(in ItemInfo item, bool despawned, bool pickedUp, CSteamID pickUpPlayer, Page pickupPage, byte pickupX, byte pickupY, byte pickupRot);

    internal static ClientStaticMethod<byte, byte, uint, bool> SendDestroyItem = ReflectionUtility.FindRequiredRpc<ItemManager, ClientStaticMethod<byte, byte, uint, bool>>("SendDestroyItem");

    /// <summary>
    /// If all the reflection succeeded needed for fast item distribution.
    /// </summary>
    public static bool SupportsFastKits { get; }
        = SendWearShirt != null
          && SendWearPants != null
          && SendWearHat != null
          && SendWearBackpack != null
          && SendWearVest != null
          && SendWearMask != null
          && SendWearGlasses != null
          && SendInventory != null
          && SetOwnerHasInventory != null
          && GetOwnerHasInventory != null
          && SendItemRemove != null
          && GetItemsSlots != null;

    /// <summary>
    /// Enumerate items along the grid instead of the order they were added.
    /// </summary>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    public static ItemPageIterator EnumerateAlongGrid(Items items, bool reverse = false)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        GameThread.AssertCurrent();

        return new ItemPageIterator(items, reverse);
    }

    /// <summary>
    /// Enumerate through all dropped items around the center of the level.
    /// </summary>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static DroppedItemIterator EnumerateDroppedItems()
    {
        GameThread.AssertCurrent();

        return new DroppedItemIterator();
    }

    /// <summary>
    /// Enumerate through all dropped items around the center of the level.
    /// </summary>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static DroppedItemIterator EnumerateDroppedItems(Vector3 center)
    {
        GameThread.AssertCurrent();

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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static DroppedItemIterator EnumerateDroppedItems(Vector3 center, byte maxRegionDistance)
    {
        GameThread.AssertCurrent();

        if (!Regions.tryGetCoordinate(center, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        return new DroppedItemIterator(x, y, maxRegionDistance);
    }

    /// <summary>
    /// Enumerate through all dropped items around the given <paramref name="region"/>.
    /// </summary>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static DroppedItemIterator EnumerateDroppedItems(RegionCoord region)
    {
        GameThread.AssertCurrent();

        return new DroppedItemIterator(region.x, region.y);
    }

    /// <summary>
    /// Enumerate through all dropped items around the region <paramref name="x"/>, <paramref name="y"/>.
    /// </summary>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static DroppedItemIterator EnumerateDroppedItems(byte x, byte y)
    {
        GameThread.AssertCurrent();

        return new DroppedItemIterator(x, y);
    }

    /// <summary>
    /// Enumerate through all dropped items around the given <paramref name="region"/>.
    /// </summary>
    /// <remarks>The square enumerated will have a size of <c><paramref name="maxRegionDistance"/> * 2 + 1</c> regions.</remarks>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static DroppedItemIterator EnumerateDroppedItems(RegionCoord region, byte maxRegionDistance)
    {
        GameThread.AssertCurrent();

        return new DroppedItemIterator(region.x, region.y, maxRegionDistance);
    }

    /// <summary>
    /// Enumerate through all dropped items around the region <paramref name="x"/>, <paramref name="y"/>.
    /// </summary>
    /// <remarks>The square enumerated will have a size of <c><paramref name="maxRegionDistance"/> * 2 + 1</c> regions.</remarks>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static DroppedItemIterator EnumerateDroppedItems(byte x, byte y, byte maxRegionDistance)
    {
        GameThread.AssertCurrent();

        return new DroppedItemIterator(x, y, maxRegionDistance);
    }

    /// <summary>
    /// Check if there is at least one item in the player's inventory that has the given <paramref name="asset"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    public static bool HasItem(Player player, IAssetLink<ItemAsset> asset)
    {
        return CountItems(player, asset, 1) > 0;
    }

    /// <summary>
    /// Check if there is at least one item in the player's inventory that matches a predicate.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    public static bool HasItem(Player player, Predicate<ItemJar> itemSelector)
    {
        return CountItems(player, itemSelector, 1) > 0;
    }

    /// <summary>
    /// Count the number of items in the player's inventory that have the given <paramref name="asset"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    public static int CountItems(Player player, IAssetLink<ItemAsset> asset, int max = -1)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        GameThread.AssertCurrent();

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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    public static int CountItems(Player player, Predicate<ItemJar> itemSelector, int max = -1)
    {
        if (itemSelector == null)
            throw new ArgumentNullException(nameof(itemSelector));

        GameThread.AssertCurrent();

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
            OnItemDestroyed?.Invoke(in item, despawned, false, CSteamID.Nil, 0, 0, 0, 0);
        }

        ItemManager.askClearAllItems();
        return ct;
    }

    /// <summary>
    /// Destroy the number of items in the given <paramref name="radius"/> matching an <paramref name="asset"/>.
    /// </summary>
    /// <param name="pickUpPlayer">The player that is picking up the items, if any.</param>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    /// <returns>Number of items destroyed.</returns>
    public static int DestroyDroppedItemsInRange(Vector3 position, float radius, IAssetLink<ItemAsset> asset, bool playTakeItemSound, int max = -1, bool horizontalDistanceOnly = false, CSteamID pickUpPlayer = default, Page pickupPage = (Page)byte.MaxValue, byte pickupX = 0, byte pickupY = 0, byte pickupRot = 0)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        GameThread.AssertCurrent();

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
                RemoveDroppedItemUnsafe(coord.x, coord.y, i, false, pickUpPlayer, playTakeItemSound, pickupPage, pickupX, pickupY, pickupRot);
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    /// <returns>Number of items destroyed.</returns>
    public static int DestroyDroppedItemsInRange(Vector3 position, float radius, Predicate<ItemData> itemSelector, bool playTakeItemSound, int max = -1, bool horizontalDistanceOnly = false, CSteamID pickUpPlayer = default, Page pickupPage = (Page)byte.MaxValue, byte pickupX = 0, byte pickupY = 0, byte pickupRot = 0)
    {
        if (itemSelector == null)
            throw new ArgumentNullException(nameof(itemSelector));

        GameThread.AssertCurrent();

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
                RemoveDroppedItemUnsafe(coord.x, coord.y, i, false, pickUpPlayer, playTakeItemSound, pickupPage, pickupX, pickupY, pickupRot);
                if (max >= 0 && totalItemsFound >= max)
                {
                    return totalItemsFound;
                }
            }
        }

        return totalItemsFound;
    }

    /// <summary>
    /// Destroy the number of items in the given <paramref name="radius"/>.
    /// </summary>
    /// <param name="pickUpPlayer">The player that is picking up the items, if any.</param>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    /// <returns>Number of items destroyed.</returns>
    public static int DestroyDroppedItemsInRange(Vector3 position, float radius, bool playTakeItemSound, int max = -1, bool horizontalDistanceOnly = false, CSteamID pickUpPlayer = default, Page pickupPage = (Page)byte.MaxValue, byte pickupX = 0, byte pickupY = 0, byte pickupRot = 0)
    {
        GameThread.AssertCurrent();

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
                RemoveDroppedItemUnsafe(coord.x, coord.y, i, false, pickUpPlayer, playTakeItemSound, pickupPage, pickupX, pickupY, pickupRot);
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    public static bool DestroyDroppedItem(ItemData item, bool despawned, bool playTakeItemSound = false)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        GameThread.AssertCurrent();

        ItemInfo itemInfo = FindItem(item.instanceID, item.point);

        if (!itemInfo.HasValue)
            return false;

        RegionCoord region = itemInfo.Coord;
        RemoveDroppedItemUnsafe(region.x, region.y, itemInfo.Index, despawned, CSteamID.Nil, playTakeItemSound, 0, 0, 0, 0);
        return true;
    }

    /// <summary>
    /// Property clean up and replicate destroying (taking) a dropped item that was picked up by a player.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    public static bool DestroyDroppedItem(ItemData item, bool despawned, WarfarePlayer pickUpPlayer, Page pickupPage = (Page)byte.MaxValue, byte pickupX = 0, byte pickupY = 0, byte pickupRot = 0, bool playTakeItemSound = false)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        GameThread.AssertCurrent();

        ItemInfo itemInfo = FindItem(item.instanceID, item.point);

        if (!itemInfo.HasValue)
            return false;

        RegionCoord region = itemInfo.Coord;
        RemoveDroppedItemUnsafe(region.x, region.y, itemInfo.Index, despawned, despawned ? CSteamID.Nil : pickUpPlayer.Steam64, playTakeItemSound, pickupPage, pickupX, pickupY, pickupRot);
        return true;
    }

    /// <summary>
    /// Property clean up and replicate destroying (taking) a dropped item.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    public static void DestroyDroppedItem(byte x, byte y, int index, bool despawned, bool playTakeItemSound = false)
    {
        GameThread.AssertCurrent();

        if (!Regions.checkSafe(x, y))
            throw new ArgumentOutOfRangeException("(x, y)", "X and/or Y coordinate invalid.");

        if (index >= ItemManager.regions[x, y].items.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "No item with the given index.");

        RemoveDroppedItemUnsafe(x, y, index, despawned, CSteamID.Nil, playTakeItemSound, 0, 0, 0, 0);
    }

    /// <summary>
    /// Property clean up and replicate destroying (taking) a dropped item that was picked up by a player.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    public static void DestroyDroppedItem(byte x, byte y, int index, bool despawned, WarfarePlayer pickUpPlayer, bool playTakeItemSound, Page pickupPage, byte pickupX, byte pickupY, byte pickupRot)
    {
        GameThread.AssertCurrent();

        if (!Regions.checkSafe(x, y))
            throw new ArgumentOutOfRangeException("(x, y)", "X and/or Y coordinate invalid.");

        if (index >= ItemManager.regions[x, y].items.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "No item with the given index.");


        RemoveDroppedItemUnsafe(x, y, index, despawned, despawned ? CSteamID.Nil : pickUpPlayer.Steam64, playTakeItemSound, pickupPage, pickupX, pickupY, pickupRot);
    }

    internal static void RemoveDroppedItemUnsafe(byte x, byte y, int index, bool despawned, CSteamID pickUpPlayer, bool playTakeItemSound, Page pickupPage, byte pickupX, byte pickupY, byte pickupRot)
    {
        ItemRegion region = ItemManager.regions[x, y];
        ItemData item = region.items[index];

        SendDestroyItem.Invoke(ENetReliability.Reliable, Regions.GatherRemoteClientConnections(x, y, ItemManager.ITEM_REGIONS), x, y, item.instanceID, playTakeItemSound);

        region.items.RemoveAt(index);

        ItemInfo itemInfo = new ItemInfo(item, index, new RegionCoord(x, y));
        OnItemDestroyed?.Invoke(itemInfo, despawned, !despawned && pickUpPlayer.GetEAccountType() == EAccountType.k_EAccountTypeIndividual, despawned ? CSteamID.Nil : pickUpPlayer, pickupPage, pickupX, pickupY, pickupRot);
    }

    internal static void InvokeOnItemDestroyed(in ItemInfo item, bool despawned, bool pickedUp, CSteamID pickUpPlayer, Page pickupPage, byte pickupX, byte pickupY, byte pickupRot)
    {
        OnItemDestroyed?.Invoke(item, despawned, pickedUp, pickUpPlayer, pickupPage, pickupX, pickupY, pickupRot);
    }

    /// <summary>
    /// Prevents modifications to the inventory from being replicated. Only works if <see cref="SupportsFastKits"/> is <see langword="true"/>.
    /// </summary>
    public static void IsolateInventory(Player player, out bool oldValue)
    {
        oldValue = GetOwnerHasInventory!(player.inventory);
        SetOwnerHasInventory!(player.inventory, false);
    }

    /// <summary>
    /// Reverses <see cref="IsolateInventory"/> which prevents modifications to the inventory from being replicated. Only works if <see cref="SupportsFastKits"/> is <see langword="true"/>.
    /// </summary>
    public static void UndoIsolateInventory(Player player, bool oldValue)
    {
        if (oldValue)
            SetOwnerHasInventory!(player.inventory, true);
    }

    /// <summary>
    /// Get an array of kit item abstractions from a player's inventory and clothing.
    /// </summary>
    public static IKitItem[] ItemsFromInventory(WarfarePlayer player, bool addClothes = true, bool addItems = true, AssetRedirectService? assetRedirectService = null)
    {
        GameThread.AssertCurrent();
        if (!addItems && !addClothes)
            return Array.Empty<IKitItem>();
        List<IKitItem> items = new List<IKitItem>(32);
        RedirectType type;
        if (addItems)
        {
            Items[] ia = player.UnturnedPlayer.inventory.items;
            for (byte page = 0; page < PlayerInventory.STORAGE; ++page)
            {
                Items it = ia[page];
                byte ct = it.getItemCount();
                // check to make sure only one item in gun slot
                if (ct > 1 && page < PlayerInventory.SLOTS)
                {
                    ct = 1;
                }
                for (int index = ct - 1; index >= 0; --index)
                {
                    ItemJar jar = it.items[index];
                    ItemAsset asset = jar.GetAsset();
                    if (asset == null)
                        continue;
                    if (assetRedirectService != null && assetRedirectService.TryFindRedirectType(asset, out type, out _, out string? variant))
                        items.Add(new AssetRedirectPageKitItem(0u, jar.x, jar.y, jar.rot, (Page)page, type, variant));
                    else items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference(asset.GUID), jar.x, jar.y, jar.rot, (Page)page, jar.item.amount, jar.item.state));
                }
            }
        }
        if (addClothes)
        {
            FactionInfo? playerFaction = player.Team.Faction;
            PlayerClothing playerClothes = player.UnturnedPlayer.clothing;
            if (playerClothes.shirtAsset != null)
            {
                if (playerFaction != null && assetRedirectService != null && assetRedirectService.TryFindRedirectType(playerClothes.shirtAsset, out type, out FactionInfo? faction, out string? variant, clothingOnly: true) && (faction == null || faction == playerFaction))
                    items.Add(new AssetRedirectClothingKitItem(0u, type, ClothingType.Shirt, variant));
                else
                    items.Add(new SpecificClothingKitItem(0u, new UnturnedAssetReference(playerClothes.shirtAsset.GUID), ClothingType.Shirt, playerClothes.shirtState));
            }
            if (playerClothes.pantsAsset != null)
            {
                if (playerFaction != null && assetRedirectService != null && assetRedirectService.TryFindRedirectType(playerClothes.pantsAsset, out type, out FactionInfo? faction, out string? variant, clothingOnly: true) && (faction == null || faction == playerFaction))
                    items.Add(new AssetRedirectClothingKitItem(0u, type, ClothingType.Pants, variant));
                else
                    items.Add(new SpecificClothingKitItem(0u, new UnturnedAssetReference(playerClothes.pantsAsset.GUID), ClothingType.Pants, playerClothes.pantsState));
            }
            if (playerClothes.vestAsset != null)
            {
                if (playerFaction != null && assetRedirectService != null && assetRedirectService.TryFindRedirectType(playerClothes.vestAsset, out type, out FactionInfo? faction, out string? variant, clothingOnly: true) && (faction == null || faction == playerFaction))
                    items.Add(new AssetRedirectClothingKitItem(0u, type, ClothingType.Vest, variant));
                else
                    items.Add(new SpecificClothingKitItem(0u, new UnturnedAssetReference(playerClothes.vestAsset.GUID), ClothingType.Vest, playerClothes.vestState));
            }
            if (playerClothes.hatAsset != null)
            {
                if (playerFaction != null && assetRedirectService != null && assetRedirectService.TryFindRedirectType(playerClothes.hatAsset, out type, out FactionInfo? faction, out string? variant, clothingOnly: true) && (faction == null || faction == playerFaction))
                    items.Add(new AssetRedirectClothingKitItem(0u, type, ClothingType.Hat, variant));
                else
                    items.Add(new SpecificClothingKitItem(0u, new UnturnedAssetReference(playerClothes.hatAsset.GUID), ClothingType.Hat, playerClothes.hatState));
            }
            if (playerClothes.maskAsset != null)
            {
                if (playerFaction != null && assetRedirectService != null && assetRedirectService.TryFindRedirectType(playerClothes.maskAsset, out type, out FactionInfo? faction, out string? variant, clothingOnly: true) && (faction == null || faction == playerFaction))
                    items.Add(new AssetRedirectClothingKitItem(0u, type, ClothingType.Mask, variant));
                else
                    items.Add(new SpecificClothingKitItem(0u, new UnturnedAssetReference(playerClothes.maskAsset.GUID), ClothingType.Mask, playerClothes.maskState));
            }
            if (playerClothes.backpackAsset != null)
            {
                if (playerFaction != null && assetRedirectService != null && assetRedirectService.TryFindRedirectType(playerClothes.backpackAsset, out type, out FactionInfo? faction, out string? variant, clothingOnly: true) && (faction == null || faction == playerFaction))
                    items.Add(new AssetRedirectClothingKitItem(0u, type, ClothingType.Backpack, variant));
                else
                    items.Add(new SpecificClothingKitItem(0u, new UnturnedAssetReference(playerClothes.backpackAsset.GUID), ClothingType.Backpack, playerClothes.backpackState));
            }
            if (playerClothes.glassesAsset != null)
            {
                if (playerFaction != null && assetRedirectService != null && assetRedirectService.TryFindRedirectType(playerClothes.glassesAsset, out type, out FactionInfo? faction, out string? variant, clothingOnly: true) && (faction == null || faction == playerFaction))
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
        GameThread.AssertCurrent();
        if (!player.IsOnline)
            return;
        // removes the primaries/secondaries from the third person model
        player.UnturnedPlayer.equipment.sendSlot(0);
        player.UnturnedPlayer.equipment.sendSlot(1);
    }

    /// <summary>
    /// Sets and replicates the player's clothing at a given slot.
    /// </summary>
    /// <remarks>Requires <see cref="SupportsFastKits"/> to be <see langword="true"/>.</remarks>
    public static void SendWearClothing(Player player, ItemAsset? asset, ClothingType type, byte quality, byte[] state, bool playEffect)
    {
        ClientInstanceMethod<Guid, byte, byte[], bool> inv =
            (type switch
            {
                ClothingType.Shirt => SendWearShirt,
                ClothingType.Pants => SendWearPants,
                ClothingType.Hat => SendWearHat,
                ClothingType.Backpack => SendWearBackpack,
                ClothingType.Vest => SendWearVest,
                ClothingType.Mask => SendWearMask,
                ClothingType.Glasses => SendWearGlasses,
                _ => null
            })!;

        inv.InvokeAndLoopback(player.clothing.GetNetId(), ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), asset?.GUID ?? Guid.Empty, quality, state, playEffect);
    }

    /// <summary>
    /// Effeciently remove all items and clothes from a player's inventory.
    /// </summary>
    public static void ClearInventory(WarfarePlayer player, bool clothes = true)
    {
        GameThread.AssertCurrent();
        if (!player.IsOnline)
            return;

        ItemTrackingPlayerComponent? comp = player.ComponentOrNull<ItemTrackingPlayerComponent>();
        if (comp != null)
        {
            comp.ItemTransformations.Clear();
            comp.ItemDropTransformations.Clear();
        }

        Player nativePlayer = player.UnturnedPlayer;
        if (SupportsFastKits)
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
                {
                    SendItemRemove!.Invoke(nativePlayer.inventory, i, inv[i].items[it]);
                }
            }

            Items pg = inv[PlayerInventory.SLOTS];
            pg.clear();

            bool[,] itemMask = GetItemsSlots!(pg);
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
                    SendWearShirt!.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), Guid.Empty, 100, blank, false);
                if (nativePlayer.clothing.pants != 0)
                    SendWearPants!.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), Guid.Empty, 100, blank, false);
                if (nativePlayer.clothing.hat != 0)
                    SendWearHat!.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), Guid.Empty, 100, blank, false);
                if (nativePlayer.clothing.backpack != 0)
                    SendWearBackpack!.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), Guid.Empty, 100, blank, false);
                if (nativePlayer.clothing.vest != 0)
                    SendWearVest!.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), Guid.Empty, 100, blank, false);
                if (nativePlayer.clothing.mask != 0)
                    SendWearMask!.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), Guid.Empty, 100, blank, false);
                if (nativePlayer.clothing.glasses != 0)
                    SendWearGlasses!.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), Guid.Empty, 100, blank, false);
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
    public static void GiveItems(WarfarePlayer player, IKitItem[] items, ILogger logger, AssetRedirectService assetRedirectService, IFactionDataStore factionDataStore, bool clear)
    {
        GameThread.AssertCurrent();
        if (!player.IsOnline)
            return;

        if (clear)
            ClearInventory(player, true);

        Team team = player.Team;
        Player nativePlayer = player.UnturnedPlayer;

        if (SupportsFastKits)
        {
            int flag = 0;
            bool hasPlayedEffect = false;
            for (int i = 0; i < items.Length; ++i)
            {
                IKitItem item = items[i];
                if (item is not IClothingKitItem clothingJar)
                    continue;
                ItemAsset? asset = item.GetItem(null, team, out _, out byte[] state, assetRedirectService, factionDataStore);
                if (asset == null || asset.type != clothingJar.Type.GetItemType())
                    continue;

                if ((flag & (1 << (int)clothingJar.Type)) != 0)
                    continue;
                
                flag |= 1 << (int)clothingJar.Type;
                SendWearClothing(nativePlayer, asset, clothingJar.Type, 100, state, !hasPlayedEffect);
                hasPlayedEffect = true;
            }

            byte[] blank = Array.Empty<byte>();
            for (int i = 0; i < 7; ++i)
            {
                if (((flag >> i) & 1) != 0)
                    continue;

                SendWearClothing(nativePlayer, null, (ClothingType)i, 100, blank, false);
            }

            Items[] p = nativePlayer.inventory.items;

            IsolateInventory(nativePlayer, out bool oldValue);

            List<Item>? toAddLater = null;
            for (int i = 0; i < items.Length; ++i)
            {
                IKitItem item = items[i];
                if (item is not IPageKitItem jar)
                    continue;

                ItemAsset? asset = item.GetItem(null, team, out byte amt, out byte[] state, assetRedirectService, factionDataStore);
                if ((int)jar.Page < PlayerInventory.PAGES - 2 && asset != null)
                {
                    Items page = p[(int)jar.Page];
                    Item itm = new Item(asset.id, amt, 100, state);
                    // ensures multiple items are not put in the slots (causing the ghost gun issue)
                    if (jar.Page is Page.Primary or Page.Secondary)
                    {
                        if (page.getItemCount() > 0)
                        {
                            logger.LogWarning("[GIVE ITEMS] Duplicate {0} defined: {1}.", jar.Page.ToString().ToLowerInvariant(), item);
                            logger.LogInformation("[GIVE ITEMS] Removing {0} in place of duplicate.", page.items[0].GetAsset().itemName);
                            (toAddLater ??= new List<Item>(4)).Add(page.items[0].item);
                            page.removeItem(0);
                        }
                    }
                    else if (IsOutOfBounds(page, jar.X, jar.Y, asset.size_x, asset.size_y, jar.Rotation))
                    {
                        logger.LogWarning("Out of bounds item in {0} defined: {1}.", jar.Page, item);
                        (toAddLater ??= new List<Item>(4)).Add(itm);
                        continue;
                    }

                    int ic2 = page.getItemCount();
                    for (int j = 0; j < ic2; ++j)
                    {
                        ItemJar? jar2 = page.getItem((byte)j);
                        if (jar2 == null || !IsOverlapping(jar.X, jar.Y, asset.size_x, asset.size_y, jar2.x, jar2.y, jar2.size_x, jar2.size_y, jar.Rotation, jar2.rot))
                            continue;

                        logger.LogWarning("[GIVE ITEMS] Overlapping item in {0} defined: {1}.", jar.Page, item);
                        logger.LogInformation("[GIVE ITEMS] Removing {0} ({1}, {2} @ {3}), in place of duplicate.", jar2.GetAsset().itemName, jar2.x, jar2.y, jar2.rot);
                        page.removeItem((byte)j--);
                        (toAddLater ??= new List<Item>(4)).Add(jar2.item);
                    }

                    page.addItem(jar.X, jar.Y, jar.Rotation, itm);
                }
                else
                {
                    logger.LogWarning("[GIVE ITEMS] Unknown asset: {0}", (jar is ISpecificKitItem i2 ? i2.Item.ToString() : (jar is IAssetRedirectKitItem a2 ? a2.RedirectType.ToString() : jar.ToString()) + "."));
                }
            }

            // try to add removed items later
            if (toAddLater is { Count: > 0 })
            {
                for (int i = 0; i < toAddLater.Count; ++i)
                {
                    Item item = toAddLater[i];
                    logger.LogWarning("[GIVE ITEMS] Had to re-add item: {0}.", item.GetAsset()?.itemName);
                    if (!nativePlayer.inventory.tryAddItemAuto(item, false, false, false, !hasPlayedEffect))
                    {
                        ItemManager.dropItem(item, player.Position, !hasPlayedEffect, true, false);
                    }

                    if (!hasPlayedEffect)
                        hasPlayedEffect = true;
                }
            }

            UndoIsolateInventory(nativePlayer, oldValue);
            SendPages(player);
        }
        else
        {
            foreach (IKitItem item in items)
            {
                if (item is not IClothingKitItem clothing)
                    continue;

                ItemAsset? asset = item.GetItem(null, team, out byte amt, out byte[] state, assetRedirectService, factionDataStore);
                if (asset is null)
                {
                    logger.LogWarning("[GIVE ITEMS] Unknown asset: {0}.", clothing);
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
                logger.LogWarning("[GIVE ITEMS] Invalid or mismatched clothing type: {0}.", clothing);
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

                ItemAsset? asset = item.GetItem(null, team, out byte amt, out byte[] state, assetRedirectService, factionDataStore);
                if (asset is null)
                {
                    logger.LogWarning("[GIVE ITEMS] Unknown asset: {0}.", item);
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
        GameThread.AssertCurrent();
        if (!player.IsOnline)
            return;

        Items[] il = player.UnturnedPlayer.inventory.items;
        SendInventory!.Invoke(player.UnturnedPlayer.inventory.GetNetId(), ENetReliability.Reliable, player.Connection,
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
        GameThread.AssertCurrent();
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static ItemInfo FindItem(uint instanceId)
    {
        return FindItem(instanceId, (byte)(Regions.WORLD_SIZE / 2), (byte)(Regions.WORLD_SIZE / 2));
    }

    /// <summary>
    /// Find a item by it's instance ID, with help from a position to prevent having to search every region.
    /// </summary>
    /// <remarks>All regions will be searched if it's not found near the expected position.</remarks>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static ItemInfo FindItem(uint instanceId, byte expectedRegionX, byte expectedRegionY)
    {
        GameThread.AssertCurrent();

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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static ItemInfo FindItem(uint instanceId, IAssetLink<ItemAsset> expectedAsset, Vector3 expectedPosition)
    {
        GameThread.AssertCurrent();

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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static bool IsItemInRange(Vector3 position, float radius, IAssetLink<ItemAsset> asset, bool horizontalDistanceOnly = false)
    {
        return GetClosestItemInRange(position, radius, asset, horizontalDistanceOnly).Item != null;
    }

    /// <summary>
    /// Check for a nearby item matching a predicate to <paramref name="position"/> within the given <paramref name="radius"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static bool IsItemInRange(Vector3 position, float radius, Predicate<ItemData> itemSelector, bool horizontalDistanceOnly = false)
    {
        return GetClosestItemWhere(position, radius, itemSelector, horizontalDistanceOnly).Item != null;
    }

    /// <summary>
    /// Check for a nearby item to <paramref name="position"/> within the given <paramref name="radius"/>.
    /// </summary>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static bool IsItemInRange(Vector3 position, float radius, bool horizontalDistanceOnly = false)
    {
        return GetClosestItemInRange(position, radius, horizontalDistanceOnly).Item != null;
    }

    /// <summary>
    /// Find the closest item with the given <paramref name="asset"/> to <paramref name="position"/> within the given <paramref name="radius"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static ItemInfo GetClosestItemInRange(Vector3 position, float radius, IAssetLink<ItemAsset> asset, bool horizontalDistanceOnly = false)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        GameThread.AssertCurrent();

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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static ItemInfo GetClosestItem(Vector3 position, IAssetLink<ItemAsset> asset, bool horizontalDistanceOnly = false)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        GameThread.AssertCurrent();

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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static ItemInfo GetClosestItemInRange(Vector3 position, float radius, bool horizontalDistanceOnly = false)
    {
        GameThread.AssertCurrent();

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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static ItemInfo GetClosestItem(Vector3 position, bool horizontalDistanceOnly = false)
    {
        GameThread.AssertCurrent();

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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static ItemInfo GetClosestItemWhere(Vector3 position, float radius, Predicate<ItemData> itemSelector, bool horizontalDistanceOnly = false)
    {
        if (itemSelector == null)
            throw new ArgumentNullException(nameof(itemSelector));

        GameThread.AssertCurrent();

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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static ItemInfo GetClosestItemWhere(Vector3 position, Predicate<ItemData> itemSelector, bool horizontalDistanceOnly = false)
    {
        if (itemSelector == null)
            throw new ArgumentNullException(nameof(itemSelector));

        GameThread.AssertCurrent();

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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static int CountItemsWhere(Vector3 position, float radius, Predicate<ItemData> itemSelector, int max = -1, bool horizontalDistanceOnly = false)
    {
        if (itemSelector == null)
            throw new ArgumentNullException(nameof(itemSelector));

        GameThread.AssertCurrent();

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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static int CountItemsWhere(Predicate<ItemData> itemSelector, int max = -1)
    {
        if (itemSelector == null)
            throw new ArgumentNullException(nameof(itemSelector));

        GameThread.AssertCurrent();

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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static int CountItemsInRange(Vector3 position, float radius, IAssetLink<ItemAsset> asset, int max = -1, bool horizontalDistanceOnly = false)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        GameThread.AssertCurrent();

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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static int CountItems(IAssetLink<ItemAsset> asset, int max = -1)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        GameThread.AssertCurrent();

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
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    [Pure]
    public static int CountItemsInRange(Vector3 position, float radius, int max = -1, bool horizontalDistanceOnly = false)
    {
        GameThread.AssertCurrent();

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