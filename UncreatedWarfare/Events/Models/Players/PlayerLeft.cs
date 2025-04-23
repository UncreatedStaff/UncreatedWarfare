using System;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Event listener args which handles <see cref="Provider.onServerConnected"/>.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class PlayerLeft : PlayerEvent, IActionLoggableEvent
{
    /*
     *  All this data is made available for events that need to access it after the player object has already been destroyed.
     */

    /// <summary>
    /// The position of the player when they left.
    /// </summary>
    public required Vector3 Position { get; init; }

    /// <summary>
    /// The amount of time the player was online.
    /// </summary>
    public required TimeSpan TimeOnline { get; init; }

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

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        return new ActionLogEntry(ActionLogTypes.Disconnect,
            $"{Player} @ {Position:F2}, {Rotation:F2}, Team: {Team}, Time online: {TimeOnline:c}",
            Player.Steam64.m_SteamID
        );
    }
}