using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Cosmetics;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Util.Inventory;

public class FastItemDistributionService : FallbackItemDistributionService
{
    private static readonly List<Item> WorkingItemList = new List<Item>(4);

    private readonly IKitItemResolver _itemResolver;
    private readonly DroppedItemTracker _droppedItemTracker;
    private readonly CosmeticInstancer _cosmeticInstancer;
    private bool _isAutoClearing;

    public FastItemDistributionService(IKitItemResolver itemResolver, DroppedItemTracker droppedItemTracker, CosmeticInstancer cosmeticInstancer
#if ITEM_DISTRO_DEBUG
        , ILogger<FastItemDistributionService> logger
#endif
    ) : base(itemResolver, droppedItemTracker, cosmeticInstancer
#if ITEM_DISTRO_DEBUG
        , logger
#endif    
    )
    {
        if (!ItemUtility.SupportsFastKits)
            throw new NotSupportedException("Fast kits not supported.");

        _itemResolver = itemResolver;
        _droppedItemTracker = droppedItemTracker;
        _cosmeticInstancer = cosmeticInstancer;
    }

    public override int ClearInventory<TState>(WarfarePlayer player, TState state)
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        GameThread.AssertCurrent();
        ItemUtility.OnClearingInventory(player);

        Player nativePlayer = player.UnturnedPlayer;
        if (_isAutoClearing)
            DebugLog($" - Clearing inventory of {nativePlayer.channel.owner.playerID.steamID}.");
        else
            DebugLog($" Clearing inventory of {nativePlayer.channel.owner.playerID.steamID}.");

        // clears the inventory quickly
        if (nativePlayer.equipment.asset != null)
        {
            nativePlayer.equipment.dequip();
        }

        int ct = 0;

        // clear all items
        for (Page page = 0; page < Page.Storage; ++page)
        {
            using IDisposable? profiler2 = ProfilerUtil.Profile("Clear one page");

            Items items = nativePlayer.inventory.items[(int)page];
            bool[,] fill = ItemUtility.GetItemsSlots!(items);
            byte count = items.getItemCount();

            for (int index = count - 1; index >= 0; index--)
            {
                ItemJar jar = items.getItem((byte)index);
                if (typeof(TState) != typeof(IItemDistributionService.DefaultItemDistributionState) && !state.ShouldClearItem(jar, page, jar.GetAsset()))
                {
                    continue;
                }

                RemoveItemFast(nativePlayer.inventory, items, index, jar, page, fill);
                ++ct;
            }
        }

        if (!state.ClearClothes)
            return ct;

        WorkingItemList.Clear();

        byte[] emptyState = Array.Empty<byte>();
        PlayerClothing clothing = nativePlayer.clothing;
        NetId clothingNetId = clothing.GetNetId();
        PooledTransportConnectionList tcList = Provider.GatherRemoteClientConnections();

        if (clothing.shirtAsset != null && state.ShouldClearItem(ClothingType.Shirt, clothing.shirtAsset, clothing.shirtState, clothing.shirtQuality))
        {
            using IDisposable? profiler2 = ProfilerUtil.Profile("Clear shirt");
            StoreSkippedItemsFromPage(nativePlayer, Page.Shirt, WorkingItemList);
            ItemUtility.SendWearShirt!.InvokeAndLoopback(clothingNetId, ENetReliability.Reliable, tcList, Guid.Empty, 100, emptyState, false);
            ++ct;
            DebugLog(" - Cleared shirt.");
        }
        if (clothing.pantsAsset != null && state.ShouldClearItem(ClothingType.Pants, clothing.pantsAsset, clothing.pantsState, clothing.pantsQuality))
        {
            using IDisposable? profiler2 = ProfilerUtil.Profile("Clear pants");
            StoreSkippedItemsFromPage(nativePlayer, Page.Pants, WorkingItemList);
            ItemUtility.SendWearPants!.InvokeAndLoopback(clothingNetId, ENetReliability.Reliable, tcList, Guid.Empty, 100, emptyState, false);
            ++ct;
            DebugLog(" - Cleared pants.");
        }
        if (clothing.vestAsset != null && state.ShouldClearItem(ClothingType.Vest, clothing.vestAsset, clothing.vestState, clothing.vestQuality))
        {
            using IDisposable? profiler2 = ProfilerUtil.Profile("Clear vest");
            StoreSkippedItemsFromPage(nativePlayer, Page.Vest, WorkingItemList);
            ItemUtility.SendWearVest!.InvokeAndLoopback(clothingNetId, ENetReliability.Reliable, tcList, Guid.Empty, 100, emptyState, false);
            ++ct;
            DebugLog(" - Cleared vest.");
        }
        if (clothing.hatAsset != null && state.ShouldClearItem(ClothingType.Hat, clothing.hatAsset, clothing.hatState, clothing.hatQuality))
        {
            using IDisposable? profiler2 = ProfilerUtil.Profile("Clear hat");
            ItemUtility.SendWearHat!.InvokeAndLoopback(clothingNetId, ENetReliability.Reliable, tcList, Guid.Empty, 100, emptyState, false);
            ++ct;
            DebugLog(" - Cleared hat.");
        }
        if (clothing.maskAsset != null && state.ShouldClearItem(ClothingType.Mask, clothing.maskAsset, clothing.maskState, clothing.maskQuality))
        {
            using IDisposable? profiler2 = ProfilerUtil.Profile("Clear mask");
            ItemUtility.SendWearMask!.InvokeAndLoopback(clothingNetId, ENetReliability.Reliable, tcList, Guid.Empty, 100, emptyState, false);
            ++ct;
            DebugLog(" - Cleared mask.");
        }
        if (clothing.backpackAsset != null && state.ShouldClearItem(ClothingType.Backpack, clothing.backpackAsset, clothing.backpackState, clothing.backpackQuality))
        {
            using IDisposable? profiler2 = ProfilerUtil.Profile("Clear backpack");
            StoreSkippedItemsFromPage(nativePlayer, Page.Backpack, WorkingItemList);
            ItemUtility.SendWearBackpack!.InvokeAndLoopback(clothingNetId, ENetReliability.Reliable, tcList, Guid.Empty, 100, emptyState, false);
            ++ct;
            DebugLog(" - Cleared backpack.");
        }
        if (clothing.glassesAsset != null && state.ShouldClearItem(ClothingType.Glasses, clothing.glassesAsset, clothing.glassesState, clothing.glassesQuality))
        {
            using IDisposable? profiler2 = ProfilerUtil.Profile("Clear glasses");
            ItemUtility.SendWearGlasses!.InvokeAndLoopback(clothingNetId, ENetReliability.Reliable, tcList, Guid.Empty, 100, emptyState, false);
            ++ct;
            DebugLog(" - Cleared glasses.");
        }

        // re-add items that were skipped over removed clothing to the inventory or drop it
        if (WorkingItemList.Count > 0)
        {
            using IDisposable? profiler2 = ProfilerUtil.Profile("Re-add uncleared items");
            foreach (Item item in WorkingItemList)
            {
                ItemAsset asset = item.GetAsset();
                if (asset == null)
                    continue;

                if (TryAddItemAuto(nativePlayer.inventory, item, asset, out Page page))
                {
                    DebugLog($" - Added back {asset.itemName} to {page}.");
                    continue;
                }

                _droppedItemTracker.SetNextDroppedItemInstigator(item, player.Steam64.m_SteamID);
                ItemManager.dropItem(item, player.Position, false, true, true);
                DebugLog($" - Dropped extra item {asset.itemName}.");
            }

            WorkingItemList.Clear();
        }

        if (!_isAutoClearing)
            ItemUtility.UpdateSlots(player);

        return ct;
    }

    private void StoreSkippedItemsFromPage(Player nativePlayer, Page page, List<Item> list)
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        Items pg = nativePlayer.inventory.items[(int)page];
        int itemCt = pg.getItemCount();
        if (itemCt == 0)
        {
            DebugLog($" - No skipped items for {page}.");
            return;
        }

        DebugLog($" - Storing {itemCt} skipped item(s):");
        bool[,] fill = ItemUtility.GetItemsSlots!(pg);
        for (int i = itemCt - 1; i >= 0; --i)
        {
            ItemJar jar = pg.getItem((byte)i);
            list.Add(jar.item);
            RemoveItemFast(nativePlayer.inventory, pg, i, jar, page, fill);
        }
    }

    private void RemoveItemFast(PlayerInventory inv, Items items, int index, ItemJar jar, Page page, bool[,] fill)
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        DebugLog($" - Removing item {jar.item.GetAsset().itemName} at {jar.x}, {jar.y}.");
        items.items.RemoveAt(index);
        int sx = jar.size_x, sy = jar.size_y;
        if (jar.rot % 2 == 1)
        {
            sx = jar.size_y; sy = jar.size_x;
        }

        sx += jar.x; sy += jar.y;

        for (int x = jar.x; x < sx; ++x)
        for (int y = jar.y; y < sy; ++y)
        {
            if (x < items.width && y < items.height)
                fill[x, y] = false;
        }

        ItemUtility.SendItemRemove!.Invoke(inv, (byte)page, jar);
    }

    public override int GiveItems<TState>(IEnumerable<IItem> items, WarfarePlayer player, TState state)
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        GameThread.AssertCurrent();

        Player nativePlayer = player.UnturnedPlayer;

        DebugLog($" Giving items to {nativePlayer.channel.owner.playerID.steamID}.");

        if (ItemUtility.HasAnyItems(player))
        {
            _isAutoClearing = true;
            try
            {
                if (typeof(TState) != typeof(IItemDistributionService.DefaultItemDistributionState) && state is IItemClearState clearState)
                {
                    ClearInventory(player, clearState);
                }
                else
                {
                    ClearInventory(player, new IItemDistributionService.DefaultItemDistributionState());
                }
            }
            finally
            {
                _isAutoClearing = false;
            }
        }
        else
        {
            ItemUtility.OnClearingInventory(player);
        }

        Kit? stateKit = state.Kit;
        Team stateTeam = state.RequestingTeam;

        // sfx for equipping clothes
        bool hasPlayedEffect = state.Silent;

        int ct = 0;

        int clothingFlag = 0;

        bool oldIsolationValue = false;

        IEnumerator<IItem> enumerator = items.GetEnumerator();
        try
        {
            // loop through all items and send clothes first, since clothes have to be equipped to make room for items
            while (enumerator.MoveNext())
            {
                if (enumerator.Current is not IClothingItem clothingItem || (clothingFlag & (1 << (int)clothingItem.ClothingType)) != 0)
                    continue;

                using IDisposable? profiler2 = ProfilerUtil.Profile("Add one clothing");

                KitItemResolutionResult result = _itemResolver.ResolveKitItem(clothingItem, stateKit, stateTeam);

                EItemType clothingItemType = clothingItem.ClothingType.GetItemType();
                if (result.Asset == null || result.Asset.type != clothingItemType || !state.ShouldGrantItem(clothingItem, ref result) || result.Asset is not ItemClothingAsset clothingAsset || result.Asset.type != clothingItemType)
                    continue;

                DebugLog($" - Setting {clothingItem.ClothingType} to {clothingAsset.itemName}.");
                clothingFlag |= 1 << (int)clothingItem.ClothingType;
                _cosmeticInstancer.SetClothing(clothingItem.ClothingType, player, clothingAsset, 100, result.State, !hasPlayedEffect);
                hasPlayedEffect = true;
                ++ct;
            }

            // try to reset the enumerator but some don't support it
            try
            {
                enumerator.Reset();
            }
            catch
            {
                enumerator.Dispose();
                enumerator = items.GetEnumerator();
            }

            ItemUtility.IsolateInventory(nativePlayer, out oldIsolationValue);

            PlayerInventory inventory = player.UnturnedPlayer.inventory;

            while (enumerator.MoveNext())
            {
                IItem? item = enumerator.Current;

                if (item is IClothingItem)
                    continue;

                if (item is not IPageItem pageItem)
                    continue;

                using IDisposable? profiler2 = ProfilerUtil.Profile("Add one item");

                KitItemResolutionResult result = _itemResolver.ResolveKitItem(pageItem, stateKit, stateTeam);

                byte x = pageItem.X, y = pageItem.Y, rot = pageItem.Rotation;
                Page page = pageItem.Page;
                if (page is Page.Primary or Page.Secondary && inventory.items[(int)page].getItemCount() > 0)
                {
                    page = Page.Hands;
                }

                if (result.Asset == null || !state.ShouldGrantItem(pageItem, ref result, ref x, ref y, ref page, ref rot) || result.Asset == null)
                {
                    DebugLog($" - Skipped granting ({pageItem.X}, {pageItem.Y}) ({result.Asset}).");
                    continue;
                }

                if (ItemUtility.IsOutOfBounds(inventory.items[(int)page], x, y, result.Asset.size_x, result.Asset.size_y, rot))
                {
                    DebugLog($" - Item {result.Asset} ({pageItem.X}, {pageItem.Y}) was out of bounds at {x}, {y}.");
                    x = pageItem.X;
                    y = pageItem.Y;
                    rot = pageItem.Rotation;
                    page = pageItem.Page;
                }
                else if (page is Page.Primary or Page.Secondary && inventory.items[(int)page].getItemCount() > 0)
                {
                    DebugLog($" - Item {result.Asset} tried to add to {page} slot but an item is already there. Adding to hands instead.");
                    page = Page.Hands;
                }

                Item newItem = new Item(result.Asset.id, result.Amount, result.Quality, result.State ?? result.Asset.getState(true));

                if (TryAddItemNoSlotReplication(inventory, newItem, result.Asset, x, y, page, rot))
                {
                    state.OnAddingPreviousItem(in result, x, y, rot, page, newItem);
                    continue;
                }

                if (!TryAddItemAuto(inventory, newItem, result.Asset, out page))
                {
                    _droppedItemTracker.SetNextDroppedItemInstigator(newItem, player.Steam64.m_SteamID);
                    Vector3 position = player.Position;
                    ItemManager.dropItem(newItem, position, !hasPlayedEffect, true, true);
                    state.OnDroppingPreviousItem(in result, position, newItem);
                    DebugLog($" - Dropped item {result.Asset.itemName} since it couldn't be auto-added.");
                }
                else if (typeof(TState) != typeof(IItemDistributionService.DefaultItemDistributionState) && ItemUtility.TryFindItem(inventory, newItem, page, out x, out y, out rot))
                {
                    state.OnAddingPreviousItem(in result, x, y, rot, page, newItem);
                }
            }
        }
        finally
        {
            enumerator.Dispose();

            ItemUtility.UndoIsolateInventory(nativePlayer, oldIsolationValue);
        }

        ItemUtility.SendPages(player);
        ItemUtility.UpdateSlots(player);
        return ct;
    }

    // copied from PlayerInventory.tryAddItem but without replicating the new slot.
    private bool TryAddItemNoSlotReplication(PlayerInventory inventory, Item item, ItemAsset asset, byte x, byte y, Page page, byte rot)
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        if ((int)page < PlayerInventory.SLOTS)
        {
            if (!asset.slot.canEquipInPage((byte)page))
            {
                DebugLog($" - Expected slot for {asset.itemName} ({page}) is invalid for {asset.slot}-configured items.");
                return false;
            }

            rot = 0;
        }

        int ct = inventory.getItemCount((byte)page);
        if (ct >= 200)
        {
            DebugLog($" - Expected page for {asset.itemName} {page} ({x}, {y}) @ {ItemUtility.RotationToDegrees(rot)}deg has too many ({ct}) items already.");
            return false;
        }

        if (!inventory.items[(int)page].checkSpaceEmpty(x, y, asset.size_x, asset.size_y, rot))
        {
            DebugLog($" - Expected position for {asset.itemName} {page} ({x}, {y}) @ {ItemUtility.RotationToDegrees(rot)}deg is obstructed.");
            return false;
        }

        DebugLog($" - Adding {asset.itemName} to {page} at ({x}, {y}) @ {ItemUtility.RotationToDegrees(rot)}deg.");
        inventory.items[(int)page].addItem(x, y, rot, item);
        return true;
    }

    // copied from PlayerInventory.tryAddItemAuto
    private bool TryAddItemAuto(PlayerInventory inventory, Item item, ItemAsset asset, out Page pg)
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        for (Page page = Page.Hands; page < Page.Storage; ++page)
        {
            if (!inventory.items[(int)page].tryAddItem(item))
                continue;

            DebugLog($" - Fallback auto-added item {asset.itemName} to {page}.");
            pg = page;
            return true;
        }

        DebugLog($" - Fallback auto-add for item {asset.itemName} couldn't find any space.");
        pg = (Page)byte.MaxValue;
        return false;
    }
}