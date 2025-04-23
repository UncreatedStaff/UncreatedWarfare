using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Items;

[EventModel(EventSynchronizationContext.Pure)]
public class ItemDestroyed
{
    /// <summary>
    /// If the item despawned naturally.
    /// </summary>
    public required bool Despawned { get; init; }

    /// <summary>
    /// If the item was picked up by a player.
    /// </summary>
    public required bool PickedUp { get; init; }

    /// <summary>
    /// The player that picked up the item, if any.
    /// </summary>
    public required WarfarePlayer? PickUpPlayer { get; init; }

    /// <summary>
    /// The Steam64 ID of the player that picked up the item, if any.
    /// </summary>
    public required CSteamID PickUpPlayerId { get; init; }

    /// <summary>
    /// An object representing the item that was dropped.
    /// </summary>
    public required ItemData DroppedItem { get; init; }

    /// <summary>
    /// Extra information about the item.
    /// </summary>
    public required Item Item { get; init; }

    /// <summary>
    /// The player that originally dropped the item, if any.
    /// </summary>
    public required WarfarePlayer? DropPlayer { get; init; }

    /// <summary>
    /// The Steam64 ID of the player that originally dropped the item, if any.
    /// </summary>
    public required CSteamID DropPlayerId { get; init; }

    /// <summary>
    /// If this was a pick-up, the page the item went to.
    /// </summary>
    public required Page PickUpPage { get; init; }

    /// <summary>
    /// If this was a pick-up, the X-coord the item went to.
    /// </summary>
    public required byte PickUpX { get; init; }

    /// <summary>
    /// If this was a pick-up, the Y-coord the item went to.
    /// </summary>
    public required byte PickUpY { get; init; }

    /// <summary>
    /// If this was a pick-up, the rotation the item went to.
    /// </summary>
    public required byte PickUpRotation { get; init; }
}
