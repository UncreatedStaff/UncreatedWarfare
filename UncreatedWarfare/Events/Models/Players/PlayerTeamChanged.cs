using System;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Events.Models.Players;

public class PlayerTeamChanged : PlayerEvent, IActionLoggableEvent
{
    public required CSteamID GroupId { get; init; }
    public required Team Team { get; init; }
    public required Team OldTeam { get; init; }
    public required bool WasByAdminCommand { get; init; }
    public bool DidLeave => Team.IsValid;

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        return new ActionLogEntry(ActionLogTypes.ChangeTeam,
            $"{OldTeam} -> {Team}, Group id: {GroupId}, Via admin command: {(WasByAdminCommand ? "T" : "F")}",
            Player.Steam64.m_SteamID
        );
    }
}