using System;
using System.Linq;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Events.Models.Buildables;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs;

namespace Uncreated.Warfare.Events.Models.Fobs;

/// <summary>
/// Event listener args which fires after the main structure of any <see cref="IBuildableFob"/> subclass is destroyed.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class FobDestroyed : IActionLoggableEvent, IFobNeedsUIUpdateEvent
{
    /// <summary>
    /// The <see cref="IBuildableFob"/> that was destroyed.
    /// </summary>
    public required IBuildableFob Fob { get; init; }

    /// <summary>
    /// The event that caused the FOB to be destroyed.
    /// </summary>
    public required IBaseBuildableDestroyedEvent Event { get; init; }

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        return new ActionLogEntry(ActionLogTypes.FobDestroyed,
            $"FOB \"{Fob.Name}\" for team {Fob.Team} # {Fob.Buildable.InstanceId} @ {Fob.Buildable.Position:F2}, {Fob.Buildable.Rotation:F2}, " +
            $"Damagers: [ {(Fob is BunkerFob f ? string.Join(", ", f.DamageTracker.Contributors.Select(x => $"{x.m_SteamID} - {f.DamageTracker.GetDamageContributionPercentage(x):P2}")) : "?")} ]",
            Event.InstigatorId
        );
    }

    IFob IFobNeedsUIUpdateEvent.Fob => Fob;
}
