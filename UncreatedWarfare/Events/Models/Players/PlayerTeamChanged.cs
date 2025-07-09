using System;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Events.Models.Flags;
using Uncreated.Warfare.Events.Models.Fobs;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Events.Models.Players;

[EventModel(EventSynchronizationContext.Pure)]
public class PlayerTeamChanged : PlayerEvent, IActionLoggableEvent, IPlayerNeedsFobUIUpdateEvent, IFlagsNeedUIUpdateEvent
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

    LanguageSetEnumerator IFlagsNeedUIUpdateEvent.EnumerateApplicableSets(ITranslationService translationService, ref bool ticketsOnly)
    {
        return new LanguageSetEnumerator(Player);
    }
}