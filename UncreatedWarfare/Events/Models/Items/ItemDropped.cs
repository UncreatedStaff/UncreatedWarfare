using Uncreated.Warfare.Kits.Items;

namespace Uncreated.Warfare.Events.Models.Items;

/// <summary>
/// Invoked by <see cref="PlayerInventory.ReceiveDropItem"/>.
/// </summary>
public class ItemDropped : PlayerEvent
{
    /// <summary>
    /// Extra information about the dropped item.
    /// </summary>
    public required ItemData? DroppedItem { get; init; }
    
    /// <summary>
    /// Extra information about the item.
    /// </summary>
    public required Item? Item { get; init; }

    /// <summary>
    /// The position of the item where it was originally dropped.
    /// </summary>
    /// <remarks>Items are not simulated on the server so this may change on clients.</remarks>
    public Vector3 ServersidePoint => DroppedItem?.point ?? Vector3.zero;

    /// <summary>
    /// The unique instance ID of this item.
    /// </summary>
    public uint InstanceId => DroppedItem?.instanceID ?? uint.MaxValue;

    /// <summary>
    /// The coordinates of the region the item was dropped in.
    /// </summary>
    public required RegionCoord RegionPosition { get; init; }

    /// <summary>
    /// The region the item was dropped in.
    /// </summary>
    public required ItemRegion? Region { get; init; }

    /// <summary>
    /// The index of the item within it's region.
    /// </summary>
    public required ushort Index { get; init; }

    /// <summary>
    /// The page of the old item that was dropped before it was removed.
    /// </summary>
    public required Page OldPage { get; init; }

    /// <summary>
    /// The X-position of the old item that was dropped before it was removed.
    /// </summary>
    public required byte OldX { get; init; }

    /// <summary>
    /// The Y-position of the old item that was dropped before it was removed.
    /// </summary>
    public required byte OldY { get; init; }

    /// <summary>
    /// The rotation of the old item that was dropped before it was removed.
    /// </summary>
    public required byte OldRotation { get; init;}
}