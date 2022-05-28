using Rocket.API;
using SDG.Unturned;
using Steamworks;
using System.Collections.Generic;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Commands;

public class LoadCommand : IRocketCommand
{
    private readonly List<string> _permissions = new List<string>(1) { "uc.load" };
    private readonly List<string> _aliases = new List<string>(0);
    public AllowedCaller AllowedCaller => AllowedCaller.Player;
    public string Name => "load";
    public string Help => "loads supplies into a logistics truck";
    public string Syntax => "/load";
    public List<string> Aliases => _aliases;
	public List<string> Permissions => _permissions;
    public void Execute(IRocketPlayer caller, string[] command)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayer? player = UCPlayer.FromIRocketPlayer(caller);

        if (player is null)
            return;

        if (command.Length == 2)
        {
            string action = command[0].ToLower();
            if (action == "build" || action == "ammo")
            {
                if (int.TryParse(command[1], out int amount) && amount > 0)
                {
                    InteractableVehicle? vehicle = UCBarricadeManager.GetVehicleFromLook(player.Player.look);
                    if (vehicle is not null)
                    {
                        if (vehicle.lockedOwner == CSteamID.Nil || vehicle.lockedGroup == CSteamID.Nil)
                            return;

                        if (VehicleBay.VehicleExists(vehicle.asset.GUID, out var data) && data.Type == EVehicleType.LOGISTICS)
                        {
                            if (F.IsInMain(vehicle.transform.position))
                            {
                                if (vehicle.speed >= -1 && vehicle.speed <= 1)
                                {
                                    if (!vehicle.transform.TryGetComponent(out VehicleComponent c))
                                    {
                                        c = vehicle.transform.gameObject.AddComponent<VehicleComponent>();
                                        c.Initialize(vehicle);
                                    }

                                    ESupplyType type = ESupplyType.BUILD;
                                    if (action == "build") type = ESupplyType.BUILD;
                                    else if (action == "ammo") type = ESupplyType.AMMO;

                                    if (c.forceSupplyLoop == null)
                                        c.StartForceLoadSupplies(player, type, amount);
                                    ActionLog.Add(EActionLogType.LOAD_SUPPLIES, type.ToString(), player);
                                }
                                else
                                    player.Message("load_e_toofast");
                            }
                            else
                                player.Message("load_e_notinmain");
                        }
                        else
                            player.Message("load_e_notlogi");
                    }
                    else
                        player.Message("load_e_novehicle");
                }
                else
                    player.Message("load_e_invalidamount");
            }
            else
                player.Message("load_e_usage");
        }
        else
            player.Message("load_e_usage");
    }
}
