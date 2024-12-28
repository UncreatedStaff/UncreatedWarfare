using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Events.Models.Vehicles;

/// <summary>
/// Event listener args which handles a patch listening for when a vehicle is requested to be damaged.
/// Can be cancelled to disallow damage from being done.
/// </summary>
/// 
public class DamageVehicleRequested : CancellableEvent
{
    public required WarfareVehicle Vehicle { get; init; }
    public required ushort PendingDamage { get; set; }
    public required bool CanRepair { get; set; }
    public required CSteamID? Instigator { get; init; }
    public required EDamageOrigin DamageOrigin { get; init; }
}