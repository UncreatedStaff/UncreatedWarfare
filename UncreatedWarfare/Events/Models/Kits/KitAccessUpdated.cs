using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare.Events.Models.Kits;

/// <summary>
/// Invoked after a player's kit access is changed.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public sealed class KitAccessUpdated
{
    /// <summary>
    /// The kit that was updated.
    /// </summary>
    public required Kit Kit { get; init; }

    /// <summary>
    /// The player who's kit access was updated.
    /// </summary>
    public required CSteamID PlayerId { get; init; }

    /// <summary>
    /// Whether or not the player can access the kit.
    /// </summary>
    public required bool HasAccess { get; init; }

    /// <summary>
    /// The type/reason for the player's access.
    /// </summary>
    public required KitAccessType AccessType { get; init; }
}