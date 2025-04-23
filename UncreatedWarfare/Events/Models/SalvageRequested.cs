using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Structures;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Events.Models;

/// <summary>
/// Base class for <see cref="SalvageBarricadeRequested"/> and <see cref="SalvageStructureRequested"/>.
/// </summary>
public abstract class SalvageRequested : CancellablePlayerEvent
{
    protected IBuildable? BuildableCache;

    /// <summary>
    /// Abstracted <see cref="IBuildable"/> of the buildable.
    /// </summary>
    public abstract IBuildable Buildable { get; }

    /// <summary>
    /// Coordinate of the barricade region in <see cref="BarricadeManager.regions"/>.
    /// </summary>
    public required RegionCoord RegionPosition { get; init; }

    /// <summary>
    /// Instance Id of the buildable.
    /// </summary>
    public required uint InstanceId { get; init; }

    /// <summary>
    /// The Unity model of the buildable.
    /// </summary>
    public abstract Transform Transform { get; }

    /// <summary>
    /// The team that was responsible for the buildable being destroyed.
    /// </summary>
    public required Team InstigatorTeam { get; init; }
}