using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare.Events.Models.Kits;

/// <summary>
/// Invoked when a player's kit access is updated. Either <see cref="KitLevelAccessUpdated"/> or <see cref="KitAccessUpdated"/>.
/// </summary>
public interface IKitAccessUpdated
{
    /// <summary>
    /// The player who's access was updated.
    /// </summary>
    CSteamID PlayerId { get; }

    /// <summary>
    /// Whether or not the player was given access to the kit or it was revoked.
    /// </summary>
    bool HasAccess { get; }

    /// <summary>
    /// The player who instigated the update, if known.
    /// </summary>
    CSteamID Instigator { get; }

    /// <summary>
    /// The type/reason for the player's access.
    /// </summary>
    KitAccessType AccessType { get; }
}
