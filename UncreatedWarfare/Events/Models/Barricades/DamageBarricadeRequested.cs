using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.Events.Models.Barricades;

/// <summary>
/// Event listener args which handles <see cref="BarricadeManager.onDamageBarricadeRequested"/>.
/// </summary>
[EventModel(SynchronizationContext = EventSynchronizationContext.Global, SynchronizedModelTags = [ "modify_inventory", "modify_world" ])]
public sealed class DamageBarricadeRequested(BarricadeRegion region) : DamageRequested(region)
{
    /// <inheritdoc />
    public override bool IsCancelled => base.IsCancelled || ServersideData.barricade.isDead;

    /// <summary>
    /// The barricade's object and model data.
    /// </summary>
    public required BarricadeDrop Barricade { get; init; }

    /// <summary>
    /// The barricade's server-side data.
    /// </summary>
    public required BarricadeData ServersideData { get; init; }

    /// <summary>
    /// The origin of the damage to the barricade.
    /// </summary>
    public required EDamageOrigin DamageOrigin { get; init; }

    /// <summary>
    /// The item or vehicle that cause the damage to the barricade.
    /// </summary>
    public required IAssetLink<Asset>? PrimaryAsset { get; init; }

    /// <summary>
    /// The item or vehicle that cause the damage to the barricade. This may be an alternate item like the grenade thrown at a landmine, etc.
    /// </summary>
    public required IAssetLink<Asset>? SecondaryAsset { get; init; }

    /// <summary>
    /// The region the barricade was placed in.
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

    /// <summary>
    /// The index of the vehicle region in <see cref="BarricadeManager.vehicleRegions"/>. <see cref="ushort.MaxValue"/> if the barricade is not planted.
    /// </summary>
    /// <remarks>Also known as 'plant'.</remarks>
    public required ushort VehicleRegionIndex { get; init; }

    /// <summary>
    /// If this barricade is placed on a vehicle.
    /// </summary>
    public bool IsOnVehicle => VehicleRegionIndex != ushort.MaxValue;
}