#if DEBUG
//#define DEBUG_LOGGING
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
#if DEBUG_LOGGING
    private readonly ILogger<FallbackItemDistributionService> _logger;
#endif

    private bool _isAutoClearing;

    public FallbackItemDistributionService(IKitItemResolver itemResolver, DroppedItemTracker droppedItemTracker
#if DEBUG_LOGGING
        , ILogger<FallbackItemDistributionService> logger
#endif
    )
    {
        _itemResolver = itemResolver;
        _droppedItemTracker = droppedItemTracker;
#if DEBUG_LOGGING
        _logger = logger;
#endif
    }

    [Conditional("DEBUG_LOGGING"), StringFormatMethod("fmt"), UsedImplicitly]
    private void DebugLog(string fmt, params object?[]? args)
    {
#if DEBUG_LOGGING
        _logger.LogDebug(fmt, args);
#endif
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

            byte alreadyEqupped = 0;

            // loop through all items and send clothes first, since clothes have to be equipped to make room for items
            while (enumerator.MoveNext())
            {
                if (enumerator.Current is not IClothingItem clothingItem
                    || clothingItem.ClothingType > ClothingType.Glasses)
                {
                    continue;
                }

                ClothingItem clothingHelper = new ClothingItem(clothing, clothingItem.ClothingType);
                if ((alreadyEqupped & clothingHelper.Flag) != 0)
                    continue;

                KitItemResolutionResult result = _itemResolver.ResolveKitItem(clothingItem, stateKit, stateTeam);
                if (!clothingHelper.ValidAsset(result.Asset) || !state.ShouldGrantItem(clothingItem, ref result) || !clothingHelper.ValidAsset(result.Asset))
                {
                    continue;
                }

                alreadyEqupped |= clothingHelper.Flag;
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

                AddItem(in result, x, y, page, rot, inventory, ref hasPlayedEffect, ref state);
            }
        }
        finally
        {
            enumerator.Dispose();
        }

        ItemUtility.UpdateSlots(player);

        return ct;
    }

    private ItemJar? AddItem<TState>(in KitItemResolutionResult result, byte x, byte y, Page page, byte rot, PlayerInventory inventory, ref bool hasPlayedEffect, ref TState state) where TState : IItemDistributionState
    {
        Item newItem = new Item(result.Asset!.id, result.Amount, result.Quality, result.State ?? result.Asset.getState(true));

        if (x != byte.MaxValue && y != byte.MaxValue && page != (Page)byte.MaxValue && rot < 4
            && inventory.tryAddItem(newItem, x, y, (byte)page, rot))
        {
            state.OnAddingPreviousItem(in result, x, y, rot, page, newItem);
            ItemJar? jar = inventory.getItem((byte)page, inventory.getIndex((byte)page, x, y));
            return jar;
        }

        if (!inventory.tryAddItem(newItem, false, false))
        {
            _droppedItemTracker.SetNextDroppedItemInstigator(newItem, inventory.channel.owner.playerID.steamID.m_SteamID);
            Vector3 position = inventory.player.transform.position;
            ItemManager.dropItem(newItem, position, !hasPlayedEffect, true, true);
            state.OnDroppingPreviousItem(in result, position, newItem);
        }
        else if (ItemUtility.TryFindItem(inventory, newItem, out x, out y, out page, out rot))
        {
            if (typeof(TState) != typeof(IItemDistributionService.DefaultItemDistributionState) && ItemUtility.TryFindItem(inventory, newItem, out x, out y, out page, out rot))
            {
                state.OnAddingPreviousItem(in result, x, y, rot, page, newItem);
            }

            ItemJar? jar = inventory.getItem((byte)page, inventory.getIndex((byte)page, x, y));
            return jar;
        }

        return null;
    }

    /// <inheritdoc />
    public int RestockItems<TState>(IEnumerable<IItem> items, WarfarePlayer player, TState state) where TState : IItemDistributionState
    {
        GameThread.AssertCurrent();

        Kit? stateKit = state.Kit;
        Team stateTeam = state.RequestingTeam;

        int ct = 0;

        Player nativePlayer = player.UnturnedPlayer;

#if DEBUG_LOGGING
        using IDisposable? scope = _logger.BeginScope(player);
        DebugLog("Restocking...");
#endif

        PlayerInventory inventory = nativePlayer.inventory;
        PlayerEquipment equipment = nativePlayer.equipment;

        ItemTrackingPlayerComponent itemTracking = player.Component<ItemTrackingPlayerComponent>();

        List<(IItem, KitItemResolutionResult)> leftoverItems = new List<(IItem, KitItemResolutionResult)>(16);
        List<ItemJar> matchedItems = new List<ItemJar>(16);


        //_ = _droppedItemTracker.DestroyItemsDroppedByPlayerAsync(player.Steam64, true);

        // list of all attached items that are expected in a fresh kit
        List<ItemCaliberAsset> kitAttachedItems = new List<ItemCaliberAsset>(8);
        
        // list of all attachment items on all guns in the inventory
        List<ushort> startingAttachedItems = new List<ushort>(8);
        for (int pg = 0; pg < PlayerInventory.STORAGE; ++pg)
        {
            Items page = inventory.items[pg];
            for (int i = page.getItemCount() - 1; i >= 0; --i)
            {
                ItemJar jar = page.getItem((byte)i);
                if (jar.item.metadata.Length != 18 || jar.item.GetAsset() is not ItemGunAsset)
                    continue;

                for (int a = 0; a < 5; ++a)
                {
                    ushort id = BitConverter.ToUInt16(jar.item.metadata, a * 2);
                    if (id != 0)
                        startingAttachedItems.Add(id);
                }
            }
        }

        DebugLog(" Found starting attached items: {0}.", startingAttachedItems);

        foreach (IItem item in items)
        {
            if (item is IClothingItem)
                continue;

            if (item is not IPageItem pageItem)
                continue;

            KitItemResolutionResult result = _itemResolver.ResolveKitItem(pageItem, stateKit, stateTeam);
            if (result is { Asset: ItemGunAsset, State.Length: 18 })
            {
                for (int a = 0; a < 5; ++a)
                {
                    ushort id = BitConverter.ToUInt16(result.State, a * 2);
                    if (id != 0 && Assets.find(EAssetType.ITEM, id) is ItemCaliberAsset attachmentAsset)
                    {
                        if (!startingAttachedItems.Remove(attachmentAsset.id))
                            kitAttachedItems.Add(attachmentAsset);
                    }
                }
            }

            KitItemResolutionResult origResult = result;
            // find item position
            if (!itemTracking.TryGetCurrentItemPosition(pageItem.Page, pageItem.X, pageItem.Y, out Page page, out byte x, out byte y, out bool isDropped, out Item? itemInstance))
            {
                DebugLog(" Current position not found: {0}.", pageItem);
                leftoverItems.Add((item, origResult));
                continue;
            }

            if (isDropped)
            {
                DebugLog(" Current position dropped: {0}.", pageItem);
                leftoverItems.Add((item, origResult));
                continue;
            }

            byte index = inventory.getIndex((byte)page, x, y);
            ItemJar? jar = inventory.getItem((byte)page, index);

            if (jar != null && matchedItems.Contains(jar))
            {
                DebugLog(" Jar already matched: {0} ({1}, {2}).", pageItem, jar.x, jar.y);
                leftoverItems.Add((item, origResult));
                continue;
            }

            byte refXTmp = pageItem.X, refYTmp = pageItem.Y;
            Page refPgTmp = pageItem.Page;
            byte rot = jar?.rot ?? pageItem.Rotation;

            ItemAsset? asset = result.Asset;

            if (asset == null || !state.ShouldGrantItem(pageItem, ref result, ref refXTmp, ref refYTmp, ref refPgTmp, ref rot) || result.Asset == null)
            {
                DebugLog(" Shouldnt grant: {0} ({1}).", pageItem, result.Asset);
                // item was marked as shouldn't be granted
                if (jar != null)
                    matchedItems.Add(jar);
                continue;
            }

            if (jar == null || (itemInstance != null && jar.item != itemInstance) || jar.item.id != asset.id || result.Asset != asset)
            {
                DebugLog(" Wrong item type: {0} ({1}).", pageItem, result.Asset);
                leftoverItems.Add((item, origResult));
                continue;
            }

            // found original kit item
            DebugLog(" Found original: {0} ({1}): {2}, ({3}, {4}).", pageItem, result.Asset, page, x, y);
            matchedItems.Add(jar);
            RestockItem(equipment, inventory, page, x, y, jar, in result, ref ct);
            ++ct;
        }

        DebugLog(" New original attached items: {0}.", startingAttachedItems.ToArray());
        DebugLog(" Found kit attached items: {0}.", kitAttachedItems.Select(x => x.id).ToArray());

        DebugLog(" Running fallbacks...");
        // fallback to existing items
        foreach ((IItem item, KitItemResolutionResult result) in leftoverItems)
        {
            IPageItem pageItem = (IPageItem)item;

            ItemAsset? asset = result.Asset;

            byte refXTmp = pageItem.X, refYTmp = pageItem.Y;
            Page refPgTmp = pageItem.Page;
            byte rot = pageItem.Rotation;

            KitItemResolutionResult res = result;

            if (asset == null || !state.ShouldGrantItem(pageItem, ref res, ref refXTmp, ref refYTmp, ref refPgTmp, ref rot) || result.Asset == null)
            {
                continue;
            }

            ConsumeMissingItem(nativePlayer, in result, matchedItems, startingAttachedItems, kitAttachedItems, ref ct, refPgTmp, refXTmp, refYTmp, rot, ref state);
        }

        // add missing attachments
        DebugLog(" Running attachments...");
        for (int i = kitAttachedItems.Count - 1; i >= 0; --i)
        {
            ItemCaliberAsset c = kitAttachedItems[i];
            KitItemResolutionResult res = new KitItemResolutionResult(c, c.getState(EItemOrigin.ADMIN), c.amount, 100);

            ConsumeMissingItem(nativePlayer, in res, matchedItems, startingAttachedItems, kitAttachedItems, ref ct, (Page)byte.MaxValue,
                byte.MaxValue, byte.MaxValue, byte.MaxValue, ref state);
        }

        ItemUtility.UpdateSlots(player);

        return ct;
    }

    private void ConsumeMissingItem<TState>(Player player,
        in KitItemResolutionResult result,
        List<ItemJar> matchedItems,
        List<ushort> startingAttachedItems,
        List<ItemCaliberAsset> kitAttachedItems,
        ref int ct,
        Page page,
        byte x,
        byte y,
        byte rot,
        ref TState state) where TState : IItemDistributionState
    {
        PlayerInventory inventory = player.inventory;
        PlayerEquipment equipment = player.equipment;

        ItemAsset asset = result.Asset!;

        ItemJar? jar = null;
        Page foundPage = 0;
        // find an item of the same type
        for (int pg = 0; pg < PlayerInventory.STORAGE && jar == null; ++pg)
        {
            int itemCt = inventory.items[pg].getItemCount();
            for (int i = 0; i < itemCt; ++i)
            {
                ItemJar j = inventory.items[pg].getItem((byte)i);

                if (asset.id != j.item.id)
                    continue;

                if (matchedItems.Contains(j))
                    continue;

                matchedItems.Add(j);
                jar = j;
                foundPage = (Page)pg;
                break;
            }
        }

        // found a matching item
        if (jar != null)
        {
            DebugLog(" Found restocking item {0} ({1}, {2}): {3}", foundPage, jar.x, jar.y, asset);
            RestockItem(equipment, inventory, foundPage, jar.x, jar.y, jar, in result, ref ct);
            return;
        }

        // find oldest dropped item
        foreach (ItemData droppedItem in _droppedItemTracker
                     .EnumerateDroppedItems(player.channel.owner.playerID.steamID)
                     .OrderBy(x => x.lastDropped))
        {
            DebugLog(" Checking item {0} against {1}.", droppedItem.item.id, asset.id);
            if (droppedItem.item.id != asset.id)
            {
                continue;
            }

            DebugLog(" Found dropped item: {0}, {1} (#{2}).", asset, droppedItem.point, droppedItem.instanceID);
            ItemUtility.DestroyDroppedItem(droppedItem, true, false);
            break;
        }

        // find item attached to a gun
        int attachmentIndex = startingAttachedItems.IndexOf(asset.id);
        if (attachmentIndex != -1)
        {
            startingAttachedItems.RemoveAtFast(attachmentIndex);
            DebugLog(" Consumed attached attachment: {0}", asset);
            return;
        }

        // finally add the item if we've exhausted all other possibilities
        bool hasPlayedEffect = true;
        DebugLog(" Adding item: {0}, {1} ({2}, {3} @ {4}).", asset, page, x, y, rot);

        // remove default attachments from needed attachments
        if (result.State.Length == 18 && result.Asset is ItemGunAsset)
        {
            for (int a = 0; a < 5; ++a)
            {
                ushort id = BitConverter.ToUInt16(result.State, a * 2);
                int index = kitAttachedItems.FindIndex(x => x.id == id);
                if (index != -1)
                    kitAttachedItems.RemoveAt(index);
            }
        }
        jar = AddItem(in result, x, y, page, rot, inventory, ref hasPlayedEffect, ref state);
        ++ct;
        if (jar != null)
            matchedItems.Add(jar);
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