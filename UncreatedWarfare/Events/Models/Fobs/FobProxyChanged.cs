using System;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Fobs;

namespace Uncreated.Warfare.Events.Models.Fobs;

/// <summary>
/// Event listener args which fires after <see cref="ResourceFob"/> becomes proxied by enemies or spawnable again.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class FobProxyChanged : IActionLoggableEvent
{
    /// <summary>
    /// The <see cref="ResourceFob"/> that was proxied or unproxied by enemies.
    /// </summary>
    public required ResourceFob Fob { get; init; }
    /// <summary>
    /// The new proxy state of the Fob.
    /// </summary>
    public required bool IsProxied { get; init; }

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        return new ActionLogEntry(ActionLogTypes.FobUpdated,
            $"FOB \"{Fob.Name}\" for team {Fob.Team} # {Fob.Buildable.InstanceId} @ {Fob.Buildable.Position:F2}, {Fob.Buildable.Rotation:F2}, " +
            $"Proxy updated: {IsProxied}",
            0
        );
    }
}
