using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.SQL;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Vehicles;
using VehicleSpawn = Uncreated.Warfare.Vehicles.VehicleSpawn;

namespace Uncreated.Warfare.Commands;
public class VehicleBayCommand : AsyncCommand
{
    private const string SYNTAX = "/vehiclebay";
    private const string HELP = "Sets up the vehicle bay.";

    public VehicleBayCommand() : base("vehiclebay", EAdminType.STAFF)
    {
        AddAlias("vb");
    }

    public override async Task Execute(CommandInteraction ctx, CancellationToken token)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertGamemode<IVehicles>();

        ctx.AssertRanByPlayer();
        if (!VehicleSpawner.Loaded || !Data.Singletons.TryGetSingleton(out VehicleBay bay))
        {
            ctx.SendGamemodeError();
            return;
        }

        ctx.AssertHelpCheck(0, "/vehiclebay <help|add|remove|savemeta|set|delay|crewseats|register|deregister|force|link|unlink|check> [help|parameters...] - Manage vehicle spawners, signs, and the vehicle bay.");

        if (ctx.MatchParameter(0, "delay"))
        {
            ctx.AssertPermissions(EAdminType.MODERATOR);

            ctx.AssertHelpCheck(1, "/vehiclebay delay <add|remove> <all|time|flag|percent|staging|none> [value] [!][gamemode] - Modify request delays of vehicle.");

            SqlItem<VehicleData>? data = await GetVehicleTarget(ctx, bay, token).ThenToUpdate(token);
            if (data?.Item is not null)
            {
                await data.Enter(token).ThenToUpdate(token);
                try
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
                        int rem = data.Item.Delays.Length;
                        if (rem > 0)
                        {
                            data.Item.Delays = Array.Empty<Delay>();
                            await data.SaveItem(token).ThenToUpdate(token);
                            VehicleSpawner.UpdateSigns(data.Item.VehicleID);
                        }
                        ctx.Reply(T.VehicleBayRemovedDelay, rem);
                        return;
                    }
                    DelayType type;
                    if (ctx.MatchParameter(2, "time"))
                        type = DelayType.Time;
                    else if (ctx.MatchParameter(2, "flag", "objective", "objectives"))
                        type = DelayType.Flag;
                    else if (ctx.MatchParameter(2, "flagpercent", "percent"))
                        type = DelayType.FlagPercentage;
                    else if (ctx.MatchParameter(2, "staging", "prep"))
                        type = DelayType.OutOfStaging;
                    else if (ctx.MatchParameter(2, "none"))
                        type = DelayType.None;
                    else
                    {
                        if (adding)
                            ctx.SendCorrectUsage("/vehiclebay delay add <time|flag|percent|staging|none> [value] [!][gamemode]");
                        else
                            ctx.SendCorrectUsage("/vehiclebay delay remove <all|time|flag|percent|staging|none> [value] [!][gamemode]");
                        return;
                    }
                    if (type == DelayType.None && ctx.ArgumentCount < 4)
                    {
                        if (adding)
                            ctx.SendCorrectUsage("/vehiclebay delay add none [!]<gamemode>");
                        else
                            ctx.SendCorrectUsage("/vehiclebay delay remove none [!]<gamemode>");
                        return;
                    }
                    string? gamemode = null;
                    if (type == DelayType.OutOfStaging || type == DelayType.None)
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

                    if (string.IsNullOrEmpty(gamemode) && type == DelayType.None)
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
                    if (type != DelayType.OutOfStaging && type != DelayType.None && !ctx.TryGet(3, out val))
                    {
                        ctx.SendCorrectUsage("/vehiclebay delay " + (adding ? "add" : "remove") + " " + ctx.Get(2)! + " <value (float)>" + (string.IsNullOrEmpty(gamemode) ? string.Empty : " [!]" + gamemode));
                        return;
                    }

                    if (adding)
                    {
                        ctx.LogAction(EActionLogType.SET_VEHICLE_DATA_PROPERTY, "ADDED DELAY " + type.ToString() + " VALUE: " + val.ToString()
                            + " GAMEMODE?: " + (gamemode == null ? "ANY" : gamemode.ToUpper()));
                        Delay.AddDelay(ref data.Item.Delays, type, val, gamemode);
                        if (VehicleSigns.Loaded)
                            VehicleSpawner.UpdateSigns(data.Item.VehicleID);
                        foreach (VehicleSpawn spawn in data.Item.EnumerateSpawns)
                        {
                            VehicleBayComponent? svc = spawn.Component;
                            if (svc != null) svc.UpdateTimeDelay();
                        }
                        await data.SaveItem(token).ThenToUpdate(token);
                        ctx.Reply(T.VehicleBayAddedDelay, type, val, string.IsNullOrEmpty(gamemode) ? "any" : gamemode!);
                    }
                    else
                    {
                        int rem = 0;
                        while (Delay.RemoveDelay(ref data.Item.Delays, type, val, gamemode))
                            ++rem;
                        if (rem > 0)
                        {
                            if (VehicleSigns.Loaded)
                                VehicleSpawner.UpdateSigns(data.Item.VehicleID);
                            foreach (VehicleSpawn spawn in data.Item.EnumerateSpawns)
                            {
                                VehicleBayComponent? svc = spawn.Component;
                                if (svc != null) svc.UpdateTimeDelay();
                            }
                            await data.SaveItem(token).ThenToUpdate(token);
                            ctx.LogAction(EActionLogType.SET_VEHICLE_DATA_PROPERTY, "REMOVED " + rem.ToString(Data.AdminLocale) + " DELAY(S) " + type + " VALUE: " + val.ToString(Data.AdminLocale)
                                + " GAMEMODE?: " + (gamemode == null ? "ANY" : gamemode.ToUpper()));
                        }
                        ctx.Reply(T.VehicleBayRemovedDelay, rem);
                    }
                }
                finally
                {
                    data.Release();
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
                SqlItem<VehicleData>? data = await GetVehicleTarget(ctx, bay, token).ThenToUpdate(token);
                if (data?.Item == null)
                {
                    ctx.LogAction(EActionLogType.CREATE_VEHICLE_DATA, $"{vehicle.asset.vehicleName} / {vehicle.asset.id} / {vehicle.asset.GUID:N}");
                    await bay.AddRequestableVehicle(vehicle, token).ConfigureAwait(false);
                    await UCWarfare.ToUpdate(token);
                    ctx.Reply(T.VehicleBayAdded, vehicle.asset);
                }
                else
                {
                    await UCWarfare.ToUpdate(token);
                    ctx.Reply(T.VehicleBayAlreadyAdded, vehicle.asset);
                }
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
                if (await bay.RemoveRequestableVehicle(vehicle.asset.GUID, token).ThenToUpdate(token))
                {
                    ctx.LogAction(EActionLogType.DELETE_VEHICLE_DATA, $"{vehicle.asset.vehicleName} / {vehicle.asset.id} / {vehicle.asset.GUID:N}");
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
                SqlItem<VehicleData>? data = await GetVehicleTarget(ctx, bay, token).ThenToUpdate(token);
                if (data?.Item != null)
                {
                    ctx.LogAction(EActionLogType.SET_VEHICLE_DATA_PROPERTY, $"{vehicle.asset.vehicleName} / {vehicle.asset.id} / {vehicle.asset.GUID:N} - SAVED METADATA");
                    await data.Enter(token).ThenToUpdate(token);
                    try
                    {
                        data.Item.SaveMetaData(vehicle);
                        await data.SaveItem(token).ThenToUpdate(token);
                        ctx.Reply(T.VehicleBaySavedMeta, vehicle.asset);
                    }
                    finally
                    {
                        data.Release();
                    }
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

            SqlItem<VehicleData>? data = await GetVehicleTarget(ctx, bay, token).ConfigureAwait(false);
            if (data?.Item is not null)
            {
                if (ctx.MatchParameter(1, "items", "item", "inventory"))
                {
                    await data.Enter(token).ThenToUpdate(token);
                    try
                    {
                        List<Guid> items = new List<Guid>();

                        for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
                        {
                            if (page == PlayerInventory.AREA)
                                continue;

                            byte pageCount = ctx.Caller!.Player.inventory.getItemCount(page);

                            for (byte index = 0; index < pageCount; index++)
                            {
                                if (Assets.find(EAssetType.ITEM,
                                        ctx.Caller!.Player.inventory.getItem(page, index).item.id) is ItemAsset a)
                                    items.Add(a.GUID);
                            }
                        }

                        VehicleAsset? asset = Assets.find<VehicleAsset>(data.Item.VehicleID);
                        ctx.LogAction(EActionLogType.SET_VEHICLE_DATA_PROPERTY,
                            $"{asset?.vehicleName ?? "null"} / {(asset == null ? "0" : asset.id.ToString(Data.AdminLocale))} / {data.Item.VehicleID:N} - SET ITEMS");
                        data.Item.Items = items.ToArray();
                        await data.SaveItem(token).ThenToUpdate(token);
                        if (items.Count == 0)
                            ctx.Reply(T.VehicleBayClearedItems, asset!);
                        else
                            ctx.Reply(T.VehicleBaySetItems, asset!, items.Count);
                    }
                    finally
                    {
                        data.Release();
                    }
                }
                else if (ctx.TryGet(2, out string value) && ctx.TryGet(1, out string property))
                {
                    (SetPropertyResult result, MemberInfo? info) = await bay.SetProperty(data.Item, property, value, token).ThenToUpdate(token);
                    switch (result)
                    {
                        case SetPropertyResult.Success:
                            VehicleAsset? asset = Assets.find<VehicleAsset>(data.Item.VehicleID);
                            ctx.LogAction(EActionLogType.SET_VEHICLE_DATA_PROPERTY,
                                $"{asset?.vehicleName ?? "null"} / {(asset == null ? 0 : asset.id)} / {data.Item.VehicleID:N} - SET " +
                                (info?.Name ?? property).ToUpper() + " >> " + value.ToUpper());
                            ctx.Reply(T.VehicleBaySetProperty!, property, asset, value);
                            VehicleSpawner.UpdateSigns(data.Item.VehicleID);
                            break;
                        default:
                        case SetPropertyResult.ObjectNotFound:
                            ctx.Reply(T.VehicleBayNotAdded);
                            break;
                        case SetPropertyResult.PropertyNotFound:
                            ctx.Reply(T.VehicleBayInvalidProperty, property);
                            break;
                        case SetPropertyResult.ParseFailure:
                        case SetPropertyResult.TypeNotSettable:
                            ctx.Reply(T.VehicleBayInvalidSetValue, value, property);
                            break;
                        case SetPropertyResult.PropertyProtected:
                            ctx.Reply(T.VehicleBayNotCommandSettable, property);
                            break;
                    }
                }
                else
                    ctx.SendCorrectUsage("/vehiclebay set <items|property> [value]");
            }
            else
            {
                await UCWarfare.ToUpdate();
                ctx.Reply(T.VehicleBayNoTarget);
            }
        }
        else if (ctx.MatchParameter(0, "crewseats", "seats", "crew"))
        {
            ctx.AssertPermissions(EAdminType.MODERATOR);

            ctx.AssertHelpCheck(1, "/vehiclebay <crewseats|seats|crew> <add|remove> <seat index> - Registers or deregisters a seat index as requiring crewman to enter.");

            if (ctx.MatchParameter(1, "add", "a", "create"))
            {
                SqlItem<VehicleData>? data = await GetVehicleTarget(ctx, bay, token).ConfigureAwait(false);
                if (data?.Item is not null)
                {
                    if (ctx.TryGet(2, out byte seat))
                    {
                        VehicleAsset? asset = Assets.find<VehicleAsset>(data.Item.VehicleID);
                        if (!data.Item.CrewSeats.Contains(seat))
                        {
                            ctx.LogAction(EActionLogType.SET_VEHICLE_DATA_PROPERTY, $"{asset?.vehicleName ?? "null"} /" +
                                $" {(asset == null ? "null" : asset.id.ToString(Data.AdminLocale))} / {data.Item.VehicleID:N} - ADDED CREW SEAT {seat}.");
                            await bay.AddCrewSeat(data, seat, token).ThenToUpdate(token);
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
                SqlItem<VehicleData>? data = await GetVehicleTarget(ctx, bay, token).ConfigureAwait(false);
                if (data?.Item is not null)
                {
                    if (ctx.TryGet(2, out byte seat))
                    {
                        VehicleAsset? asset = Assets.find<VehicleAsset>(data.Item.VehicleID);
                        if (data.Item.CrewSeats.Contains(seat))
                        {
                            ctx.LogAction(EActionLogType.SET_VEHICLE_DATA_PROPERTY, $"{asset?.vehicleName ?? "null"} /" +
                                $" {(asset == null ? "null" : asset.id.ToString(Data.AdminLocale))} / {data.Item.VehicleID:N} - REMOVED CREW SEAT {seat}.");
                            await bay.RemoveCrewSeat(data, seat, token).ThenToUpdate(token);
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
                await spawn.SpawnVehicle(token).ThenToUpdate(token);
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
    private async Task<SqlItem<VehicleData>?> GetVehicleTarget(CommandInteraction ctx, VehicleBay bay, CancellationToken token = default)
    {
        if (ctx.TryGetTarget(out InteractableVehicle vehicle))
        {
            SqlItem<VehicleData>? item = await bay.GetDataProxy(vehicle.asset.GUID, token).ConfigureAwait(false);
            if (item?.Item != null)
                return item;
        }

        VehicleSpawn spawn;
        if (ctx.TryGetTarget(out BarricadeDrop drop))
        {
            if (VehicleSigns.Loaded)
            {
                if (drop.interactable is InteractableSign sign)
                {
                    if (VehicleSigns.SignExists(sign, out VehicleSign sign2))
                    {
                        if (sign2.VehicleBay is not null)
                        {
                            SqlItem<VehicleData>? item = await bay.GetDataProxy(sign2.VehicleBay.VehicleGuid, token).ConfigureAwait(false);
                            if (item?.Item != null)
                                return item;
                        }
                    }
                }
            }
            if (VehicleSpawner.SpawnExists(drop.instanceID, EStructType.BARRICADE, out spawn))
            {
                SqlItem<VehicleData>? item = await bay.GetDataProxy(spawn.VehicleGuid, token).ConfigureAwait(false);
                if (item?.Item != null)
                    return item;
            }
        }
        if (ctx.TryGetTarget(out StructureDrop drop2) && VehicleSpawner.SpawnExists(drop2.instanceID, EStructType.STRUCTURE, out spawn))
        {
            SqlItem<VehicleData>? item = await bay.GetDataProxy(spawn.VehicleGuid, token).ConfigureAwait(false);
            if (item?.Item != null)
                return item;
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
