using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Vehicles.UI;

[UnturnedUI(BasePath = "Canvas")]
public class VehicleHUD : UnturnedUI
{
    // todo: translations for this class
    public readonly UnturnedLabel MissileWarning = new UnturnedLabel("VH_MissileWarning");
    public readonly UnturnedLabel MissileWarningDriver = new UnturnedLabel("VH_MissileWarningDriver");
    public readonly UnturnedLabel FlareCount = new UnturnedLabel("VH_FlareCount");

    public VehicleHUD(AssetConfiguration assetConfig, ILoggerFactory loggerFactory)
        : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:VehicleHud"), staticKey: true, debugLogging: false)
    {
        
    }
    
    public void ShowForPlayer(WarfarePlayer player, WarfareVehicle vehicle, bool displayFlareCount)
    {
        SendToPlayer(player.Connection);

        MissileWarning.SetVisibility(player.Connection, false);
        MissileWarningDriver.SetVisibility(player.Connection, false);
        FlareCount.SetVisibility(player.Connection, displayFlareCount);
        
        if (displayFlareCount)
            FlareCount.SetText(player.Connection, "FLARES: " + vehicle.FlareEmitter?.TotalFlaresLeft);
    }

    public void UpdateFlaresForRelevantPassengers(WarfareVehicle vehicle)
    {
        for (int i = 0; i < vehicle.Vehicle.passengers.Length; i++)
        {
            Passenger passenger = vehicle.Vehicle.passengers[i];
            if (passenger.player == null)
                continue;
            
            if (i != 0 && !vehicle.Info.IsCrewSeat(i)) // driver can always see the flare count, but other passengers must be crew in order to see it
                continue;
            
            FlareCount.SetText(passenger.player.transportConnection, "FLARES: " + vehicle.FlareEmitter?.TotalFlaresLeft);
        }
    }

    public void HideForPlayer(WarfarePlayer player)
    {
        ClearFromPlayer(player.Connection);
    }
    
    public void ToggleMissileWarning(WarfareVehicle vehicle, bool isEnabled)
    {
        for (byte i = 0; i < vehicle.Vehicle.passengers.Length; i++)
        {
            Passenger passenger = vehicle.Vehicle.passengers[i];
            if (passenger?.player == null)
                continue;

            MissileWarning.SetVisibility(passenger.player.transportConnection, isEnabled);
            if (i != 0) // drivers get an extra line of warning text telling them to drop flares 
                continue;
            
            MissileWarningDriver.SetVisibility(passenger.player.transportConnection, isEnabled);
            MissileWarningDriver.SetText(passenger.player.transportConnection, "PRESS '<b><color=#ffffff><plugin_1/></color></b>' FOR FLARES");
        }
    }
}