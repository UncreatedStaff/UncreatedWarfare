using System;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Events.Models.Buildables;
using Uncreated.Warfare.Fobs;

namespace Uncreated.Warfare.Events.Models.Fobs;

/// <summary>
/// Event listener args which fires after a FOB is deregistered.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class FobDeregistered : IActionLoggableEvent, IFobNeedsUIUpdateEvent
{
    /// <summary>
    /// The FOB that was deregistered.
    /// </summary>
    public required IFob Fob { get; init; }
    
    /// <summary>
    /// If this FOB is a <see cref="IBuildableFob"/> and was deregistered due to its buildable being destroyed/remove, the event containing this information. 
    /// </summary>
    public IBaseBuildableDestroyedEvent? BuildableDestroyedEvent { get; init; }

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        return new ActionLogEntry(ActionLogTypes.FobRemoved,
            $"FOB \"{Fob.Name}\" for team {Fob.Team} ({Fob.GetType().Name})",
            0
        );
    }

    IFob IFobNeedsUIUpdateEvent.Fob => Fob;
}
