using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Structures;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Models.Buildables;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Buildables;

/// <summary>
/// Base class for <see cref="DamageBarricadeRequested"/> and <see cref="DamageStructureRequested"/>.
/// </summary>
public abstract class DamageRequested : CancellableEvent
{
    protected IBuildable? BuildableCache;
    protected BuildableSave? SaveCache;

    /// <summary>
    /// The player that tried to damage the barricade, if any.
    /// </summary>
    public required WarfarePlayer? Instigator { get; init; }

    /// <summary>
    /// The steam ID player that tried to damage the barricade, if any.
    /// </summary>
    public required CSteamID InstigatorId { get; init; }

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
    /// The index of the buildable in it's region.
    /// </summary>
    public required ushort RegionIndex { get; init; }

    /// <summary>
    /// Total damage to do to the buildable.
    /// </summary>
    /// <remarks>Can be changed.</remarks>
    public required ushort PendingDamage { get; set; }

    /// <summary>
    /// The team that was responsible for the buildable being destroyed.
    /// </summary>
    public required Team InstigatorTeam { get; init; }
}