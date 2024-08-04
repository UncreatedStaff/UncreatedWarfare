using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Items;
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
}
