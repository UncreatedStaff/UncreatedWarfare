using DanielWillett.ReflectionTools;
using SDG.NetPak;
using SDG.NetTransport;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Util.Inventory;
using Uncreated.Warfare.Util.Region;

namespace Uncreated.Warfare.Util;

/// <summary>
/// Helper functions for dropped items and inventory pages.
/// </summary>
public static class ItemUtility
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static readonly ClientInstanceMethod<Guid, byte, byte[], bool>? SendWearShirt
        = ReflectionUtility.FindRpc<PlayerClothing, ClientInstanceMethod<Guid, byte, byte[], bool>>("SendWearShirt");
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static readonly ClientInstanceMethod<Guid, byte, byte[], bool>? SendWearPants
        = ReflectionUtility.FindRpc<PlayerClothing, ClientInstanceMethod<Guid, byte, byte[], bool>>("SendWearPants");
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static readonly ClientInstanceMethod<Guid, byte, byte[], bool>? SendWearHat
        = ReflectionUtility.FindRpc<PlayerClothing, ClientInstanceMethod<Guid, byte, byte[], bool>>("SendWearHat");
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static readonly ClientInstanceMethod<Guid, byte, byte[], bool>? SendWearBackpack
        = ReflectionUtility.FindRpc<PlayerClothing, ClientInstanceMethod<Guid, byte, byte[], bool>>("SendWearBackpack");
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static readonly ClientInstanceMethod<Guid, byte, byte[], bool>? SendWearVest
        = ReflectionUtility.FindRpc<PlayerClothing, ClientInstanceMethod<Guid, byte, byte[], bool>>("SendWearVest");
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static readonly ClientInstanceMethod<Guid, byte, byte[], bool>? SendWearMask
        = ReflectionUtility.FindRpc<PlayerClothing, ClientInstanceMethod<Guid, byte, byte[], bool>>("SendWearMask");
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static readonly ClientInstanceMethod<Guid, byte, byte[], bool>? SendWearGlasses
        = ReflectionUtility.FindRpc<PlayerClothing, ClientInstanceMethod<Guid, byte, byte[], bool>>("SendWearGlasses");

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static readonly ClientInstanceMethod? SendInventory
        = ReflectionUtility.FindRpc<PlayerInventory, ClientInstanceMethod>("SendInventory");

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static readonly InstanceGetter<Items, bool[,]>? GetItemsSlots
        = Accessor.GenerateInstanceGetter<Items, bool[,]>("slots", throwOnError: false);

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static readonly InstanceSetter<PlayerInventory, bool>? SetOwnerHasInventory
        = Accessor.GenerateInstanceSetter<PlayerInventory, bool>("ownerHasInventory", throwOnError: false);
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static readonly InstanceGetter<PlayerInventory, bool>? GetOwnerHasInventory
        = Accessor.GenerateInstanceGetter<PlayerInventory, bool>("ownerHasInventory", throwOnError: false);

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static readonly Action<PlayerInventory, byte, ItemJar>? SendItemRemove
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
    /// Convert an item rotation to an angle in degrees clockwise.
    /// </summary>
    /// <param name="rotation">Rotation value zero to three.</param>
    /// <returns>An angle in degrees, either 0, 90, 180, or 270.</returns>
    public static int RotationToDegrees(byte rotation)
    {
        return rotation % 4 * 90;
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
    public static List<IItem> ItemsFromInventory(WarfarePlayer player, bool addClothes = true, bool addItems = true, AssetRedirectService? assetRedirectService = null)
    {
        GameThread.AssertCurrent();
        if (!addItems && !addClothes)
            return new List<IItem>(0);
        
        List<IItem> items = new List<IItem>(32);
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
                        items.Add(new RedirectedPageItem(jar.x, jar.y, (Page)page, jar.rot, type, variant));
                    else items.Add(new ConcretePageItem(jar.x, jar.y, (Page)page, jar.rot, AssetLink.Create(asset), jar.item.state, jar.item.amount));
                }
            }
        }
        if (addClothes)
        {
            FactionInfo? playerFaction = player.Team.Faction;
            PlayerClothing playerClothes = player.UnturnedPlayer.clothing;
            if (playerClothes.shirtAsset != null)
            {
                if (playerFaction != null && assetRedirectService != null && assetRedirectService.TryFindRedirectType(playerClothes.shirtAsset, out type, out FactionInfo? faction, out string? variant, clothingOnly: true) && (faction == null || Equals(faction, playerFaction)))
                    items.Add(new RedirectedClothingItem(ClothingType.Shirt, type, variant));
                else
                    items.Add(new ConcreteClothingItem(ClothingType.Shirt, AssetLink.Create(playerClothes.shirtAsset), playerClothes.shirtState));
            }
            if (playerClothes.pantsAsset != null)
            {
                if (playerFaction != null && assetRedirectService != null && assetRedirectService.TryFindRedirectType(playerClothes.pantsAsset, out type, out FactionInfo? faction, out string? variant, clothingOnly: true) && (faction == null || Equals(faction, playerFaction)))
                    items.Add(new RedirectedClothingItem(ClothingType.Pants, type, variant));
                else
                    items.Add(new ConcreteClothingItem(ClothingType.Pants, AssetLink.Create(playerClothes.pantsAsset), playerClothes.pantsState));
            }
            if (playerClothes.vestAsset != null)
            {
                if (playerFaction != null && assetRedirectService != null && assetRedirectService.TryFindRedirectType(playerClothes.vestAsset, out type, out FactionInfo? faction, out string? variant, clothingOnly: true) && (faction == null || Equals(faction, playerFaction)))
                    items.Add(new RedirectedClothingItem(ClothingType.Vest, type, variant));
                else
                    items.Add(new ConcreteClothingItem(ClothingType.Vest, AssetLink.Create(playerClothes.vestAsset), playerClothes.vestState));
            }
            if (playerClothes.hatAsset != null)
            {
                if (playerFaction != null && assetRedirectService != null && assetRedirectService.TryFindRedirectType(playerClothes.hatAsset, out type, out FactionInfo? faction, out string? variant, clothingOnly: true) && (faction == null || Equals(faction, playerFaction)))
                    items.Add(new RedirectedClothingItem(ClothingType.Hat, type, variant));
                else
                    items.Add(new ConcreteClothingItem(ClothingType.Hat, AssetLink.Create(playerClothes.hatAsset), playerClothes.hatState));
            }
            if (playerClothes.maskAsset != null)
            {
                if (playerFaction != null && assetRedirectService != null && assetRedirectService.TryFindRedirectType(playerClothes.maskAsset, out type, out FactionInfo? faction, out string? variant, clothingOnly: true) && (faction == null || Equals(faction, playerFaction)))
                    items.Add(new RedirectedClothingItem(ClothingType.Mask, type, variant));
                else
                    items.Add(new ConcreteClothingItem(ClothingType.Mask, AssetLink.Create(playerClothes.maskAsset), playerClothes.maskState));
            }
            if (playerClothes.backpackAsset != null)
            {
                if (playerFaction != null && assetRedirectService != null && assetRedirectService.TryFindRedirectType(playerClothes.backpackAsset, out type, out FactionInfo? faction, out string? variant, clothingOnly: true) && (faction == null || Equals(faction, playerFaction)))
                    items.Add(new RedirectedClothingItem(ClothingType.Backpack, type, variant));
                else
                    items.Add(new ConcreteClothingItem(ClothingType.Backpack, AssetLink.Create(playerClothes.backpackAsset), playerClothes.backpackState));
            }
            if (playerClothes.glassesAsset != null)
            {
                if (playerFaction != null && assetRedirectService != null && assetRedirectService.TryFindRedirectType(playerClothes.glassesAsset, out type, out FactionInfo? faction, out string? variant, clothingOnly: true) && (faction == null || Equals(faction, playerFaction)))
                    items.Add(new RedirectedClothingItem(ClothingType.Glasses, type, variant));
                else
                    items.Add(new ConcreteClothingItem(ClothingType.Glasses, AssetLink.Create(playerClothes.glassesAsset), playerClothes.glassesState));
            }
        }

        return items;
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
    /// Handles logic that needs ran with a player's inventory is cleared.
    /// </summary>
    public static void OnClearingInventory(WarfarePlayer player)
    {
        GameThread.AssertCurrent();

        player.ComponentOrNull<ItemTrackingPlayerComponent>()?.Reset();
    }

    /// <summary>
    /// Find an item in an inventory by it's <see cref="Item"/> reference.
    /// </summary>
    public static bool TryFindItem(PlayerInventory inventory, Item item, out byte x, out byte y, out Page page, out byte rot)
    {
        int maxPage = PlayerInventory.PAGES - 2;
        for (byte pg = 0; pg < maxPage; ++pg)
        {
            Items pageGrp = inventory.items[pg];
            int itemCt = pageGrp.getItemCount();
            for (int i = 0; i < itemCt; ++i)
            {
                ItemJar jar = pageGrp.getItem((byte)i);
                if (ReferenceEquals(jar.item, item))
                {
                    x = jar.x;
                    y = jar.y;
                    page = (Page)pg;
                    rot = jar.rot;
                    return true;
                }
            }
        }

        x = byte.MaxValue;
        y = byte.MaxValue;
        page = (Page)byte.MaxValue;
        rot = 0;
        return false;
    }

    /// <summary>
    /// Check if a player has any items or clothes equipped (if <paramref name="clothes"/> is <see langword="true"/>).
    /// </summary>
    public static bool HasAnyItems(WarfarePlayer player, bool clothes = true)
    {
        if (clothes)
        {
            PlayerClothing clothing = player.UnturnedPlayer.clothing;
            if (clothing.backpackAsset != null
                || clothing.glassesAsset != null
                || clothing.hatAsset != null
                || clothing.pantsAsset != null
                || clothing.maskAsset != null
                || clothing.shirtAsset != null
                || clothing.vestAsset != null)
            {
                return true;
            }
        }

        PlayerInventory inventory = player.UnturnedPlayer.inventory;

        for (byte page = 0; page < PlayerInventory.STORAGE; page++)
        {
            byte count = inventory.getItemCount(page);
            if (count > 0)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Effeciently remove all items and clothes from a player's inventory.
    /// </summary>
    public static void ClearInventory(WarfarePlayer player, bool clothes = true)
    {
        GameThread.AssertCurrent();
        if (!player.IsOnline)
            return;

        OnClearingInventory(player);

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
                int maxPage = PlayerInventory.STORAGE;
                for (int i = 0; i < maxPage; ++i)
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
    public static ItemJar? GetHeldItem(this WarfarePlayer player, out Page page)
    {
        GameThread.AssertCurrent();
        if (player.IsOnline)
        {
            PlayerEquipment eq = player.UnturnedPlayer.equipment;
            if (eq.asset != null)
            {
                PlayerInventory inventory = player.UnturnedPlayer.inventory;
                byte pg = eq.equippedPage;
                page = (Page)pg;
                return inventory.getItem(pg, inventory.getIndex(pg, eq.equipped_x, eq.equipped_y));
            }
        }

        page = (Page)byte.MaxValue;
        return null;
    }

    /// <summary>
    /// Get the <see cref="ItemJar"/> that's at a specific position in the inventory.
    /// </summary>
    public static ItemJar? GetItemAt(this WarfarePlayer player, Page page, byte x, byte y)
    {
        return GetItemAt(player, page, x, y, out _);
    }

    /// <summary>
    /// Get the <see cref="ItemJar"/> that's at a specific position in the inventory.
    /// </summary>
    public static ItemJar? GetItemAt(this WarfarePlayer player, Page page, byte x, byte y, out byte index)
    {
        GameThread.AssertCurrent();
        if (player.IsOnline)
        {
            PlayerInventory inventory = player.UnturnedPlayer.inventory;
            index = inventory.getIndex((byte)page, x, y);
            if (index != byte.MaxValue)
                return inventory.getItem((byte)page, index);
        }
        else
        {
            index = byte.MaxValue;
        }

        return null;
    }

    /// <summary>
    /// Get the <see cref="ItemJar"/> that's at a specific position in the inventory.
    /// </summary>
    public static ItemJar? GetItemAt(this PlayerInventory inventory, Page page, byte x, byte y)
    {
        return GetItemAt(inventory, page, x, y, out _);
    }

    /// <summary>
    /// Get the <see cref="ItemJar"/> that's at a specific position in the inventory.
    /// </summary>
    public static ItemJar? GetItemAt(this PlayerInventory inventory, Page page, byte x, byte y, out byte index)
    {
        GameThread.AssertCurrent();

        index = inventory.getIndex((byte)page, x, y);
        if (index != byte.MaxValue)
            return inventory.getItem((byte)page, index);

        return null;
    }

    /// <summary>
    /// Check if two items are overlapping that are in the same page.
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
    /// Check if an item can be moved from one location to another.
    /// </summary>
    public static bool CanPerformMove(PlayerInventory inventory, ItemJar item, Page page, byte x, byte y, byte rot)
    {
        Items destinationPage = inventory.items[(int)page];

        byte sx = item.size_x, sy = item.size_y;

        int itemCt = destinationPage.getItemCount();
        for (int i = 0; i < itemCt; ++i)
        {
            ItemJar cmpItem = destinationPage.getItem((byte)i);
            if (cmpItem == item)
                continue;

            if (IsOverlapping(cmpItem.x, cmpItem.y, cmpItem.size_x, cmpItem.size_y, x, y, sx, sy, cmpItem.rot, rot))
                return false;
        }

        return !IsOutOfBounds(destinationPage, x, y, sx, sy, rot);
    }

    /// <summary>
    /// Check if an item can be swapped with another item.
    /// </summary>
    public static bool CanPerformSwap(PlayerInventory inventory, ItemJar item1, Page item1Page, ItemJar item2, Page item2Page)
    {
        if (item1 == item2)
        {
            return item1Page == item2Page;
        }

        Items destinationPage1 = inventory.items[(int)item2Page];
        Items destinationPage2 = inventory.items[(int)item1Page];

        byte sx1 = item1.size_x, sy1 = item1.size_y,
             sx2 = item2.size_x, sy2 = item2.size_y;

        byte x1 = item1.x, y1 = item1.y,
             x2 = item2.x, y2 = item2.y;

        byte rot1 = item1.rot, rot2 = item2.rot;

        int itemCt = destinationPage1.getItemCount();
        for (int i = 0; i < itemCt; ++i)
        {
            ItemJar cmpItem = destinationPage1.getItem((byte)i);
            if (cmpItem == item1 || cmpItem == item2)
                continue;

            if (IsOverlapping(cmpItem.x, cmpItem.y, cmpItem.size_x, cmpItem.size_y, x1, y1, sx1, sy1, cmpItem.rot, rot1))
                return false;
        }

        itemCt = destinationPage2.getItemCount();
        for (int i = 0; i < itemCt; ++i)
        {
            ItemJar cmpItem = destinationPage2.getItem((byte)i);
            if (cmpItem == item1 || cmpItem == item2)
                continue;

            if (IsOverlapping(cmpItem.x, cmpItem.y, cmpItem.size_x, cmpItem.size_y, x2, y2, sx2, sy2, cmpItem.rot, rot2))
                return false;
        }

        // items swapping in same page overlapping themselves
        if (item1Page == item2Page && IsOverlapping(x1, y1, sx1, sy1, x2, y2, sx2, sy2, rot1, rot2))
        {
            return false;
        }

        return !IsOutOfBounds(destinationPage1, x1, y1, sx1, sy1, rot1) && !IsOutOfBounds(destinationPage2, x2, y2, sx2, sy2, rot2);
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
        if (rotation % 2 == 1)
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

    /// <summary>
    /// Guesses the type of weapon <paramref name="gun"/> is based off it's ammo and size.
    /// </summary>
    [Pure]
    public static FirearmClass GetFirearmClass(ItemGunAsset gun)
    {
        Asset? magazineAsset = Assets.find(EAssetType.ITEM, gun.getMagazineID());
        ItemMagazineAsset? magazine = magazineAsset as ItemMagazineAsset;

        if (magazine?.pellets > 1)
        {
            if (gun.size_x <= 3)
                return FirearmClass.SmallShotgun;
            else
                return FirearmClass.Shotgun;
        }
        else if (gun.size_x == 2)
        {
            if (gun.hasAuto)
                return FirearmClass.MachinePistol;
            else
                return FirearmClass.Pistol;
        }
        else if (gun.size_x == 3)
        {
            if (gun.hasAuto)
                return FirearmClass.LargeMachinePistol;
            else
                return FirearmClass.MediumSidearm;
        }
        else if (gun.size_x == 4)
        {
            if (gun.projectile != null && gun.vehicleDamage < 100)
                return FirearmClass.GrenadeLauncher;

            if (gun.playerDamageMultiplier.damage < 40)
                return FirearmClass.SubmachineGun;
            else if (gun.playerDamageMultiplier.damage < 60)
                return FirearmClass.Rifle;
            else
                return FirearmClass.BattleRifle;
        }
        else if (gun.size_x == 5)
        {
            if (gun.hasAuto)
            {
                if (gun.ammoMax < 45)
                    return FirearmClass.BattleRifle;
                else if (gun.playerDamageMultiplier.damage < 60)
                    return FirearmClass.LightMachineGun;
                else
                    return FirearmClass.GeneralPurposeMachineGun;
            }
            else
            {
                if (gun.projectile != null)
                {
                    if (gun.vehicleDamage < 500)
                        return FirearmClass.LightAntiTank;
                    else
                        return FirearmClass.HeavyAntiTank;
                }

                if (gun.playerDamageMultiplier.damage < 100)
                    return FirearmClass.DMR;
                else
                    return FirearmClass.Sniper;
            }
        }

        return FirearmClass.TooDifficultToClassify;
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