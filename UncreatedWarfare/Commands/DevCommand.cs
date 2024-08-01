using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Layouts.Insurgency;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Commands;

[Command("dev")]
[HelpMetadata(nameof(GetHelpMetadata))]
public class DevCommand : IExecutableCommand
{
    private const string Syntax = "/dev <caches|addintel|quickbuild|logmeta|checkvehicle|getpos|onfob|aatest> [parameters...]";

    private static readonly PermissionLeaf PermissionCacheEditor    = new PermissionLeaf("commands.dev.cache_editor",  unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionAddIntel       = new PermissionLeaf("commands.dev.add_intel",     unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionQuickBuild     = new PermissionLeaf("commands.dev.quick_build",   unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionLogMeta        = new PermissionLeaf("commands.dev.log_meta",      unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionCheckVehicle   = new PermissionLeaf("commands.dev.check_vehicle", unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionAATest         = new PermissionLeaf("commands.dev.aa_test",       unturned: false, warfare: true);

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Assorted development commands.",
            Parameters =
            [
                new CommandParameter("Caches")
                {
                    Aliases = [ "cache" ],
                    Description = "Interact with the Insurgency cache location editor.",
                    Permission = PermissionCacheEditor,
                    Parameters =
                    [
                        new CommandParameter("Add")
                        {
                            Aliases = [ "create" ],
                            Description = "Add a new cache location at the caller's position, or a barricade's position if they're looking at it.",
                            Parameters =
                            [
                                new CommandParameter("Name", typeof(string))
                                {
                                    Description = "Name of the new cache location. Defaults to the closest location node.",
                                    IsOptional = true,
                                    IsRemainder = true
                                }
                            ]
                        },
                        new CommandParameter("Next")
                        {
                            Aliases = [ "continue", "start" ],
                            Description = "Select the next cache location in the list."
                        },
                        new CommandParameter("Nearest")
                        {
                            Aliases = [ "closest", "near" ],
                            Description = "Select the cache location closest to the caller."
                        },
                        new CommandParameter("Remove")
                        {
                            Aliases = [ "delete" ],
                            Description = "Delete the selected cache location."
                        },
                        new CommandParameter("disable")
                        {
                            Description = "Disable the selected cache location. This will not delete the location."
                        },
                        new CommandParameter("enable")
                        {
                            Description = "Enable the selected cache location if it's been disabled."
                        },
                        new CommandParameter("goto")
                        {
                            Description = "Teleport to the selected cache location."
                        },
                        new CommandParameter("move")
                        {
                            Aliases = [ "stloc", "stpos" ],
                            Description = "Set the selected cache location's position to the caller's position, or a barricade's position if they're looking at it."
                        },
                        new CommandParameter("stop")
                        {
                            Aliases = [ "break" ],
                            Description = "Exit the cache editor."
                        },
                    ]
                },
                new CommandParameter("AddIntel")
                {
                    Description = "Add intel points to the attackers in the active insurgency game.",
                    Permission = PermissionAddIntel,
                    Parameters =
                    [
                        new CommandParameter("Amount", typeof(int))
                        {
                            Description = "Number of intel points to add."
                        }
                    ]
                },
                new CommandParameter("QuickBuild")
                {
                    Aliases = [ "build", "qb" ],
                    Description = "Skips digging the buildable you're looking at.",
                    Permission = PermissionQuickBuild
                },
                new CommandParameter("LogMeta")
                {
                    Aliases = [ "logstate", "metadata" ],
                    Description = "Logs the metadata of the barricade you're looking at in Base64.",
                    Permission = PermissionLogMeta
                },
                new CommandParameter("CheckVehicle")
                {
                    Aliases = [ "cv" ],
                    Description = "Logs information about the vehicle you're looking at or inside of.",
                    Permission = PermissionCheckVehicle
                },
                new CommandParameter("AATest")
                {
                    Aliases = [ "aa" ],
                    Description = "Spawns an AA target at a specified point.",
                    Permission = PermissionAATest
                }
            ]
        };
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertHelpCheck(0, Syntax + " - Developer commands for config setup.");

        if (Context.MatchParameter(0, "caches", "cache"))
        {
            await Context.AssertPermissions(PermissionCacheEditor, token);
            await UniTask.SwitchToMainThread(token);

            Context.AssertHelpCheck(1, CacheLocationsEditCommand.Syntax);
            ++Context.ArgumentOffset;
            await CacheLocationsEditCommand.Execute(Context);
        }
        else if (Context.MatchParameter(0, "addintel"))
        {
            await Context.AssertPermissions(PermissionAddIntel, token);
            await UniTask.SwitchToMainThread(token);

            Context.AssertGamemode(out Insurgency insurgency);

            Context.AssertHelpCheck(1, "/dev addintel <amount>. Adds intelligence points to the attacking team.");

            if (Context.TryGet(1, out int points))
            {
                insurgency.AddIntelligencePoints(points, Context.Player);

                Context.LogAction(ActionLogType.AddIntel, "ADDED " + points.ToString(Data.AdminLocale) + " OF INTEL");
                Context.ReplyString($"Added {points} intelligence points.", "ebd491");
            }
            else throw Context.ReplyString("You must supply a valid number of intelligence points (negative or positive).", "c7a29f");
        }
        else if (Context.MatchParameter(0, "quickbuild", "build", "qb"))
        {
            await Context.AssertPermissions(PermissionQuickBuild, token);
            await UniTask.SwitchToMainThread(token);

            Context.AssertHelpCheck(1, "/dev <quickbuild|build|qb>. Skips digging the buildable you're looking at.");

            Context.AssertRanByPlayer();

            if (!Context.TryGetTargetInfo(out RaycastInfo? cast, RayMasks.BARRICADE | RayMasks.STRUCTURE | RayMasks.VEHICLE))
                throw Context.ReplyString("You are not looking at a barricade, structure, or vehicle.", "c7a29f");
            
            if (!cast.transform.TryGetComponent(out IShovelable shovelable))
                throw Context.ReplyString($"This {cast.transform.tag.ToLowerInvariant()} ({cast.transform.name}) is not buildable.", "c7a29f");

            shovelable.QuickShovel(Context.Player);

            Context.ReplyString($"Successfully built or repaired {cast.transform.name}", "ebd491");
        }
        else if (Context.MatchParameter(0, "logmeta", "logstate", "metadata"))
        {
            await Context.AssertPermissions(PermissionLogMeta, token);
            await UniTask.SwitchToMainThread(token);

            Context.AssertHelpCheck(1, "/dev <logmeta|logstate|metadata>. Logs the metadata of the barricade you're looking at in Base64.");

            Context.AssertRanByPlayer();

            if (!Context.TryGetBarricadeTarget(out BarricadeDrop? drop))
                throw Context.ReplyString("You are not looking at a barricade.", "c7a29f");

            byte[] stateArray = drop.GetServersideData().barricade.state;
            string state = Convert.ToBase64String(stateArray);

            L.Log($"State of {drop.asset.itemName}: ({stateArray.Length.ToString(Context.Culture)} B) \"{state}\".", ConsoleColor.DarkCyan);

            Context.ReplyString($"Metadata state ({stateArray.Length.ToString(Context.Culture)} B) has been logged to console.", "ebd491");
        }
        else if (Context.MatchParameter(0, "checkvehicle", "cv"))
        {
            await Context.AssertPermissions(PermissionCheckVehicle, token);
            await UniTask.SwitchToMainThread(token);

            Context.AssertGamemode<IVehicles>();

            Context.AssertRanByPlayer();

            Context.AssertHelpCheck(1, "/dev <checkvehicle|cv>. Logs information about the vehicle you're looking at or inside of.");

            if (!Context.TryGetVehicleTarget(out InteractableVehicle? vehicle))
            {
                throw Context.ReplyString("You are not inside or looking at a vehicle.", "c7a29f");
            }

            if (!vehicle.transform.TryGetComponent(out VehicleComponent component))
                throw Context.ReplyString("This vehicle does have a VehicleComponent.", "c7a29f");
            
            Context.ReplyString("Vehicle logged successfully. Check console", "ebd491");

            L.Log($"{vehicle.asset.vehicleName.ToUpper()}", ConsoleColor.Cyan);

            L.Log($"    Is In VehicleBay: {component.IsInVehiclebay}\n", ConsoleColor.Cyan);

            if (component.IsInVehiclebay)
            {
                L.Log($"    Team:    {component.Data!.Item!.Team}", ConsoleColor.Cyan);
                L.Log($"    Type:    {component.Data.Item.Type}", ConsoleColor.Cyan);
                L.Log($"    Tickets: {component.Data.Item.TicketCost}", ConsoleColor.Cyan);
                L.Log($"    Branch:  {component.Data.Item.Branch}\n", ConsoleColor.Cyan);
            }

            L.Log($"    Quota: {component.Quota}/{component.RequiredQuota}\n", ConsoleColor.Cyan);

            L.Log("    Usage Table:", ConsoleColor.Cyan);
            foreach (KeyValuePair<ulong, double> entry in component.UsageTable)
            {
                L.Log($"        {entry.Key}'s time in vehicle: {entry.Value} s", ConsoleColor.Cyan);
            }

            L.Log("    Transport Table:", ConsoleColor.Cyan);
            foreach (KeyValuePair<ulong, Vector3> entry in component.TransportTable)
            {
                L.Log($"        {entry.Key}'s starting position: {entry.Value}", ConsoleColor.Cyan);
            }

            L.Log("    Damage Table:", ConsoleColor.Cyan);
            foreach (KeyValuePair<ulong, KeyValuePair<ushort, DateTime>> entry in component.DamageTable)
            {
                L.Log($"        {entry.Key}'s damage so far: {entry.Value.Key} ({(DateTime.Now - entry.Value.Value).TotalSeconds} seconds ago)", ConsoleColor.Cyan);
            }
        }
        else if (Context.MatchParameter(0, "aatest", "aa"))
        {
            await Context.AssertPermissions(PermissionAATest, token);
            await UniTask.SwitchToMainThread(token);

            Context.AssertGamemode<IVehicles>();

            VehicleBay? bay = Data.Singletons.GetSingleton<VehicleBay>();
            if (bay == null)
                throw Context.SendGamemodeError();

            Context.AssertHelpCheck(1, "/dev <aatest|aa>. Spawns an AA target at a specified point.");

            if (!Context.TryGet(1, out VehicleAsset? asset, out _, true))
            {
                if (!Context.HasArgs(2))
                    throw Context.ReplyString($"Please specify a vehicle name.", "ebd491");
                
                throw Context.ReplyString($"A vehicle called '{Context.Get(1)!} does not exist", "ebd491");
            }

            InteractableVehicle? vehicle = await VehicleSpawner.SpawnLockedVehicle(asset.GUID,
                Context.Player.UnturnedPlayer.transform.TransformPoint(new Vector3(0, 300, 200)), Quaternion.Euler(0, 0, 0),
                token: token).ConfigureAwait(false);

            await UniTask.SwitchToMainThread(token);

            Context.ReplyString(
                $"Successfully spawned AA target: {(vehicle == null ? asset.GUID.ToString("N") : vehicle.asset.vehicleName)}",
                "ebd491");
        }
        else throw Context.SendCorrectUsage(Syntax);
    }
}
