using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Vehicles.UI;

[UnturnedUI(BasePath = "Canvas")]
public class VehicleHUD : UnturnedUI
{
    public readonly UnturnedLabel MissileWarning = new UnturnedLabel("VH_MissileWarning");
    public readonly UnturnedLabel MissileWarningDriver = new UnturnedLabel("VH_MissileWarningDriver");
    public readonly UnturnedLabel FlareCount = new UnturnedLabel("VH_FlareCount");

    private readonly VehicleTranslations _translations;
    private readonly IPlayerService _playerService;

    public VehicleHUD(
        AssetConfiguration assetConfig,
        ILoggerFactory loggerFactory,
        TranslationInjection<VehicleTranslations> translations,
        IPlayerService playerService)
        : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:VehicleHud"), staticKey: true, debugLogging: false)
    {
        _translations = translations.Value;
        _playerService = playerService;
    }
    
    public void ShowForPlayer(WarfarePlayer player, WarfareVehicle vehicle, bool displayFlareCount)
    {
        SendToPlayer(player.Connection);

        MissileWarning.SetVisibility(player.Connection, false);
        MissileWarningDriver.SetVisibility(player.Connection, false);
        FlareCount.SetVisibility(player.Connection, displayFlareCount);
        
        if (displayFlareCount && vehicle.FlareEmitter is not null)
            FlareCount.SetText(player.Connection, _translations.VehicleHUDFlareCount.Translate(vehicle.FlareEmitter.TotalFlaresLeft, player));
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
            
            if (vehicle.FlareEmitter is not null)
            {
                WarfarePlayer pl = _playerService.GetOnlinePlayer(passenger.player);
                FlareCount.SetText(passenger.player.transportConnection, _translations.VehicleHUDFlareCount.Translate(vehicle.FlareEmitter.TotalFlaresLeft, pl));
            }
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

            ITransportConnection tc = passenger.player.transportConnection;
            WarfarePlayer? player = null;
            if (isEnabled)
            {
                player = _playerService.GetOnlinePlayer(passenger.player);
                if (!player.Locale.IsDefaultLanguage || !_translations.VehicleHUDLockedOnWarning.HasDefaultValue)
                {
                    MissileWarning.SetText(tc, _translations.VehicleHUDLockedOnWarning.Translate(player));
                }
            }

            MissileWarning.SetVisibility(tc, isEnabled);

            if (i != 0) // drivers get an extra line of warning text telling them to drop flares 
                continue;

            player ??= _playerService.GetOnlinePlayer(passenger.player);

            MissileWarningDriver.SetVisibility(tc, isEnabled);
            MissileWarningDriver.SetText(tc, _translations.VehicleHUDLockedOnWarningDriver.Translate(player));
        }
    }
}