using Uncreated.Warfare.Kits.Items;

namespace Uncreated.Warfare.Events.Models.Items;

/// <summary>
/// Invoked by <see cref="PlayerInventory.onDropItemRequested"/>.
/// </summary>
[EventModel(SynchronizationContext = EventSynchronizationContext.PerPlayer, SynchronizedModelTags = [ "modify_inventory", "modify_item_regions", "modify_useable" ])]
public class DropItemRequested : CancellablePlayerEvent
{
    /// <summary>
    /// Extra information about the item.
    /// </summary>
    public required Item Item { get; init; }

    /// <summary>
    /// The asset of the item being dropped.
    /// </summary>
    public required ItemAsset? Asset { get; init; }

    /// <summary>
    /// The position of the item where it will be dropped.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public required Vector3 Position { get; set; }

    /// <summary>
    /// The page of the item that will be dropped.
    /// </summary>
    public required Page Page { get; init; }

    /// <summary>
    /// The X-position of the item that will be dropped.
    /// </summary>
    public required byte X { get; init; }

    /// <summary>
    /// The Y-position of the item that will be dropped.
    /// </summary>
    public required byte Y { get; init; }

    /// <summary>
    /// The index of the item within it's page that will be dropped.
    /// </summary>
    public required byte Index { get; init; }

    /// <summary>
    /// The rotation of the item that will be dropped.
    /// </summary>
    public required byte Rotation { get; init; }
}