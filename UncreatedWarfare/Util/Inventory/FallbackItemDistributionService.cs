using System;
using System.Collections.Generic;
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
                if (typeof(TState) != typeof(IItemDistributionService.DefaultItemDistributionState) && !state.ShouldClearItem(jar, (Page)page, jar.GetAsset()))
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

        // clear clothes (why nelson)
        if (clothing.backpackAsset != null && state.ShouldClearItem(ClothingType.Backpack, clothing.backpackAsset, clothing.backpackState, clothing.backpackQuality))
        {
            // items skipped that are still in this page will be added to a list and removed then re-added or dropped later
            StoreSkippedItemsFromPage(nativePlayer, Page.Backpack, WorkingItemList);
            ItemAsset old = clothing.backpackAsset;
            clothing.askWearBackpack(0, 0, blank, !hasPlayedEffect);
            RemoveAutoItem(nativePlayer, ref ct, old);
            hasPlayedEffect = true;
        }

        if (clothing.glassesAsset != null && state.ShouldClearItem(ClothingType.Glasses, clothing.glassesAsset, clothing.glassesState, clothing.glassesQuality))
        {
            ItemAsset old = clothing.glassesAsset;
            clothing.askWearGlasses(0, 0, blank, !hasPlayedEffect);
            RemoveAutoItem(nativePlayer, ref ct, old);
            hasPlayedEffect = true;
        }

        if (clothing.hatAsset != null && state.ShouldClearItem(ClothingType.Hat, clothing.hatAsset, clothing.hatState, clothing.hatQuality))
        {
            ItemAsset old = clothing.hatAsset;
            clothing.askWearHat(0, 0, blank, !hasPlayedEffect);
            RemoveAutoItem(nativePlayer, ref ct, old);
            hasPlayedEffect = true;
        }

        if (clothing.pantsAsset != null && state.ShouldClearItem(ClothingType.Pants, clothing.pantsAsset, clothing.pantsState, clothing.pantsQuality))
        {
            StoreSkippedItemsFromPage(nativePlayer, Page.Pants, WorkingItemList);
            ItemAsset old = clothing.pantsAsset;
            clothing.askWearPants(0, 0, blank, !hasPlayedEffect);
            RemoveAutoItem(nativePlayer, ref ct, old);
            hasPlayedEffect = true;
        }

        if (clothing.maskAsset != null && state.ShouldClearItem(ClothingType.Mask, clothing.maskAsset, clothing.maskState, clothing.maskQuality))
        {
            ItemAsset old = clothing.maskAsset;
            clothing.askWearMask(0, 0, blank, !hasPlayedEffect);
            RemoveAutoItem(nativePlayer, ref ct, old);
            hasPlayedEffect = true;
        }

        if (clothing.shirtAsset != null && state.ShouldClearItem(ClothingType.Shirt, clothing.shirtAsset, clothing.shirtState, clothing.shirtQuality))
        {
            StoreSkippedItemsFromPage(nativePlayer, Page.Shirt, WorkingItemList);
            ItemAsset old = clothing.shirtAsset;
            clothing.askWearShirt(0, 0, blank, !hasPlayedEffect);
            RemoveAutoItem(nativePlayer, ref ct, old);
            hasPlayedEffect = true;
        }

        if (clothing.vestAsset != null && state.ShouldClearItem(ClothingType.Vest, clothing.vestAsset, clothing.vestState, clothing.vestQuality))
        {
            StoreSkippedItemsFromPage(nativePlayer, Page.Vest, WorkingItemList);
            ItemAsset old = clothing.vestAsset;
            clothing.askWearVest(0, 0, blank, !hasPlayedEffect);
            RemoveAutoItem(nativePlayer, ref ct, old);
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

        static void StoreSkippedItemsFromPage(Player nativePlayer, Page page, List<Item> list)
        {
            Items pg = nativePlayer.inventory.items[(int)page];
            int itemCt = pg.getItemCount();
            for (int i = itemCt - 1; i >= 0; --i)
            {
                list.Add(pg.getItem((byte)i).item);
                pg.removeItem((byte)i);
            }
        }

        static void RemoveAutoItem(Player nativePlayer, ref int ct, ItemAsset expectedAsset)
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
                if (enumerator.Current is not IClothingItem clothingItem)
                    continue;

                KitItemResolutionResult result = _itemResolver.ResolveKitItem(clothingItem, stateKit, stateTeam);
                switch (clothingItem.ClothingType)
                {
                    // send clothes (why nelson)
                    case ClothingType.Shirt:                                                                       // rechecking because ref could change it
                        if (result.Asset is not ItemShirtAsset || !state.ShouldGrantItem(clothingItem, ref result) || result.Asset is not ItemShirtAsset shirtAsset)
                            continue;

                        clothing.askWearShirt(shirtAsset, result.Quality, result.State ?? result.Asset.getState(true), !hasPlayedEffect);
                        break;

                    case ClothingType.Pants:
                        if (result.Asset is not ItemPantsAsset || !state.ShouldGrantItem(clothingItem, ref result) || result.Asset is not ItemPantsAsset pantsAsset)
                            continue;

                        clothing.askWearPants(pantsAsset, result.Quality, result.State ?? result.Asset.getState(true), !hasPlayedEffect);
                        break;

                    case ClothingType.Vest:
                        if (result.Asset is not ItemVestAsset || !state.ShouldGrantItem(clothingItem, ref result) || result.Asset is not ItemVestAsset vestAsset)
                            continue;

                        clothing.askWearVest(vestAsset, result.Quality, result.State ?? result.Asset.getState(true), !hasPlayedEffect);
                        break;

                    case ClothingType.Hat:
                        if (result.Asset is not ItemHatAsset || !state.ShouldGrantItem(clothingItem, ref result) || result.Asset is not ItemHatAsset hatAsset)
                            continue;

                        clothing.askWearHat(hatAsset, result.Quality, result.State ?? result.Asset.getState(true), !hasPlayedEffect);
                        break;

                    case ClothingType.Mask:
                        if (result.Asset is not ItemMaskAsset || !state.ShouldGrantItem(clothingItem, ref result) || result.Asset is not ItemMaskAsset maskAsset)
                            continue;

                        clothing.askWearMask(maskAsset, result.Quality, result.State ?? result.Asset.getState(true), !hasPlayedEffect);
                        break;

                    case ClothingType.Backpack:
                        if (result.Asset is not ItemBackpackAsset || !state.ShouldGrantItem(clothingItem, ref result) || result.Asset is not ItemBackpackAsset backpackAsset)
                            continue;

                        clothing.askWearBackpack(backpackAsset, result.Quality, result.State ?? result.Asset.getState(true), !hasPlayedEffect);
                        break;

                    case ClothingType.Glasses:
                        if (result.Asset is not ItemGlassesAsset || !state.ShouldGrantItem(clothingItem, ref result) || result.Asset is not ItemGlassesAsset glassesAsset)
                            continue;

                        clothing.askWearGlasses(glassesAsset, result.Quality, result.State ?? result.Asset.getState(true), !hasPlayedEffect);
                        break;

                    default:
                        continue;
                }

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
}