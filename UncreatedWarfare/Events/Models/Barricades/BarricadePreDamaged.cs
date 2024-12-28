using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Events.Models.Vehicles;

/// <summary>
/// Event listener args which handles a patch listening just before a vehicle is damage (before the InteractableVehicle's health is changed).
/// </summary>
/// 
public class BarricadePreDamaged
{
    public required BarricadeDrop Drop { get; init; }
    public required IBuildable Buildable { get; init; }
    public required ushort PendingDamage { get; init; }
    public required CSteamID? Instigator { get; init; }
    public required EDamageOrigin DamageOrigin { get; init; }
}
