using System;
using Uncreated.Warfare.Events.Logging;

namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Handles <see cref="PlayerEquipment.OnPunch_Global"/>.
/// </summary>
public class PlayerPunched : PlayerEvent, IActionLoggableEvent
{
    public required EPlayerPunch PunchType { get; init; }

    public required InputInfo InputInfo { get; init; }

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        return new ActionLogEntry(ActionLogTypes.Punch,
            $"{(PunchType == EPlayerPunch.LEFT ? "Left hand" : "Right hand")} - {ActionLoggerService.DescribeInput(InputInfo)}",
            Player.Steam64.m_SteamID
        );
    }
}