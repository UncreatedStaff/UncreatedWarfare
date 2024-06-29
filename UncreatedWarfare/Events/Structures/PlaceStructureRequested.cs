﻿using SDG.Unturned;
using Steamworks;
using System;
using UnityEngine;

namespace Uncreated.Warfare.Events.Structures;

/// <summary>
/// Event listener args which handles <see cref="StructureManager.onDamageStructureRequested"/>.
/// </summary>
public class PlaceStructureRequested : CancellableEvent
{
    private Vector3 _position;
    private StructureRegion _region;
    private RegionCoord _regionPosition;

    /// <summary>
    /// The player that initially tried to place the structure.
    /// </summary>
    public required UCPlayer? OriginalPlacer { get; init; }

    /// <summary>
    /// Structure instantiation data.
    /// </summary>
    public required Structure Structure { get; init; }

    /// <summary>
    /// Coordinate of the structure region in <see cref="StructureManager.regions"/>.
    /// </summary>
    public required RegionCoord RegionPosition
    {
        get => _regionPosition;
        init => _regionPosition = value;
    }

    /// <summary>
    /// The region the structure will be placed in.
    /// </summary>
    public required StructureRegion Region
    {
        get => _region;
        init => _region = value;
    }

    /// <summary>
    /// The exact position of the structure.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    /// <exception cref="ArgumentException">Position is not in a region.</exception>
    public required Vector3 Position
    {
        get => _position;
        set
        {
            if (!Regions.tryGetCoordinate(value, out byte x, out byte y))
                throw new ArgumentException("This is not a valid position for a structure. It must be in a region.", nameof(value));
            
            _position = value;
            _region = StructureManager.regions[x, y];
            _regionPosition = new RegionCoord(x, y);
        }
    }

    /// <summary>
    /// The exact rotation of the structure.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public required Quaternion Rotation { get; set; }

    /// <summary>
    /// The player that owns the structure's Steam ID, or <see cref="CSteamID.Nil"/>.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public required CSteamID Owner { get; set; }

    /// <summary>
    /// The group that owns the structure's Steam ID, or <see cref="CSteamID.Nil"/>.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public required CSteamID GroupOwner { get; set; }

    /// <summary>
    /// Asset of the structure being placed.
    /// </summary>
    public ItemStructureAsset Asset => Structure.asset;
}