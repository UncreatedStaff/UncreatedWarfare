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
using VehicleSpawn = Uncreated.Warfare.Vehicles.VehicleSpawn;

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
        public async void Execute(IRocketPlayer caller, string[] command)
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
                            player.Message("vehiclebay_added", vehicle.asset == null || vehicle.asset.vehicleName == null ? vehicle.id.ToString(Data.Locale) : vehicle.asset.vehicleName);
                        }
                        else // error
                            player.Message("vehiclebay_e_exist", vehicle.asset == null || vehicle.asset.vehicleName == null ? vehicle.id.ToString(Data.Locale) : vehicle.asset.vehicleName);
                    }
                    // remove vehicle from vehicle bay
                    else if (op == "remove" || op == "r")
                    {
                        if (VehicleBay.VehicleExists(vehicle.id, out _))
                        {
                            VehicleBay.RemoveRequestableVehicle(vehicle.id);
                            player.Message("vehiclebay_removed", vehicle.asset == null || vehicle.asset.vehicleName == null ? vehicle.id.ToString(Data.Locale) : vehicle.asset.vehicleName);
                        }
                        else // error
                            player.Message("vehiclebay_e_noexist");
                    }
                    else if (op == "savemeta")
                    {
                        if (VehicleBay.VehicleExists(vehicle.id, out var data))
                        {
                            data.SaveMetaData(vehicle);
                            VehicleBay.Save();
                            player.Message("vehiclebay_savemeta", vehicle.asset == null || vehicle.asset.vehicleName == null ? vehicle.id.ToString(Data.Locale) : vehicle.asset.vehicleName);
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
                    property = command[1];
                    newValue = command[2];

                    if (op == "set" || op == "s")
                    {
                        VehicleBay.SetProperty(x => x.VehicleID == vehicle.id, property, newValue, out bool founditem, out bool set, out bool parsed, out bool foundproperty, out bool allowedToChange);
                        if (!founditem) // error - kit does not exist
                        {
                            player.Message("vehiclebay_e_noexist");
                        }
                        else if (!foundproperty) // error - invalid property name
                        {
                            player.Message("vehiclebay_e_invalidprop", property);
                        }
                        else if (!parsed) // error - invalid argument value
                        {
                            player.Message("vehiclebay_e_invalidarg", newValue, property);
                        }
                        else if (!allowedToChange) // error - invalid argument value
                        {
                            player.Message("vehiclebay_e_not_settable", property);
                        }
                        else if (!set) // error - invalid argument value
                        {
                            player.Message("vehiclebay_e_noexist");
                        }
                        else
                        {
                            player.Message("vehiclebay_setprop", property.ToUpper(), vehicle.asset == null || vehicle.asset.vehicleName == null ? vehicle.id.ToString(Data.Locale) : vehicle.asset.vehicleName, newValue.ToUpper());

                            if (VehicleBay.VehicleExists(vehicle.id, out VehicleData data))
                            {
                                List<VehicleSpawn> spawners = data.GetSpawners();
                                for (int i = 0; i < spawners.Count; i++)
                                {
                                    List<VehicleSign> signs = VehicleSigns.GetLinkedSigns(spawners[i]);
                                    for (int s = 0; s < signs.Count; s++)
                                        await signs[s].InvokeUpdate();
                                }
                            }
                        }
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
                return;
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
                            player.Message("correct_usage", "/vehiclebay <register|deregister> <vehicle ID>");
                    }
                    else if (command.Length == 1)
                    {
                        string op = command[0].ToLower();

                        if (op == "link")
                        {
                            if (player.Player.TryGetPlaytimeComponent(out Components.PlaytimeComponent c))
                            {
                                if (VehicleSpawner.IsRegistered(barricade.instanceID, out c.currentlylinking, EStructType.BARRICADE))
                                {
                                    player.Message("vehiclebay_link_started");
                                } else
                                {
                                    F.Log("Couldn't get sign from " + barricade.barricade.id);
                                    player.Message("vehiclebay_e_spawnnoexist");
                                }
                            }
                        } else
                        if (op == "deregister" || op == "dereg" || op == "unregister" || op == "unreg")
                        {
                            if (VehicleSpawner.IsRegistered(barricade.instanceID, out _, EStructType.BARRICADE))
                            {
                                VehicleSpawner.DeleteSpawn(barricade.instanceID, EStructType.BARRICADE);
                                player.Message("vehiclebay_spawn_deregistered");
                            }
                            else
                                player.Message("vehiclebay_e_spawnnoexist");
                        }
                        else if (op == "force")
                        {
                            if (VehicleSpawner.IsRegistered(barricadeDrop.instanceID, out VehicleSpawn spawn, EStructType.BARRICADE))
                            {
                                VehicleAsset asset;
                                if (spawn.HasLinkedVehicle(out InteractableVehicle veh))
                                {
                                    veh.forceRemoveAllPlayers();
                                    VehicleManager.askVehicleDestroy(veh);
                                    asset = veh.asset;
                                } else
                                {
                                    asset = UCAssetManager.FindVehicleAsset(spawn.VehicleID);
                                }
                                spawn.CancelVehicleRespawnTimer();
                                spawn.SpawnVehicle();
                                player.Message("vehiclebay_spawn_forced", asset == null || asset.vehicleName == null ? spawn.VehicleID.ToString(Data.Locale) : asset.vehicleName);
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
                                    player.Message("vehiclebay_check_registered", spawn.SpawnPadInstanceID.ToString(Data.Locale), asset.vehicleName, spawn.VehicleID);
                                else
                                    player.Message("vehiclebay_e_idnotfound", spawn.VehicleID);
                            }
                            else
                                player.Message("vehiclebay_check_notregistered");
                        }
                        else
                            player.Message("correct_usage", "/vehiclebay <register|deregister|force|check> <vehicle ID>");
                    }
                    else
                        player.Message("correct_usage", "/vehiclebay <register|deregister|force|check> <vehicle ID>");
                }
                else
                {
                    if (command.Length > 0 && command[0].ToLower() == "link")
                    {
                        if (player.Player.TryGetPlaytimeComponent(out Components.PlaytimeComponent c))
                        {
                            if (barricadeDrop.model.TryGetComponent(out InteractableSign sign))
                            {
                                if (c.currentlylinking != null)
                                {
                                    if (VehicleSigns.SignExists(sign, out _))
                                    {
                                        await VehicleSigns.UnlinkSign(sign);
                                    }
                                    await VehicleSigns.LinkSign(sign, c.currentlylinking);
                                    player.Message("vehiclebay_link_finished");
                                    c.currentlylinking = null;
                                }
                                else player.Message("vehiclebay_link_not_started");
                            }
                            else player.Message("vehiclebay_e_novehicle");
                        }
                        else player.Message("vehiclebay_e_novehicle");
                    }
                    else player.Message("vehiclebay_e_novehicle");
                }
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
                                player.Message("correct_usage", "/vehiclebay <register|deregister> <vehicle ID>");
                        }
                        else if (command.Length == 1)
                        {
                            string op = command[0].ToLower();
                            if (op == "link")
                            {
                                if (player.Player.TryGetPlaytimeComponent(out Components.PlaytimeComponent c))
                                {
                                    if (VehicleSpawner.IsRegistered(structure.instanceID, out c.currentlylinking, EStructType.STRUCTURE))
                                    {
                                        player.Message("vehiclebay_link_started");
                                    }
                                    else
                                    {
                                        player.Message("vehiclebay_e_spawnnoexist");
                                    }
                                }
                            }
                            else if (op == "deregister" || op == "dereg")
                            {
                                if (VehicleSpawner.IsRegistered(structureDrop.instanceID, out _, EStructType.STRUCTURE))
                                {
                                    VehicleSpawner.DeleteSpawn(structureDrop.instanceID, EStructType.STRUCTURE);
                                    player.Message("vehiclebay_spawn_remove");
                                }
                                else
                                    player.Message("vehiclebay_e_spawnnoexist");
                            }
                            else if (op == "force")
                            {
                                if (VehicleSpawner.IsRegistered(structureDrop.instanceID, out VehicleSpawn spawn, EStructType.STRUCTURE))
                                {
                                    VehicleAsset asset;
                                    if (spawn.HasLinkedVehicle(out InteractableVehicle veh))
                                    {
                                        veh.forceRemoveAllPlayers();
                                        VehicleManager.askVehicleDestroy(veh);
                                        asset = veh.asset;
                                    }
                                    else
                                    {
                                        asset = UCAssetManager.FindVehicleAsset(spawn.VehicleID);
                                    }
                                    spawn.CancelVehicleRespawnTimer();
                                    spawn.SpawnVehicle();
                                    player.Message("vehiclebay_spawn_forced", asset == null || asset.vehicleName == null ? spawn.VehicleID.ToString(Data.Locale) : asset.vehicleName);
                                }
                                else
                                    player.Message("vehiclebay_e_spawnnoexist");
                            }
                            else if (op == "check")
                            {
                                if (VehicleSpawner.IsRegistered(structureDrop.instanceID, out VehicleSpawn spawn, EStructType.STRUCTURE))
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
                                player.Message("correct_usage", "/vehiclebay <register|deregister> <vehicle ID>");
                        }
                        else
                            player.Message("correct_usage", "/vehiclebay <register|deregister> <vehicle ID>");
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
