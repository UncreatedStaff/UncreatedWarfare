using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Items;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.ItemTracking;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Kits.Items;

/// <summary>
/// Helps keep up with where items have been moved to track held item's back to their original kit item.
/// </summary>
[PlayerComponent]
public class ItemTrackingPlayerComponent : IPlayerComponent, IEventListener<ItemDropped>, IEventListener<ItemMoved>, IEventListener<ItemDestroyed>, IEventListener<ItemPickupRequested>
{
    internal List<ItemTransformation> ItemTransformations = new List<ItemTransformation>(16);
    internal List<ItemDropTransformation> ItemDropTransformations = new List<ItemDropTransformation>(16);

    /// <summary>
    /// Cached transformations of the current kit.
    /// </summary>
    internal IReadOnlyList<KitLayoutTransformation>? KitLayoutTransformations;

#nullable disable

    public WarfarePlayer Player { get; private set; }

#nullable restore

    void IPlayerComponent.Init(IServiceProvider serviceProvider, bool isOnJoin) { }

    /// <summary>
    /// Reset tracking items. Should be done when clearing inventory or making clothing changes.
    /// </summary>
    public void Reset()
    {
        ItemTransformations.Clear();
        ItemDropTransformations.Clear();
    }

    public bool TryGetCurrentItemPosition(Page origPage, byte origX, byte origY, out Page page, out byte x, out byte y, out bool isDropped, [MaybeNullWhen(false)] out Item item)
    {
        for (int i = 0; i < ItemDropTransformations.Count; ++i)
        {
            ItemDropTransformation t = ItemDropTransformations[i];
            if (t.OldPage != origPage || t.OldX != origX || t.OldY != origY)
                continue;

            page = (Page)byte.MaxValue;
            x = byte.MaxValue;
            y = byte.MaxValue;
            isDropped = true;
            item = t.Item;
            return true;
        }

        for (int i = ItemTransformations.Count - 1; i >= 0; --i)
        {
            ItemTransformation t = ItemTransformations[i];
            if (t.OldPage != origPage || t.OldX != origX || t.OldY != origY)
                continue;

            page = t.NewPage;
            x = t.NewX;
            y = t.NewY;
            isDropped = false;
            item = t.Item;
            byte index = Player.UnturnedPlayer.inventory.getIndex((byte)page, x, y);
            if (ReferenceEquals(item, Player.UnturnedPlayer.inventory.getItem((byte)page, index)?.item))
                return true;

            ItemTransformations.RemoveAt(i);
        }

        isDropped = false;

        byte oldIndex = Player.UnturnedPlayer.inventory.getIndex((byte)origPage, origX, origY);
        if (oldIndex != byte.MaxValue)
        {
            page = origPage;
            x = origX;
            y = origY;
            item = Player.UnturnedPlayer.inventory.getItem((byte)origPage, oldIndex)?.item;
            return item != null;
        }

        page = (Page)byte.MaxValue;
        x = byte.MaxValue;
        y = byte.MaxValue;
        item = null;
        return false;
    }

    public void GetOriginalItemPosition(Page page, byte x, byte y, out Page origPage, out byte origX, out byte origY)
    {
        origPage = page;
        origX = x;
        origY = y;

        ItemJar? jar = Player.UnturnedPlayer.inventory.GetItemAt(page, x, y);
        if (jar == null)
            return;

        Item item = jar.item;

        for (int i = 0; i < ItemTransformations.Count; ++i)
        {
            ItemTransformation t = ItemTransformations[i];
            if (t.Item != item)
                continue;

            origX = t.OldX;
            origY = t.OldY;
            origPage = t.OldPage;
            return;
        }
    }
    
    public bool TryGetOriginalItemPosition(Item item, out Page origPage, out byte origX, out byte origY)
    {
        for (int i = 0; i < ItemDropTransformations.Count; ++i)
        {
            ItemDropTransformation t = ItemDropTransformations[i];
            if (t.Item != item)
                continue;

            origX = t.OldX;
            origY = t.OldY;
            origPage = t.OldPage;
            return true;
        }

        for (int i = 0; i < ItemTransformations.Count; ++i)
        {
            ItemTransformation t = ItemTransformations[i];
            if (t.Item != item)
                continue;

            origX = t.OldX;
            origY = t.OldY;
            origPage = t.OldPage;
            return true;
        }

        origX = byte.MaxValue;
        origY = byte.MaxValue;
        origPage = (Page)byte.MaxValue;
        return false;
    }

    void IEventListener<ItemDropped>.HandleEvent(ItemDropped e, IServiceProvider serviceProvider)
    {
        if (e.Item == null)
            return;
        
        for (int i = 0; i < ItemTransformations.Count; ++i)
        {
            ItemTransformation t = ItemTransformations[i];
            if (t.Item != e.Item)
                continue;
            
            ItemDropTransformations.Add(new ItemDropTransformation(t.OldPage, t.OldX, t.OldY, e.Item, e.OldPage, e.OldX, e.OldY, e.OldRotation));
            ItemTransformations.RemoveAtFast(i);
            return;
        }

        ItemDropTransformations.Add(new ItemDropTransformation(e.OldPage, e.OldX, e.OldY, e.Item, e.OldPage, e.OldX, e.OldY, e.OldRotation));
    }

    [EventListener(Priority = int.MinValue)]
    void IEventListener<ItemPickupRequested>.HandleEvent(ItemPickupRequested e, IServiceProvider serviceProvider)
    {
        if (!e.AutoFindFreeSpace)
            return;

        // replace item from where it was dropped if possible
        for (int i = 0; i < ItemDropTransformations.Count; i++)
        {
            ItemDropTransformation drop = ItemDropTransformations[i];
            if (drop.Item != e.Item)
                continue;

            ItemAsset asset = e.Item.GetAsset();
            if (Player.UnturnedPlayer.inventory.items[(int)drop.DroppedFromPage]
                .checkSpaceEmpty(drop.DroppedFromX, drop.DroppedFromY, asset.size_x, asset.size_y,
                    drop.DroppedFromRotation))
            {
                e.DestinationX = drop.DroppedFromX;
                e.DestinationY = drop.DroppedFromY;
                e.DestinationPage = drop.DroppedFromPage;
                e.DestinationRotation = (byte)(drop.DroppedFromRotation % 4);
            }
        }
    }

    void IEventListener<ItemMoved>.HandleEvent(ItemMoved e, IServiceProvider serviceProvider)
    {
        if (e.IsSecondaryExecution || e.NewX == e.OldX && e.NewY == e.OldY && e.NewPage == e.OldPage)
            return;

        byte origX = byte.MaxValue, origY = byte.MaxValue;
        Page origPage = (Page)byte.MaxValue;
        byte swapOrigX = byte.MaxValue, swapOrigY = byte.MaxValue;
        Page swapOrigPage = (Page)byte.MaxValue;
        if (e.Jar != null)
        {
            bool found = false;
            for (int i = 0; i < ItemTransformations.Count; ++i)
            {
                ItemTransformation t = ItemTransformations[i];
                if (t.Item != e.Jar.item)
                    continue;

                ItemTransformations[i] = new ItemTransformation(t.OldPage, e.NewPage, t.OldX, t.OldY, e.NewX, e.NewY, t.Item);
                origX = t.OldX;
                origY = t.OldY;
                origPage = t.OldPage;
                found = true;
                break;
            }

            if (!found)
            {
                ItemTransformations.Add(new ItemTransformation(e.OldPage, e.NewPage, e.OldX, e.OldY, e.NewX, e.NewY, e.Jar.item));
                origX = e.OldX;
                origY = e.OldY;
                origPage = e.OldPage;
            }
        }

        if (e is { IsSwap: true, SwappedJar: not null })
        {
            bool found = false;
            for (int i = 0; i < ItemTransformations.Count; ++i)
            {
                ItemTransformation t = ItemTransformations[i];
                if (t.Item != e.SwappedJar.item)
                    continue;

                ItemTransformations[i] = new ItemTransformation(t.OldPage, e.OldPage, t.OldX, t.OldY, e.OldX, e.OldY, t.Item);
                swapOrigX = t.OldX;
                swapOrigY = t.OldY;
                swapOrigPage = t.OldPage;
                found = true;
                break;
            }

            if (!found)
            {
                ItemTransformations.Add(new ItemTransformation(e.NewPage, e.OldPage, e.NewX, e.NewY, e.OldX, e.OldY, e.SwappedJar.item));
                swapOrigX = e.NewX;
                swapOrigY = e.NewY;
                swapOrigPage = e.NewPage;
            }
        }

        if (origX != byte.MaxValue || swapOrigX != byte.MaxValue)
            Player.Component<HotkeyPlayerComponent>().HandleItemMovedAfterTransformed(e, origX, origY, origPage, swapOrigX, swapOrigY, swapOrigPage);
    }

    void IEventListener<ItemDestroyed>.HandleEvent(ItemDestroyed e, IServiceProvider serviceProvider)
    {
        if (e.Item == null)
            return;

        if (!e.PickedUp || e.PickUpPage == (Page)byte.MaxValue || (byte)e.PickUpPage >= PlayerInventory.STORAGE)
        {
            for (int i = 0; i < ItemDropTransformations.Count; ++i)
            {
                ItemDropTransformation d = ItemDropTransformations[i];
                if (d.Item != e.Item)
                    continue;

                ItemDropTransformations.RemoveAtFast(i);
                return;
            }
        }

        byte origX = byte.MaxValue, origY = byte.MaxValue;
        Page origPage = (Page)byte.MaxValue;
        for (int i = 0; i < ItemDropTransformations.Count; ++i)
        {
            ItemDropTransformation d = ItemDropTransformations[i];
            if (d.Item != e.Item)
                continue;

            bool found = false;

            for (int j = 0; j < ItemTransformations.Count; ++j)
            {
                ItemTransformation t = ItemTransformations[j];
                if (t.Item != e.Item)
                    continue;

                ItemTransformations[j] = new ItemTransformation(t.OldPage, e.PickUpPage, t.OldX, t.OldY, e.PickUpX, e.PickUpY, t.Item);
                origX = t.OldX;
                origY = t.OldY;
                origPage = t.OldPage;
                found = true;
                break;
            }

            if (!found)
            {
                origX = d.OldX;
                origY = d.OldY;
                origPage = d.OldPage;
                ItemTransformations.Add(new ItemTransformation(d.OldPage, e.PickUpPage, d.OldX, d.OldY, e.PickUpX, e.PickUpY, e.Item));
            }

            ItemDropTransformations.RemoveAtFast(i);
            break;
        }

        Player.Component<HotkeyPlayerComponent>().HandleItemPickedUpAfterTransformed(e, origX, origY, origPage);
    }

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }
}