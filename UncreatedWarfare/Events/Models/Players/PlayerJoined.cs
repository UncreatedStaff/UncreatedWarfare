using System;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Players.Saves;

namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Event listener args which handles <see cref="Provider.onServerConnected"/>.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class PlayerJoined : PlayerEvent, IActionLoggableEvent
{
    /// <summary>
    /// If this is the first time the player has joined the server.
    /// </summary>
    public required bool IsNewPlayer { get; init; }
    
    /// <summary>
    /// Save data of the player, or a fresh save data object if they're new.
    /// </summary>
    public required BinaryPlayerSave SaveData { get; init; }

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        return new ActionLogEntry(ActionLogTypes.Connect,
            (IsNewPlayer ? "New player - " : "Returning player - ") + Player,
            Player.Steam64.m_SteamID
        );
    }
}
