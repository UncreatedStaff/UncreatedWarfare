using System;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Layouts.Flags;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Flags;

/// <summary>
/// Event listener args which fires after <see cref="WarfarePlayer"/> exits the region of a <see cref="FlagObjective"/>.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class PlayerExitedFlagRegion : PlayerEvent, IActionLoggableEvent
{
    /// <summary>
    /// The flag that the player exited.
    /// </summary>
    public required FlagObjective Flag { get; init; }

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        return new ActionLogEntry(ActionLogTypes.PlayerEnteredObjective,
            $"Exited flag {Flag.Index} \"{Flag.Name}\"",
            Steam64.m_SteamID
        );
    }
}
