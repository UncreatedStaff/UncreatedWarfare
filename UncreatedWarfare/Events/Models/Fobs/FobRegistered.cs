using System;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Fobs;

namespace Uncreated.Warfare.Events.Models.Fobs;

/// <summary>
/// Event listener args which fires after an <see cref="IFob"/> is registered. Includes other types of fobs (including Rally Points)
/// </summary>
public class FobRegistered : IActionLoggableEvent
{
    /// <summary>
    /// The FOB that was registered.
    /// </summary>
    public required IFob Fob { get; init; }

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        return new ActionLogEntry(ActionLogTypes.FobCreated,
            $"FOB \"{Fob.Name}\" for team {Fob.Team} ({Fob.GetType().Name})",
            0
        );
    }
}
