using SDG.Unturned;
using Uncreated.Warfare.Buildables;
using UnityEngine;

namespace Uncreated.Warfare.Events.Barricades;

/// <summary>
/// Event listener args which handles <see cref="BarricadeDrop.OnSalvageRequested_Global"/>.
/// </summary>
public sealed class SalvageBarricadeRequested(BarricadeRegion region) : SalvageRequested(region)
{
    /// <inheritdoc />
    public override bool IsCancelled => base.IsCancelled || ServersideData.barricade.isDead;

    /// <summary>
    /// The index of the vehicle region in <see cref="BarricadeManager.vehicleRegions"/>. <see cref="ushort.MaxValue"/> if the barricade is not planted.
    /// </summary>
    /// <remarks>Also known as 'plant'.</remarks>
    public required ushort VehicleRegionIndex { get; init; }

    /// <summary>
    /// The barricade's object and model data.
    /// </summary>
    public required BarricadeDrop Barricade { get; init; }

    /// <summary>
    /// The barricade's server-side data.
    /// </summary>
    public required BarricadeData ServersideData { get; init; }

    /// <summary>
    /// The region the barricade was placed in. This could be of type <see cref="VehicleBarricadeRegion"/>.
    /// </summary>
    public BarricadeRegion Region => (BarricadeRegion)RegionObj;

    /// <summary>
    /// Abstracted <see cref="IBuildable"/> of the barricade.
    /// </summary>
    public override IBuildable Buildable => BuildableCache ??= new BuildableBarricade(Barricade);

    /// <summary>
    /// The Unity model of the barricade.
    /// </summary>
    public override Transform Transform => Barricade.model;
}