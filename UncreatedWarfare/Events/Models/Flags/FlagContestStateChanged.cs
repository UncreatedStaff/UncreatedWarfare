using System;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Layouts.Flags;

namespace Uncreated.Warfare.Events.Models.Flags;

/// <summary>
/// Event listener args which fires after a flag's <see cref="FlagObjective.CurrentContestState"/> changes to a different <see cref="FlagContestState.ContestState"/>.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class FlagContestStateChanged : IActionLoggableEvent
{
    /// <summary>
    /// The flag that is being contested.
    /// </summary>
    public required FlagObjective Flag { get; init; }
    /// <summary>
    /// The flag's contest state before the change.
    /// </summary>
    public required FlagContestState OldState { get; init; }
    /// <summary>
    /// The flag's contest state after the change.
    /// </summary>
    public required FlagContestState NewState { get; init; }

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        return new ActionLogEntry(ActionLogTypes.FlagStateChanged,
            $"Flag {Flag.Index}: \"{Flag.Name}\" state updated from {OldState.ToString()} to {NewState.ToString()} point(s).",
            0
        );
    }
}
