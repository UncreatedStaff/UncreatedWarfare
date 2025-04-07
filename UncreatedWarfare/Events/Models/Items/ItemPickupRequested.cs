using System;
using Uncreated.Warfare.Kits.Items;

namespace Uncreated.Warfare.Events.Models.Items;

/// <summary>
/// Triggers when a player tries to pick up a dropped item.
/// </summary>
[EventModel(EventSynchronizationContext.PerPlayer, SynchronizedModelTags = [ "modify_inventory", "modify_item_regions" ])]
public class ItemPickupRequested : CancellablePlayerEvent
{
    private Page _oldPage;
    private byte _oldX, _oldY;

    /// <summary>
    /// Info about the item that's being picked up.
    /// </summary>
    public required Item Item { get; init; }

    /// <summary>
    /// Asset of the item that's being picked up.
    /// </summary>
    public required ItemAsset Asset { get; init; }

    /// <summary>
    /// The page in the inventory the item was placed into.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public required Page DestinationPage { get; set; }

    /// <summary>
    /// The x-position in the inventory page the item was placed into.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public required byte DestinationX { get; set; }

    /// <summary>
    /// The y-position in the inventory page the item was placed into.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public required byte DestinationY { get; set; }

    /// <summary>
    /// The rotation of the item in the inventory.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public required byte DestinationRotation
    {
        get;
        set
        {
            if (value > 3)
                throw new ArgumentOutOfRangeException(nameof(value), "Rotation must be [0, 3].");
            field = value;
        }
    }

    /// <summary>
    /// The old dropped item.
    /// </summary>
    public required ItemData DroppedItem { get; init; }

    /// <summary>
    /// The region of this item before it's picked up.
    /// </summary>
    public required RegionCoord DroppedItemCoord { get; init; }

    /// <summary>
    /// The index of this item in it's region before it's picked up.
    /// </summary>
    public required ushort DroppedItemIndex { get; init; }

    /// <summary>
    /// The index of this item before it's picked up.
    /// </summary>
    public required ItemRegion DroppedItemRegion { get; init; }

    /// <summary>
    /// If the item is placed wherever it'll fit.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public bool AutoFindFreeSpace
    {
        get => (byte)DestinationPage == byte.MaxValue;
        set
        {
            if (AutoFindFreeSpace == value)
                return;

            if (!value)
            {
                DestinationPage = _oldPage;
                DestinationX = _oldX;
                DestinationY = _oldY;
            }
            else
            {
                _oldPage = DestinationPage;
                _oldX = DestinationX;
                _oldY = DestinationY;
                DestinationPage = (Page)byte.MaxValue;
                DestinationX = byte.MaxValue;
                DestinationY = byte.MaxValue;
            }
        }
    }
}
