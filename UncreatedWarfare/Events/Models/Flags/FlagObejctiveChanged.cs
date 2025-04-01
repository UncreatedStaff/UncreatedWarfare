using System;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Layouts.Flags;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Events.Models.Flags;

/// <summary>
/// Event listener args which fires after a Team's (or global if <see cref="Team"/> is <see langword="null"/>)
/// objective changes <see cref="FlagObjective"/>.
/// </summary>
public class FlagObjectiveChanged : IActionLoggableEvent
{
    /// <summary>
    /// The team who's objective changed, or <see langword="null"/> for global objectives.
    /// </summary>
    public required Team? Team { get; init; }

    /// <summary>
    /// The previous objective for <see cref="Team"/>.
    /// </summary>
    /// <remarks><see langword="null"/> if the team had no objective.</remarks>
    public required FlagObjective? OldObjective { get; init; }

    /// <summary>
    /// The new current objective for <see cref="Team"/>.
    /// </summary>
    /// <remarks><see langword="null"/> if the team now has no objective.</remarks>
    public required FlagObjective? NewObjective { get; init; }

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        string msg;
        if (OldObjective == null)
        {
            if (NewObjective == null)
                msg = string.Empty;
            else
                msg = $"Flag objective changed to Flag {NewObjective.Index}: \"{NewObjective.Name}\"";
        }
        else if (NewObjective == null)
            msg = $"Flag objective changed from Flag {OldObjective.Index}: \"{OldObjective.Name}\"";
        else
            msg = $"Flag objective changed from Flag {OldObjective.Index}: \"{OldObjective.Name}\" to Flag {NewObjective.Index}: \"{NewObjective.Name}\"";

        return new ActionLogEntry(ActionLogTypes.ObjectiveChanged, msg, 0);
    }
}