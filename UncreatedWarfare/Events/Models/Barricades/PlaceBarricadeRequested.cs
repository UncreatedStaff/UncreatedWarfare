using System;
using Uncreated.Warfare.Events.Models.Buildables;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Barricades;

/// <summary>
/// Event listener args which handles <see cref="BarricadeManager.onDeployBarricadeRequested"/>.
/// </summary>
[EventModel(SynchronizationContext = EventSynchronizationContext.Global, SynchronizedModelTags = [ "modify_inventory", "modify_world" ])]
public class PlaceBarricadeRequested : CancellableEvent, IPlaceBuildableRequestedEvent
{
#nullable disable
    private BarricadeRegion _region;
    private RegionCoord _regionPosition;
#nullable restore

    /// <inheritdoc />
    public override bool IsCancelled => base.IsCancelled || TargetVehicle is not null && TargetVehicle == null; // vehicle was destroyed mid-event

    /// <summary>
    /// The player that initially tried to place the barricade.
    /// </summary>
    public required WarfarePlayer OriginalPlacer { get; init; }

    /// <summary>
    /// Barricade instantiation data.
    /// </summary>
    public required Barricade Barricade { get; init; }

    /// <summary>
    /// Asset of the barricade being placed.
    /// </summary>
    public ItemBarricadeAsset Asset => Barricade.asset;

    /// <summary>
    /// Barricade place target (where the player was looking). This could be a vehicle in which case the barricade will be planted.
    /// </summary>
    /// <remarks>If this is a vehicle, it will be stored in <see cref="TargetVehicle"/>.</remarks>
    public required Transform? HitTarget { get; init; }

    /// <inheritdoc />
    ItemPlaceableAsset IPlaceBuildableRequestedEvent.Asset => Barricade.asset;

    /// <inheritdoc />
    bool IPlaceBuildableRequestedEvent.IsStructure => false;

    /// <summary>
    /// The vehicle the player was placing the barricade on.
    /// </summary>
    public required InteractableVehicle? TargetVehicle { get; init; }

    /// <summary>
    /// The index of the vehicle region in <see cref="BarricadeManager.vehicleRegions"/>. <see cref="ushort.MaxValue"/> if the barricade is not planted.
    /// </summary>
    /// <remarks>Also known as 'plant'.</remarks>
    public required ushort VehicleRegionIndex { get; init; }

    /// <summary>
    /// Coordinate of the barricade region in <see cref="BarricadeManager.regions"/>.
    /// </summary>
    public required RegionCoord RegionPosition
    {
        get => _regionPosition;
        init => _regionPosition = value;
    }

    /// <summary>
    /// The region the barricade will be placed in.
    /// </summary>
    public required BarricadeRegion Region
    {
        get => _region;
        init => _region = value;
    }

    /// <summary>
    /// The exact position of the barricade.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    /// <exception cref="ArgumentException">Position is not in a region when not planted.</exception>
    public required Vector3 Position
    {
        get;
        set
        {
            if (IsOnVehicle)
            {
                field = value;
                return;
            }

            if (!Regions.tryGetCoordinate(value, out byte x, out byte y))
                throw new ArgumentException(
                    "This is not a valid position for a non-planted barricade. It must be in a region.", nameof(value));

            field = value;
            _region = BarricadeManager.regions[x, y];
            _regionPosition = new RegionCoord(x, y);
        }
    }

    /// <summary>
    /// The exact rotation of the barricade.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public required Quaternion Rotation { get; set; }

    /// <summary>
    /// The player that owns the barricade's Steam ID, or <see cref="CSteamID.Nil"/>.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public required CSteamID Owner { get; set; }

    /// <summary>
    /// The group that owns the barricade's Steam ID, or <see cref="CSteamID.Nil"/>.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public required CSteamID GroupOwner { get; set; }

    /// <summary>
    /// If this barricade is being placed on a vehicle.
    /// </summary>
    public bool IsOnVehicle => TargetVehicle is not null;

    /// <inheritdoc />
    object IPlaceBuildableRequestedEvent.Item => Barricade;
}