using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Gamemodes;
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
        public void Execute(IRocketPlayer caller, string[] command)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            UCPlayer? player = UCPlayer.FromIRocketPlayer(caller);
            if (player == null) return;

            InteractableVehicle? vehicle = UCBarricadeManager.GetVehicleFromLook(player.Player.look);

            if (vehicle != null)
            {
                string op = "";
                string property = "";
                string newValue = "";
                if (command.Length > 2 && command[0].Equals("delay", StringComparison.OrdinalIgnoreCase))
                {
                    if (!VehicleBay.VehicleExists(vehicle.asset.GUID, out VehicleData data))
                    {
                        player.SendChat("vehiclebay_e_noexist");
                        return;
                    }
                    // vb delay add time 30 [gamemode]
                    // vb delay add flag 1 [gamemode]
                    // vb delay add flagpercent 50 [gamemode]
                    // vb delay add staging [gamemode]
                    // vb delay add none [gamemode]
                    // vb delay remove all
                    bool adding;
                    if (command[1].Equals("add", StringComparison.OrdinalIgnoreCase))
                        adding = true;
                    else if (command[1].Equals("remove", StringComparison.OrdinalIgnoreCase))
                        adding = false;
                    else
                    {
                        player.SendChat("correct_usage", "/vehiclebay <add|remove|set|crewseats|delay>");
                        return;
                    }
                    if (!adding && command[2].Equals("all", StringComparison.OrdinalIgnoreCase) && command.Length == 3)
                    {
                        int rem = data.Delays.Length;
                        if (rem > 0)
                        {
                            data.Delays = new Delay[0];
                            VehicleSpawner.UpdateSigns(data.VehicleID);
                            VehicleBay.Save();
                        }
                        player.SendChat("vehiclebay_delay_removed", rem.ToString(Data.Locale));
                    }
                    EDelayType type;
                    if (command[2].Equals("time", StringComparison.OrdinalIgnoreCase))
                        type = EDelayType.TIME;
                    else if (command[2].Equals("flag", StringComparison.OrdinalIgnoreCase))
                        type = EDelayType.FLAG;
                    else if (command[2].Equals("flagpercent", StringComparison.OrdinalIgnoreCase))
                        type = EDelayType.FLAG_PERCENT;
                    else if (command[2].Equals("staging", StringComparison.OrdinalIgnoreCase))
                        type = EDelayType.OUT_OF_STAGING;
                    else if (command[2].Equals("none", StringComparison.OrdinalIgnoreCase))
                        type = EDelayType.NONE;
                    else
                    {
                        player.SendChat("correct_usage", "/vehiclebay delay <add|remove> <time|flag|flagpercent|staging|none> [value] [gamemode]");
                        return;
                    }
                    if (type == EDelayType.NONE && command.Length < 4)
                    {
                        player.SendChat("correct_usage", "/vehiclebay delay " + command[1].ToLower() + " none <gamemode>");
                        return;
                    }
                    string? gamemode = null;
                    if (type == EDelayType.OUT_OF_STAGING || type == EDelayType.NONE)
                    {
                        if (command.Length > 3)
                            gamemode = command[3];
                    }
                    else if (command.Length < 4)
                    {
                        player.SendChat("correct_usage", "/vehiclebay delay " + command[1].ToLower() + " " + command[2].ToLower() + " <value> [gamemode]");
                        return;
                    }
                    else if (command.Length > 4)
                        gamemode = command[4];

                    if (string.IsNullOrEmpty(gamemode) && type == EDelayType.NONE)
                    {
                        gamemode = "<";
                        foreach (string key in Gamemode.GAMEMODES.Keys)
                        {
                            if (gamemode.Length != 1) gamemode += "|";
                            gamemode += key;
                        }
                        gamemode += ">";
                        player.SendChat("correct_usage", "/vehiclebay delay " + command[1].ToLower() + " none " + gamemode);
                        return;
                    }
                    if (!string.IsNullOrEmpty(gamemode))
                    {
                        string? gm = null;
                        foreach (string key in Gamemode.GAMEMODES.Keys)
                        {
                            if (key.Equals(gamemode, StringComparison.OrdinalIgnoreCase))
                            {
                                gm = key;
                                break;
                            }
                        }
                    }

                    float val = 0;
                    if (type != EDelayType.OUT_OF_STAGING && type != EDelayType.NONE && !float.TryParse(command[3], System.Globalization.NumberStyles.Any, Data.Locale, out val))
                    {
                        player.SendChat("correct_usage", "/vehiclebay delay " + command[1].ToLower() + " " + command[2].ToLower() + " <value (float)>" + (string.IsNullOrEmpty(gamemode) ? string.Empty : " " + gamemode));
                        return;
                    }

                    if (adding)
                    {
                        data.AddDelay(type, val, gamemode);
                        VehicleBay.Save();
                        VehicleSpawner.UpdateSigns(data.VehicleID);
                        foreach (VehicleSpawn spawn in data.EnumerateSpawns())
                        {
                            VehicleBayComponent? svc = spawn.Component;
                            if (svc != null) svc.UpdateTimeDelay();
                        }
                        player.SendChat("vehiclebay_delay_added", type.ToString().ToLower(), val.ToString(Data.Locale), string.IsNullOrEmpty(gamemode) ? "any" : gamemode!);
                    }
                    else
                    {
                        int rem = 0;
                        while (data.RemoveDelay(type, val, gamemode)) rem++;
                        if (rem > 0)
                        {
                            VehicleSpawner.UpdateSigns(data.VehicleID);
                            VehicleBay.Save();
                        }
                        player.SendChat("vehiclebay_delay_removed", rem.ToString(Data.Locale));
                    }
                }
                else if (command.Length == 1)
                {
                    op = command[0].ToLower();

                    // add vehicle to vehicle bay
                    if (op == "add" || op == "a")
                    {
                        if (!VehicleBay.VehicleExists(vehicle.asset.GUID, out _))
                        {
                            VehicleBay.AddRequestableVehicle(vehicle);
                            player.SendChat("vehiclebay_added", vehicle.asset == null || vehicle.asset.vehicleName == null ? vehicle.id.ToString(Data.Locale) : vehicle.asset.vehicleName);
                        }
                        else // error
                            player.SendChat("vehiclebay_e_exist", vehicle.asset == null || vehicle.asset.vehicleName == null ? vehicle.id.ToString(Data.Locale) : vehicle.asset.vehicleName);
                    }
                    // remove vehicle from vehicle bay
                    else if (op == "remove" || op == "r")
                    {
                        if (VehicleBay.VehicleExists(vehicle.asset.GUID, out _))
                        {
                            VehicleBay.RemoveRequestableVehicle(vehicle.asset.GUID);
                            player.SendChat("vehiclebay_removed", vehicle.asset == null || vehicle.asset.vehicleName == null ? vehicle.id.ToString(Data.Locale) : vehicle.asset.vehicleName);
                        }
                        else // error
                            player.SendChat("vehiclebay_e_noexist");
                    }
                    else if (op == "savemeta")
                    {
                        if (VehicleBay.VehicleExists(vehicle.asset.GUID, out VehicleData data))
                        {
                            data.SaveMetaData(vehicle);
                            VehicleBay.Save();
                            player.SendChat("vehiclebay_savemeta", vehicle.asset == null || vehicle.asset.vehicleName == null ? vehicle.id.ToString(Data.Locale) : vehicle.asset.vehicleName);
                        }
                        else // error
                            player.SendChat("vehiclebay_e_noexist");
                    }
                    else
                        player.SendChat("correct_usage", "/vehiclebay <add|remove|set|crewseats>");
                }
                // change vehicle property
                else if (command.Length == 3)
                {
                    op = command[0].ToLower();
                    property = command[1];
                    newValue = command[2];

                    if (op == "set" || op == "s")
                    {
                        VehicleBay.SetProperty(x => x.VehicleID == vehicle.asset.GUID, property, newValue, out bool founditem, out bool set, out bool parsed, out bool foundproperty, out bool allowedToChange);
                        if (!founditem) // error - kit does not exist
                        {
                            player.SendChat("vehiclebay_e_noexist");
                        }
                        else if (!foundproperty) // error - invalid property name
                        {
                            player.SendChat("vehiclebay_e_invalidprop", property);
                        }
                        else if (!parsed) // error - invalid argument value
                        {
                            player.SendChat("vehiclebay_e_invalidarg", newValue, property);
                        }
                        else if (!allowedToChange) // error - invalid argument value
                        {
                            player.SendChat("vehiclebay_e_not_settable", property);
                        }
                        else if (!set) // error - invalid argument value
                        {
                            player.SendChat("vehiclebay_e_noexist");
                        }
                        else
                        {
                            player.SendChat("vehiclebay_setprop", property.ToUpper(), vehicle.asset == null || vehicle.asset.vehicleName == null ? vehicle.id.ToString(Data.Locale) : vehicle.asset.vehicleName, newValue.ToUpper());
                            if (vehicle.asset == null) return;
                            if (VehicleBay.VehicleExists(vehicle.asset.GUID, out VehicleData data))
                            {
                                for (int i = 0; i < VehicleSpawner.ActiveObjects.Count; i++)
                                {
                                    if (VehicleSpawner.ActiveObjects[i].VehicleID == data.VehicleID)
                                    {
                                        VehicleSpawner.ActiveObjects[i].UpdateSign();
                                    }
                                }
                            }
                            VehicleBay.Save();
                        }
                    }
                    // add or remove crew seats
                    else if (op == "crewseats" || op == "cs")
                    {
                        string seat = command[2];
                        // add crew seat
                        if (property == "add" || property == "a")
                        {
                            if (VehicleBay.VehicleExists(vehicle.asset.GUID, out VehicleData vehicleData))
                            {
                                if (byte.TryParse(seat, System.Globalization.NumberStyles.Any, Data.Locale, out var index))
                                {
                                    if (!vehicleData.CrewSeats.Contains(index))
                                    {
                                        // success
                                        VehicleBay.AddCrewmanSeat(vehicle.asset.GUID, index);
                                        player.SendChat("vehiclebay_seatadded", seat);
                                    }
                                    else
                                    {
                                        player.SendChat("vehiclebay_seatexist", seat);
                                    }
                                }
                                else
                                    player.SendChat("vehiclebay_e_invalidseat", seat);

                            }
                            else
                                player.SendChat("vehiclebay_e_noexist");
                        }
                        // remove crew seat
                        else if (property == "remove" || property == "r")
                        {
                            if (VehicleBay.VehicleExists(vehicle.asset.GUID, out VehicleData vehicleData))
                            {
                                if (byte.TryParse(seat, System.Globalization.NumberStyles.Any, Data.Locale, out var index))
                                {
                                    if (vehicleData.CrewSeats.Contains(index))
                                    {
                                        // success
                                        VehicleBay.RemoveCrewmanSeat(vehicle.asset.GUID, index);
                                        player.SendChat("vehiclebay_seatadded", seat);
                                    }
                                    else
                                    {
                                        player.SendChat("vehiclebay_seatnoexist", seat);
                                    }
                                }
                                else
                                    player.SendChat("vehiclebay_e_invalidseat", seat);

                            }
                            else
                                player.SendChat("vehiclebay_e_noexist");
                        }
                        else
                            player.SendChat("correct_usage", "/vehiclebay crewseats <add|remove|clear> <seat index>");
                    }
                    else
                        player.SendChat("correct_usage", "/vehiclebay <add|remove|set|crewseats>");
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
                            List<Guid> items = new List<Guid>();

                            for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
                            {
                                if (page == PlayerInventory.AREA)
                                    continue;

                                byte pageCount = player.Player.inventory.getItemCount(page);

                                for (byte index = 0; index < pageCount; index++)
                                {
                                    if (Assets.find(EAssetType.ITEM, player.Player.inventory.getItem(page, index).item.id) is ItemAsset a)
                                        items.Add(a.GUID);
                                }
                            }

                            VehicleBay.SetItems(vehicle.asset.GUID, items.ToArray());

                            if (items.Count == 0)
                                player.SendChat("vehiclebay_cleareditems", vehicle.asset.vehicleName, items.Count.ToString(Data.Locale));
                            else
                                player.SendChat("vehiclebay_setitems", vehicle.asset.vehicleName, items.Count.ToString(Data.Locale));
                        }
                    }
                    else
                        player.SendChat("correct_usage", "/vehiclebay <add|remove|set|crewseats|delay>");
                }
                else
                    player.SendChat("correct_usage", "/vehiclebay <add|remove|set|crewseats|delay>");
                return;
            }
            SDG.Unturned.BarricadeData? barricade = UCBarricadeManager.GetBarricadeDataFromLook(player.Player.look, out BarricadeDrop? barricadeDrop);

            if (barricade != null)
            {
                if (barricade.barricade.asset.GUID == Gamemode.Config.Barricades.VehicleBayGUID)
                {
                    if (command.Length == 2)
                    {
                        string op = command[0].ToLower();
                        string ID = command[1].ToLower();
                        if (op == "register" || op == "reg")
                        {
                            if (ushort.TryParse(ID, System.Globalization.NumberStyles.Any, Data.Locale, out ushort vehicleID))
                            {
                                VehicleAsset asset = UCAssetManager.FindVehicleAsset(vehicleID);

                                if (asset != null)
                                {
                                    if (!VehicleSpawner.IsRegistered(barricade.instanceID, out _, EStructType.BARRICADE))
                                    {
                                        VehicleSpawner.CreateSpawn(barricadeDrop!, barricade, asset.GUID);
                                        player.SendChat("vehiclebay_spawn_registered", asset.vehicleName);
                                    }
                                    else
                                        player.SendChat("vehiclebay_e_spawnexist", vehicleID.ToString(Data.Locale));
                                }
                                else
                                    player.SendChat("vehiclebay_e_idnotfound", vehicleID.ToString(Data.Locale));
                            }
                            else
                                player.SendChat("vehiclebay_e_invalidid", ID);
                        }
                        else
                            player.SendChat("correct_usage", "/vehiclebay <register|deregister> <vehicle ID>");
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
                                    player.SendChat("vehiclebay_link_started");
                                }
                                else
                                {
                                    L.Log("Couldn't get sign from " + barricade.barricade.asset.GUID);
                                    player.SendChat("vehiclebay_e_spawnnoexist");
                                }
                            }
                        }
                        else
                        if (op == "deregister" || op == "dereg" || op == "unregister" || op == "unreg")
                        {
                            if (VehicleSpawner.IsRegistered(barricade.instanceID, out _, EStructType.BARRICADE))
                            {
                                VehicleSpawner.DeleteSpawn(barricade.instanceID, EStructType.BARRICADE);
                                player.SendChat("vehiclebay_spawn_deregistered");
                            }
                            else
                                player.SendChat("vehiclebay_e_spawnnoexist");
                        }
                        else if (op == "force" || op == "respawn")
                        {
                            if (VehicleSpawner.IsRegistered(barricadeDrop!.instanceID, out VehicleSpawn spawn, EStructType.BARRICADE))
                            {
                                VehicleAsset? asset;
                                if (spawn.HasLinkedVehicle(out InteractableVehicle veh))
                                {
                                    veh.forceRemoveAllPlayers();
                                    VehicleBay.DeleteVehicle(veh);
                                    asset = veh.asset;
                                }
                                else
                                {
                                    asset = Assets.find(spawn.VehicleID) as VehicleAsset;
                                }
                                spawn.SpawnVehicle();
                                player.SendChat("vehiclebay_spawn_forced", asset == null || asset.vehicleName == null ? spawn.VehicleID.ToString("N") : asset.vehicleName);
                            }
                            else
                                player.SendChat("vehiclebay_e_spawnnoexist");
                        }
                        else if (op == "check")
                        {
                            if (VehicleSpawner.IsRegistered(barricade.instanceID, out VehicleSpawn spawn, EStructType.BARRICADE))
                            {
                                if (Assets.find(spawn.VehicleID) is VehicleAsset asset)
                                    player.SendChat("vehiclebay_check_registered", spawn.SpawnPadInstanceID.ToString(Data.Locale), asset.vehicleName, asset.id.ToString(Data.Locale) + " (" + spawn.VehicleID.ToString("N") + ")");
                                else
                                    player.SendChat("vehiclebay_e_idnotfound", spawn.VehicleID.ToString("N"));
                            }
                            else
                                player.SendChat("vehiclebay_check_notregistered");
                        }
                        else
                            player.SendChat("correct_usage", "/vehiclebay <register|deregister|force|check> <vehicle ID>");
                    }
                    else
                        player.SendChat("correct_usage", "/vehiclebay <register|deregister|force|check> <vehicle ID>");
                }
                else
                {
                    string l = command[0].ToLower();
                    if (command.Length > 0 && (l == "link" || l == "set"))
                    {
                        if (player.Player.TryGetPlaytimeComponent(out Components.PlaytimeComponent c))
                        {
                            if (barricadeDrop!.model.TryGetComponent(out InteractableSign sign)) // request sign interaction
                            {
                                if (l == "link")
                                {
                                    if (c.currentlylinking != null)
                                    {
                                        if (VehicleSigns.SignExists(sign, out _))
                                        {
                                            VehicleSigns.UnlinkSign(sign);
                                        }
                                        VehicleSigns.LinkSign(sign, c.currentlylinking);
                                        player.SendChat("vehiclebay_link_finished");
                                        c.currentlylinking = null;
                                    }
                                    else player.SendChat("vehiclebay_link_not_started");
                                }
                                else if (l == "set")
                                {
                                    if (VehicleSigns.SignExists(sign, out VehicleSign sign2))
                                    {
                                        if (VehicleSpawner.SpawnExists(sign2.bay_instance_id, sign2.bay_type, out VehicleSpawn spawn))
                                        {
                                            string op = command[0].ToLower();
                                            string property = command[1];
                                            string newValue = command[2];

                                            if (op == "set" || op == "s")
                                            {
                                                VehicleBay.SetProperty(x => x.VehicleID == spawn.VehicleID, property, newValue, out bool founditem, out bool set, out bool parsed, out bool foundproperty, out bool allowedToChange);
                                                if (!founditem) // error - kit does not exist
                                                {
                                                    player.SendChat("vehiclebay_e_noexist");
                                                }
                                                else if (!foundproperty) // error - invalid property name
                                                {
                                                    player.SendChat("vehiclebay_e_invalidprop", property);
                                                }
                                                else if (!parsed) // error - invalid argument value
                                                {
                                                    player.SendChat("vehiclebay_e_invalidarg", newValue, property);
                                                }
                                                else if (!allowedToChange) // error - invalid argument value
                                                {
                                                    player.SendChat("vehiclebay_e_not_settable", property);
                                                }
                                                else if (!set) // error - invalid argument value
                                                {
                                                    player.SendChat("vehiclebay_e_noexist");
                                                }
                                                else
                                                {
                                                    player.SendChat("vehiclebay_setprop", property.ToUpper(), !(Assets.find(spawn.VehicleID) is VehicleAsset asset) || asset.vehicleName == null ? spawn.VehicleID.ToString("N") : asset.vehicleName, newValue.ToUpper());
                                                    VehicleSpawner.UpdateSigns(spawn.VehicleID);
                                                }
                                            }
                                        }
                                        else player.SendChat("vehiclebay_e_novehicle");
                                    }
                                    else player.SendChat("vehiclebay_e_novehicle");
                                }
                                else player.SendChat("vehiclebay_e_novehicle");
                            }
                            else player.SendChat("vehiclebay_e_novehicle");
                        }
                        else player.SendChat("vehiclebay_e_novehicle");
                    }
                    else player.SendChat("vehiclebay_e_novehicle");
                }
            }
            else // check for structure
            {
                SDG.Unturned.StructureData? structure = UCBarricadeManager.GetStructureDataFromLook(player, out StructureDrop? structureDrop);
                if (structure != default)
                {
                    if (structure.structure.asset.GUID == Gamemode.Config.Barricades.VehicleBayGUID)
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
                                            VehicleSpawner.CreateSpawn(structureDrop!, structure, asset.GUID);
                                            player.SendChat("vehiclebay_spawn_registered", asset.vehicleName);
                                        }
                                        else
                                            player.SendChat("vehiclebay_e_spawnexist", vehicleID.ToString(Data.Locale));
                                    }
                                    else
                                        player.SendChat("vehiclebay_e_idnotfound", vehicleID.ToString(Data.Locale));
                                }
                                else
                                    player.SendChat("vehiclebay_e_invalidid", ID);
                            }
                            else
                                player.SendChat("correct_usage", "/vehiclebay <register|deregister> <vehicle ID>");
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
                                        player.SendChat("vehiclebay_link_started");
                                    }
                                    else
                                    {
                                        player.SendChat("vehiclebay_e_spawnnoexist");
                                    }
                                }
                            }
                            else if (op == "deregister" || op == "dereg")
                            {
                                if (VehicleSpawner.IsRegistered(structureDrop!.instanceID, out _, EStructType.STRUCTURE))
                                {
                                    VehicleSpawner.DeleteSpawn(structureDrop.instanceID, EStructType.STRUCTURE);
                                    player.SendChat("vehiclebay_spawn_deregistered");
                                }
                                else
                                    player.SendChat("vehiclebay_e_spawnnoexist");
                            }
                            else if (op == "force")
                            {
                                if (VehicleSpawner.IsRegistered(structureDrop!.instanceID, out VehicleSpawn spawn, EStructType.STRUCTURE))
                                {
                                    VehicleAsset? asset;
                                    if (spawn.HasLinkedVehicle(out InteractableVehicle veh))
                                    {
                                        veh.forceRemoveAllPlayers();
                                        VehicleBay.DeleteVehicle(veh);
                                        asset = veh.asset;
                                    }
                                    else
                                    {
                                        asset = Assets.find(spawn.VehicleID) as VehicleAsset;
                                    }
                                    spawn.SpawnVehicle();
                                    player.SendChat("vehiclebay_spawn_forced", asset == null || asset.vehicleName == null ? spawn.VehicleID.ToString("N") : asset.vehicleName);
                                }
                                else
                                    player.SendChat("vehiclebay_e_spawnnoexist");
                            }
                            else if (op == "check")
                            {
                                if (VehicleSpawner.IsRegistered(structureDrop!.instanceID, out VehicleSpawn spawn, EStructType.STRUCTURE))
                                {
                                    if (Assets.find(spawn.VehicleID) is VehicleAsset asset)
                                        player.SendChat("vehiclebay_check_registered", spawn.SpawnPadInstanceID.ToString(Data.Locale), asset.vehicleName, asset.id.ToString(Data.Locale) + " (" + spawn.VehicleID.ToString("N") + ")");
                                    else
                                        player.SendChat("vehiclebay_e_idnotfound", spawn.VehicleID.ToString("N"));
                                }
                                else
                                    player.SendChat("vehiclebay_check_notregistered");
                            }
                            else
                                player.SendChat("correct_usage", "/vehiclebay <register|deregister> <vehicle ID>");
                        }
                        else
                            player.SendChat("correct_usage", "/vehiclebay <register|deregister> <vehicle ID>");
                    }
                    else
                        player.SendChat("vehiclebay_e_novehicle");
                }
                else
                    player.SendChat("vehiclebay_e_novehicle");
            }
        }
    }
}
