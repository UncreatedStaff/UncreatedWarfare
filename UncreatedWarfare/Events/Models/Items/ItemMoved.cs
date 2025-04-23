using Uncreated.Warfare.Kits.Items;

namespace Uncreated.Warfare.Events.Models.Items;

[EventModel(EventSynchronizationContext.Pure)]
public class ItemMoved : PlayerEvent
{
    /// <summary>
    /// The original page of the item that was moved.
    /// </summary>
    public required Page OldPage { get; init; }

    /// <summary>
    /// The original X position of the item that was moved.
    /// </summary>
    public required byte OldX { get; init; }

    /// <summary>
    /// The original Y position of the item that was moved.
    /// </summary>
    public required byte OldY { get; init; }

    /// <summary>
    /// The original rotation of the item that was moved.
    /// </summary>
    public required byte OldRotation { get; init; }

    /// <summary>
    /// The page the item is at now.
    /// </summary>
    /// <remarks>Could be out of date, should be re-fetched from <see cref="Jar"/> when up-to-date info is needed.</remarks>
    public required Page NewPage { get; init; }

    /// <summary>
    /// The X position the item is at now.
    /// </summary>
    /// <remarks>Could be out of date, should be re-fetched from <see cref="Jar"/> when up-to-date info is needed.</remarks>
    public required byte NewX { get; init; }

    /// <summary>
    /// The Y position the item is at now.
    /// </summary>
    /// <remarks>Could be out of date, should be re-fetched from <see cref="Jar"/> when up-to-date info is needed.</remarks>
    public required byte NewY { get; init; }

    /// <summary>
    /// The rotation the item is at now.
    /// </summary>
    /// <remarks>Could be out of date, should be re-fetched from <see cref="Jar"/> when up-to-date info is needed.</remarks>
    public required byte NewRotation { get; init; }

    /// <summary>
    /// If the item was swapped with another item (instead of being moved to an empty spot).
    /// </summary>
    public required bool IsSwap { get; init; }

    /// <summary>
    /// If this event is invoked as the second dispatch in an item swap.
    /// </summary>
    public required bool IsSecondaryExecution { get; init; }

    /// <summary>
    /// The item being moved.
    /// </summary>
    public Item Item => Jar.item;

    /// <summary>
    /// The item being moved.
    /// </summary>
    public required ItemJar Jar { get; init; }

    /// <summary>
    /// The item that was swapped with this item, if applicable. This item now lives at the old position.
    /// </summary>
    public required ItemJar? SwappedJar { get; init; }
}
