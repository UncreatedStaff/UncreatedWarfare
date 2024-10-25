using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Layouts.Phases.Flags;

/// <summary>
/// Represents a base interface for all objective data interfaces.
/// </summary>
public interface IObjectiveData
{
    /// <summary>
    /// The zone this data belongs to.
    /// </summary>
    ActiveZoneCluster Zone { get; }
}
