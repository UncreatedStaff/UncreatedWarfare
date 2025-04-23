using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Kits.Items;

namespace Uncreated.Warfare.Events.Models.Items;

/// <summary>
/// Invoked by <see cref="PlayerInventory.ReceiveDropItem"/>.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class ItemDropped : PlayerEvent, IActionLoggableEvent
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
    /// The asset of the item being dropped.
    /// </summary>
    public required ItemAsset? Asset { get; init; }

    /// <summary>
    /// The position of the item where it was originally dropped.
    /// </summary>
    /// <remarks>Items are not simulated on the server so this may change on clients.</remarks>
    public Vector3 ServersidePoint => DroppedItem?.point ?? Vector3.zero;

    /// <summary>
    /// The position of the item where it will land. Usually this is the same as <see cref="ServersidePoint"/>.
    /// </summary>
    /// <remarks>Items are not simulated on the server so this may change on clients.</remarks>
    public required Vector3 LandingPoint { get; init; }

    /// <summary>
    /// The position of the item where it was dropped.
    /// </summary>
    /// <remarks>Items are not simulated on the server so this may change on clients.</remarks>
    public required Vector3 DropPoint { get; init; }

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
    public required byte OldRotation { get; init; }

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        return new ActionLogEntry(ActionLogTypes.DroppedItem,
            $"Item {AssetLink.ToDisplayString(Asset)} from {OldPage} @ {OldX}, {OldY}, r{OldRotation} to # {InstanceId} @ {ServersidePoint}",
            Player.Steam64.m_SteamID
        );
    }
}