using System.Collections.Generic;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.Vehicles;
public class VehicleSpawnInfo
{
    /// <summary>
    /// The asset used to spawn vehicles.
    /// </summary>
    public IAssetLink<VehicleAsset> Vehicle { get; set; }

    /// <summary>
    /// Barricade or structure where vehicles are spawned above.
    /// </summary>
    public IBuildable Spawner { get; set; }

    /// <summary>
    /// List of sign barricades linked to this spawn.
    /// </summary>
    public IList<IBuildable> SignInstanceIds { get; } = new List<IBuildable>(1);

    /// <summary>
    /// A vehicle that has been spawned from this spawn.
    /// </summary>
    public InteractableVehicle? LinkedVehicle { get; private set; }
    
    /// <summary>
    /// Link this spawn to a vehicle.
    /// </summary>
    internal void LinkVehicle(InteractableVehicle vehicle)
    {
        GameThread.AssertCurrent();

        if (vehicle == LinkedVehicle)
            return;

        InteractableVehicle? oldVehicle = LinkedVehicle;
        LinkedVehicle = vehicle;
        if (oldVehicle != null && oldVehicle.TryGetComponent(out VehicleComponent oldVehicleComponent) && oldVehicleComponent.Spawn == this)
        {
            oldVehicleComponent.UnlinkFromSpawn(this);
        }

        if (vehicle.TryGetComponent(out VehicleComponent newVehicleComponent))
        {
            newVehicleComponent.LinkToSpawn(this);
        }
    }

    /// <summary>
    /// Unlink this spawn from it's <see cref="LinkedVehicle"/>.
    /// </summary>
    internal void UnlinkVehicle()
    {
        GameThread.AssertCurrent();

        InteractableVehicle? oldVehicle = LinkedVehicle;
        if (oldVehicle is null)
            return;

        LinkedVehicle = null;
        if (oldVehicle == null || !oldVehicle.TryGetComponent(out VehicleComponent oldVehicleComponent))
        {
            return;
        }

        if (oldVehicleComponent.Spawn == this)
        {
            oldVehicleComponent.UnlinkFromSpawn(this);
        }
    }
}