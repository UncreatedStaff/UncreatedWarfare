using System;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Layouts.Flags;

namespace Uncreated.Warfare.Events.Models.Flags;

/// <summary>
/// Event listener args which fires after a flag's <see cref="FlagObjective.Contest"/>'s points change are altered.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class FlagContestPointsChanged : IActionLoggableEvent
{
    /// <summary>
    /// The flag that is being contested.
    /// </summary>
    public required FlagObjective Flag { get; init; }
    /// <summary>
    /// The change in contest points.
    /// </summary>
    public required int PointsChange { get; init; }

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        return new ActionLogEntry(ActionLogTypes.FlagStateChanged,
            $"Flag {Flag.Index}: \"{Flag.Name}\" points updated by {PointsChange} point(s).",
            0
        );
    }
}
