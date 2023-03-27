using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Commands;

public class DevCommand : AsyncCommand
{
    private const string Syntax = "/dev <addcache|gencaches|addintel|quickbuild|logmeta|checkvehicle|getpos|onfob|aatest> [parameters...]";

    public DevCommand() : base("dev", EAdminType.VANILLA_ADMIN) { }

    public override async Task Execute(CommandInteraction ctx, CancellationToken token)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertHelpCheck(0, Syntax + " - Developer commands for config setup.");

        if (ctx.MatchParameter(0, "addcache"))
        {
            ctx.AssertGamemode(out Insurgency ins);

            ctx.AssertRanByPlayer();

            ctx.AssertHelpCheck(1, "/dev addcache (at current location)");

            if (!Gamemode.Config.BarricadeInsurgencyCache.HasValue ||
                !Gamemode.Config.BarricadeInsurgencyCache.Value.Exists)
                throw ctx.ReplyString("Cache GUID is not set correctly.");

            if (ctx.TryGetTarget(out BarricadeDrop cache) && cache != null && cache.asset.GUID == Gamemode.Config.BarricadeInsurgencyCache.Value.Guid)
            {
                SerializableTransform transform = new SerializableTransform(cache.model);

                ins.AddCacheSpawn(transform);
                ctx.ReplyString("Added new cache spawn: " + transform.ToString(), "ebd491");
                ctx.LogAction(ActionLogType.AddCache, "ADDED CACHE SPAWN AT " + transform.ToString());
            }
            else throw ctx.ReplyString("You must be looking at a CACHE barricade.", "c7a29f");
        }
        else if (ctx.MatchParameter(0, "gencaches"))
        {
            ctx.AssertGamemode<Insurgency>();

            ctx.AssertHelpCheck(1, "/dev gencaches (generates a json file for all placed caches)");

            if (!Gamemode.Config.BarricadeInsurgencyCache.HasValue ||
                !Gamemode.Config.BarricadeInsurgencyCache.Value.Exists)
                throw ctx.ReplyString("Cache GUID is not set correctly.");
            Guid g = Gamemode.Config.BarricadeInsurgencyCache.Value.Guid;
            IEnumerable<BarricadeDrop> caches = UCBarricadeManager.NonPlantedBarricades.Where(b => b.asset.GUID == g);

            FileStream writer = File.Create("C" + Path.VolumeSeparatorChar + Path.DirectorySeparatorChar + Path.Combine("Users", "USER", "Desktop", "cachespanws.json"));

            StringBuilder builder = new StringBuilder();

            bool a = false;
            foreach (BarricadeDrop b in caches)
            {
                if (!a)
                {
                    builder.Append(", \n");
                    a = true;
                }

                builder.Append($"new SerializableTransform({b.model.transform.position.x.ToString(Data.AdminLocale)}f, " +
                        $"{b.model.transform.position.y.ToString(Data.AdminLocale)}f, " +
                        $"{b.model.transform.position.z.ToString(Data.AdminLocale)}f, " +
                        $"{b.model.transform.eulerAngles.x.ToString(Data.AdminLocale)}f, " +
                        $"{b.model.transform.eulerAngles.y.ToString(Data.AdminLocale)}f, " +
                        $"{b.model.transform.eulerAngles.z.ToString(Data.AdminLocale)}f)");
            }

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(builder.ToString());
            writer.Write(bytes, 0, bytes.Length);
            writer.Close();
            writer.Dispose();

            ctx.ReplyString($"Written {bytes.Length} bytes to file.");
        }
        else if (ctx.MatchParameter(0, "addintel"))
        {
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
            ctx.AssertHelpCheck(1, "/dev <quickbuild|build|qb>. Skips digging the buildable you're looking at.");

            ctx.AssertRanByPlayer();

            if (ctx.TryGetTarget(out BarricadeDrop drop))
            {
                if (drop.model.TryGetComponent<BuildableComponent>(out BuildableComponent? buildable))
                {
                    buildable.Build();
                    ctx.ReplyString($"Successfully built {drop.asset.itemName}", "ebd491");
                }
                else throw ctx.ReplyString($"This barricade ({drop.asset.itemName}) is not buildable.", "c7a29f");
            }
            else throw ctx.ReplyString($"You are not looking at a barricade.", "c7a29f");
        }
        else if (ctx.MatchParameter(0, "logmeta", "logstate", "metadata"))
        {
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
            ctx.AssertGamemode<IVehicles>();

            ctx.AssertRanByPlayer();

            ctx.AssertHelpCheck(1, "/dev <checkvehicle|cv>. Logs information about the vehicle you're looking at or inside of.");

            InteractableVehicle vehicle = ctx.Caller!.Player.movement.getVehicle();
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
            ctx.AssertRanByPlayer();

            ctx.AssertHelpCheck(1, "/dev <getpos|cvc>. Gets your current rotation and position (like /test zone).");

            ctx.ReplyString($"Your position: {ctx.Caller!.Position} - Your rotation: {ctx.Caller!.Player.transform.eulerAngles.y}", "ebd491");
        }
        else if (ctx.MatchParameter(0, "onfob", "fob"))
        {
            ctx.AssertGamemode<IFOBs>();

            ctx.AssertRanByPlayer();

            ctx.AssertHelpCheck(1, "/dev <onfob|fob>. Gets what FOB you're on, if any.");

            FOB? fob = FOB.GetNearestFOB(ctx.Caller!.Position, EFobRadius.FULL_WITH_BUNKER_CHECK, ctx.Caller!.GetTeam());
            ctx.ReplyString((fob != null
                ? $"Your nearest FOB is: {fob.Name.Colorize(fob.UIColor)} ({(ctx.Caller.Position - fob.Position).magnitude}m away)"
                : "You are not near a FOB."), "ebd491");
        }
        else if (ctx.MatchParameter(0, "aatest", "aa"))
        {
            ctx.AssertGamemode<IVehicles>();

            VehicleBay? bay = Data.Singletons.GetSingleton<VehicleBay>();
            if (bay == null)
                throw ctx.SendGamemodeError();

            ctx.AssertHelpCheck(1, "/dev <aatest|aa>. Spawns an AA target at a specified point.");

            if (ctx.TryGet(1, out VehicleAsset asset, out _, true))
            {
                InteractableVehicle? vehicle = await VehicleSpawner.SpawnLockedVehicle(asset.GUID, ctx.Caller!.Player.transform.TransformPoint(new Vector3(0, 300, 200)), Quaternion.Euler(0, 0, 0), token: token).ConfigureAwait(false);
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
