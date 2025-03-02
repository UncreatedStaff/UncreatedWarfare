using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Util.Inventory;

public class FallbackItemDistributionService : IItemDistributionService
{
    private static readonly List<Item> WorkingItemList = new List<Item>(4);

    private readonly IKitItemResolver _itemResolver;
    private readonly DroppedItemTracker _droppedItemTracker;

    private bool _isAutoClearing;

    public FallbackItemDistributionService(IKitItemResolver itemResolver, DroppedItemTracker droppedItemTracker)
    {
        _itemResolver = itemResolver;
        _droppedItemTracker = droppedItemTracker;
    }

    public int ClearInventory<TState>(WarfarePlayer player, TState state) where TState : IItemClearState
    {
        GameThread.AssertCurrent();
        ItemUtility.OnClearingInventory(player);

        Player nativePlayer = player.UnturnedPlayer;

        int ct = 0;

        int maxPage = PlayerInventory.STORAGE;

        // clear all items
        for (int page = 0; page < maxPage; page++)
        {
            Items items = nativePlayer.inventory.items[page];
            byte count = items.getItemCount();

            for (int index = count - 1; index >= 0; index--)
            {
                ItemJar jar = items.getItem((byte)index);
                if (typeof(TState) != typeof(IItemDistributionService.DefaultItemDistributionState)
                    && !state.ShouldClearItem(jar, (Page)page, jar.GetAsset()))
                {
                    continue;
                }

                items.removeItem((byte)index);
                ++ct;
            }
        }

        if (!state.ClearClothes)
            return ct;

        byte[] blank = Array.Empty<byte>();

        PlayerClothing clothing = nativePlayer.clothing;

        bool hasPlayedEffect = _isAutoClearing || state.Silent;

        WorkingItemList.Clear();

        foreach (ClothingItem clothingItem in ItemUtility.EnumerateClothingSlots(clothing))
        {
            ItemAsset? old = clothingItem.Asset;
            if (old == null || !state.ShouldClearItem(clothingItem.Type, old, clothingItem.State, clothingItem.Quality))
                continue;

            if (clothingItem.HasStorage)
            {
                // items skipped that are still in this page will be added to a list and removed then re-added or dropped later
                StoreSkippedItemsFromPage(nativePlayer, clothingItem.StoragePage, WorkingItemList);
            }
            clothingItem.AskWear(null, 0, blank, !hasPlayedEffect);
            RemoveAutoItem(nativePlayer, ref ct, old);
            hasPlayedEffect = true;
        }

        // sanity check clear all items to make sure clothes didn't somehow get added to another page
        for (int page = 0; page < maxPage; page++)
        {
            Items items = nativePlayer.inventory.items[page];
            byte count = items.getItemCount();

            for (int index = count - 1; index >= 0; index--)
            {
                ItemJar jar = items.getItem((byte)index);
                if (typeof(TState) != typeof(IItemDistributionService.DefaultItemDistributionState) && !state.ShouldClearItem(jar, (Page)page, jar.GetAsset()))
                {
                    continue;
                }

                items.removeItem((byte)index);
                ++ct;
            }
        }

        foreach (Item item in WorkingItemList)
        {
            if (nativePlayer.inventory.tryAddItemAuto(item, false, false, false, false))
                continue;

            _droppedItemTracker.SetNextDroppedItemInstigator(item, player.Steam64.m_SteamID);
            ItemManager.dropItem(item, player.Position, false, true, true);
        }

        WorkingItemList.Clear();

        if (!_isAutoClearing)
            ItemUtility.UpdateSlots(player);

        return ct;
    }

    private static void StoreSkippedItemsFromPage(Player nativePlayer, Page page, List<Item> list)
    {
        Items pg = nativePlayer.inventory.items[(int)page];
        int itemCt = pg.getItemCount();
        for (int i = itemCt - 1; i >= 0; --i)
        {
            Item item = pg.getItem((byte)i).item;
            list.Add(item);
            Console.WriteLine($"removing item: {i} ({item.GetAsset()})");
            pg.removeItem((byte)i);
        }
    }

    private static void RemoveAutoItem(Player nativePlayer, ref int ct, ItemAsset expectedAsset)
    {
        for (int pg = PlayerInventory.SLOTS; pg < PlayerInventory.STORAGE; ++pg)
        {
            Items page = nativePlayer.inventory.items[pg];

            int itemCt = page.getItemCount();
            if (itemCt == 0)
                continue;

            ItemJar jar = page.getItem((byte)(itemCt - 1));
            if (jar?.item == null || jar.item.id != expectedAsset.id)
                continue;

            page.removeItem((byte)(itemCt - 1));
            ++ct;
            break;
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

        IEnumerator<IItem> enumerator = items.GetEnumerator();
        try
        {
            PlayerClothing clothing = player.UnturnedPlayer.clothing;

            // loop through all items and send clothes first, since clothes have to be equipped to make room for items
            while (enumerator.MoveNext())
            {
                if (enumerator.Current is not IClothingItem clothingItem || clothingItem.ClothingType > ClothingType.Glasses)
                    continue;

                KitItemResolutionResult result = _itemResolver.ResolveKitItem(clothingItem, stateKit, stateTeam);
                ClothingItem clothingHelper = new ClothingItem(clothing, clothingItem.ClothingType);

                if (!clothingHelper.ValidAsset(result.Asset) || !state.ShouldGrantItem(clothingItem, ref result) || !clothingHelper.ValidAsset(result.Asset))
                {
                    continue;
                }

                clothingHelper.AskWear(result.Asset, result.Quality, result.State ?? result.Asset!.getState(true), !hasPlayedEffect);
                ++ct;
                hasPlayedEffect = true;
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
                
                if (result.Asset == null || !state.ShouldGrantItem(pageItem, ref result, ref x, ref y, ref page, ref rot) || result.Asset == null)
                {
                    continue;
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
        }

        ItemUtility.UpdateSlots(player);

        return ct;
    }

    /// <inheritdoc />
    public int RestockItems<TState>(IEnumerable<IItem> items, WarfarePlayer player, TState state) where TState : IItemDistributionState
    {
        GameThread.AssertCurrent();

        Kit? stateKit = state.Kit;
        Team stateTeam = state.RequestingTeam;

        int ct = 0;

        Player nativePlayer = player.UnturnedPlayer;

        PlayerInventory inventory = nativePlayer.inventory;
        PlayerEquipment equipment = nativePlayer.equipment;

        ItemTrackingPlayerComponent itemTracking = player.Component<ItemTrackingPlayerComponent>();

        List<(IItem, KitItemResolutionResult)> leftoverItems = new List<(IItem, KitItemResolutionResult)>(16);
        List<ItemJar> matchedItems = new List<ItemJar>(16);

        foreach (IItem item in items)
        {
            if (item is IClothingItem)
                continue;

            if (item is not IPageItem pageItem)
                continue;

            KitItemResolutionResult result = _itemResolver.ResolveKitItem(pageItem, stateKit, stateTeam);
            // find item position
            if (!itemTracking.TryGetCurrentItemPosition(pageItem.Page, pageItem.X, pageItem.Y, out Page page, out byte x, out byte y, out bool isDropped, out Item? itemInstance))
            {
                x = pageItem.X;
                y = pageItem.Y;
                page = pageItem.Page;
            }

            if (isDropped)
            {
                // they dropped the item, this is hard to deal with so just despawn it and itll be re-added later.
                if (itemInstance != null)
                    ItemUtility.DestroyDroppedItem(itemInstance, true);
                leftoverItems.Add((item, result));
                continue;
            }

            byte index = inventory.getIndex((byte)page, x, y);
            ItemJar? jar = inventory.getItem((byte)page, index);

            byte refXTmp = pageItem.X, refYTmp = pageItem.Y;
            Page refPgTmp = pageItem.Page;
            byte rot = jar?.rot ?? pageItem.Rotation;

            ItemAsset? asset = result.Asset;

            if (asset == null || !state.ShouldGrantItem(pageItem, ref result, ref refXTmp, ref refYTmp, ref refPgTmp, ref rot) || result.Asset == null)
            {
                if (jar != null)
                    matchedItems.Add(jar);
                continue;
            }

            if (jar == null || (itemInstance != null && jar.item != itemInstance) || result.Asset != asset)
            {
                leftoverItems.Add((item, result));
                continue;
            }

            matchedItems.Add(jar);
            RestockItem(equipment, inventory, page, x, y, jar, in result, ref ct);
        }

        foreach ((IItem item, KitItemResolutionResult result) in leftoverItems)
        {
            ItemJar? jar = null;
            Page page = 0;
            for (int pg = 0; pg < PlayerInventory.STORAGE; ++pg)
            {
                int itemCt = inventory.items[pg].getItemCount();
                for (int i = 0; i < itemCt; ++i)
                {
                    ItemJar j = inventory.items[pg].getItem((byte)i);

                    if (result.Asset!.id != j.item.id)
                        continue;

                    if (matchedItems.Contains(j))
                        continue;

                    matchedItems.Add(j);
                    jar = j;
                    page = (Page)pg;
                    break;
                }

                if (jar != null)
                    break;
            }

            if (jar != null)
            {
                RestockItem(equipment, inventory, page, jar.x, jar.y, jar, in result, ref ct);
            }
        }

        ItemUtility.UpdateSlots(player);

        return ct;
    }

    private static void RestockItem(PlayerEquipment equipment, PlayerInventory inventory, Page page, byte x, byte y, ItemJar jar, in KitItemResolutionResult result, ref int ct)
    {
        // equipped items need to use a different method to update.
        bool equipped = equipment.checkSelection((byte)page, x, y);
        bool hasIncremented = false;

        // amount
        if (jar.item.amount < result.Amount)
        {
            ++ct;
            hasIncremented = true;
            inventory.sendUpdateAmount((byte)page, x, y, result.Amount);
        }

        // quality
        if (jar.item.quality < result.Quality)
        {
            if (!hasIncremented)
            {
                ++ct;
                hasIncremented = true;
            }

            if (equipped)
            {
                equipment.quality = result.Quality;
                equipment.sendUpdateQuality();
            }
            else
            {
                inventory.sendUpdateQuality((byte)page, x, y, result.Quality);
            }
        }

        // if state is already up to date
        if (jar.item.state.Length == 0
            || jar.item.state.Length != result.State.Length
            || jar.item.state.SequenceEqual(result.State)
           )
        {
            return;
        }

        if (result.Asset is ItemGunAsset)
        {
            // refill ammo
            if (Assets.find(EAssetType.ITEM, BitConverter.ToUInt16(jar.item.state, (int)AttachmentType.Magazine)) is ItemMagazineAsset mag)
                jar.item.state[10] = mag.amount;

            // attachment durabilities
            jar.item.state[13] = 100;
            jar.item.state[14] = 100;
            jar.item.state[15] = 100;
            jar.item.state[16] = 100;
            jar.item.state[17] = 100;
        }
        else
        {
            // fix state for other items
            Buffer.BlockCopy(result.State, 0, jar.item.state, 0, result.State.Length);
        }

        if (!hasIncremented)
            ++ct;

        if (equipped)
        {
            equipment.sendUpdateState();
        }
        else
        {
            inventory.sendUpdateInvState((byte)page, x, y, jar.item.state);
        }
    }
}