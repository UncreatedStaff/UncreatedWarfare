using System;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Layouts.Flags;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Events.Models.Zones;
/// <summary>
/// Event listener args which fires after a <see cref="WarfarePlayer"/> enters a <see cref="Zone"/>.
/// </summary>
/// <remarks>NOTE: this event is not invoked when a player enters a <see cref="FlagObjective"/>, as it has its own events for the same purpose.</remarks>
public class PlayerEnteredZone : PlayerEvent, IActionLoggableEvent
{
    /// <summary>
    /// The zone that the player entered.
    /// </summary>
    public required Zone Zone { get; init; }

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        return new ActionLogEntry(ActionLogTypes.PlayerEnteredZone,
            Zone.Name,
            Player.Steam64.m_SteamID
        );
    }
}