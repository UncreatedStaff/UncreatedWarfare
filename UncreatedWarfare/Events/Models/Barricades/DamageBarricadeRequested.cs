using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models.Buildables;

namespace Uncreated.Warfare.Events.Models.Barricades;

/// <summary>
/// Event listener args which handles <see cref="BarricadeManager.onDamageBarricadeRequested"/>.
/// </summary>
[EventModel(SynchronizationContext = EventSynchronizationContext.Global, SynchronizedModelTags = [ "modify_inventory", "modify_world" ])]
public sealed class DamageBarricadeRequested : DamageRequested, IDamageBuildableRequestedEvent
{
    /// <inheritdoc />
    public override bool IsCancelled => base.IsCancelled || ServersideData.barricade.isDead || PendingDamage < 1;

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
    public required BarricadeRegion Region { get; init; }

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

    bool IBaseBuildableDestroyedEvent.WasSalvaged => false;
    EDamageOrigin IBaseBuildableDestroyedEvent.DamageOrigin => EDamageOrigin.Unknown;
    IAssetLink<ItemAsset>? IBaseBuildableDestroyedEvent.PrimaryAsset => null;
    IAssetLink<ItemAsset>? IBaseBuildableDestroyedEvent.SecondaryAsset => null;
}