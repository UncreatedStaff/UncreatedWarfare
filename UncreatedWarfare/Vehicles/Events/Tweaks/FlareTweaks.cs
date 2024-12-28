using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Vehicles.UI;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Vehicles.Events.Tweaks;

public class FlareTweaks : 
    IEventListener<EnterVehicle>,
    IEventListener<VehicleSwappedSeat>,
    IEventListener<ExitVehicle>
{
    public void HandleEvent(EnterVehicle e, IServiceProvider serviceProvider)
    {
        if (!e.Vehicle.Info.Type.IsAircraft() || e.Vehicle.VehicleHUD == null)
            return;
        
        if (e.PassengerIndex != 0 || !e.Vehicle.Info.IsCrewSeat(e.PassengerIndex))
            return;
       
        e.Vehicle.VehicleHUD.ShowForPlayer(e.Player, e.Vehicle, true);
    }

    public void HandleEvent(ExitVehicle e, IServiceProvider serviceProvider)
    {
        e.Vehicle.VehicleHUD?.HideForPlayer(e.Player);
    }

    public void HandleEvent(VehicleSwappedSeat e, IServiceProvider serviceProvider)
    {
        if (!e.Vehicle.Info.Type.IsAircraft() || e.Vehicle.VehicleHUD == null)
            return;
        
        e.Vehicle.VehicleHUD.HideForPlayer(e.Player);
        
        if (e.NewPassengerIndex != 0 || !e.Vehicle.Info.IsCrewSeat(e.NewPassengerIndex))
            return;
        
        e.Vehicle.VehicleHUD.ShowForPlayer(e.Player, e.Vehicle, true);
    }
}