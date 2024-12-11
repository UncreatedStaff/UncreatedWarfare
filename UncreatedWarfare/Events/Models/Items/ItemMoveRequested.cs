using Uncreated.Warfare.Kits.Items;

namespace Uncreated.Warfare.Events.Models.Items;

[EventModel(SynchronizationContext = EventSynchronizationContext.PerPlayer, SynchronizedModelTags = [ "modify_inventory" ])]
public class ItemMoveRequested : CancellablePlayerEvent
{
    /// <summary>
    /// The original page the item is coming from.
    /// </summary>
    public required Page OldPage { get; init; }

    /// <summary>
    /// The original X position of the item being moved.
    /// </summary>
    public required byte OldX { get; init; }

    /// <summary>
    /// The original Y position of the item being moved.
    /// </summary>
    public required byte OldY { get; init; }

    /// <summary>
    /// The original rotation of the item being moved.
    /// </summary>
    public required byte OldRotation { get; init; }

    /// <summary>
    /// The page the item will be moved to.
    /// </summary>
    public required Page NewPage { get; set; }

    /// <summary>
    /// The X position the item will be moved to.
    /// </summary>
    public required byte NewX { get; set; }

    /// <summary>
    /// The Y position the item will be moved to.
    /// </summary>
    public required byte NewY { get; set; }

    /// <summary>
    /// The rotation the item will be at when it is moved.
    /// </summary>
    public required byte NewRotation { get; set; }

    /// <summary>
    /// If the item is being swapped with another item (instead of moving to an empty spot).
    /// </summary>
    /// <remarks>This could be changed by changing the destination position and may not be up to date.</remarks>
    public required bool IsSwap { get; init; }

    /// <summary>
    /// The item being moved.
    /// </summary>
    public required ItemJar Jar { get; init; }

    /// <summary>
    /// The item being swapped with this item, if applicable.
    /// </summary>
    public required ItemJar? SwappingJar { get; init; }
}