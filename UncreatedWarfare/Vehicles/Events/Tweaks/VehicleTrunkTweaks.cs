﻿using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Vehicles;

namespace Uncreated.Warfare.Vehicles.Events.Tweaks;

public class VehicleTrunkTweaks :
    IEventListener<VehicleExploded>,
    IEventListener<VehicleDespawned>
{
    public void HandleEvent(VehicleExploded e, IServiceProvider serviceProvider)
    {
        if (e.Vehicle.Info.WipeTrunkOnDestroyed)
            WipeTrunkItems(e.Vehicle.Vehicle);
    }
    public void HandleEvent(VehicleDespawned e, IServiceProvider serviceProvider)
    {
        if (e.Vehicle.Info.WipeTrunkOnDestroyed)
            WipeTrunkItems(e.Vehicle.Vehicle);
        
    }
    private void WipeTrunkItems(InteractableVehicle vehicle)
    {
        int ct = vehicle.trunkItems.getItemCount();
        for (int i = ct - 1; i >= 0; --i)
            vehicle.trunkItems.removeItem((byte)i);
    }
}