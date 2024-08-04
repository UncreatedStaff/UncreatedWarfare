using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Items;

/// <summary>
/// Invoked by <see cref="ItemManager.onServerSpawningItemDrop"/>.
/// </summary>
public class ItemSpawned
{
    /// <summary>
    /// Extra information about the item.
    /// </summary>
    public required ItemData Item { get; init; }

    /// <summary>
    /// The position of the item where it was originally dropped.
    /// </summary>
    /// <remarks>Items are not simulated on the server so this may change on clients.</remarks>
    public Vector3 ServersidePoint => Item.point;

    /// <summary>
    /// The unique instance ID of this item.
    /// </summary>
    public uint InstanceId => Item.instanceID;

    /// <summary>
    /// If this item is considered as dropped by a player when it's considered for despawning.
    /// </summary>
    public bool DespawnsAsDroppedItem => Item.isDropped;

    /// <summary>
    /// The time this item was dropped relative to <see cref="Time.realtimeSinceStartup"/>.
    /// </summary>
    public float DropRealtime => Item.lastDropped;

    /// <summary>
    /// The player that dropped the item, if any.
    /// </summary>
    public required CSteamID PlayerDroppedId { get; init; }

    /// <summary>
    /// The player that dropped the item, if any.
    /// </summary>
    public required WarfarePlayer? PlayerDropped { get; init; }

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
}
