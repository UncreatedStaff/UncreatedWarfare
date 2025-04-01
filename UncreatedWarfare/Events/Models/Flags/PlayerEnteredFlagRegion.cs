using System;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Layouts.Flags;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Flags;

/// <summary>
/// Event listener args which fires after <see cref="WarfarePlayer"/> enteres the region of a <see cref="FlagObjective"/>.
/// </summary>
public class PlayerEnteredFlagRegion : PlayerEvent, IActionLoggableEvent
{
    /// <summary>
    /// The flag that the player entered.
    /// </summary>
    public required FlagObjective Flag { get; init; }

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        return new ActionLogEntry(ActionLogTypes.PlayerEnteredObjective,
            $"Entered flag {Flag.Index} \"{Flag.Name}\"",
            Steam64.m_SteamID
        );
    }
}
