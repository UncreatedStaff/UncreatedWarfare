using System;
using Uncreated.Warfare.Events.Components;

namespace Uncreated.Warfare.FOBs.SupplyCrates.Throwable;

public class ThrownVehicleCrateComponent : ThrownComponent
{
    public Action<InteractableVehicle> OnCollideWithVehicle;
    public bool HasCollidedWithVehicle { get; private set; }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != LayerMasks.VEHICLE)
            return;
        
        InteractableVehicle vehicle = other.GetComponentInParent<InteractableVehicle>();
        if (vehicle == null || vehicle.isDead)
            return;
        
        HasCollidedWithVehicle = true;
        OnCollideWithVehicle.Invoke(vehicle);
        Destroy(this);
    }

}