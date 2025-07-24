using System;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Layouts.Flags;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Events.Models.Flags;

/// <summary>
/// Fires when a flag is discovered for any team.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class FlagDiscovered : IActionLoggableEvent, IFlagsNeedUIUpdateEvent
{
    /// <summary>
    /// The flag that was discovered.
    /// </summary>
    public required FlagObjective Flag { get; init; }

    /// <summary>
    /// The team that discovered the flag.
    /// </summary>
    public required Team Team { get; init; }

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        return new ActionLogEntry(ActionLogTypes.FlagDiscovered,
            $"Flag {Flag.Index}: \"{Flag.Name}\" discovered for team {Team}.",
            0
        );
    }

    LanguageSetEnumerator IFlagsNeedUIUpdateEvent.EnumerateApplicableSets(ITranslationService translationService, ref bool ticketsOnly)
    {
        return translationService.SetOf.PlayersOnTeam(Team);
    }
}
