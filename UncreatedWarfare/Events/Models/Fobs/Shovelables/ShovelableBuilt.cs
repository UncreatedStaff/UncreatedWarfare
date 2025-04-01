using System;
using System.Linq;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.FOBs.Construction;

namespace Uncreated.Warfare.Events.Models.Fobs.Shovelables;
/// <summary>
/// Event listener args which fires after any type of <see cref="ShovelableBuildable"/> is built up.
/// </summary>
public class ShovelableBuilt : IActionLoggableEvent
{
    /// <summary>
    /// The <see cref="ShovelableBuildable"/> that was built up.
    /// </summary>
    public required ShovelableBuildable Shovelable { get; init; }

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        float totalPoints = Shovelable.Builders.TotalWorkDone;
        return new ActionLogEntry(ActionLogTypes.ShovelableBuilt,
            $"{Shovelable.IdentifyingAsset.ToDisplayString()} placed by {Shovelable.Buildable.Owner} " +
            $"({Shovelable.Buildable.Group}) @ {Shovelable.Buildable.Position:F2}, {Shovelable.Buildable.Rotation:F2}, " +
            $"Contributors: {string.Join(", ", Shovelable.Builders.Select(x => $"{x.PlayerId.m_SteamID} - {x.WorkPoints / totalPoints:P2}"))}",
            Shovelable.Buildable.Owner
        );
    }
}
