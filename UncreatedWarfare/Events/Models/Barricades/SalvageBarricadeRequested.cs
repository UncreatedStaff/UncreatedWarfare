using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models.Buildables;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Barricades;

/// <summary>
/// Event listener args which handles <see cref="BarricadeDrop.OnSalvageRequested_Global"/>.
/// </summary>
[EventModel(EventSynchronizationContext.Global, SynchronizedModelTags = [ "modify_inventory", "modify_world" ])]
public sealed class SalvageBarricadeRequested : SalvageRequested, ISalvageBuildableRequestedEvent
{
    /// <inheritdoc />
    public override bool IsCancelled => base.IsCancelled || ServersideData.barricade.isDead;

    /// <summary>
    /// The index of the vehicle region in <see cref="BarricadeManager.vehicleRegions"/>. <see cref="ushort.MaxValue"/> if the barricade is not planted.
    /// </summary>
    /// <remarks>Also known as 'plant'.</remarks>
    public required ushort VehicleRegionIndex { get; init; }

    /// <summary>
    /// If this barricade was placed on a vehicle.
    /// </summary>
    public bool IsOnVehicle => VehicleRegionIndex != ushort.MaxValue;

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
    public required BarricadeRegion Region { get; init; }

    /// <summary>
    /// Abstracted <see cref="IBuildable"/> of the barricade.
    /// </summary>
    public override IBuildable Buildable => BuildableCache ??= new BuildableBarricade(Barricade);

    /// <summary>
    /// The Unity model of the barricade.
    /// </summary>
    public override Transform Transform => Barricade.model;
    bool IBaseBuildableDestroyedEvent.WasSalvaged => true;
    EDamageOrigin IBaseBuildableDestroyedEvent.DamageOrigin => EDamageOrigin.Unknown;
    IAssetLink<ItemAsset>? IBaseBuildableDestroyedEvent.PrimaryAsset => null;
    IAssetLink<ItemAsset>? IBaseBuildableDestroyedEvent.SecondaryAsset => null;
    WarfarePlayer IBaseBuildableDestroyedEvent.Instigator => Player;
    CSteamID IBaseBuildableDestroyedEvent.InstigatorId => Player.Steam64;
}