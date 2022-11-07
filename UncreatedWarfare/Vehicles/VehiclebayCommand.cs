using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Vehicles;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;
using VehicleSpawn = Uncreated.Warfare.Vehicles.VehicleSpawn;

namespace Uncreated.Warfare.Commands;
public class VehicleBayCommand : Command
{
    private const string SYNTAX = "/vehiclebay";
    private const string HELP = "Sets up the vehicle bay.";

    public VehicleBayCommand() : base("vehiclebay", EAdminType.STAFF)
    {
        AddAlias("vb");
    }

    public override void Execute(CommandInteraction ctx)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertGamemode<IVehicles>();

        ctx.AssertRanByPlayer();

        if (!VehicleSpawner.Loaded || !VehicleBay.Loaded)
        {
            ctx.SendGamemodeError();
            return;
        }

        ctx.AssertHelpCheck(0, "/vehiclebay <help|add|remove|savemeta|set|delay|crewseats|register|deregister|force|link|unlink|check> [help|parameters...] - Manage vehicle spawners, signs, and the vehicle bay.");

        if (ctx.MatchParameter(0, "delay"))
        {
            ctx.AssertPermissions(EAdminType.MODERATOR);

            ctx.AssertHelpCheck(1, "/vehiclebay delay <add|remove> <all|time|flag|percent|staging|none> [value] [!][gamemode] - Modify request delays of vehicle.");

            VehicleData? data = GetVehicleTarget(ctx);
            if (data is not null)
            {
                bool adding;
                if (ctx.MatchParameter(1, "add", "a", "new"))
                    adding = true;
                else if (ctx.MatchParameter(1, "remove", "r", "rem"))
                    adding = false;
                else
                {
                    ctx.SendCorrectUsage("/vehiclebay delay <add|remove> <all|time|flag|percent|staging|none> [value] [!][gamemode]");
                    return;
                }
                if (ctx.MatchParameter(2, "all", "clear"))
                {
                    if (adding)
                    {
                        ctx.SendCorrectUsage("/vehiclebay delay add <time|flag|percent|staging|none> [value] [!][gamemode]");
                        return;
                    }
                    int rem = data.Delays.Length;
                    if (rem > 0)
                    {
                        data.Delays = new Delay[0];
                        VehicleSpawner.UpdateSigns(data.VehicleID);
                        VehicleBay.SaveSingleton();
                    }
                    ctx.Reply(T.VehicleBayRemovedDelay, rem);
                    return;
                }
                EDelayType type;
                if (ctx.MatchParameter(2, "time"))
                    type = EDelayType.TIME;
                else if (ctx.MatchParameter(2, "flag", "objective", "objectives"))
                    type = EDelayType.FLAG;
                else if (ctx.MatchParameter(2, "flagpercent", "percent"))
                    type = EDelayType.FLAG_PERCENT;
                else if (ctx.MatchParameter(2, "staging", "prep"))
                    type = EDelayType.OUT_OF_STAGING;
                else if (ctx.MatchParameter(2, "none"))
                    type = EDelayType.NONE;
                else
                {
                    if (adding)
                        ctx.SendCorrectUsage("/vehiclebay delay add <time|flag|percent|staging|none> [value] [!][gamemode]");
                    else
                        ctx.SendCorrectUsage("/vehiclebay delay remove <all|time|flag|percent|staging|none> [value] [!][gamemode]");
                    return;
                }
                if (type == EDelayType.NONE && ctx.ArgumentCount < 4)
                {
                    if (adding)
                        ctx.SendCorrectUsage("/vehiclebay delay add none [!]<gamemode>");
                    else
                        ctx.SendCorrectUsage("/vehiclebay delay remove none [!]<gamemode>");
                    return;
                }
                string? gamemode = null;
                if (type == EDelayType.OUT_OF_STAGING || type == EDelayType.NONE)
                {
                    if (ctx.HasArg(3))
                        gamemode = ctx.Get(3)!;
                }
                else if (ctx.ArgumentCount < 4)
                {
                    ctx.SendCorrectUsage("/vehiclebay delay " + ctx.Get(1)!.ToLower() + " " + ctx.Get(1)!.ToLower() + " <value> [gamemode]");
                    return;
                }
                else if (ctx.ArgumentCount > 4)
                    gamemode = ctx.Get(4)!;

                if (string.IsNullOrEmpty(gamemode) && type == EDelayType.NONE)
                {
                    gamemode = "<";
                    foreach (KeyValuePair<string, Type> gm in Gamemode.Gamemodes)
                    {
                        if (gamemode.Length != 1) gamemode += "|";
                        gamemode += gm.Key;
                    }
                    gamemode += ">";
                    if (adding)
                        ctx.SendCorrectUsage("/vehiclebay delay add none [!]" + gamemode);
                    else
                        ctx.SendCorrectUsage("/vehiclebay delay remove none [!]" + gamemode);
                    return;
                }
                if (!string.IsNullOrEmpty(gamemode))
                {
                    string? gm = null;
                    foreach (KeyValuePair<string, Type> gm2 in Gamemode.Gamemodes)
                    {
                        if (gm2.Key.Equals(gamemode, StringComparison.OrdinalIgnoreCase))
                        {
                            gm = gm2.Key;
                            break;
                        }
                    }
                    if (string.IsNullOrEmpty(gm))
                    {
                        gamemode = "<";
                        foreach (KeyValuePair<string, Type> gm2 in Gamemode.Gamemodes)
                        {
                            if (gamemode.Length != 1) gamemode += "|";
                            gamemode += gm2.Key;
                        }
                        gamemode += ">";
                        if (adding)
                            ctx.SendCorrectUsage("/vehiclebay delay add <type> [value] [!]" + gamemode);
                        else
                            ctx.SendCorrectUsage("/vehiclebay delay remove <type> [value] [!]" + gamemode);
                        return;
                    }
                    else gamemode = gm;
                }

                float val = 0;
                if (type != EDelayType.OUT_OF_STAGING && type != EDelayType.NONE && !ctx.TryGet(3, out val))
                {
                    ctx.SendCorrectUsage("/vehiclebay delay " + (adding ? "add" : "remove") + " " + ctx.Get(2)! + " <value (float)>" + (string.IsNullOrEmpty(gamemode) ? string.Empty : " [!]" + gamemode));
                    return;
                }

                if (adding)
                {
                    ctx.LogAction(EActionLogType.SET_VEHICLE_DATA_PROPERTY, "ADDED DELAY " + type.ToString() + " VALUE: " + val.ToString()
                        + " GAMEMODE?: " + (gamemode == null ? "ANY" : gamemode.ToUpper()));
                    Delay.AddDelay(ref data.Delays, type, val, gamemode);
                    VehicleBay.SaveSingleton();
                    if (VehicleSigns.Loaded)
                        VehicleSpawner.UpdateSigns(data.VehicleID);
                    foreach (VehicleSpawn spawn in data.EnumerateSpawns)
                    {
                        VehicleBayComponent? svc = spawn.Component;
                        if (svc != null) svc.UpdateTimeDelay();
                    }
                    ctx.Reply(T.VehicleBayAddedDelay, type, val, string.IsNullOrEmpty(gamemode) ? "any" : gamemode!);
                }
                else
                {
                    int rem = 0;
                    while (Delay.RemoveDelay(ref data.Delays, type, val, gamemode)) rem++;
                    if (rem > 0)
                    {
                        if (VehicleSigns.Loaded)
                            VehicleSpawner.UpdateSigns(data.VehicleID);
                        VehicleBay.SaveSingleton();
                        ctx.LogAction(EActionLogType.SET_VEHICLE_DATA_PROPERTY, "REMOVED " + rem.ToString(Data.Locale) + " DELAY(S) " + type.ToString() + " VALUE: " + val.ToString()
                            + " GAMEMODE?: " + (gamemode == null ? "ANY" : gamemode.ToUpper()));
                    }
                    ctx.Reply(T.VehicleBayRemovedDelay, rem);
                }
            }
            else
                ctx.Reply(T.VehicleBayNoTarget);
        }
        else if (ctx.MatchParameter(0, "add", "a", "create"))
        {
            ctx.AssertPermissions(EAdminType.MODERATOR);

            ctx.AssertHelpCheck(1, "/vehiclebay <add|a|create> - Adds the vehicle you're looking at to the vehicle bay.");

            if (ctx.TryGetTarget(out InteractableVehicle vehicle))
            {
                if (!VehicleBay.VehicleExists(vehicle.asset.GUID, out _))
                {
                    ctx.LogAction(EActionLogType.CREATE_VEHICLE_DATA, $"{vehicle.asset.vehicleName} / {vehicle.asset.id} / {vehicle.asset.GUID:N}");
                    VehicleBay.AddRequestableVehicle(vehicle);
                    ctx.Reply(T.VehicleBayAdded, vehicle.asset);
                }
                else
                    ctx.Reply(T.VehicleBayAlreadyAdded, vehicle.asset);
            }
            else
                ctx.Reply(T.VehicleBayNoTarget);
        }
        else if (ctx.MatchParameter(0, "remove", "r", "delete"))
        {
            ctx.AssertPermissions(EAdminType.MODERATOR);

            ctx.AssertHelpCheck(1, "/vehiclebay <remove|r|delete> - Removes the vehicle you're looking at from the vehicle bay.");

            if (ctx.TryGetTarget(out InteractableVehicle vehicle))
            {
                if (VehicleBay.VehicleExists(vehicle.asset.GUID, out _))
                {
                    ctx.LogAction(EActionLogType.DELETE_VEHICLE_DATA, $"{vehicle.asset.vehicleName} / {vehicle.asset.id} / {vehicle.asset.GUID:N}");
                    VehicleBay.RemoveRequestableVehicle(vehicle.asset.GUID);
                    ctx.Reply(T.VehicleBayRemoved, vehicle.asset);
                }
                else
                    ctx.Reply(T.VehicleBayNotAdded, vehicle.asset);
            }
            else
                ctx.Reply(T.VehicleBayNoTarget);
        }
        else if (ctx.MatchParameter(0, "savemeta", "savemetadata", "metadata"))
        {
            ctx.AssertPermissions(EAdminType.MODERATOR);

            ctx.AssertHelpCheck(1, "/vehiclebay <savemeta|savemetadata|metadata> - Saves the barricades that are placed on the current vehicle to the vehicle bay.");

            if (ctx.TryGetTarget(out InteractableVehicle vehicle))
            {
                if (VehicleBay.VehicleExists(vehicle.asset.GUID, out VehicleData data))
                {
                    ctx.LogAction(EActionLogType.SET_VEHICLE_DATA_PROPERTY, $"{vehicle.asset.vehicleName} / {vehicle.asset.id} / {vehicle.asset.GUID:N} - SAVED METADATA");
                    data.SaveMetaData(vehicle);
                    VehicleBay.SaveSingleton();
                    ctx.Reply(T.VehicleBaySavedMeta, vehicle.asset);
                }
                else
                    ctx.Reply(T.VehicleBayNotAdded);
            }
            else
                ctx.Reply(T.VehicleBayNoTarget);
        }
        else if (ctx.MatchParameter(0, "set", "s"))
        {
            ctx.AssertPermissions(EAdminType.MODERATOR);

            ctx.AssertHelpCheck(1, "/vehiclebay <set|s> <items|property> [value] - Sets the trunk items to your current inventory or any other property to the given value.");

            if (ctx.MatchParameter(1, "items", "item", "inventory"))
            {
                VehicleData? data = GetVehicleTarget(ctx);
                if (data is not null)
                {
                    List<Guid> items = new List<Guid>();

                    for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
                    {
                        if (page == PlayerInventory.AREA)
                            continue;

                        byte pageCount = ctx.Caller!.Player.inventory.getItemCount(page);

                        for (byte index = 0; index < pageCount; index++)
                        {
                            if (Assets.find(EAssetType.ITEM, ctx.Caller!.Player.inventory.getItem(page, index).item.id) is ItemAsset a)
                                items.Add(a.GUID);
                        }
                    }

                    VehicleAsset? asset = Assets.find<VehicleAsset>(data.VehicleID);
                    ctx.LogAction(EActionLogType.SET_VEHICLE_DATA_PROPERTY, $"{asset?.vehicleName ?? "null"} / {(asset == null ? "0" : asset.id.ToString(Data.Locale))} / {data.VehicleID:N} - SET ITEMS");
                    VehicleBay.SetItems(data.VehicleID, items.ToArray());

                    if (items.Count == 0)
                        ctx.Reply(T.VehicleBayClearedItems, asset!);
                    else
                        ctx.Reply(T.VehicleBaySetItems, asset!, items.Count);
                }
                else
                    ctx.Reply(T.VehicleBayNoTarget);
            }
            else if (ctx.TryGet(2, out string value) && ctx.TryGet(1, out string property))
            {
                VehicleData? data = GetVehicleTarget(ctx);
                if (data is not null)
                {
                    ESetFieldResult result = VehicleBay.SetProperty(data, ref property, value);
                    switch (result)
                    {
                        case ESetFieldResult.SUCCESS:
                            VehicleAsset? asset = Assets.find<VehicleAsset>(data.VehicleID);
                            ctx.LogAction(EActionLogType.SET_VEHICLE_DATA_PROPERTY, $"{asset?.vehicleName ?? "null"} / {(asset == null ? 0 : asset.id)} / {data.VehicleID:N} - SET " + property.ToUpper() + " >> " + value.ToUpper());
                            ctx.Reply(T.VehicleBaySetProperty!, property, asset, value);
                            VehicleSpawner.UpdateSigns(data.VehicleID);
                            VehicleBay.SaveSingleton();
                            break;
                        default:
                        case ESetFieldResult.OBJECT_NOT_FOUND:
                            ctx.Reply(T.VehicleBayNotAdded);
                            break;
                        case ESetFieldResult.FIELD_NOT_FOUND:
                            ctx.Reply(T.VehicleBayInvalidProperty, property);
                            break;
                        case ESetFieldResult.FIELD_NOT_SERIALIZABLE:
                        case ESetFieldResult.INVALID_INPUT:
                            ctx.Reply(T.VehicleBayInvalidSetValue, value, property);
                            break;
                        case ESetFieldResult.FIELD_PROTECTED:
                            ctx.Reply(T.VehicleBayNotJsonSettable, property);
                            break;
                    }
                }
                else
                    ctx.Reply(T.VehicleBayNoTarget);
            }
            else
                ctx.SendCorrectUsage("/vehiclebay set <items|property> [value]");
        }
        else if (ctx.MatchParameter(0, "crewseats", "seats", "crew"))
        {
            ctx.AssertPermissions(EAdminType.MODERATOR);

            ctx.AssertHelpCheck(1, "/vehiclebay <crewseats|seats|crew> <add|remove> <seat index> - Registers or deregisters a seat index as requiring crewman to enter.");

            if (ctx.MatchParameter(1, "add", "a", "create"))
            {
                VehicleData? data = GetVehicleTarget(ctx);
                if (data is not null)
                {
                    if (ctx.TryGet(2, out byte seat))
                    {
                        VehicleAsset? asset = Assets.find<VehicleAsset>(data.VehicleID);
                        if (!data.CrewSeats.Contains(seat))
                        {
                            ctx.LogAction(EActionLogType.SET_VEHICLE_DATA_PROPERTY, $"{asset?.vehicleName ?? "null"} / {(asset == null ? "null" : asset.id.ToString(Data.Locale))} / {data.VehicleID:N} - ADDED CREW SEAT {seat}.");
                            VehicleBay.AddCrewmanSeat(data.VehicleID, seat);
                            ctx.Reply(T.VehicleBaySeatAdded, seat, asset!);
                        }
                        else
                            ctx.Reply(T.VehicleBaySeatAlreadyAdded, seat, asset!);
                    }
                    else
                        ctx.SendCorrectUsage("/vehiclebay crewseats add <seat index>");
                }
                else
                    ctx.Reply(T.VehicleBayNoTarget);
            }
            else if (ctx.MatchParameter(1, "remove", "r", "delete"))
            {
                VehicleData? data = GetVehicleTarget(ctx);
                if (data is not null)
                {
                    if (ctx.TryGet(2, out byte seat))
                    {
                        VehicleAsset? asset = Assets.find<VehicleAsset>(data.VehicleID);
                        if (data.CrewSeats.Contains(seat))
                        {
                            ctx.LogAction(EActionLogType.SET_VEHICLE_DATA_PROPERTY, $"{asset?.vehicleName ?? "null"} / {(asset == null ? "null" : asset.id.ToString(Data.Locale))} / {data.VehicleID:N} - REMOVED CREW SEAT {seat}.");
                            VehicleBay.RemoveCrewmanSeat(data.VehicleID, seat);
                            ctx.Reply(T.VehicleBaySeatRemoved, seat, asset!);
                        }
                        else
                            ctx.Reply(T.VehicleBaySeatNotAdded, seat, asset!);
                    }
                    else
                        ctx.SendCorrectUsage("/vehiclebay crewseats remove <seat index>");
                }
                else
                    ctx.Reply(T.VehicleBayNoTarget);
            }
            else
                ctx.SendCorrectUsage("/vehiclebay crewseats <add|remove> <seat index>");
        }
        else if (ctx.MatchParameter(0, "register", "reg"))
        {
            ctx.AssertPermissions(EAdminType.MODERATOR);

            ctx.AssertHelpCheck(1, "/vehiclebay <register|reg> <vehicle id> - Sets the vehicle spawner you're looking at to spawn the given vehicle id (guid or uint16).");

            VehicleAsset? asset;
            if (ctx.TryGet(1, out ushort id))
            {
                asset = Assets.find(EAssetType.VEHICLE, id) as VehicleAsset;
            }
            else if (ctx.TryGet(1, out Guid guid))
            {
                asset = Assets.find(guid) as VehicleAsset;
            }
            else
            {
                if (ctx.HasArg(1))
                    ctx.Reply(T.VehicleBayInvalidInput, ctx.Get(0)!);
                else
                    ctx.SendCorrectUsage("/vehiclebay register <vehicle id or guid>");
                return;
            }
            if (asset is not null)
            {
                if (ctx.TryGetTarget(out BarricadeDrop barricade))
                {
                    if (Gamemode.Config.StructureVehicleBay.MatchGuid(barricade.asset.GUID))
                    {
                        if (!VehicleSpawner.IsRegistered(barricade.instanceID, out _, EStructType.BARRICADE))
                        {
                            ctx.LogAction(EActionLogType.REGISTERED_SPAWN, $"{asset.vehicleName} / {asset.id} / {asset.GUID:N} - " +
                                $"REGISTERED BARRICADE SPAWN AT {barricade.model.transform.position:N2} ID: {barricade.instanceID}");
                            VehicleSpawner.CreateSpawn(barricade, barricade.GetServersideData(), asset.GUID);
                            ctx.Reply(T.VehicleBaySpawnRegistered, asset);
                        }
                        else
                            ctx.Reply(T.VehicleBaySpawnAlreadyRegistered, asset);
                    }
                    else
                    {
                        ctx.Reply(T.VehicleBayInvalidBayItem, barricade.asset);
                    }
                }
                else if (ctx.TryGetTarget(out StructureDrop structure))
                {
                    if (Gamemode.Config.StructureVehicleBay.MatchGuid(structure.asset.GUID))
                    {
                        if (!VehicleSpawner.IsRegistered(structure.instanceID, out _, EStructType.STRUCTURE))
                        {
                            ctx.LogAction(EActionLogType.REGISTERED_SPAWN, $"{asset.vehicleName} / {asset.id} / {asset.GUID:N} - " +
                                $"REGISTERED STRUCTURE SPAWN AT {structure.model.transform.position:N2} ID: {structure.instanceID}");
                            VehicleSpawner.CreateSpawn(structure, structure.GetServersideData(), asset.GUID);
                            ctx.Reply(T.VehicleBaySpawnRegistered, asset);
                        }
                        else
                            ctx.Reply(T.VehicleBaySpawnAlreadyRegistered, asset);
                    }
                    else
                    {
                        ctx.Reply(T.VehicleBayInvalidBayItem, structure.asset);
                    }
                }
                else
                    ctx.Reply(T.VehicleBayNoTarget);
            }
            else
                ctx.Reply(T.VehicleBayInvalidInput, ctx.Get(1)!);
        }
        else if (ctx.MatchParameter(0, "deregister", "dereg"))
        {
            ctx.AssertPermissions(EAdminType.MODERATOR);

            ctx.AssertHelpCheck(1, "/vehiclebay <deregister|dereg> - Disables the vehicle spawner you're looking at from spawning vehicles.");

            VehicleSpawn? spawn = GetBayTarget(ctx);
            if (spawn is not null)
            {
                VehicleSpawner.DeleteSpawn(spawn.InstanceId, spawn.StructureType);
                VehicleAsset? asset = Assets.find<VehicleAsset>(spawn.VehicleGuid);
                if (asset is not null)
                    ctx.LogAction(EActionLogType.DEREGISTERED_SPAWN, $"{asset.vehicleName} / {asset.id} / {asset.GUID:N} - " +
                                                               $"DEREGISTERED SPAWN ID: {spawn.InstanceId}");
                else
                    ctx.LogAction(EActionLogType.DEREGISTERED_SPAWN, $"{spawn.VehicleGuid:N} - " +
                        $"DEREGISTERED SPAWN ID: {spawn.InstanceId}");
                ctx.Reply(T.VehicleBaySpawnDeregistered, asset!);
            }
            else if (ctx.TryGetTarget(out BarricadeDrop _) || ctx.TryGetTarget(out StructureDrop _))
                ctx.Reply(T.VehicleBaySpawnNotRegistered);
            else
                ctx.Reply(T.VehicleBayNoTarget);
        }
        else if (ctx.MatchParameter(0, "force", "respawn"))
        {
            ctx.AssertPermissions(EAdminType.MODERATOR);

            ctx.AssertHelpCheck(1, "/vehiclebay <force|respawn> - Deletes the linked vehicle to the spawner you're looking at from the world and spawns another.");

            VehicleSpawn? spawn = GetBayTarget(ctx);
            if (spawn is not null)
            {
                VehicleAsset? asset;
                if (spawn.HasLinkedVehicle(out InteractableVehicle veh))
                {
                    VehicleBay.DeleteVehicle(veh);
                    asset = veh.asset;
                }
                else
                {
                    asset = Assets.find(spawn.VehicleGuid) as VehicleAsset;
                }
                if (asset != null)
                    ctx.LogAction(EActionLogType.VEHICLE_BAY_FORCE_SPAWN, $"{asset.vehicleName} / {asset.id} / {asset.GUID:N} - " +
                                                               $"FORCED VEHICLE TO SPAWN ID: {spawn.InstanceId}");
                else
                    ctx.LogAction(EActionLogType.VEHICLE_BAY_FORCE_SPAWN, $"{spawn.VehicleGuid:N} - FORCED VEHICLE TO SPAWN ID: {spawn.InstanceId}");
                spawn.SpawnVehicle();
                ctx.Reply(T.VehicleBayForceSuccess, asset!);
            }
            else
                ctx.Reply(T.VehicleBayNoTarget);
        }
        else if (ctx.MatchParameter(0, "link"))
        {
            ctx.AssertHelpCheck(1, "/vehiclebay <link> - Use while looking at a spawner to start linking, then on a sign to link the sign to the spawner.");

            if (!VehicleSigns.Loaded)
            {
                ctx.SendGamemodeError();
                return;
            }
            if (ctx.TryGetTarget(out BarricadeDrop drop) && drop.interactable is InteractableSign sign)
            {
                if (ctx.Caller!.Player.TryGetPlayerData(out Components.UCPlayerData c))
                {
                    if (c.currentlylinking != null)
                    {
                        if (VehicleSigns.SignExists(sign, out _))
                            VehicleSigns.UnlinkSign(sign);
                        VehicleBayComponent? c2 = c.currentlylinking.Component;
                        if (c2 != null)
                        {
                            ctx.LogAction(EActionLogType.LINKED_VEHICLE_BAY_SIGN, $"{drop.asset.itemName} / {drop.asset.id} / {drop.asset.GUID:N} ID: {drop.instanceID}- " +
                                $"LINKED TO SPAWN AT {c2.transform.position:N2} ID: {c.currentlylinking.InstanceId}");
                        }
                        else
                        {
                            ctx.LogAction(EActionLogType.LINKED_VEHICLE_BAY_SIGN, $"{drop.asset.itemName} / {drop.asset.id} / {drop.asset.GUID:N} ID: {drop.instanceID}- " +
                                $"LINKED TO SPAWN ID: {c.currentlylinking.InstanceId}");
                        }
                        VehicleSigns.LinkSign(sign, c.currentlylinking);
                        ctx.Reply(T.VehicleBayLinkFinished,
                            (c2 == null
                                ? null
                                : (c2.Spawn is null
                                    ? null
                                    : Assets.find<VehicleAsset>(c2.Spawn.VehicleGuid)))!);
                        c.currentlylinking = null;
                    }
                    else
                        ctx.Reply(T.VehicleBayLinkNotStarted);
                }
                else
                    ctx.SendUnknownError();
            }
            else
            {
                VehicleSpawn? spawn = GetBayTarget(ctx);
                if (spawn is not null)
                {
                    if (ctx.Caller!.Player.TryGetPlayerData(out Components.UCPlayerData c))
                    {
                        c.currentlylinking = spawn;
                        ctx.Reply(T.VehicleBayLinkStarted);
                    }
                    else
                        ctx.SendUnknownError();
                }
                else
                    ctx.Reply(T.VehicleBaySpawnNotRegistered);
            }
        }
        else if (ctx.MatchParameter(0, "unlink"))
        {
            ctx.AssertHelpCheck(1, "/vehiclebay <unlink> - Use while looking at a sign to unlink it from it's spawner.");

            if (!VehicleSigns.Loaded)
            {
                ctx.SendGamemodeError();
                return;
            }
            VehicleSign? sign = GetSignTarget(ctx);
            if (sign is not null && sign.SignDrop is not null && sign.SignDrop.interactable is InteractableSign sign2)
            {
                VehicleSigns.UnlinkSign(sign2);
                ctx.Reply(T.VehicleBayUnlinked, (sign.VehicleBay is null ? null : Assets.find<VehicleAsset>(sign.VehicleBay.VehicleGuid))!);
            }
            else
                ctx.Reply(T.VehicleBayNoTarget);
        }
        else if (ctx.MatchParameter(0, "check", "id", "wtf"))
        {
            ctx.AssertHelpCheck(1, "/vehiclebay <check|id|wtf> - Tells you what vehicle spawns from the spawner you're looking at.");

            VehicleSpawn? spawn = GetBayTarget(ctx);
            if (spawn is not null)
            {
                VehicleAsset? asset = Assets.find<VehicleAsset>(spawn.VehicleGuid);
                ctx.Reply(T.VehicleBayCheck, spawn.InstanceId, asset!, asset == null ? (ushort)0 : asset.id);
            }
            else
                ctx.Reply(T.VehicleBaySpawnNotRegistered);
        }
        else ctx.SendCorrectUsage("/vehiclebay <help|add|remove|savemeta|set|delay|crewseats|register|deregister|force|link|unlink|check> [help|parameters...]");
    }

    /// <summary>Linked vehicle >> Sign barricade >> Spawner barricade >> Spawner structure</summary>
    private VehicleData? GetVehicleTarget(CommandInteraction ctx)
    {
        if (ctx.TryGetTarget(out InteractableVehicle vehicle) && VehicleBay.VehicleExists(vehicle.asset.GUID, out VehicleData data))
        {
            return data;
        }
        StructureDrop drop2;
        VehicleSpawn spawn;
        if (ctx.TryGetTarget(out BarricadeDrop drop))
        {
            if (VehicleSigns.Loaded)
            {
                if (drop.interactable is InteractableSign sign)
                {
                    if (VehicleSigns.SignExists(sign, out VehicleSign sign2))
                    {
                        if (sign2.VehicleBay is not null && VehicleBay.VehicleExists(sign2.VehicleBay.VehicleGuid, out data))
                        {
                            return data;
                        }
                    }
                }
            }
            if ((VehicleSpawner.SpawnExists(drop.instanceID, EStructType.BARRICADE, out spawn) &&
                VehicleBay.VehicleExists(spawn.VehicleGuid, out data)) ||
                (ctx.TryGetTarget(out drop2) && VehicleSpawner.SpawnExists(drop2.instanceID, EStructType.STRUCTURE, out spawn) &&
                VehicleBay.VehicleExists(spawn.VehicleGuid, out data)))
                return data;
            else return null;
        }
        if (ctx.TryGetTarget(out drop2))
        {
            return VehicleSpawner.SpawnExists(drop2.instanceID, EStructType.STRUCTURE, out spawn) && VehicleBay.VehicleExists(spawn.VehicleGuid, out data) ? data : null;
        }
        return null;
    }
    /// <summary>Linked vehicle >> Sign barricade >> Spawner barricade >> Spawner structure</summary>
    private VehicleSpawn? GetBayTarget(CommandInteraction ctx)
    {
        if (ctx.TryGetTarget(out InteractableVehicle vehicle) && VehicleSpawner.HasLinkedSpawn(vehicle.instanceID, out VehicleSpawn spawn))
        {
            return spawn;
        }
        StructureDrop drop2;
        if (ctx.TryGetTarget(out BarricadeDrop drop))
        {
            if (VehicleSigns.Loaded)
            {
                if (drop.interactable is InteractableSign sign)
                {
                    if (VehicleSigns.SignExists(sign, out VehicleSign sign2))
                    {
                        if (sign2.VehicleBay is not null)
                            return sign2.VehicleBay;
                    }
                }
            }
            if (VehicleSpawner.SpawnExists(drop.instanceID, EStructType.BARRICADE, out spawn) || (ctx.TryGetTarget(out drop2) && VehicleSpawner.SpawnExists(drop2.instanceID, EStructType.STRUCTURE, out spawn)))
                return spawn;
            else return null;
        }
        if (ctx.TryGetTarget(out drop2))
        {
            return VehicleSpawner.SpawnExists(drop2.instanceID, EStructType.STRUCTURE, out spawn) ? spawn : null;
        }
        return null;
    }
    /// <summary>Sign barricade >> Spawner barricade >> Spawner structure >> Linked Vehicle</summary>
    private VehicleSign? GetSignTarget(CommandInteraction ctx)
    {
        if (!VehicleSigns.Loaded) return null;
        VehicleSpawn spawn;
        StructureDrop drop2;
        if (ctx.TryGetTarget(out BarricadeDrop drop))
        {
            if (drop.interactable is InteractableSign sign)
            {
                if (VehicleSigns.SignExists(sign, out VehicleSign sign2))
                {
                    return sign2;
                }
            }
            if (VehicleSpawner.SpawnExists(drop.instanceID, EStructType.BARRICADE, out spawn) || (ctx.TryGetTarget(out drop2) && VehicleSpawner.SpawnExists(drop2.instanceID, EStructType.STRUCTURE, out spawn)))
            {
                VehicleSign[] signs = VehicleSigns.GetLinkedSigns(spawn).ToArray();
                if (signs.Length == 1)
                    return signs[0];
            }
            return null;
        }
        if (ctx.TryGetTarget(out drop2) && VehicleSpawner.SpawnExists(drop2.instanceID, EStructType.STRUCTURE, out spawn) || (ctx.TryGetTarget(out InteractableVehicle vehicle) && VehicleSpawner.HasLinkedSpawn(vehicle.instanceID, out spawn)))
        {
            VehicleSign[] signs = VehicleSigns.GetLinkedSigns(spawn).ToArray();
            if (signs.Length == 1)
                return signs[0];
        }
        return null;
    }
}
