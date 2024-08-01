using System.Collections.Generic;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Layouts.Phases.Flags;
public interface IFlagRotationPhase : ILayoutPhase
{
    /// <summary>
    /// Ordered list of all zones in rotation, from team 1 to team 2.
    /// </summary>
    /// <remarks>This list should not include the main base zones.</remarks>
    IReadOnlyList<ActiveZoneCluster> ActiveZones { get; }

    /// <summary>
    /// The team that the zone list starts at.
    /// </summary>
    ActiveZoneCluster StartingTeam { get; }

    /// <summary>
    /// The team the zone list ends at.
    /// </summary>
    ActiveZoneCluster EndingTeam { get; }
}
