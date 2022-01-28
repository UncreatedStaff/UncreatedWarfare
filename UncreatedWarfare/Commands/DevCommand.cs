using Rocket.API;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Commands
{
    public class DevCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "dev";
        public string Help => "Dev command for various server setup features.";
        public string Syntax => "/dev [arguments]";
        public List<string> Aliases => new List<string>(0);
        public List<string> Permissions => new List<string>(1) { "uc.dev" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UCPlayer player = UCPlayer.FromIRocketPlayer(caller);

            if (command.Length > 0 && command[0].ToLower() == "addcache")
            {
                if (Data.Is(out Insurgency insurgency))
                {
                    SerializableTransform transform = new SerializableTransform(player.Player.transform);
                    Gamemode.Config.MapConfig.AddCacheSpawn(transform);
                    player.Message("Added new cache spawn: " + transform.ToString().Colorize("ebd491"));
                }
                else
                    player.Message("Gamemode must be Insurgency in order to use this command.".Colorize("c7a29f"));
            }
            else if (command.Length > 1 && command[0].ToLower() == "addintel")
            {
                if (Data.Is(out Insurgency insurgency))
                {
                    if (int.TryParse(command[1], out int points))
                    {
                        insurgency.AddIntelligencePoints(points);

                        player.Message($"Added {points} intelligence points.".Colorize("ebd491"));
                    }
                    else
                        player.Message($"'{command[1]}' is not a valid number of intelligence points.".Colorize("c7a29f"));
                }
                else
                    player.Message("Gamemode must be Insurgency in order to use this command.".Colorize("c7a29f"));
            }
            else if (command.Length == 1 && command[0].ToLower() == "quickbuild" || command[0].ToLower() == "qb")
            {
                var barricade = BarricadeManager.FindBarricadeByRootTransform(UCBarricadeManager.GetBarricadeTransformFromLook(player.Player.look));

                if (barricade != null)
                {
                    if (barricade.model.TryGetComponent<BuildableComponent>(out var buildable))
                    {
                        buildable.Build();
                        player.Message($"Successfully built {barricade.asset.itemName}".Colorize("ebd491"));
                    }
                    else
                        player.Message($"This barricade ({barricade.asset.itemName}) is not buildable.".Colorize("c7a29f"));
                }
                else
                    player.Message($"You are not looking at a barricade.".Colorize("c7a29f"));
            }
            else if (command.Length == 1 && command[0].ToLower() == "logmeta")
            {
                var barricade = BarricadeManager.FindBarricadeByRootTransform(UCBarricadeManager.GetBarricadeTransformFromLook(player.Player.look));

                if (barricade != null)
                {
                    var data = barricade.GetServersideData();
                    string state = System.Convert.ToBase64String(data.barricade.state);
                    L.Log($"BARRICADE STATE: {state}");
                    player.Message($"Metadata state has been logged to console. State: {state}".Colorize("ebd491"));
                }
                else
                    player.Message($"You are not looking at a barricade.".Colorize("c7a29f"));

            }
            else if (command.Length == 1 && (command[0].ToLower() == "checkvehicle" || command[0].ToLower() == "cvc"))
            {
                var vehicle = player.Player.movement.getVehicle();
                if (vehicle is null)
                    vehicle = UCBarricadeManager.GetVehicleFromLook(player.Player.look);

                if (vehicle is not null)
                {
                    if (vehicle.transform.TryGetComponent(out VehicleComponent component))
                    {
                        player.Message($"Vehicle logged successfully. Check console".Colorize("ebd491"));

                        L.Log($"{vehicle.asset.vehicleName.ToUpper()}");

                        L.Log($"    Is In VehicleBay: {component.isInVehiclebay}\n");

                        if (component.isInVehiclebay)
                        {
                            L.Log($"    Team: {component.Data.Team}");
                            L.Log($"    Type: {component.Data.Type}");
                            L.Log($"    Tickets: {component.Data.TicketCost}");
                            L.Log($"    Branch: {component.Data.Branch}\n");
                        }

                        L.Log($"    Quota: {component.Quota}/{component.RequiredQuota}\n");

                        L.Log($"    Usage Table:");
                        foreach (var entry in component.UsageTable)
                        {
                            L.Log($"        {entry.Key}'s time in vehicle: {entry.Value} s");
                        }
                        L.Log($"    Transport Table:");
                        foreach (var entry in component.TransportTable)
                        {
                            L.Log($"        {entry.Key}'s starting position: {entry.Value}");
                        }
                        L.Log($"    Damage Table:");
                        foreach (var entry in component.DamageTable)
                        {
                            L.Log($"        {entry.Key}'s damage so far: {entry.Value.Key} ({(DateTime.Now - entry.Value.Value).TotalSeconds} seconds ago)");
                        }
                    }
                    else
                        player.Message($"This vehicle does have a VehicleComponent".Colorize("c7a29f"));
                }
                else
                    player.Message($"You are not inside or looking at a vehicle.".Colorize("c7a29f"));

            }
            else if (command.Length == 1 && (command[0].ToLower() == "getpos" || command[0].ToLower() == "cvc"))
            {
                player.Message($"Your position: {player.Position} - Your rotation: {player.Player.transform.eulerAngles.y}".Colorize("ebd491"));
            }
            else
                player.Message($"Dev command did not recognise those arguments.".Colorize("dba29e"));
        }
    }
}
