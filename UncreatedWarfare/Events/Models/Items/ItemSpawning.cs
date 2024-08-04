using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Items;

/// <summary>
/// Invoked by <see cref="ItemManager.onServerSpawningItemDrop"/>.
/// </summary>
public class ItemSpawning : CancellableEvent
{
    /// <summary>
    /// Extra information about the item.
    /// </summary>
    public required Item Item { get; init; }

    /// <summary>
    /// The unique instance ID of this item.
    /// </summary>
    public required uint InstanceId { get; init; }

    /// <summary>
    /// The player that dropped the item, if any.
    /// </summary>
    public required CSteamID PlayerDroppedId { get; init; }

    /// <summary>
    /// The player that dropped the item, if any.
    /// </summary>
    public required WarfarePlayer? PlayerDropped { get; init; }

    /// <summary>
    /// The position of the item where it will be dropped.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public required Vector3 Position { get; set; }

    /// <summary>
    /// If the drop sound is played on drop.
    /// </summary>
    public required bool PlayDropEffect { get; init; }

    /// <summary>
    /// If the items are spread out randomly.
    /// </summary>
    public required bool IsWideSpread { get; init; }

    /// <summary>
    /// If the item should be considered as dropped by a player when being despawned.
    /// </summary>
    public required bool IsDroppedByPlayer { get; init; }
}
