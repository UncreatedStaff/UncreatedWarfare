using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Handles a player being revived.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class PlayerRevived : PlayerEvent
{
    /// <summary>
    /// The player (medic) that revived the downed player, or <see langword="null"/> if the player was revived by other means.
    /// </summary>
    public required WarfarePlayer? Medic { get; init; }
}