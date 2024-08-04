using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Event listener args which handles <see cref="Provider.onServerConnected"/>.
/// </summary>
public class PlayerLeft : PlayerEvent
{
    /*
     *  All this data is made available for events that need to access it after the player object has already been destroyed.
     */

    /// <summary>
    /// The position of the player when they left.
    /// </summary>
    public required Vector3 Position { get; init; }

    /// <summary>
    /// The rotation of the player when they left.
    /// </summary>
    public required Quaternion Rotation { get; init; }

    /// <summary>
    /// The position of the player's aim when they left.
    /// </summary>
    /// <remarks>Used for raycasts.</remarks>
    public required Vector3 LookPosition { get; init; }

    /// <summary>
    /// The forward vector of the player's aim when they left.
    /// </summary>
    /// <remarks>Used for raycasts.</remarks>
    public required Vector3 LookForward { get; init; }

    /// <summary>
    /// The team the player was on when they left.
    /// </summary>
    public required Team? Team { get; init; }

    /// <summary>
    /// Token that gets cancelled once the player is about to be disconnected.
    /// </summary>
    public required CancellationToken DisconnectToken { get; init; }
}