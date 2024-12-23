using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Events.Models.Vehicles;

/// <summary>
/// Event listener args which handles a patch listening just before a vehicle is damage (before the InteractableVehicle's health is changed).
/// </summary>
/// 
public class VehiclePreDamaged
{
    public required WarfareVehicle Vehicle { get; init; }
    public required ushort PendingDamage { get; init; }
    public required bool CanRepair { get; init; }
    public required CSteamID? InstantaneousInstigator { get; init; }
    public required CSteamID? LastKnownInstigator { get; init; }
    public required EDamageOrigin InstantaneousDamageOrigin { get; init; }
}
