using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare.Events.Models.Kits;

/// <summary>
/// Invoked after a player's kit level access is changed.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public sealed class KitLevelAccessUpdated : IKitAccessUpdated
{
    /// <summary>
    /// The kit that was updated.
    /// </summary>
    public required PublicKitLevel KitLevel { get; init; }

    /// <summary>
    /// The player who's kit access was updated.
    /// </summary>
    public required CSteamID PlayerId { get; init; }

    /// <summary>
    /// Whether or not the player can access the kit.
    /// </summary>
    public required bool HasAccess { get; init; }

    /// <summary>
    /// The player who instigated the update, if known.
    /// </summary>
    public required CSteamID Instigator { get; init; }

    KitAccessType IKitAccessUpdated.AccessType => HasAccess ? KitAccessType.Credits : KitAccessType.Unknown;
}