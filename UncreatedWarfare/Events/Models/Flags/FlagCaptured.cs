using System;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Layouts.Flags;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Events.Models.Flags;

/// <summary>
/// Event listener args which fires after a <see cref="FlagObjective"/> is captured. "Captured" means that a flag's <see cref="FlagObjective.Owner"/> changed to
/// a winning <see cref="Team"/> after they won the flag contest of a neutral flag.
/// </summary>
public class FlagCaptured : IActionLoggableEvent
{
    /// <summary>
    /// The flag that was captured.
    /// </summary>
    public required FlagObjective Flag { get; init; }
    /// <summary>
    /// The team that captured the flag by means of leading the flag contest.
    /// </summary>
    public required Team Capturer { get; init; }

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        return new ActionLogEntry(ActionLogTypes.FlagCaptured,
            $"Flag {Flag.Index}: \"{Flag.Name}\" captured for team {Capturer} (on flag: {string.Join(", ", Flag.Players)}).",
            0
        );
    }
}
