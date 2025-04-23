using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Events.Models.Vehicles;

/// <summary>
/// Event listener args which handles a patch listening just before a vehicle is damage (before the InteractableVehicle's health is changed).
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class VehiclePreDamaged
{
    public required WarfareVehicle Vehicle { get; init; }
    public required ushort PendingDamage { get; set; }
    public required bool CanRepair { get; set; }
    public required CSteamID? InstantaneousInstigator { get; init; }
    public required CSteamID? LastKnownInstigator { get; init; }
    public required EDamageOrigin InstantaneousDamageOrigin { get; init; }
}
