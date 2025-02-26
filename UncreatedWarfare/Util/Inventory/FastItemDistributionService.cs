using SDG.NetTransport;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Util.Inventory;

public class FastItemDistributionService : IItemDistributionService
{
    private static readonly List<Item> WorkingItemList = new List<Item>(4);

    private readonly IKitItemResolver _itemResolver;
    private readonly DroppedItemTracker _droppedItemTracker;

    private bool _isAutoClearing;

    public FastItemDistributionService(IKitItemResolver itemResolver, DroppedItemTracker droppedItemTracker)
    {
        if (!ItemUtility.SupportsFastKits)
            throw new NotSupportedException("Fast kits not supported.");

        _itemResolver = itemResolver;
        _droppedItemTracker = droppedItemTracker;
    }

    public int ClearInventory<TState>(WarfarePlayer player, TState state) where TState : IItemClearState
    {
        GameThread.AssertCurrent();
        ItemUtility.OnClearingInventory(player);

        Player nativePlayer = player.UnturnedPlayer;

        // clears the inventory quickly
        nativePlayer.equipment.dequip();

        int ct = 0;
        int maxPage = PlayerInventory.STORAGE;

        // clear all items
        for (int page = 0; page < maxPage; page++)
        {
            Items items = nativePlayer.inventory.items[page];
            bool[,] fill = ItemUtility.GetItemsSlots!(items);
            byte count = items.getItemCount();

            for (int index = count - 1; index >= 0; index--)
            {
                ItemJar jar = items.getItem((byte)index);
                if (typeof(TState) != typeof(IItemDistributionService.DefaultItemDistributionState) && !state.ShouldClearItem(jar, (Page)page, jar.GetAsset()))
                {
                    continue;
                }

                RemoveItemFast(nativePlayer.inventory, items, index, jar, fill);
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
            StoreSkippedItemsFromPage(nativePlayer, Page.Shirt, WorkingItemList);
            ItemUtility.SendWearShirt!.InvokeAndLoopback(clothingNetId, ENetReliability.Reliable, tcList, Guid.Empty, 100, emptyState, false);
            ++ct;
        }
        if (clothing.pantsAsset != null && state.ShouldClearItem(ClothingType.Pants, clothing.pantsAsset, clothing.pantsState, clothing.pantsQuality))
        {
            StoreSkippedItemsFromPage(nativePlayer, Page.Pants, WorkingItemList);
            ItemUtility.SendWearPants!.InvokeAndLoopback(clothingNetId, ENetReliability.Reliable, tcList, Guid.Empty, 100, emptyState, false);
            ++ct;
        }
        if (clothing.vestAsset != null && state.ShouldClearItem(ClothingType.Vest, clothing.vestAsset, clothing.vestState, clothing.vestQuality))
        {
            StoreSkippedItemsFromPage(nativePlayer, Page.Vest, WorkingItemList);
            ItemUtility.SendWearVest!.InvokeAndLoopback(clothingNetId, ENetReliability.Reliable, tcList, Guid.Empty, 100, emptyState, false);
            ++ct;
        }
        if (clothing.hatAsset != null && state.ShouldClearItem(ClothingType.Hat, clothing.hatAsset, clothing.hatState, clothing.hatQuality))
        {
            ItemUtility.SendWearHat!.InvokeAndLoopback(clothingNetId, ENetReliability.Reliable, tcList, Guid.Empty, 100, emptyState, false);
            ++ct;
        }
        if (clothing.maskAsset != null && state.ShouldClearItem(ClothingType.Mask, clothing.maskAsset, clothing.maskState, clothing.maskQuality))
        {
            ItemUtility.SendWearMask!.InvokeAndLoopback(clothingNetId, ENetReliability.Reliable, tcList, Guid.Empty, 100, emptyState, false);
            ++ct;
        }
        if (clothing.backpackAsset != null && state.ShouldClearItem(ClothingType.Backpack, clothing.backpackAsset, clothing.backpackState, clothing.backpackQuality))
        {
            StoreSkippedItemsFromPage(nativePlayer, Page.Backpack, WorkingItemList);
            ItemUtility.SendWearBackpack!.InvokeAndLoopback(clothingNetId, ENetReliability.Reliable, tcList, Guid.Empty, 100, emptyState, false);
            ++ct;
        }
        if (clothing.glassesAsset != null && state.ShouldClearItem(ClothingType.Glasses, clothing.glassesAsset, clothing.glassesState, clothing.glassesQuality))
        {
            ItemUtility.SendWearGlasses!.InvokeAndLoopback(clothingNetId, ENetReliability.Reliable, tcList, Guid.Empty, 100, emptyState, false);
            ++ct;
        }

        // re-add items that were skipped over removed clothing to the inventory or drop it
        if (WorkingItemList.Count > 0)
        {
            foreach (Item item in WorkingItemList)
            {
                bool added = false;
                for (int page = PlayerInventory.SLOTS; page < maxPage; ++page)
                {
                    if (!nativePlayer.inventory.items[page].tryAddItem(item, false))
                        continue;

                    added = true;
                    break;
                }

                if (added)
                    continue;

                _droppedItemTracker.SetNextDroppedItemInstigator(item, player.Steam64.m_SteamID);
                ItemManager.dropItem(item, player.Position, false, true, true);
            }

            WorkingItemList.Clear();
        }

        if (!_isAutoClearing)
            ItemUtility.UpdateSlots(player);

        return ct;

        static void StoreSkippedItemsFromPage(Player nativePlayer, Page page, List<Item> list)
        {
            Items pg = nativePlayer.inventory.items[(int)page];
            int itemCt = pg.getItemCount();
            if (itemCt == 0)
                return;

            bool[,] fill = ItemUtility.GetItemsSlots!(pg);
            for (int i = itemCt - 1; i >= 0; --i)
            {
                ItemJar jar = pg.getItem((byte)i);
                list.Add(jar.item);
                RemoveItemFast(nativePlayer.inventory, pg, i, jar, fill);
            }
        }

        static void RemoveItemFast(PlayerInventory inv, Items items, int index, ItemJar jar, bool[,] fill)
        {
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

            ItemUtility.SendItemRemove!.Invoke(inv, (byte)index, jar);
        }
    }

    public int GiveItems<TState>(IEnumerable<IItem> items, WarfarePlayer player, TState state) where TState : IItemDistributionState
    {
        GameThread.AssertCurrent();

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

        Player nativePlayer = player.UnturnedPlayer;
        bool oldIsolationValue = false;

        IEnumerator<IItem> enumerator = items.GetEnumerator();
        try
        {
            // loop through all items and send clothes first, since clothes have to be equipped to make room for items
            while (enumerator.MoveNext())
            {
                if (enumerator.Current is not IClothingItem clothingItem || (clothingFlag & (1 << (int)clothingItem.ClothingType)) != 0)
                    continue;

                KitItemResolutionResult result = _itemResolver.ResolveKitItem(clothingItem, stateKit, stateTeam);

                EItemType clothingItemType = clothingItem.ClothingType.GetItemType();
                if (result.Asset == null || result.Asset.type != clothingItemType || !state.ShouldGrantItem(clothingItem, ref result) || result.Asset == null || result.Asset.type != clothingItemType)
                    continue;

                clothingFlag |= 1 << (int)clothingItem.ClothingType;
                ItemUtility.SendWearClothing(nativePlayer, result.Asset, clothingItem.ClothingType, 100, result.State, !hasPlayedEffect);
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

                KitItemResolutionResult result = _itemResolver.ResolveKitItem(pageItem, stateKit, stateTeam);

                byte x = pageItem.X, y = pageItem.Y, rot = pageItem.Rotation;
                Page page = pageItem.Page;
                if (page is Page.Primary or Page.Secondary && inventory.items[(int)page].getItemCount() > 0)
                {
                    page = Page.Hands;
                }

                if (result.Asset == null || !state.ShouldGrantItem(pageItem, ref result, ref x, ref y, ref page, ref rot) || result.Asset == null)
                {
                    continue;
                }

                if (ItemUtility.IsOutOfBounds(inventory.items[(int)page], x, y, result.Asset.size_x, result.Asset.size_y, rot))
                {
                    x = pageItem.X;
                    y = pageItem.Y;
                    rot = pageItem.Rotation;
                    page = pageItem.Page;
                }
                else if (page is Page.Primary or Page.Secondary && inventory.items[(int)page].getItemCount() > 0)
                {
                    page = Page.Hands;
                }

                Item newItem = new Item(result.Asset.id, result.Amount, result.Quality, result.State ?? result.Asset.getState(true));


                if (inventory.tryAddItem(newItem, x, y, (byte)page, rot))
                {
                    state.OnAddingPreviousItem(in result, x, y, rot, page, newItem);
                    continue;
                }

                if (!inventory.tryAddItem(newItem, false, false))
                {
                    _droppedItemTracker.SetNextDroppedItemInstigator(newItem, player.Steam64.m_SteamID);
                    Vector3 position = player.Position;
                    ItemManager.dropItem(newItem, position, !hasPlayedEffect, true, true);
                    state.OnDroppingPreviousItem(in result, position, newItem);
                }
                else if (typeof(TState) != typeof(IItemDistributionService.DefaultItemDistributionState) && ItemUtility.TryFindItem(inventory, newItem, out x, out y, out page, out rot))
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

    /// <inheritdoc />
    public int RestockItems<TState>(IEnumerable<IItem> items, WarfarePlayer player, TState state) where TState : IItemDistributionState
    {
        throw new NotImplementedException();
    }
}
