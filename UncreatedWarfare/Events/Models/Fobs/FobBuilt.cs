using System;
using System.Linq;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.FOBs.Construction;

namespace Uncreated.Warfare.Events.Models.Fobs;

/// <summary>
/// Event listener args which fires after a <see cref="BunkerFob"/> is built up.
/// </summary>
public class FobBuilt : IActionLoggableEvent
{
    /// <summary>
    /// The <see cref="BunkerFob"/> that was built up.
    /// </summary>
    public required BunkerFob Fob { get; init; }

    /// <summary>
    /// The shovelable replaced by the FOB. This buildable is not alive.
    /// </summary>
    public required ShovelableBuildable Shovelable { get; init; }

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        float total = Shovelable.Builders.TotalWorkDone;
        return new ActionLogEntry(ActionLogTypes.FobBuilt,
            $"FOB \"{Fob.Name}\" for team {Fob.Team} # {Fob.Buildable.InstanceId} @ {Fob.Buildable.Position:F2}, {Fob.Buildable.Rotation:F2}, " +
            $"Shovelers: [ {string.Join(", ", Shovelable.Builders.Select(x => $"{x.PlayerId.m_SteamID} - {x.WorkPoints / total:P2}"))} ]",
            Fob.Creator
        );
    }
}
