using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Items;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Kits.Items;

[PlayerComponent]
public class HotkeyPlayerComponent : IPlayerComponent, IEventListener<ItemDropped>
{
#nullable disable
    
    private ILogger<HotkeyPlayerComponent> _logger;
    
    public WarfarePlayer Player { get; private set; }

#nullable restore

    // updated when the player's kit is changed, only contains bindings for current kit
    internal List<KitHotkey>? HotkeyBindings;

    void IPlayerComponent.Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
#if DEBUG
        _logger = serviceProvider.GetRequiredService<ILogger<HotkeyPlayerComponent>>();
#endif
    }

    void IEventListener<ItemDropped>.HandleEvent(ItemDropped e, IServiceProvider serviceProvider)
    {
        if (HotkeyBindings is not { Count: > 0 } || e.Item == null)
            return;

        // move hotkey to a different item of the same type
        ItemTrackingPlayerComponent trackingComponent = Player.Component<ItemTrackingPlayerComponent>();

        trackingComponent.GetOriginalItemPosition(e.OldPage, e.OldX, e.OldY, out Page page, out byte x, out byte y);

        HandleItemDropped(e.Item, x, y, page);
    }
    
    internal void HandleItemDropped(Item item, byte x, byte y, Page page)
    {
        if (HotkeyBindings == null)
            return;

        ItemAsset itemAsset = item.GetAsset();
        if (itemAsset == null)
            return;

        foreach (KitHotkey binding in HotkeyBindings)
        {
            if (binding.X != x || binding.Y != y || binding.Page != page)
                continue;

            int hotkeyIndex = KitItemUtility.GetHotkeyIndex(binding.Slot);
            if (hotkeyIndex == byte.MaxValue)
                continue;

            // find another item to bind to
            for (int p = PlayerInventory.SLOTS; p < PlayerInventory.STORAGE; ++p)
            {
                if (!KitItemUtility.CanBindHotkeyTo(itemAsset, (Page)p))
                    continue;

                SDG.Unturned.Items items = Player.UnturnedPlayer.inventory.items[p];
                foreach (ItemJar itemJar in new ItemPageIterator(items, true))
                {
                    if (itemJar.item.id != itemAsset.id)
                        continue;

                    Player.UnturnedPlayer.equipment.ServerBindItemHotkey((byte)hotkeyIndex, itemAsset, (byte)p, itemJar.x, itemJar.y);
                    _logger.LogConditional($"Updating dropped hotkey: {itemAsset.itemName} at {(byte)p}, ({itemJar.x}, {itemJar.y}).");
                    return;
                }
            }
        }
    }

    internal void HandleItemPickedUpAfterTransformed(ItemDestroyed e, byte origX, byte origY, Page origPage)
    {
        // resend hotkeys from picked up item
        if (!Player.Equals(e.PickUpPlayer) || !Provider.isInitialized || HotkeyBindings == null || origX >= byte.MaxValue)
            return;

        ItemAsset asset = e.Item.GetAsset();
        foreach (KitHotkey binding in HotkeyBindings)
        {
            if (binding.X != origX || binding.Y != origY || binding.Page != origPage)
                continue;

            byte index = KitItemUtility.GetHotkeyIndex(binding.Slot);
            if (index == byte.MaxValue || !KitItemUtility.CanBindHotkeyTo(asset, e.PickUpPage))
                continue;

            e.PickUpPlayer.UnturnedPlayer.equipment.ServerBindItemHotkey(index, asset, (byte)e.PickUpPage, e.PickUpX, e.PickUpY);
            _logger.LogConditional($"Updating old hotkey (picked up): {asset.itemName} at {e.PickUpPage}, ({e.PickUpX}, {e.PickUpY}).");
            break;
        }
    }

    internal void HandleItemMovedAfterTransformed(ItemMoved e, byte origX, byte origY, Page origPage, byte swapOrigX, byte swapOrigY, Page swapOrigPage)
    {
        // move hotkey to the moved item
        if (HotkeyBindings == null)
            return;

        ItemAsset itemAsset = e.Item.GetAsset();
        if (itemAsset == null)
            return;

        int ct = 0;
        foreach (KitHotkey binding in HotkeyBindings)
        {
            Page page;
            byte x, y;

            if (binding.X == origX && binding.Y == origY && binding.Page == origPage)
            {
                // primary item
                page = e.NewPage;
                x = e.NewX;
                y = e.NewY;
                ct |= 1;
            }
            else if (binding.X == swapOrigX && binding.Y == swapOrigY && binding.Page == swapOrigPage)
            {
                // swapped item
                page = e.OldPage;
                x = e.OldX;
                y = e.OldY;
                ct |= 2;
            }
            else continue;

            byte hotkeyIndex = KitItemUtility.GetHotkeyIndex(binding.Slot);
            if (hotkeyIndex == byte.MaxValue)
                continue;

            Player.UnturnedPlayer.equipment.ServerBindItemHotkey(hotkeyIndex, itemAsset, (byte)page, x, y);
            _logger.LogConditional($"Updating old hotkey: {itemAsset.itemName} at {page}, ({x}, {y}).");
            if (ct == 3)
                break;
        }
    }

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }
}