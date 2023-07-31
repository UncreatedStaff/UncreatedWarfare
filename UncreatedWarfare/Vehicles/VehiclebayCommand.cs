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
    public VehicleBayCommand() : base("vehiclebay", EAdminType.STAFF)
    {
        AddAlias("vb");
        Structure = new CommandStructure
        {
            Description = "Manage vehicle data and vehicle spawners.",
            Parameters = new CommandParameter[]
            {
                new CommandParameter("Add")
                {
                    Aliases = new string[] { "a", "create" },
                    Description = "Adds the vehicle you're looking at to the vehicle bay."
                },
                new CommandParameter("Remove")
                {
                    Aliases = new string[] { "delete", "r" },
                    Description = "Removes the vehicle you're looking at from the vehicle bay."
                },
                new CommandParameter("SaveMeta")
                {
                    Aliases = new string[] { "savemetadata", "metadata" },
                    Description = "Saves the barricades that are placed on the current vehicle to the vehicle bay."
                },
                new CommandParameter("Set")
                {
                    Aliases = new string[] { "s" },
                    Description = "Sets the trunk items to your current inventory or any other property to the given value.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Items")
                        {
                            Aliases = new string[] { "item", "inventory" },
                            Description = "Sets the trunk items to your current inventory."
                        },
                        new CommandParameter("Property", typeof(string))
                        {
                            Description = "Sets a property of your target vehicle's data.",
                            Parameters = new CommandParameter[]
                            {
                                new CommandParameter("Value", typeof(object))
                            }
                        }
                    }
                },
                new CommandParameter("Delay")
                {
                    Aliases = new string[] { "delays" },
                    Description = "Modify request delays of your target vehicle.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Add")
                        {
                            Aliases = new string[] { "a", "new" },
                            Parameters = new CommandParameter[]
                            {
                                new CommandParameter("Type", "Time", "Flag", "Percent", "Staging", "None")
                                {
                                    Description = "Remove the first matching delay.",
                                    Parameters = new CommandParameter[]
                                    {
                                        new CommandParameter("Gamemode", typeof(string))
                                        {
                                            Description = "Put an exclamation point before to act as a blacklist.",
                                            Parameters = new CommandParameter[]
                                            {
                                                new CommandParameter("Value", typeof(float))
                                                {
                                                    Description = "Value is not allowed on all types of delays."
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        new CommandParameter("Remove")
                        {
                            Aliases = new string[] { "r", "delete" },
                            Parameters = new CommandParameter[]
                            {
                                new CommandParameter("All")
                                {
                                    Aliases = new string[] { "clear" },
                                    Description = "Clears all delays.",
                                    Parameters = new CommandParameter[]
                                    {
                                        new CommandParameter("Gamemode", typeof(string))
                                        {
                                            Description = "Put an exclamation point before to act as a blacklist.",
                                            Parameters = new CommandParameter[]
                                            {
                                                new CommandParameter("Value", typeof(float))
                                                {
                                                    Description = "Value is not allowed on all types of delays."
                                                }
                                            }
                                        }
                                    }
                                },
                                new CommandParameter("Type", "Time", "Flag", "Percent", "Staging", "None")
                                {
                                    Description = "Remove the first matching delay.",
                                    Parameters = new CommandParameter[]
                                    {
                                        new CommandParameter("Gamemode", typeof(string))
                                        {
                                            Description = "Put an exclamation point before to act as a blacklist.",
                                            Parameters = new CommandParameter[]
                                            {
                                                new CommandParameter("Value", typeof(float))
                                                {
                                                    Description = "Value is not allowed on all types of delays."
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                        }
                    }
                },
                new CommandParameter("CrewSeats")
                {
                    Aliases = new string[] { "seats", "crew" },
                    Description = "Specify which seats must be manned by crew kits (pilots, crewmen, etc).",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Add")
                        {
                            Aliases = new string[] { "a", "create" },
                            Parameters = new CommandParameter[]
                            {
                                new CommandParameter("Seat", typeof(byte))
                                {
                                    Description = "Seat index starts at zero (for driver)."
                                }
                            }
                        },
                        new CommandParameter("Remove")
                        {
                            Aliases = new string[] { "r", "delete" },
                            Parameters = new CommandParameter[]
                            {
                                new CommandParameter("Seat", typeof(byte))
                                {
                                    Description = "Seat index starts at zero (for driver)."
                                }
                            }
                        }
                    }
                },
                new CommandParameter("Register")
                {
                    Aliases = new string[] { "reg" },
                    Description = "Link a vehicle spawn to a vehicle and save it.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Vehicle", typeof(VehicleAsset))
                    }
                },
                new CommandParameter("Deregister")
                {
                    Aliases = new string[] { "dereg" },
                    Description = "Unlink a vehicle spawn and unsave it."
                },
                new CommandParameter("Force")
                {
                    Aliases = new string[] { "respawn" },
                    Description = "Force a vehicle spawn to despawn it's active vehicle and respawn it."
                },
                new CommandParameter("Link")
                {
                    Description = "Begin or end a vehicle sign link."
                },
                new CommandParameter("Unlink")
                {
                    Description = "Unlink a sign and vehicle spawn."
                },
                new CommandParameter("Check")
                {
                    Aliases = new string[] { "id", "wtf" },
                    Description = "Get which vehicle is linked to the targetted spawner."
                }
            }
        };
    }

    public override async Task Execute(CommandInteraction ctx, CancellationToken token)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertGamemode(out IVehicles vehicleGm);
        ctx.AssertGamemode(out IStructureSaving structureGm);
        ctx.AssertRanByPlayer();

        VehicleBay bay = vehicleGm.VehicleBay;
        VehicleSpawner spawner = vehicleGm.VehicleSpawner;
        StructureSaver saver = structureGm.StructureSaver;

        ctx.AssertHelpCheck(0, "/vehiclebay <help|add|remove|savemeta|set|delay|crewseats|register|deregister|force|link|unlink|check> [help|parameters...] - Manage vehicle spawners, signs, and the vehicle bay.");

        if (ctx.MatchParameter(0, "delay", "delays"))
        {
            ctx.AssertPermissions(EAdminType.MODERATOR);

            ctx.AssertHelpCheck(1, "/vehiclebay delay <add|remove> <all|time|flag|percent|staging|none> [value] [!][gamemode] - Modify request delays of vehicle.");
            
            SqlItem<VehicleData>? data = await GetVehicleTarget(ctx, bay, spawner, token).ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);
            if (data?.Item is not null)
            {
                await data.Enter(token).ConfigureAwait(false);
                try
                {
                    await UCWarfare.ToUpdate(token);
                    bool adding;
                    if (ctx.MatchParameter(1, "add", "a", "new"))
                        adding = true;
                    else if (ctx.MatchParameter(1, "remove", "r", "delete"))
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
                            await data.SaveItem(token).ConfigureAwait(false);
                            await UCWarfare.ToUpdate(token);
                            Signs.UpdateVehicleBaySigns(null, data.Item);
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
                        ctx.SendCorrectUsage(adding
                            ? "/vehiclebay delay add <time|flag|percent|staging|none> [value] [!][gamemode]"
                            : "/vehiclebay delay remove <all|time|flag|percent|staging|none> [value] [!][gamemode]");
                        return;
                    }
                    if (type == DelayType.None && ctx.ArgumentCount < 4)
                    {
                        ctx.SendCorrectUsage(adding
                            ? "/vehiclebay delay add none [!]<gamemode>"
                            : "/vehiclebay delay remove none [!]<gamemode>");
                        return;
                    }
                    string? gamemode = null;
                    if (type is DelayType.OutOfStaging or DelayType.None)
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
                        ctx.LogAction(ActionLogType.SetVehicleDataProperty, "ADDED DELAY " + type + " VALUE: " + val.ToString(Data.AdminLocale)
                            + " GAMEMODE?: " + (gamemode == null ? "ANY" : gamemode.ToUpper()));
                        Delay.AddDelay(ref data.Item.Delays, type, val, gamemode);
                        Signs.UpdateVehicleBaySigns(null, data.Item);
                        foreach (SqlItem<VehicleSpawn> spawn in spawner.EnumerateSpawns(data))
                        {
                            VehicleBayComponent? svc = spawn.Item?.Structure?.Item?.Buildable?.Model.GetComponent<VehicleBayComponent>();
                            if (svc != null) svc.UpdateTimeDelay();
                        }
                        await data.SaveItem(token).ConfigureAwait(false);
                        await UCWarfare.ToUpdate(token);
                        ctx.Reply(T.VehicleBayAddedDelay, type, val, string.IsNullOrEmpty(gamemode) ? "any" : gamemode!);
                    }
                    else
                    {
                        int rem = 0;
                        while (Delay.RemoveDelay(ref data.Item.Delays, type, val, gamemode))
                            ++rem;
                        if (rem > 0)
                        {
                            Signs.UpdateVehicleBaySigns(null, data.Item);
                            foreach (SqlItem<VehicleSpawn> spawn in spawner.EnumerateSpawns(data))
                            {
                                VehicleBayComponent? svc = spawn.Item?.Structure?.Item?.Buildable?.Model.GetComponent<VehicleBayComponent>();
                                if (svc != null) svc.UpdateTimeDelay();
                            }
                            await data.SaveItem(token).ConfigureAwait(false);
                            await UCWarfare.ToUpdate(token);
                            ctx.LogAction(ActionLogType.SetVehicleDataProperty, "REMOVED " + rem.ToString(Data.AdminLocale) + " DELAY(S) " + type + " VALUE: " + val.ToString(Data.AdminLocale)
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
                SqlItem<VehicleData>? data = await GetVehicleTarget(ctx, bay, spawner, token).ConfigureAwait(false);
                await UCWarfare.ToUpdate(token);
                if (data?.Item == null)
                {
                    ctx.LogAction(ActionLogType.CreateVehicleData, $"{vehicle.asset.vehicleName} / {vehicle.asset.id} / {vehicle.asset.GUID:N}");
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
                if (await bay.RemoveRequestableVehicle(vehicle.asset.GUID, token).ConfigureAwait(false))
                {
                    await UCWarfare.ToUpdate(token);
                    ctx.LogAction(ActionLogType.DeleteVehicleData, $"{vehicle.asset.vehicleName} / {vehicle.asset.id} / {vehicle.asset.GUID:N}");
                    ctx.Reply(T.VehicleBayRemoved, vehicle.asset);
                }
                else
                {
                    await UCWarfare.ToUpdate(token);
                    ctx.Reply(T.VehicleBayNotAdded, vehicle.asset);
                }
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
                SqlItem<VehicleData>? data = await GetVehicleTarget(ctx, bay, spawner, token).ConfigureAwait(false);
                if (data?.Item != null)
                {
                    ctx.LogAction(ActionLogType.SetVehicleDataProperty, $"{vehicle.asset.vehicleName} / {vehicle.asset.id} / {vehicle.asset.GUID:N} - SAVED METADATA");
                    await data.Enter(token).ConfigureAwait(false);
                    try
                    {
                        await UCWarfare.ToUpdate(token);
                        data.Item.SaveMetaData(vehicle);
                        await data.SaveItem(token).ConfigureAwait(false);
                        await UCWarfare.ToUpdate(token);
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

            SqlItem<VehicleData>? data = await GetVehicleTarget(ctx, bay, spawner, token).ConfigureAwait(false);
            if (data?.Item is not null)
            {
                if (ctx.MatchParameter(1, "items", "item", "inventory"))
                {
                    await data.Enter(token).ConfigureAwait(false);
                    await UCWarfare.ToUpdate(token);
                    try
                    {
                        List<Guid> items = new List<Guid>();

                        for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
                        {
                            if (page == PlayerInventory.AREA)
                                continue;

                            byte pageCount = ctx.Caller.Player.inventory.getItemCount(page);

                            for (byte index = 0; index < pageCount; index++)
                            {
                                if (Assets.find(EAssetType.ITEM, ctx.Caller.Player.inventory.getItem(page, index).item.id) is ItemAsset a)
                                    items.Add(a.GUID);
                            }
                        }

                        VehicleAsset? asset = Assets.find<VehicleAsset>(data.Item.VehicleID);
                        ctx.LogAction(ActionLogType.SetVehicleDataProperty,
                            $"{asset?.vehicleName ?? "null"} / {(asset == null ? "0" : asset.id.ToString(Data.AdminLocale))} / {data.Item.VehicleID:N} - SET ITEMS");
                        data.Item.Items = items.ToArray();
                        await data.SaveItem(token).ConfigureAwait(false);
                        await UCWarfare.ToUpdate(token);
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
                    (SetPropertyResult result, MemberInfo? info) = await bay.SetProperty(data.Item, property, value, token).ConfigureAwait(false);
                    await UCWarfare.ToUpdate(token);
                    switch (result)
                    {
                        case SetPropertyResult.Success:
                            VehicleAsset? asset = Assets.find<VehicleAsset>(data.Item.VehicleID);
                            ctx.LogAction(ActionLogType.SetVehicleDataProperty,
                                $"{asset?.vehicleName ?? "null"} / {(asset == null ? 0 : asset.id)} / {data.Item.VehicleID:N} - SET " +
                                (info?.Name ?? property).ToUpper() + " >> " + value.ToUpper());
                            ctx.Reply(T.VehicleBaySetProperty!, property, asset, value);
                            Signs.UpdateVehicleBaySigns(null, data.Item);
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
                SqlItem<VehicleData>? data = await GetVehicleTarget(ctx, bay, spawner, token).ConfigureAwait(false);
                if (data?.Item is not null)
                {
                    if (ctx.TryGet(2, out byte seat))
                    {
                        VehicleAsset? asset = Assets.find<VehicleAsset>(data.Item.VehicleID);
                        if (!data.Item.CrewSeats.Contains(seat))
                        {
                            ctx.LogAction(ActionLogType.SetVehicleDataProperty, $"{asset?.vehicleName ?? "null"} /" +
                                $" {(asset == null ? "null" : asset.id.ToString(Data.AdminLocale))} / {data.Item.VehicleID:N} - ADDED CREW SEAT {seat}.");
                            await bay.AddCrewSeat(data, seat, token).ConfigureAwait(false);
                            await UCWarfare.ToUpdate(token);
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
                SqlItem<VehicleData>? data = await GetVehicleTarget(ctx, bay, spawner, token).ConfigureAwait(false);
                if (data?.Item is not null)
                {
                    if (ctx.TryGet(2, out byte seat))
                    {
                        VehicleAsset? asset = Assets.find<VehicleAsset>(data.Item.VehicleID);
                        if (data.Item.CrewSeats.Contains(seat))
                        {
                            ctx.LogAction(ActionLogType.SetVehicleDataProperty, $"{asset?.vehicleName ?? "null"} /" +
                                $" {(asset == null ? "null" : asset.id.ToString(Data.AdminLocale))} / {data.Item.VehicleID:N} - REMOVED CREW SEAT {seat}.");
                            await bay.RemoveCrewSeat(data, seat, token).ConfigureAwait(false);
                            await UCWarfare.ToUpdate(token);
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
            
            if (!ctx.TryGet(1, out VehicleAsset asset, out _, true))
            {
                if (ctx.HasArg(1))
                    ctx.Reply(T.VehicleBayInvalidInput, ctx.Get(1)!);
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
                        if (!spawner.TryGetSpawn(barricade, out _))
                        {
                            SqlItem<VehicleData>? data = bay.GetDataProxySync(asset.GUID);
                            if (data?.Item == null)
                                throw ctx.Reply(T.VehicleBayInvalidInput, asset.GUID.ToString("N"));
                            (SqlItem<SavedStructure> save, _) = await saver.AddBarricade(barricade, token).ConfigureAwait(false);
                            await spawner.CreateSpawn(save, data, null, token).ConfigureAwait(false);
                            await UCWarfare.ToUpdate(token);
                            ctx.LogAction(ActionLogType.RegisteredSpawn, $"{asset.vehicleName} / {asset.id} / {asset.GUID:N} - " +
                                                                          $"REGISTERED BARRICADE SPAWN AT {barricade.model.transform.position:N2} ID: {barricade.instanceID}");
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
                        if (!spawner.TryGetSpawn(structure, out _))
                        {
                            SqlItem<VehicleData>? data = bay.GetDataProxySync(asset.GUID);
                            if (data?.Item == null)
                                throw ctx.Reply(T.VehicleBayInvalidInput, asset.GUID.ToString("N"));
                            (SqlItem<SavedStructure> save, _) = await saver.AddStructure(structure, token).ConfigureAwait(false);
                            await spawner.CreateSpawn(save, data, null, token).ConfigureAwait(false);
                            await UCWarfare.ToUpdate(token);
                            ctx.LogAction(ActionLogType.RegisteredSpawn, $"{asset.vehicleName} / {asset.id} / {asset.GUID:N} - " +
                                                                          $"REGISTERED STRUCTURE SPAWN AT {structure.model.transform.position:N2} ID: {structure.instanceID}");
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

            SqlItem<VehicleSpawn>? spawn = GetBayTarget(ctx, spawner);
            if (spawn?.Item is { } spawn2)
            {
                await spawner.RemoveSpawn(spawn, token).ConfigureAwait(false);
                await UCWarfare.ToUpdate(token);
                VehicleData? data = spawn2.Vehicle?.Item;
                if (data is not null && Assets.find(data.VehicleID) is VehicleAsset asset)
                {
                    ctx.LogAction(ActionLogType.DeregisteredSpawn, $"{asset.vehicleName} / {asset.id} / {asset.GUID:N} - DEREGISTERED SPAWN KEY: {spawn2.StructureKey}.");
                    ctx.Reply(T.VehicleBaySpawnDeregistered, asset);
                }
                else
                {
                    ctx.LogAction(ActionLogType.DeregisteredSpawn, $"{spawn2.VehicleKey} - DEREGISTERED SPAWN ID: {spawn2.StructureKey}");
                    ctx.Reply(T.VehicleBaySpawnDeregistered, null!);
                }
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

            SqlItem<VehicleSpawn>? spawn = GetBayTarget(ctx, spawner);
            if (spawn?.Item is { } spawn2)
            {
                if (spawn2.HasLinkedVehicle(out InteractableVehicle veh))
                    VehicleSpawner.DeleteVehicle(veh);

                InteractableVehicle? vehicle = await VehicleSpawner.SpawnVehicle(spawn, token).ConfigureAwait(false);
                await UCWarfare.ToUpdate(token);
                if (vehicle != null)
                {
                    ctx.LogAction(ActionLogType.VehicleBayForceSpawn,
                        $"{vehicle.asset.vehicleName} / {vehicle.asset.id} / {vehicle.asset.GUID:N} - FORCED VEHICLE TO SPAWN ID: {spawn2.StructureKey}");
                    ctx.Reply(T.VehicleBayForceSuccess, vehicle.asset);
                }
                else
                    throw ctx.SendUnknownError();
            }
            else
                ctx.Reply(T.VehicleBayNoTarget);
        }
        else if (ctx.MatchParameter(0, "link"))
        {
            ctx.AssertHelpCheck(1, "/vehiclebay <link> - Use while looking at a spawner to start linking, then on a sign to link the sign to the spawner.");
            
            if (ctx.TryGetTarget(out BarricadeDrop drop) && drop.interactable is InteractableSign)
            {
                if (ctx.Caller.Player.TryGetPlayerData(out Components.UCPlayerData c))
                {
                    if (c.Currentlylinking is { Item: { } } proxy)
                    {
                        await spawner.UnlinkSign(drop, token).ConfigureAwait(false);
                        if (await spawner.LinkSign(drop, proxy, token).ConfigureAwait(false))
                        {
                            await UCWarfare.ToUpdate(token);
                            ctx.LogAction(ActionLogType.LinkedVehicleBaySign,
                                $"{drop.asset.itemName} / {drop.asset.id} / {drop.asset.GUID:N} ID: {drop.instanceID} - " +
                                $"LINKED TO SPAWN AT {drop.model.transform.position:N2} KEY: {c.Currentlylinking.PrimaryKey}");
                            VehicleData? data = proxy?.Item?.Vehicle?.Item;
                            VehicleAsset? asset = data == null ? null : Assets.find<VehicleAsset>(data.VehicleID);
                            ctx.Reply(T.VehicleBayLinkFinished, asset!);
                            c.Currentlylinking = null;
                        }
                        else
                        {
                            await UCWarfare.ToUpdate(token);
                            throw ctx.SendUnknownError();
                        }
                    }
                    else
                        ctx.Reply(T.VehicleBayLinkNotStarted);
                }
                else
                    ctx.SendUnknownError();
            }
            else
            {
                SqlItem<VehicleSpawn>? spawn = GetBayTarget(ctx, spawner);
                if (spawn?.Item is not null)
                {
                    if (ctx.Caller.Player.TryGetPlayerData(out Components.UCPlayerData c))
                    {
                        c.Currentlylinking = spawn;
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
            
            BarricadeDrop? sign = GetSignTarget(ctx, spawner, out SqlItem<VehicleSpawn> spawn);
            if (sign != null && !sign.GetServersideData().barricade.isDead && sign.interactable is InteractableSign)
            {
                await spawner.UnlinkSign(sign, token).ConfigureAwait(false);
                await UCWarfare.ToUpdate(token);
                ctx.Reply(T.VehicleBayUnlinked, spawn.Item?.Vehicle?.Item is { } data ? Assets.find<VehicleAsset>(data.VehicleID) : null!);
            }
            else
                ctx.Reply(T.VehicleBayNoTarget);
        }
        else if (ctx.MatchParameter(0, "check", "id", "wtf"))
        {
            ctx.AssertHelpCheck(1, "/vehiclebay <check|id|wtf> - Tells you what vehicle spawns from the spawner you're looking at.");

            SqlItem<VehicleSpawn>? spawn = GetBayTarget(ctx, spawner);
            if (spawn is not null)
            {
                VehicleAsset? asset = spawn.Item?.Vehicle?.Item is { } data ? Assets.find<VehicleAsset>(data.VehicleID) : null;
                ctx.Reply(T.VehicleBayCheck, (uint)spawn.PrimaryKey.Key, asset!, asset == null ? (ushort)0 : asset.id);
            }
            else
                ctx.Reply(T.VehicleBaySpawnNotRegistered);
        }
        else ctx.SendCorrectUsage("/vehiclebay <help|add|remove|savemeta|set|delay|crewseats|register|deregister|force|link|unlink|check> [help|parameters...]");
    }

    /// <summary>Linked vehicle >> Sign barricade >> Spawner barricade >> Spawner structure</summary>
    public static async Task<SqlItem<VehicleData>?> GetVehicleTarget(CommandInteraction ctx, VehicleBay bay, VehicleSpawner spawner, CancellationToken token = default)
    {
        if (ctx.TryGetTarget(out InteractableVehicle vehicle))
        {
            SqlItem<VehicleData>? item = await bay.GetDataProxy(vehicle.asset.GUID, token).ConfigureAwait(false);
            if (item?.Item != null)
                return item;
        }

        if (ctx.TryGetTarget(out BarricadeDrop drop) && spawner.TryGetSpawn(drop, out SqlItem<VehicleSpawn> spawn))
        {
            return spawn.Item?.Vehicle;
        }
        if (ctx.TryGetTarget(out StructureDrop drop2) && spawner.TryGetSpawn(drop2, out spawn))
        {
            return spawn.Item?.Vehicle;
        }
        return null;
    }
    /// <summary>Linked vehicle >> Sign barricade >> Spawner barricade >> Spawner structure</summary>
    public static SqlItem<VehicleSpawn>? GetBayTarget(CommandInteraction ctx, VehicleSpawner spawner)
    {
        if (ctx.TryGetTarget(out InteractableVehicle vehicle) && spawner.TryGetSpawn(vehicle, out SqlItem<VehicleSpawn> spawn))
        {
            return spawn;
        }
        if (ctx.TryGetTarget(out BarricadeDrop drop) && spawner.TryGetSpawn(drop, out spawn))
        {
            return spawn;
        }
        if (ctx.TryGetTarget(out StructureDrop drop2) && spawner.TryGetSpawn(drop2, out spawn))
        {
            return spawn;
        }
        return null;
    }
    /// <summary>Sign barricade >> Spawner barricade >> Spawner structure >> Linked Vehicle</summary>
    public static BarricadeDrop? GetSignTarget(CommandInteraction ctx, VehicleSpawner spawner, out SqlItem<VehicleSpawn> spawn)
    {
        if (ctx.TryGetTarget(out BarricadeDrop drop) && spawner.TryGetSpawn(drop, out spawn))
        {
            return drop;
        }
        if (ctx.TryGetTarget(out StructureDrop drop2) && spawner.TryGetSpawn(drop2, out spawn) || (ctx.TryGetTarget(out InteractableVehicle vehicle) && spawner.TryGetSpawn(vehicle, out spawn)))
        {
            return spawn?.Item?.Sign?.Item?.Buildable?.Drop as BarricadeDrop;
        }

        spawn = null!;
        return null;
    }
}
