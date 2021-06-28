
using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Kits
{
    public class Command_vehiclebay : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "vehiclebay";
        public string Help => "Set's up the vehicle bay";
        public string Syntax => "/vehiclebay";
        public List<string> Aliases => new List<string>() { "vb" };
        public List<string> Permissions => new List<string>() { "uc.vehiclebay" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;

            InteractableVehicle vehicle = UCBarricadeManager.GetVehicleFromLook(player.Player.look);

            if (vehicle != null)
            {
                string op = "";
                string property = "";
                string newValue = "";

                if (command.Length == 1)
                {
                    op = command[0].ToLower();

                    // add vehicle to vehicle bay
                    if (op == "add" || op == "a")
                    {
                        if (!VehicleBay.VehicleExists(vehicle.id, out _))
                        {
                            VehicleBay.AddRequestableVehicle(vehicle);
                            player.Message("vehiclebay_added");
                        }
                        else // error
                            player.Message("vehiclebay_e_exist");
                    }
                    // remove vehicle from vehicle bay
                    else if (op == "remove" || op == "r")
                    {
                        if (VehicleBay.VehicleExists(vehicle.id, out _))
                        {
                            VehicleBay.RemoveRequestableVehicle(vehicle.id);
                            player.Message("vehiclebay_removed");
                        }
                        else // error
                            player.Message("vehiclebay_e_noexist");
                    }
                    else
                        player.Message("correct_usage", "/vehiclebay <add|remove|set|crewseats>");
                }
                // change vehicle property
                else if (command.Length == 3)
                {
                    op = command[0].ToLower();
                    property = command[1].ToLower();
                    newValue = command[2];

                    if (op == "set" || op == "s")
                    {
                        VehicleBay.SetProperty(vehicle.id, property, newValue, out bool propertyIsValid, out bool vehicleExists, out bool argIsValid);

                        if (!propertyIsValid) // error - invalid property name
                        {
                            player.Message("vehiclebay_e_invalidprop", property);
                            return;
                        }
                        if (!vehicleExists) // error - kit does not exist
                        {
                            player.Message("vehiclebay_e_noexist", vehicleExists);
                            return;
                        }
                        if (!argIsValid) // error - invalid argument value
                        {
                            player.Message("vehiclebay_e_invalidarg", newValue, property);
                            return;
                        }
                        // success
                        player.Message("vehiclebay_setprop", property, vehicle.asset.vehicleName, newValue);
                    }
                    // add or remove crew seats
                    else if (op == "crewseats" || op == "cs")
                    {
                        string seat = command[2];
                        // add crew seat
                        if (property == "add" || property == "a")
                        {
                            if (VehicleBay.VehicleExists(vehicle.id, out var vehicleData))
                            {
                                if (byte.TryParse(seat, System.Globalization.NumberStyles.Any, Data.Locale, out var index))
                                {
                                    if (!vehicleData.CrewSeats.Contains(index))
                                    {
                                        // success
                                        VehicleBay.AddCrewmanSeat(vehicle.id, index);
                                        player.Message("vehiclebay_seatadded", seat);
                                    }
                                    else
                                    {
                                        player.Message("vehiclebay_seatexist", seat);
                                    }
                                }
                                else
                                    player.Message("vehiclebay_e_invalidseat", seat);

                            }
                            else
                                player.Message("vehiclebay_e_noexist");
                        }
                        // remove crew seat
                        else if (property == "remove" || property == "r")
                        {
                            if (VehicleBay.VehicleExists(vehicle.id, out var vehicleData))
                            {
                                if (byte.TryParse(seat, System.Globalization.NumberStyles.Any, Data.Locale, out var index))
                                {
                                    if (vehicleData.CrewSeats.Contains(index))
                                    {
                                        // success
                                        VehicleBay.RemoveCrewmanSeat(vehicle.id, index);
                                        player.Message("vehiclebay_seatadded", seat);
                                    }
                                    else
                                    {
                                        player.Message("vehiclebay_seatnoexist", seat);
                                    }
                                }
                                else
                                    player.Message("vehiclebay_e_invalidseat", seat);

                            }
                            else
                                player.Message("vehiclebay_e_noexist");
                        }
                        else
                            player.Message("correct_usage", "/vehiclebay crewseats <add|remove|clear> <seat index>");
                    }
                    else
                        player.Message("correct_usage", "/vehiclebay <add|remove|set|crewseats>");
                }
                // set /ammo item lits
                else if (command.Length == 2)
                {
                    op = command[0].ToLower();
                    property = command[1].ToLower();

                    if (op == "set" || op == "s")
                    {
                        if (property == "items")
                        {
                            List<ushort> items = new List<ushort>();

                            for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
                            {
                                if (page == PlayerInventory.AREA)
                                    continue;

                                byte pageCount = player.Player.inventory.getItemCount(page);

                                for (byte index = 0; index < pageCount; index++)
                                {
                                    items.Add(player.Player.inventory.getItem(page, index).item.id);
                                }
                            }

                            VehicleBay.SetItems(vehicle.id, items);

                            if (items.Count == 0)
                                player.Message("vehiclebay_cleareditems", vehicle.asset.vehicleName, items.Count);
                            else
                                player.Message("vehiclebay_setitems", vehicle.asset.vehicleName, items.Count);
                        }
                    }
                    else
                        player.Message("correct_usage", "/vehiclebay <add|remove|set|crewseats>");
                }
                else
                    player.Message("correct_usage", "/vehiclebay <add|remove|set|crewseats>");
            }
            BarricadeData barricade = UCBarricadeManager.GetBarricadeDataFromLook(player.Player.look, out BarricadeDrop barricadeDrop);

            if (barricade != null)
            {
                if (barricade.barricade.id == UCWarfare.Config.VehicleBaySettings.VehicleSpawnerID)
                {
                    if (command.Length == 2)
                    {
                        string op = command[0].ToLower();
                        string ID = command[1].ToLower();

                        if (op == "register" || op == "reg")
                        {
                            if (ushort.TryParse(ID, System.Globalization.NumberStyles.Any, Data.Locale, out var vehicleID))
                            {
                                VehicleAsset asset = UCAssetManager.FindVehicleAsset(vehicleID);

                                if (asset != null)
                                {
                                    if (!VehicleSpawner.IsRegistered(barricade.instanceID, out _, EStructType.BARRICADE))
                                    {
                                        VehicleSpawner.CreateSpawn(barricadeDrop, barricade, vehicleID);
                                        player.Message("vehiclebay_spawn_registered", asset.vehicleName);
                                    }
                                    else
                                        player.Message("vehiclebay_e_spawnexist", vehicleID);
                                }
                                else
                                    player.Message("vehiclebay_e_idnotfound", vehicleID);
                            }
                            else
                                player.Message("vehiclebay_e_invalidid", ID);
                        }
                        else
                            player.Message("correct_usage", "/vehiclebay <register|unregister> <vehicle ID>");
                    }
                    else if (command.Length == 1)
                    {
                        string op = command[0].ToLower();

                        if (op == "deregister" || op == "dereg")
                        {
                            if (VehicleSpawner.IsRegistered(barricade.instanceID, out _, EStructType.BARRICADE))
                            {
                                VehicleSpawner.DeleteSpawn(barricade.instanceID, EStructType.BARRICADE);
                                player.Message("vehiclebay_spawn_remove");
                            }
                            else
                                player.Message("vehiclebay_e_spawnnoexist");
                        }
                        else if (op == "check")
                        {
                            if (VehicleSpawner.IsRegistered(barricade.instanceID, out Vehicles.VehicleSpawn spawn, EStructType.BARRICADE))
                            {
                                VehicleAsset asset = UCAssetManager.FindVehicleAsset(spawn.VehicleID);
                                if (asset != null)
                                    player.Message("vehiclebay_check_registered", spawn.SpawnPadInstanceID.ToString(), asset.vehicleName, spawn.VehicleID);
                                else
                                    player.Message("vehiclebay_e_idnotfound", spawn.VehicleID);
                            }
                            else
                                player.Message("vehiclebay_check_notregistered");
                        }
                        else
                            player.Message("correct_usage", "/vehiclebay <register|unregister> <vehicle ID>");
                    }
                    else
                        player.Message("correct_usage", "/vehiclebay <register|unregister> <vehicle ID>");
                }
                else
                    player.Message("vehiclebay_e_novehicle");
            }
            else // check for structure
            {
                StructureData structure = UCBarricadeManager.GetStructureDataFromLook(player, out StructureDrop structureDrop);
                if (structure != default)
                {
                    if (structure.structure.id == UCWarfare.Config.VehicleBaySettings.VehicleSpawnerID)
                    {
                        if (command.Length == 2)
                        {
                            string op = command[0].ToLower();
                            string ID = command[1].ToLower();

                            if (op == "register" || op == "reg")
                            {
                                if (ushort.TryParse(ID, System.Globalization.NumberStyles.Any, Data.Locale, out var vehicleID))
                                {
                                    VehicleAsset asset = UCAssetManager.FindVehicleAsset(vehicleID);

                                    if (asset != null)
                                    {
                                        if (!VehicleSpawner.IsRegistered(structure.instanceID, out _, EStructType.STRUCTURE))
                                        {
                                            VehicleSpawner.CreateSpawn(structureDrop, structure, vehicleID);
                                            player.Message("vehiclebay_spawn_registered", asset.vehicleName);
                                        }
                                        else
                                            player.Message("vehiclebay_e_spawnexist", vehicleID);
                                    }
                                    else
                                        player.Message("vehiclebay_e_idnotfound", vehicleID);
                                }
                                else
                                    player.Message("vehiclebay_e_invalidid", ID);
                            }
                            else
                                player.Message("correct_usage", "/vehiclebay <register|unregister> <vehicle ID>");
                        }
                        else if (command.Length == 1)
                        {
                            string op = command[0].ToLower();

                            if (op == "deregister" || op == "dereg")
                            {
                                if (VehicleSpawner.IsRegistered(structureDrop.instanceID, out _, EStructType.STRUCTURE))
                                {
                                    VehicleSpawner.DeleteSpawn(structureDrop.instanceID, EStructType.STRUCTURE);
                                    player.Message("vehiclebay_spawn_remove");
                                }
                                else
                                    player.Message("vehiclebay_e_spawnnoexist");
                            }
                            else if (op == "check")
                            {
                                if (VehicleSpawner.IsRegistered(structureDrop.instanceID, out var spawn, EStructType.STRUCTURE))
                                {
                                    var asset = UCAssetManager.FindVehicleAsset(spawn.VehicleID);
                                    if (asset != null)
                                        player.Message("vehiclebay_check_registered", spawn.SpawnPadInstanceID.ToString(), asset.vehicleName, spawn.VehicleID);
                                    else
                                        player.Message("vehiclebay_e_idnotfound", spawn.VehicleID);
                                }
                                else
                                    player.Message("vehiclebay_check_notregistered");
                            }
                            else
                                player.Message("correct_usage", "/vehiclebay <register|unregister> <vehicle ID>");
                        }
                        else
                            player.Message("correct_usage", "/vehiclebay <register|unregister> <vehicle ID>");
                    }
                    else
                        player.Message("vehiclebay_e_novehicle");
                }
                else
                    player.Message("vehiclebay_e_novehicle");
            }
        }
    }
}
