using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Commands;

public class DevCommand : AsyncCommand
{
    private const string Syntax = "/dev <caches|addintel|quickbuild|logmeta|checkvehicle|getpos|onfob|aatest> [parameters...]";

    public DevCommand() : base("dev", EAdminType.ADMIN) { }

    public override async Task Execute(CommandInteraction ctx, CancellationToken token)
    {
        ctx.AssertHelpCheck(0, Syntax + " - Developer commands for config setup.");

        if (ctx.MatchParameter(0, "caches", "cache"))
        {
            ctx.AssertHelpCheck(1, CacheLocationsEditCommand.Syntax);
            ++ctx.Offset;
            await CacheLocationsEditCommand.Execute(ctx);
        }
        else if (ctx.MatchParameter(0, "addintel"))
        {
            ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);
            ctx.AssertGamemode(out Insurgency insurgency);

            ctx.AssertHelpCheck(1, "/dev addintel <amount>. Adds intelligence points to the attacking team.");

            if (ctx.TryGet(1, out int points))
            {
                insurgency.AddIntelligencePoints(points, ctx.Caller);

                ctx.LogAction(ActionLogType.AddIntel, "ADDED " + points.ToString(Data.AdminLocale) + " OF INTEL");
                ctx.ReplyString($"Added {points} intelligence points.", "ebd491");
            }
            else throw ctx.ReplyString($"You must supply a valid number of intelligence points (negative or positive).", "c7a29f");
        }
        else if (ctx.MatchParameter(0, "quickbuild", "build", "qb"))
        {
            ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);
            ctx.AssertHelpCheck(1, "/dev <quickbuild|build|qb>. Skips digging the buildable you're looking at.");

            ctx.AssertRanByPlayer();

            RaycastInfo cast = DamageTool.raycast(new Ray(ctx.Caller.Player.look.aim.position, ctx.Caller.Player.look.aim.forward), 4f, RayMasks.BARRICADE | RayMasks.STRUCTURE | RayMasks.VEHICLE, ctx.Caller.Player);
            if (cast.transform == null)
                throw ctx.ReplyString("You are not looking at a barricade, structure, or vehicle.", "c7a29f");
            if (cast.transform.TryGetComponent(out IShovelable shovelable))
            {
                shovelable.QuickShovel(ctx.Caller);
                ctx.ReplyString($"Successfully built or repaired {cast.transform.name}", "ebd491");
            }
            else throw ctx.ReplyString($"This {cast.transform.tag.ToLowerInvariant()} ({cast.transform.name}) is not buildable.", "c7a29f");
        }
        else if (ctx.MatchParameter(0, "logmeta", "logstate", "metadata"))
        {
            ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);
            ctx.AssertHelpCheck(1, "/dev <logmeta|logstate|metadata>. Logs the metadata of the barricade you're looking at in Base64.");

            ctx.AssertRanByPlayer();

            if (ctx.TryGetTarget(out BarricadeDrop drop))
            {
                BarricadeData data = drop.GetServersideData();
                string state = Convert.ToBase64String(data.barricade.state);
                L.Log($"BARRICADE STATE: {state}", ConsoleColor.DarkCyan);
                ctx.ReplyString($"Metadata state has been logged to console. State: {state}", "ebd491");
            }
            else throw ctx.ReplyString($"You are not looking at a barricade.", "c7a29f");
        }
        else if (ctx.MatchParameter(0, "checkvehicle", "cv"))
        {
            ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);
            ctx.AssertGamemode<IVehicles>();

            ctx.AssertRanByPlayer();

            ctx.AssertHelpCheck(1, "/dev <checkvehicle|cv>. Logs information about the vehicle you're looking at or inside of.");

            InteractableVehicle vehicle = ctx.Caller.Player.movement.getVehicle();
            if (vehicle is null && !(ctx.TryGetTarget(out vehicle) && vehicle is not null))
                throw ctx.ReplyString("You are not inside or looking at a vehicle.", "c7a29f");
            if (vehicle.transform.TryGetComponent(out VehicleComponent component))
            {
                ctx.ReplyString("Vehicle logged successfully. Check console", "ebd491");

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
                    L.Log($"        {entry.Key}'s time in vehicle: {entry.Value} s", ConsoleColor.Cyan);

                L.Log("    Transport Table:", ConsoleColor.Cyan);
                foreach (KeyValuePair<ulong, Vector3> entry in component.TransportTable)
                    L.Log($"        {entry.Key}'s starting position: {entry.Value}", ConsoleColor.Cyan);

                L.Log("    Damage Table:", ConsoleColor.Cyan);
                foreach (KeyValuePair<ulong, KeyValuePair<ushort, DateTime>> entry in component.DamageTable)
                    L.Log($"        {entry.Key}'s damage so far: {entry.Value.Key} ({(DateTime.Now - entry.Value.Value).TotalSeconds} seconds ago)", ConsoleColor.Cyan);
            }
            else throw ctx.ReplyString($"This vehicle does have a VehicleComponent", "c7a29f");
        }
        else if (ctx.MatchParameter(0, "getpos", "cvc"))
        {
            ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);
            ctx.AssertRanByPlayer();

            ctx.AssertHelpCheck(1, "/dev <getpos|cvc>. Gets your current rotation and position (like /test zone).");

            ctx.ReplyString($"Your position: {ctx.Caller.Position} - Your rotation: {ctx.Caller.Player.transform.eulerAngles.y}", "ebd491");
        }
        else if (ctx.MatchParameter(0, "onfob", "fob"))
        {
            ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);
            ctx.AssertGamemode<IFOBs>();

            ctx.AssertRanByPlayer();

            ctx.AssertHelpCheck(1, "/dev <onfob|fob>. Gets what FOB you're on, if any.");

            FOB? fob = Data.Singletons.GetSingleton<FOBManager>()?.FindNearestFOB<FOB>(ctx.Caller.Position, ctx.Caller.GetTeam());
            ctx.ReplyString((fob != null
                ? $"Your nearest FOB is: {fob.Name.Colorize(fob.GetUIColor())} ({(ctx.Caller.Position - fob.transform.position).magnitude}m away)"
                : "You are not near a FOB."), "ebd491");
        }
        else if (ctx.MatchParameter(0, "aatest", "aa"))
        {
            ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);
            ctx.AssertGamemode<IVehicles>();

            VehicleBay? bay = Data.Singletons.GetSingleton<VehicleBay>();
            if (bay == null)
                throw ctx.SendGamemodeError();

            ctx.AssertHelpCheck(1, "/dev <aatest|aa>. Spawns an AA target at a specified point.");

            if (ctx.TryGet(1, out VehicleAsset asset, out _, true))
            {
                InteractableVehicle? vehicle = await VehicleSpawner.SpawnLockedVehicle(asset.GUID, ctx.Caller.Player.transform.TransformPoint(new Vector3(0, 300, 200)), Quaternion.Euler(0, 0, 0), token: token).ConfigureAwait(false);
                await UCWarfare.ToUpdate(token);
                ctx.ReplyString($"Successfully spawned AA target: {(vehicle == null ? asset.GUID.ToString("N") : vehicle.asset.vehicleName)}", "ebd491");
            }
            else if (!ctx.HasArgs(2))
                throw ctx.ReplyString($"Please specify a vehicle name.", "ebd491");
            else
                throw ctx.ReplyString($"A vehicle called '{ctx.Get(1)!} does not exist", "ebd491");
        }
        else throw ctx.SendCorrectUsage(Syntax);
    }
}
