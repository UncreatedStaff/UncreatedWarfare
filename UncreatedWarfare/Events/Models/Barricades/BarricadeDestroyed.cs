﻿using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Barricades;

/// <summary>
/// Event listener args which handles a patch on <see cref="BarricadeManager.destroyBarricade(BarricadeDrop, byte, byte, ushort)"/>.
/// </summary>
public class BarricadeDestroyed : IBuildableDestroyedEvent
{
    private IBuildable? _buildableCache;

    /// <summary>
    /// Player that destroyed the barricade, if any.
    /// </summary>
    public required WarfarePlayer? Instigator { get; init; }

    /// <summary>
    /// Steam ID of the player that destroyed the barricade, or <see cref="CSteamID.Nil"/>.
    /// </summary>
    public required CSteamID InstigatorId { get; init; }

    /// <summary>
    /// The barricade's object and model data.
    /// </summary>
    public required BarricadeDrop Barricade { get; init; }

    /// <summary>
    /// The barricade's server-side data.
    /// </summary>
    public required BarricadeData ServersideData { get; init; }

    /// <summary>
    /// Coordinate of the barricade region in <see cref="BarricadeManager.regions"/>.
    /// </summary>
    public required RegionCoord RegionPosition { get; init; }

    /// <summary>
    /// The region the barricade was placed in. This could be of type <see cref="VehicleBarricadeRegion"/>.
    /// </summary>
    public required BarricadeRegion Region { get; init; }

    /// <summary>
    /// The index of the vehicle region in <see cref="BarricadeManager.vehicleRegions"/>. <see cref="ushort.MaxValue"/> if the barricade is not planted.
    /// </summary>
    /// <remarks>Also known as 'plant'.</remarks>
    public required ushort VehicleRegionIndex { get; init; }

    /// <summary>
    /// Origin of the damage that caused the barricade to be destroyed.
    /// </summary>
    public required EDamageOrigin DamageOrigin { get; init; }

    /// <summary>
    /// Instance Id of the barricade that was destroyed.
    /// </summary>
    public required uint InstanceId { get; init; }

    /// <summary>
    /// Primary item used to destroy the barricade.
    /// </summary>
    public IAssetLink<ItemAsset>? PrimaryAsset { get; init; }

    /// <summary>
    /// Secondary item used to destroy the barricade.
    /// </summary>
    public IAssetLink<ItemAsset>? SecondaryAsset { get; init; }

    /// <summary>
    /// The Unity model of the barricade.
    /// </summary>
    public Transform Transform => Barricade.model;

    /// <summary>
    /// If this barricade is being placed on a vehicle.
    /// </summary>
    public bool IsOnVehicle => VehicleRegionIndex != ushort.MaxValue;

    /// <summary>
    /// Abstracted <see cref="IBuildable"/> of the barricade.
    /// </summary>
    public IBuildable Buildable => _buildableCache ??= new BuildableBarricade(Barricade);
    object IBuildableDestroyedEvent.Region => Region;
}