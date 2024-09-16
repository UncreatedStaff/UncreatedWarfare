using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players.Permissions;

namespace Uncreated.Warfare.Commands;

[Command("vehiclebay", "vb")]
[MetadataFile(nameof(GetHelpMetadata))]
public class VehicleBayCommand : IExecutableCommand
{
    private static readonly PermissionLeaf PermissionAdd        = new PermissionLeaf("commands.vehiclebay.edit.add",           unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionRemove     = new PermissionLeaf("commands.vehiclebay.edit.remove",        unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionSet        = new PermissionLeaf("commands.vehiclebay.edit.set",           unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionRegister   = new PermissionLeaf("commands.vehiclebay.spawn.register",     unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionDeregister = new PermissionLeaf("commands.vehiclebay.spawn.deregister",   unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionForce      = new PermissionLeaf("commands.vehiclebay.spawn.force",        unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionLinkSign   = new PermissionLeaf("commands.vehiclebay.spawn.signs.link",   unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionUnlinkSign = new PermissionLeaf("commands.vehiclebay.spawn.signs.unlink", unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionCheck      = new PermissionLeaf("commands.vehiclebay.spawn.check",        unturned: false, warfare: true);

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Manage vehicle data and vehicle spawners.",
            Parameters =
            [
                new CommandParameter("Add")
                {
                    Aliases = [ "a", "create" ],
                    Permission = PermissionAdd,
                    Description = "Adds the vehicle you're looking at to the vehicle bay."
                },
                new CommandParameter("Remove")
                {
                    Aliases = [ "delete", "r" ],
                    Permission = PermissionRemove,
                    Description = "Removes the vehicle you're looking at from the vehicle bay."
                },
                new CommandParameter("SaveMeta")
                {
                    Aliases = [ "savemetadata", "metadata" ],
                    Permission = PermissionSet,
                    Description = "Saves the barricades that are placed on the current vehicle to the vehicle bay."
                },
                new CommandParameter("Set")
                {
                    Aliases = [ "s" ],
                    Permission = PermissionSet,
                    Description = "Sets the trunk items to your current inventory or any other property to the given value.",
                    Parameters =
                    [
                        new CommandParameter("Items")
                        {
                            Aliases = [ "item", "inventory" ],
                            Description = "Sets the trunk items to your current inventory."
                        },
                        new CommandParameter("Property", typeof(string))
                        {
                            Description = "Sets a property of your target vehicle's data.",
                            Parameters =
                            [
                                new CommandParameter("Value", typeof(object))
                            ]
                        }
                    ]
                },
                new CommandParameter("Delay")
                {
                    Aliases = [ "delays" ],
                    Permission = PermissionSet,
                    Description = "Modify request delays of your target vehicle.",
                    Parameters =
                    [
                        new CommandParameter("Add")
                        {
                            Aliases = [ "a", "new" ],
                            Parameters =
                            [
                                new CommandParameter("Type", "Time", "Flag", "Percent", "Staging", "None")
                                {
                                    Description = "Remove the first matching delay.",
                                    Parameters =
                                    [
                                        new CommandParameter("Gamemode", typeof(string))
                                        {
                                            Description = "Put an exclamation point before to act as a blacklist.",
                                            Parameters =
                                            [
                                                new CommandParameter("Value", typeof(float))
                                                {
                                                    Description = "Value is not allowed on all types of delays."
                                                }
                                            ]
                                        }
                                    ]
                                }
                            ]
                        },
                        new CommandParameter("Remove")
                        {
                            Aliases = [ "r", "delete" ],
                            Parameters =
                            [
                                new CommandParameter("All")
                                {
                                    Aliases = [ "clear" ],
                                    Description = "Clears all delays.",
                                    Parameters =
                                    [
                                        new CommandParameter("Gamemode", typeof(string))
                                        {
                                            Description = "Put an exclamation point before to act as a blacklist.",
                                            Parameters =
                                            [
                                                new CommandParameter("Value", typeof(float))
                                                {
                                                    Description = "Value is not allowed on all types of delays."
                                                }
                                            ]
                                        }
                                    ]
                                },
                                new CommandParameter("Type", "Time", "Flag", "Percent", "Staging", "None")
                                {
                                    Description = "Remove the first matching delay.",
                                    Parameters =
                                    [
                                        new CommandParameter("Gamemode", typeof(string))
                                        {
                                            Description = "Put an exclamation point before to act as a blacklist.",
                                            Parameters =
                                            [
                                                new CommandParameter("Value", typeof(float))
                                                {
                                                    Description = "Value is not allowed on all types of delays."
                                                }
                                            ]
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                },
                new CommandParameter("CrewSeats")
                {
                    Aliases = [ "seats", "crew" ],
                    Permission = PermissionSet,
                    Description = "Specify which seats must be manned by crew kits (pilots, crewmen, etc).",
                    Parameters =
                    [
                        new CommandParameter("Add")
                        {
                            Aliases = [ "a", "create" ],
                            Parameters =
                            [
                                new CommandParameter("Seat", typeof(byte))
                                {
                                    Description = "Seat index starts at zero (for driver)."
                                }
                            ]
                        },
                        new CommandParameter("Remove")
                        {
                            Aliases = [ "r", "delete" ],
                            Parameters =
                            [
                                new CommandParameter("Seat", typeof(byte))
                                {
                                    Description = "Seat index starts at zero (for driver)."
                                }
                            ]
                        }
                    ]
                },
                new CommandParameter("Register")
                {
                    Aliases = [ "reg" ],
                    Permission = PermissionRegister,
                    Description = "Link a vehicle spawn to a vehicle and save it.",
                    Parameters =
                    [
                        new CommandParameter("Vehicle", typeof(VehicleAsset))
                    ]
                },
                new CommandParameter("Deregister")
                {
                    Aliases = [ "dereg" ],
                    Permission = PermissionDeregister,
                    Description = "Unlink a vehicle spawn and unsave it."
                },
                new CommandParameter("Force")
                {
                    Aliases = [ "respawn" ],
                    Permission = PermissionForce,
                    Description = "Force a vehicle spawn to despawn it's active vehicle and respawn it."
                },
                new CommandParameter("Link")
                {
                    Description = "Begin or end a vehicle sign link.",
                    Permission = PermissionLinkSign
                },
                new CommandParameter("Unlink")
                {
                    Description = "Unlink a sign and vehicle spawn.",
                    Permission = PermissionUnlinkSign
                },
                new CommandParameter("Check")
                {
                    Aliases = [ "id", "wtf" ],
                    Permission = PermissionCheck,
                    Description = "Get which vehicle is linked to the targetted spawner."
                }
            ]
        };
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
#if false
        Context.AssertRanByPlayer();

        VehicleBay bay = vehicleGm.VehicleBay;
        VehicleSpawner spawner = vehicleGm.VehicleSpawner;
        StructureSaver saver = structureGm.StructureSaver;

        Context.AssertHelpCheck(0, "/vehiclebay <help|add|remove|savemeta|set|delay|crewseats|register|deregister|force|link|unlink|check> [help|parameters...] - Manage vehicle spawners, signs, and the vehicle bay.");

        if (Context.MatchParameter(0, "delay", "delays"))
        {
            await Context.AssertPermissions(PermissionSet, token);
            
            Context.AssertHelpCheck(1, "/vehiclebay delay <add|remove> <all|time|flag|percent|staging|none> [value] [!][gamemode] - Modify request delays of vehicle.");
            
            SqlItem<VehicleData>? data = await GetVehicleTarget(Context, bay, spawner, token).ConfigureAwait(false);
            await UniTask.SwitchToMainThread(token);
            if (data?.Item is not null)
            {
                await data.Enter(token).ConfigureAwait(false);
                try
                {
                    await UniTask.SwitchToMainThread(token);
                    bool adding;
                    if (Context.MatchParameter(1, "add", "a", "new"))
                        adding = true;
                    else if (Context.MatchParameter(1, "remove", "r", "delete"))
                        adding = false;
                    else
                    {
                        Context.SendCorrectUsage("/vehiclebay delay <add|remove> <all|time|flag|percent|staging|none> [value] [!][gamemode]");
                        return;
                    }
                    if (Context.MatchParameter(2, "all", "clear"))
                    {
                        if (adding)
                        {
                            Context.SendCorrectUsage("/vehiclebay delay add <time|flag|percent|staging|none> [value] [!][gamemode]");
                            return;
                        }
                        int rem = data.Item.Delays.Length;
                        if (rem > 0)
                        {
                            data.Item.Delays = Array.Empty<Delay>();
                            await data.SaveItem(token).ConfigureAwait(false);
                            await UniTask.SwitchToMainThread(token);
                            Signs.UpdateVehicleBaySigns(null, data.Item);
                        }
                        Context.Reply(T.VehicleBayRemovedDelay, rem);
                        return;
                    }

                    DelayType type;
                    if (Context.MatchParameter(2, "time"))
                        type = DelayType.Time;
                    else if (Context.MatchParameter(2, "flag", "objective", "objectives"))
                        type = DelayType.Flag;
                    else if (Context.MatchParameter(2, "flagpercent", "percent"))
                        type = DelayType.FlagPercentage;
                    else if (Context.MatchParameter(2, "staging", "prep"))
                        type = DelayType.OutOfStaging;
                    else if (Context.MatchParameter(2, "teammates", "players"))
                        type = DelayType.Teammates;
                    else if (Context.MatchParameter(2, "none"))
                        type = DelayType.None;
                    else
                    {
                        Context.SendCorrectUsage(adding
                            ? "/vehiclebay delay add <time|flag|percent|staging|teammates|none> [value] [!][gamemode]"
                            : "/vehiclebay delay remove <all|time|flag|percent|staging|teammates|none> [value] [!][gamemode]");
                        return;
                    }
                    if (type == DelayType.None && Context.ArgumentCount < 4)
                    {
                        Context.SendCorrectUsage(adding
                            ? "/vehiclebay delay add none [!]<gamemode>"
                            : "/vehiclebay delay remove none [!]<gamemode>");
                        return;
                    }
                    string? gamemode = null;
                    if (type is DelayType.OutOfStaging or DelayType.None)
                    {
                        if (Context.HasArgs(4))
                            gamemode = Context.Get(3)!;
                    }
                    else if (Context.ArgumentCount < 4)
                    {
                        Context.SendCorrectUsage("/vehiclebay delay " + Context.Get(1)!.ToLower() + " " + Context.Get(1)!.ToLower() + " <value> [gamemode]");
                        return;
                    }
                    else if (Context.ArgumentCount > 4)
                        gamemode = Context.Get(4)!;
                    
                    if (string.IsNullOrEmpty(gamemode) && type == DelayType.None)
                    {
                        gamemode = "<";
                        foreach (KeyValuePair<GamemodeType, Type> gm in Gamemode.Gamemodes)
                        {
                            if (gamemode.Length != 1) gamemode += "|";
                            gamemode += gm.Key.ToString();
                        }
                        gamemode += ">";
                        if (adding)
                            Context.SendCorrectUsage("/vehiclebay delay add none [!]" + gamemode);
                        else
                            Context.SendCorrectUsage("/vehiclebay delay remove none [!]" + gamemode);
                        return;
                    }
                    if (!string.IsNullOrEmpty(gamemode))
                    {
                        string? gm = null;
                        foreach (KeyValuePair<GamemodeType, Type> gm2 in Gamemode.Gamemodes)
                        {
                            if (gm2.Key.ToString().Equals(gamemode, StringComparison.OrdinalIgnoreCase))
                            {
                                gm = gm2.Key.ToString();
                                break;
                            }
                        }
                        if (string.IsNullOrEmpty(gm))
                        {
                            gamemode = "<";
                            foreach (KeyValuePair<GamemodeType, Type> gm2 in Gamemode.Gamemodes)
                            {
                                if (gamemode.Length != 1) gamemode += "|";
                                gamemode += gm2.Key.ToString();
                            }
                            gamemode += ">";
                            if (adding)
                                Context.SendCorrectUsage("/vehiclebay delay add <type> [value] [!]" + gamemode);
                            else
                                Context.SendCorrectUsage("/vehiclebay delay remove <type> [value] [!]" + gamemode);
                            return;
                        }
                        else gamemode = gm;
                    }

                    float val = 0;
                    if (type != DelayType.OutOfStaging && type != DelayType.None && !Context.TryGet(3, out val))
                    {
                        Context.SendCorrectUsage("/vehiclebay delay " + (adding ? "add" : "remove") + " " + Context.Get(2)! + " <value (float)>" + (string.IsNullOrEmpty(gamemode) ? string.Empty : " [!]" + gamemode));
                        return;
                    }
                    
                    if (adding)
                    {
                        Context.LogAction(ActionLogType.SetVehicleDataProperty, "ADDED DELAY " + type + " VALUE: " + val.ToString(CultureInfo.InvariantCulture)
                            + " GAMEMODE?: " + (gamemode == null ? "ANY" : gamemode.ToUpper()));
                        Delay[] itemDelays = data.Item.Delays;
                        Delay.AddDelay(ref itemDelays, type, val, gamemode);
                        data.Item.Delays = itemDelays;
                        Signs.UpdateVehicleBaySigns(null, data.Item);
                        foreach (SqlItem<VehicleSpawn> spawn in spawner.EnumerateSpawns(data))
                        {
                            VehicleBayComponent? svc = spawn.Item?.Structure?.Item?.Buildable?.Model.GetComponent<VehicleBayComponent>();
                            svc?.UpdateTimeDelay();
                        }
                        await data.SaveItem(token).ConfigureAwait(false);
                        await UniTask.SwitchToMainThread(token);
                        Context.Reply(T.VehicleBayAddedDelay, type, val, string.IsNullOrEmpty(gamemode) ? "any" : gamemode);
                    }
                    else
                    {
                        int rem = 0;
                        Delay[] itemDelays = data.Item.Delays;
                        while (Delay.RemoveDelay(ref itemDelays, type, val, gamemode))
                            ++rem;
                        data.Item.Delays = itemDelays;
                        if (rem > 0)
                        {
                            Signs.UpdateVehicleBaySigns(null, data.Item);
                            foreach (SqlItem<VehicleSpawn> spawn in spawner.EnumerateSpawns(data))
                            {
                                VehicleBayComponent? svc = spawn.Item?.Structure?.Item?.Buildable?.Model.GetComponent<VehicleBayComponent>();
                                svc?.UpdateTimeDelay();
                            }
                            await data.SaveItem(token).ConfigureAwait(false);
                            await UniTask.SwitchToMainThread(token);
                            Context.LogAction(ActionLogType.SetVehicleDataProperty, "REMOVED " + rem.ToString(CultureInfo.InvariantCulture) + " DELAY(S) " + type + " VALUE: " + val.ToString(CultureInfo.InvariantCulture)
                                + " GAMEMODE?: " + (gamemode == null ? "ANY" : gamemode.ToUpper()));
                        }
                        Context.Reply(T.VehicleBayRemovedDelay, rem);
                    }
                }
                finally
                {
                    data.Release();
                }
            }
            else
                Context.Reply(T.VehicleBayNoTarget);
        }
        else if (Context.MatchParameter(0, "add", "a", "create"))
        {
            await Context.AssertPermissions(PermissionAdd, token);
            await UniTask.SwitchToMainThread(token);

            Context.AssertHelpCheck(1, "/vehiclebay <add|a|create> - Adds the vehicle you're looking at to the vehicle bay.");

            if (Context.TryGetVehicleTarget(out InteractableVehicle? vehicle))
            {
                SqlItem<VehicleData>? data = await GetVehicleTarget(Context, bay, spawner, token).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(token);
                if (data?.Item == null)
                {
                    Context.LogAction(ActionLogType.CreateVehicleData, $"{vehicle.asset.vehicleName} / {vehicle.asset.id} / {vehicle.asset.GUID:N}");
                    await bay.AddRequestableVehicle(vehicle, token).ConfigureAwait(false);
                    await UniTask.SwitchToMainThread(token);
                    Context.Reply(T.VehicleBayAdded, vehicle.asset);
                }
                else
                {
                    await UniTask.SwitchToMainThread(token);
                    Context.Reply(T.VehicleBayAlreadyAdded, vehicle.asset);
                }
            }
            else
                Context.Reply(T.VehicleBayNoTarget);
        }
        else if (Context.MatchParameter(0, "remove", "r", "delete"))
        {
            await Context.AssertPermissions(PermissionRemove, token);
            await UniTask.SwitchToMainThread(token);

            Context.AssertHelpCheck(1, "/vehiclebay <remove|r|delete> - Removes the vehicle you're looking at from the vehicle bay.");

            if (Context.TryGetVehicleTarget(out InteractableVehicle? vehicle))
            {
                if (await bay.RemoveRequestableVehicle(vehicle.asset.GUID, token).ConfigureAwait(false))
                {
                    await UniTask.SwitchToMainThread(token);
                    Context.LogAction(ActionLogType.DeleteVehicleData, $"{vehicle.asset.vehicleName} / {vehicle.asset.id} / {vehicle.asset.GUID:N}");
                    Context.Reply(T.VehicleBayRemoved, vehicle.asset);
                }
                else
                {
                    await UniTask.SwitchToMainThread(token);
                    Context.Reply(T.VehicleBayNotAdded, vehicle.asset);
                }
            }
            else
                Context.Reply(T.VehicleBayNoTarget);
        }
        else if (Context.MatchParameter(0, "savemeta", "savemetadata", "metadata"))
        {
            await Context.AssertPermissions(PermissionSet, token);
            await UniTask.SwitchToMainThread(token);

            Context.AssertHelpCheck(1, "/vehiclebay <savemeta|savemetadata|metadata> - Saves the barricades that are placed on the current vehicle to the vehicle bay.");

            if (Context.TryGetVehicleTarget(out InteractableVehicle? vehicle))
            {
                SqlItem<VehicleData>? data = await GetVehicleTarget(Context, bay, spawner, token).ConfigureAwait(false);
                if (data?.Item != null)
                {
                    Context.LogAction(ActionLogType.SetVehicleDataProperty, $"{vehicle.asset.vehicleName} / {vehicle.asset.id} / {vehicle.asset.GUID:N} - SAVED METADATA");
                    await data.Enter(token).ConfigureAwait(false);
                    try
                    {
                        await UniTask.SwitchToMainThread(token);
                        data.Item.SaveMetaData(vehicle);
                        await data.SaveItem(token).ConfigureAwait(false);
                        await UniTask.SwitchToMainThread(token);
                        Context.Reply(T.VehicleBaySavedMeta, vehicle.asset);
                    }
                    finally
                    {
                        data.Release();
                    }
                }
                else
                    Context.Reply(T.VehicleBayNotAdded);
            }
            else
                Context.Reply(T.VehicleBayNoTarget);
        }
        else if (Context.MatchParameter(0, "set", "s"))
        {
            await Context.AssertPermissions(PermissionSet, token);
            await UniTask.SwitchToMainThread(token);

            Context.AssertHelpCheck(1, "/vehiclebay <set|s> <items|property> [value] - Sets the trunk items to your current inventory or any other property to the given value.");

            SqlItem<VehicleData>? data = await GetVehicleTarget(Context, bay, spawner, token).ConfigureAwait(false);
            if (data?.Item is not null)
            {
                if (Context.MatchParameter(1, "items", "item", "inventory"))
                {
                    await data.Enter(token).ConfigureAwait(false);
                    await UniTask.SwitchToMainThread(token);
                    try
                    {
                        List<Guid> items = new List<Guid>();

                        for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
                        {
                            if (page == PlayerInventory.AREA)
                                continue;

                            byte pageCount = Context.Player.UnturnedPlayer.inventory.getItemCount(page);

                            for (byte index = 0; index < pageCount; index++)
                            {
                                if (Assets.find(EAssetType.ITEM, Context.Player.UnturnedPlayer.inventory.getItem(page, index).item.id) is ItemAsset a)
                                    items.Add(a.GUID);
                            }
                        }

                        VehicleAsset? asset = Assets.find<VehicleAsset>(data.Item.VehicleID);
                        Context.LogAction(ActionLogType.SetVehicleDataProperty,
                            $"{asset?.vehicleName ?? "null"} / {(asset == null ? "0" : asset.id.ToString(CultureInfo.InvariantCulture))} / {data.Item.VehicleID:N} - SET ITEMS");
                        data.Item.Items = items.ToArray();
                        await data.SaveItem(token).ConfigureAwait(false);
                        await UniTask.SwitchToMainThread(token);
                        if (items.Count == 0)
                            Context.Reply(T.VehicleBayClearedItems, asset!);
                        else
                            Context.Reply(T.VehicleBaySetItems, asset!, items.Count);
                    }
                    finally
                    {
                        data.Release();
                    }
                }
                else if (Context.TryGet(2, out string value) && Context.TryGet(1, out string property))
                {
                    (SetPropertyResult result, MemberInfo? info) = await bay.SetProperty(data.Item, property, value, token).ConfigureAwait(false);
                    await UniTask.SwitchToMainThread(token);
                    switch (result)
                    {
                        case SetPropertyResult.Success:
                            VehicleAsset? asset = Assets.find<VehicleAsset>(data.Item.VehicleID);
                            Context.LogAction(ActionLogType.SetVehicleDataProperty,
                                $"{asset?.vehicleName ?? "null"} / {asset?.id ?? 0} / {data.Item.VehicleID:N} - SET " +
                                (info?.Name ?? property).ToUpper() + " >> " + value.ToUpper());
                            Context.Reply(T.VehicleBaySetProperty!, property, asset, value);
                            Signs.UpdateVehicleBaySigns(null, data.Item);
                            break;
                        default:
                        case SetPropertyResult.ObjectNotFound:
                            Context.Reply(T.VehicleBayNotAdded);
                            break;
                        case SetPropertyResult.PropertyNotFound:
                            Context.Reply(T.VehicleBayInvalidProperty, property);
                            break;
                        case SetPropertyResult.ParseFailure:
                        case SetPropertyResult.TypeNotSettable:
                            Context.Reply(T.VehicleBayInvalidSetValue, value, property);
                            break;
                        case SetPropertyResult.PropertyProtected:
                            Context.Reply(T.VehicleBayNotCommandSettable, property);
                            break;
                    }
                }
                else
                    Context.SendCorrectUsage("/vehiclebay set <items|property> [value]");
            }
            else
            {
                Context.Reply(T.VehicleBayNoTarget);
            }
        }
        else if (Context.MatchParameter(0, "crewseats", "seats", "crew"))
        {
            await Context.AssertPermissions(PermissionSet, token);
            await UniTask.SwitchToMainThread(token);

            Context.AssertHelpCheck(1, "/vehiclebay <crewseats|seats|crew> <add|remove> <seat index> - Registers or deregisters a seat index as requiring crewman to enter.");

            if (Context.MatchParameter(1, "add", "a", "create"))
            {
                SqlItem<VehicleData>? data = await GetVehicleTarget(Context, bay, spawner, token).ConfigureAwait(false);
                if (data?.Item is not null)
                {
                    if (Context.TryGet(2, out byte seat))
                    {
                        VehicleAsset? asset = Assets.find<VehicleAsset>(data.Item.VehicleID);
                        if (!data.Item.CrewSeats.Contains(seat))
                        {
                            Context.LogAction(ActionLogType.SetVehicleDataProperty, $"{asset?.vehicleName ?? "null"} /" +
                                $" {(asset == null ? "null" : asset.id.ToString(CultureInfo.InvariantCulture))} / {data.Item.VehicleID:N} - ADDED CREW SEAT {seat}.");
                            await bay.AddCrewSeat(data, seat, token).ConfigureAwait(false);
                            await UniTask.SwitchToMainThread(token);
                            Context.Reply(T.VehicleBaySeatAdded, seat, asset!);
                        }
                        else
                            Context.Reply(T.VehicleBaySeatAlreadyAdded, seat, asset!);
                    }
                    else
                        Context.SendCorrectUsage("/vehiclebay crewseats add <seat index>");
                }
                else
                    Context.Reply(T.VehicleBayNoTarget);
            }
            else if (Context.MatchParameter(1, "remove", "r", "delete"))
            {
                SqlItem<VehicleData>? data = await GetVehicleTarget(Context, bay, spawner, token).ConfigureAwait(false);
                if (data?.Item is not null)
                {
                    if (Context.TryGet(2, out byte seat))
                    {
                        VehicleAsset? asset = Assets.find<VehicleAsset>(data.Item.VehicleID);
                        if (data.Item.CrewSeats.Contains(seat))
                        {
                            Context.LogAction(ActionLogType.SetVehicleDataProperty, $"{asset?.vehicleName ?? "null"} /" +
                                $" {(asset == null ? "null" : asset.id.ToString(CultureInfo.InvariantCulture))} / {data.Item.VehicleID:N} - REMOVED CREW SEAT {seat}.");
                            await bay.RemoveCrewSeat(data, seat, token).ConfigureAwait(false);
                            await UniTask.SwitchToMainThread(token);
                            Context.Reply(T.VehicleBaySeatRemoved, seat, asset!);
                        }
                        else
                            Context.Reply(T.VehicleBaySeatNotAdded, seat, asset!);
                    }
                    else
                        Context.SendCorrectUsage("/vehiclebay crewseats remove <seat index>");
                }
                else
                    Context.Reply(T.VehicleBayNoTarget);
            }
            else
                Context.SendCorrectUsage("/vehiclebay crewseats <add|remove> <seat index>");
        }
        else if (Context.MatchParameter(0, "register", "reg"))
        {
            await Context.AssertPermissions(PermissionRegister, token);
            await UniTask.SwitchToMainThread(token);

            Context.AssertHelpCheck(1, "/vehiclebay <register|reg> <vehicle id> - Sets the vehicle spawner you're looking at to spawn the given vehicle id (guid or uint16).");
            
            if (!Context.TryGet(1, out VehicleAsset? asset, out _, true))
            {
                if (Context.HasArgs(2))
                    Context.Reply(T.VehicleBayInvalidInput, Context.Get(1)!);
                else
                    Context.SendCorrectUsage("/vehiclebay register <vehicle id or guid>");
                return;
            }
            if (asset is not null)
            {
                if (Context.TryGetTarget(out BarricadeDrop barricade))
                {
                    if (Gamemode.Config.StructureVehicleBay.MatchGuid(barricade.asset.GUID))
                    {
                        if (!spawner.TryGetSpawn(barricade, out _))
                        {
                            SqlItem<VehicleData>? data = bay.GetDataProxySync(asset.GUID);
                            if (data?.Item == null)
                                throw Context.Reply(T.VehicleBayInvalidInput, asset.GUID.ToString("N"));
                            (SqlItem<SavedStructure> save, _) = await saver.AddBarricade(barricade, token).ConfigureAwait(false);
                            await spawner.CreateSpawn(save, data, null, token).ConfigureAwait(false);
                            await UniTask.SwitchToMainThread(token);
                            Context.LogAction(ActionLogType.RegisteredSpawn, $"{asset.vehicleName} / {asset.id} / {asset.GUID:N} - " +
                                                                          $"REGISTERED BARRICADE SPAWN AT {barricade.model.transform.position:N2} ID: {barricade.instanceID}");
                            Context.Reply(T.VehicleBaySpawnRegistered, asset);
                        }
                        else
                            Context.Reply(T.VehicleBaySpawnAlreadyRegistered, asset);
                    }
                    else
                    {
                        Context.Reply(T.VehicleBayInvalidBayItem, barricade.asset);
                    }
                }
                else if (Context.TryGetTarget(out StructureDrop structure))
                {
                    if (Gamemode.Config.StructureVehicleBay.MatchGuid(structure.asset.GUID))
                    {
                        if (!spawner.TryGetSpawn(structure, out _))
                        {
                            SqlItem<VehicleData>? data = bay.GetDataProxySync(asset.GUID);
                            if (data?.Item == null)
                                throw Context.Reply(T.VehicleBayInvalidInput, asset.GUID.ToString("N"));
                            (SqlItem<SavedStructure> save, _) = await saver.AddStructure(structure, token).ConfigureAwait(false);
                            await spawner.CreateSpawn(save, data, null, token).ConfigureAwait(false);
                            await UniTask.SwitchToMainThread(token);
                            Context.LogAction(ActionLogType.RegisteredSpawn, $"{asset.vehicleName} / {asset.id} / {asset.GUID:N} - " +
                                                                          $"REGISTERED STRUCTURE SPAWN AT {structure.model.transform.position:N2} ID: {structure.instanceID}");
                            Context.Reply(T.VehicleBaySpawnRegistered, asset);
                        }
                        else
                            Context.Reply(T.VehicleBaySpawnAlreadyRegistered, asset);
                    }
                    else
                    {
                        Context.Reply(T.VehicleBayInvalidBayItem, structure.asset);
                    }
                }
                else
                    Context.Reply(T.VehicleBayNoTarget);
            }
            else
                Context.Reply(T.VehicleBayInvalidInput, Context.Get(1)!);
        }
        else if (Context.MatchParameter(0, "deregister", "dereg"))
        {
            await Context.AssertPermissions(PermissionDeregister, token);
            await UniTask.SwitchToMainThread(token);

            Context.AssertHelpCheck(1, "/vehiclebay <deregister|dereg> - Disables the vehicle spawner you're looking at from spawning vehicles.");

            SqlItem<VehicleSpawn>? spawn = GetBayTarget(Context, spawner);
            if (spawn?.Item is { } spawn2)
            {
                await spawner.RemoveSpawn(spawn, token).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(token);
                VehicleData? data = spawn2.Vehicle?.Item;
                if (data is not null && Assets.find(data.VehicleID) is VehicleAsset asset)
                {
                    Context.LogAction(ActionLogType.DeregisteredSpawn, $"{asset.vehicleName} / {asset.id} / {asset.GUID:N} - DEREGISTERED SPAWN KEY: {spawn2.StructureKey}.");
                    Context.Reply(T.VehicleBaySpawnDeregistered, asset);
                }
                else
                {
                    Context.LogAction(ActionLogType.DeregisteredSpawn, $"{spawn2.VehicleKey} - DEREGISTERED SPAWN ID: {spawn2.StructureKey}");
                    Context.Reply(T.VehicleBaySpawnDeregistered, null!);
                }
            }
            else if (Context.TryGetTarget(out BarricadeDrop _) || Context.TryGetTarget(out StructureDrop _))
                Context.Reply(T.VehicleBaySpawnNotRegistered);
            else
                Context.Reply(T.VehicleBayNoTarget);
        }
        else if (Context.MatchParameter(0, "force", "respawn"))
        {
            await Context.AssertPermissions(PermissionForce, token);
            await UniTask.SwitchToMainThread(token);

            Context.AssertHelpCheck(1, "/vehiclebay <force|respawn> - Deletes the linked vehicle to the spawner you're looking at from the world and spawns another.");

            SqlItem<VehicleSpawn>? spawn = GetBayTarget(Context, spawner);
            if (spawn?.Item is { } spawn2)
            {
                if (spawn2.HasLinkedVehicle(out InteractableVehicle veh))
                    VehicleSpawner.DeleteVehicle(veh);

                InteractableVehicle? vehicle = await VehicleSpawner.SpawnVehicle(spawn, token).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(token);
                if (vehicle != null)
                {
                    Context.LogAction(ActionLogType.VehicleBayForceSpawn,
                        $"{vehicle.asset.vehicleName} / {vehicle.asset.id} / {vehicle.asset.GUID:N} - FORCED VEHICLE TO SPAWN ID: {spawn2.StructureKey}");
                    Context.Reply(T.VehicleBayForceSuccess, vehicle.asset);
                }
                else
                    throw Context.SendUnknownError();
            }
            else
                Context.Reply(T.VehicleBayNoTarget);
        }
        else if (Context.MatchParameter(0, "link"))
        {
            await Context.AssertPermissions(PermissionLinkSign, token);
            await UniTask.SwitchToMainThread(token);

            Context.AssertHelpCheck(1, "/vehiclebay <link> - Use while looking at a spawner to start linking, then on a sign to link the sign to the spawner.");
            
            if (Context.TryGetBarricadeTarget(out BarricadeDrop? drop) && drop.interactable is InteractableSign)
            {
                if (Context.Player.UnturnedPlayer.TryGetPlayerData(out Components.UCPlayerData c))
                {
                    if (c.Currentlylinking is { Item: not null } proxy)
                    {
                        await spawner.UnlinkSign(drop, token).ConfigureAwait(false);
                        if (await spawner.LinkSign(drop, proxy, token).ConfigureAwait(false))
                        {
                            await UniTask.SwitchToMainThread(token);
                            Context.LogAction(ActionLogType.LinkedVehicleBaySign,
                                $"{drop.asset.itemName} / {drop.asset.id} / {drop.asset.GUID:N} ID: {drop.instanceID} - " +
                                $"LINKED TO SPAWN AT {drop.model.transform.position:N2} KEY: {c.Currentlylinking.PrimaryKey}");
                            VehicleData? data = proxy.Item?.Vehicle?.Item;
                            VehicleAsset? asset = data == null ? null : Assets.find<VehicleAsset>(data.VehicleID);
                            Context.Reply(T.VehicleBayLinkFinished, asset!);
                            c.Currentlylinking = null;
                        }
                        else
                        {
                            await UniTask.SwitchToMainThread(token);
                            throw Context.SendUnknownError();
                        }
                    }
                    else
                        Context.Reply(T.VehicleBayLinkNotStarted);
                }
                else
                    Context.SendUnknownError();
            }
            else
            {
                SqlItem<VehicleSpawn>? spawn = GetBayTarget(Context, spawner);
                if (spawn?.Item is not null)
                {
                    if (Context.Player.UnturnedPlayer.TryGetPlayerData(out Components.UCPlayerData c))
                    {
                        c.Currentlylinking = spawn;
                        Context.Reply(T.VehicleBayLinkStarted);
                    }
                    else
                        Context.SendUnknownError();
                }
                else
                    Context.Reply(T.VehicleBaySpawnNotRegistered);
            }
        }
        else if (Context.MatchParameter(0, "unlink"))
        {
            await Context.AssertPermissions(PermissionUnlinkSign, token);
            await UniTask.SwitchToMainThread(token);

            Context.AssertHelpCheck(1, "/vehiclebay <unlink> - Use while looking at a sign to unlink it from it's spawner.");
            
            BarricadeDrop? sign = GetSignTarget(Context, spawner, out SqlItem<VehicleSpawn> spawn);
            if (sign != null && !sign.GetServersideData().barricade.isDead && sign.interactable is InteractableSign)
            {
                await spawner.UnlinkSign(sign, token).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(token);
                Context.Reply(T.VehicleBayUnlinked, spawn.Item?.Vehicle?.Item is { } data ? Assets.find<VehicleAsset>(data.VehicleID) : null!);
            }
            else
                Context.Reply(T.VehicleBayNoTarget);
        }
        else if (Context.MatchParameter(0, "check", "id", "wtf"))
        {
            await Context.AssertPermissions(PermissionCheck, token);
            await UniTask.SwitchToMainThread(token);

            Context.AssertHelpCheck(1, "/vehiclebay <check|id|wtf> - Tells you what vehicle spawns from the spawner you're looking at.");

            SqlItem<VehicleSpawn>? spawn = GetBayTarget(Context, spawner);
            if (spawn is not null)
            {
                VehicleAsset? asset = spawn.Item?.Vehicle?.Item is { } data ? Assets.find<VehicleAsset>(data.VehicleID) : null;
                Context.Reply(T.VehicleBayCheck, spawn.PrimaryKey.Key, asset!, asset?.id ?? 0);
            }
            else
                Context.Reply(T.VehicleBaySpawnNotRegistered);
        }
        else Context.SendCorrectUsage("/vehiclebay <help|add|remove|savemeta|set|delay|crewseats|register|deregister|force|link|unlink|check> [help|parameters...]");
#endif
    }
#if false
    /// <summary>Linked vehicle >> Sign barricade >> Spawner barricade >> Spawner structure</summary>
    public static async Task<SqlItem<VehicleData>?> GetVehicleTarget(CommandContext ctx, VehicleBay bay, VehicleSpawner spawner, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);
        if (ctx.TryGetVehicleTarget(out InteractableVehicle? vehicle))
        {
            SqlItem<VehicleData>? item = await bay.GetDataProxy(vehicle.asset.GUID, token).ConfigureAwait(false);
            if (item?.Item != null)
                return item;
        }

        if (ctx.TryGetBarricadeTarget(out BarricadeDrop? drop) && spawner.TryGetSpawn(drop, out SqlItem<VehicleSpawn> spawn))
        {
            return spawn.Item?.Vehicle;
        }
        if (ctx.TryGetStructureTarget(out StructureDrop? drop2) && spawner.TryGetSpawn(drop2, out spawn))
        {
            return spawn.Item?.Vehicle;
        }
        return null;
    }
    /// <summary>Linked vehicle >> Sign barricade >> Spawner barricade >> Spawner structure</summary>
    public static SqlItem<VehicleSpawn>? GetBayTarget(CommandContext ctx, VehicleSpawner spawner)
    {
        if (ctx.TryGetVehicleTarget(out InteractableVehicle? vehicle) && spawner.TryGetSpawn(vehicle, out SqlItem<VehicleSpawn> spawn))
        {
            return spawn;
        }
        if (ctx.TryGetBarricadeTarget(out BarricadeDrop? drop) && spawner.TryGetSpawn(drop, out spawn))
        {
            return spawn;
        }
        if (ctx.TryGetStructureTarget(out StructureDrop? drop2) && spawner.TryGetSpawn(drop2, out spawn))
        {
            return spawn;
        }
        return null;
    }
    /// <summary>Sign barricade >> Spawner barricade >> Spawner structure >> Linked Vehicle</summary>
    public static BarricadeDrop? GetSignTarget(CommandContext ctx, VehicleSpawner spawner, out SqlItem<VehicleSpawn> spawn)
    {
        if (ctx.TryGetBarricadeTarget(out BarricadeDrop? drop) && spawner.TryGetSpawn(drop, out spawn))
        {
            return drop;
        }
        if (ctx.TryGetStructureTarget(out StructureDrop? drop2) && spawner.TryGetSpawn(drop2, out spawn) || (ctx.TryGetVehicleTarget(out InteractableVehicle? vehicle) && spawner.TryGetSpawn(vehicle, out spawn)))
        {
            return spawn?.Item?.Sign?.Item?.Buildable?.Drop as BarricadeDrop;
        }

        spawn = null!;
        return null;
    }
#endif
}