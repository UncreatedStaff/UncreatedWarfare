using System;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Layouts.Flags;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Events.Models.Flags;

/// <summary>
/// Event listener args which fires after a <see cref="FlagObjective"/> is re-captured, meaning another team had started clearing the flag
/// but hadn't yet neutralized it, then the leading team brought it back up to maximum points.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class FlagRecaptured : IActionLoggableEvent, IFlagsNeedUIUpdateEvent
{
    /// <summary>
    /// The flag that was cleared.
    /// </summary>
    public required FlagObjective Flag { get; init; }

    /// <summary>
    /// The team that recaptured their flag.
    /// </summary>
    public required Team Owner { get; init; }

    /// <summary>
    /// The amount of points the team recaptured.
    /// </summary>
    public required int Amount { get; init; }

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        return new ActionLogEntry(ActionLogTypes.FlagCaptured,
            $"Flag {Flag.Index}: \"{Flag.Name}\" recaptured for team {Owner} (on flag: {string.Join(", ", Flag.Players)}).",
            0
        );
    }

    LanguageSetEnumerator IFlagsNeedUIUpdateEvent.EnumerateApplicableSets(ITranslationService translationService, ref bool ticketsOnly)
    {
        return translationService.SetOf.PlayersOnTeam();
    }
}
