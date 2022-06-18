using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;

public class DevCommand : Command
{
    private const string SYNTAX = "/dev <addcache|gencaches|addintel|quickbuild|logmeta|checkvehicle|getpos|onfob|aatest> [parameters...]";

    public DevCommand() : base("dev", EAdminType.VANILLA_ADMIN) { }

    public override void Execute(CommandInteraction ctx)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertHelpCheck(0, SYNTAX + " - Developer commands for config setup.");

        if (ctx.MatchParameter(0, "addcache"))
        {
            ctx.AssertGamemode<Insurgency>();

            ctx.AssertRanByPlayer();

            ctx.AssertHelpCheck(1, "/dev addcache (at current location)");

            if (ctx.TryGetTarget(out BarricadeDrop cache) && cache != null && cache.asset.GUID == Gamemode.Config.Barricades.InsurgencyCacheGUID)
            {
                SerializableTransform transform = new SerializableTransform(cache.model);
                Gamemode.Config.MapConfig.AddCacheSpawn(transform);
                ctx.Reply("Added new cache spawn: " + transform.ToString().Colorize("ebd491"));
                ctx.LogAction(EActionLogType.ADD_CACHE, "ADDED CACHE SPAWN AT " + transform.ToString());
            }
            else throw ctx.Reply("You must be looking at a CACHE barricade.".Colorize("c7a29f"));
        }
        else if (ctx.MatchParameter(0, "gencaches"))
        {
            ctx.AssertGamemode<Insurgency>();

            ctx.AssertHelpCheck(1, "/dev gencaches (generates a json file for all placed caches)");

            IEnumerable<BarricadeDrop> caches = UCBarricadeManager.AllBarricades.Where(b =>
                b.asset.GUID == Gamemode.Config.Barricades.InsurgencyCacheGUID);

            FileStream writer = File.Create("C" + Path.VolumeSeparatorChar + Path.DirectorySeparatorChar + Path.Combine("Users", "USER", "Desktop", "cachespanws.json"));

            string line = "";

            bool a = false;
            foreach (BarricadeDrop b in caches)
            {
                if (!a)
                {
                    line += ", \n";
                    a = true;
                }

                line += $"new SerializableTransform({b.model.transform.position.x.ToString(Data.Locale)}f, " +
                        $"{b.model.transform.position.y.ToString(Data.Locale)}f, " +
                        $"{b.model.transform.position.z.ToString(Data.Locale)}f, " +
                        $"{b.model.transform.eulerAngles.x.ToString(Data.Locale)}f, " +
                        $"{b.model.transform.eulerAngles.y.ToString(Data.Locale)}f, " +
                        $"{b.model.transform.eulerAngles.z.ToString(Data.Locale)}f)";
            }

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(line);
            writer.Write(bytes, 0, bytes.Length);
            writer.Close();
            writer.Dispose();

            ctx.Reply($"Written {bytes.Length} bytes to file.");
        }
        else if (ctx.MatchParameter(0, "addintel"))
        {
            ctx.AssertGamemode(out Insurgency insurgency);

            ctx.AssertHelpCheck(1, "/dev addintel <amount>. Adds intelligence points to the attacking team.");

            if (ctx.TryGet(1, out int points))
            {
                insurgency.AddIntelligencePoints(points);

                ctx.LogAction(EActionLogType.ADD_INTEL, "ADDED " + points.ToString(Data.Locale) + " OF INTEL");
                ctx.Reply($"Added {points} intelligence points.".Colorize("ebd491"));
            }
            else throw ctx.Reply($"You must supply a valid number of intelligence points (negative or positive).".Colorize("c7a29f"));
        }
        else if (ctx.MatchParameter(0, "quickbuild", "build", "qb"))
        {
            ctx.AssertHelpCheck(1, "/dev <quickbuild|build|qb>. Skips digging the buildable you're looking at.");

            ctx.AssertRanByPlayer();

            if (ctx.TryGetTarget(out BarricadeDrop drop))
            {
                if (drop.model.TryGetComponent<BuildableComponent>(out var buildable))
                {
                    buildable.Build();
                    ctx.Reply($"Successfully built {drop.asset.itemName}".Colorize("ebd491"));
                }
                else throw ctx.Reply($"This barricade ({drop.asset.itemName}) is not buildable.".Colorize("c7a29f"));
            }
            else throw ctx.Reply($"You are not looking at a barricade.".Colorize("c7a29f"));
        }
        else if (ctx.MatchParameter(0, "logmeta", "logstate", "metadata"))
        {
            ctx.AssertHelpCheck(1, "/dev <logmeta|logstate|metadata>. Logs the metadata of the barricade you're looking at in Base64.");

            ctx.AssertRanByPlayer();

            if (ctx.TryGetTarget(out BarricadeDrop drop))
            {
                var data = drop.GetServersideData();
                string state = Convert.ToBase64String(data.barricade.state);
                L.Log($"BARRICADE STATE: {state}");
                ctx.Reply($"Metadata state has been logged to console. State: {state}".Colorize("ebd491"));
            }
            else throw ctx.Reply($"You are not looking at a barricade.".Colorize("c7a29f"));
        }
        else if (ctx.MatchParameter(0, "checkvehicle", "cv"))
        {
            ctx.AssertGamemode<IVehicles>();

            ctx.AssertRanByPlayer();

            ctx.AssertHelpCheck(1, "/dev <checkvehicle|cv>. Logs information about the vehicle you're looking at or inside of.");

            InteractableVehicle vehicle = ctx.Caller!.Player.movement.getVehicle();
            if (vehicle is null && !(ctx.TryGetTarget(out vehicle) && vehicle is not null))
                throw ctx.Reply($"You are not inside or looking at a vehicle.".Colorize("c7a29f"));
            if (vehicle.transform.TryGetComponent(out VehicleComponent component))
            {
                ctx.Reply($"Vehicle logged successfully. Check console".Colorize("ebd491"));

                L.Log($"{vehicle.asset.vehicleName.ToUpper()}");

                L.Log($"    Is In VehicleBay: {component.isInVehiclebay}\n");

                if (component.isInVehiclebay)
                {
                    L.Log($"    Team: {component.Data.Team}");
                    L.Log($"    Type: {component.Data.Type}");
                    L.Log($"    Tickets: {component.Data.TicketCost}");
                    L.Log($"    Branch: {component.Data.Branch}\n");
                }

                L.Log($"    Quota: {component.Quota}/{component.RequiredQuota}\n");

                L.Log($"    Usage Table:");
                foreach (var entry in component.UsageTable)
                    L.Log($"        {entry.Key}'s time in vehicle: {entry.Value} s");

                L.Log($"    Transport Table:");
                foreach (var entry in component.TransportTable)
                    L.Log($"        {entry.Key}'s starting position: {entry.Value}");

                L.Log($"    Damage Table:");
                foreach (var entry in component.DamageTable)
                    L.Log($"        {entry.Key}'s damage so far: {entry.Value.Key} ({(DateTime.Now - entry.Value.Value).TotalSeconds} seconds ago)");
            }
            else throw ctx.Reply($"This vehicle does have a VehicleComponent".Colorize("c7a29f"));
        }
        else if (ctx.MatchParameter(0, "getpos", "cvc"))
        {
            ctx.AssertRanByPlayer();

            ctx.AssertHelpCheck(1, "/dev <getpos|cvc>. Gets your current rotation and position (like /test zone).");

            ctx.Reply($"Your position: {ctx.Caller!.Position} - Your rotation: {ctx.Caller!.Player.transform.eulerAngles.y}".Colorize("ebd491"));
        }
        else if (ctx.MatchParameter(0, "onfob", "fob"))
        {
            ctx.AssertGamemode<IFOBs>();

            ctx.AssertRanByPlayer();

            ctx.AssertHelpCheck(1, "/dev <onfob|fob>. Gets what FOB you're on, if any.");

            FOB? fob = FOB.GetNearestFOB(ctx.Caller!.Position, EFOBRadius.FULL_WITH_BUNKER_CHECK, ctx.Caller!.GetTeam());
            if (fob != null)
                ctx.Reply($"Your nearest FOB is: {fob.Name.Colorize(fob.UIColor)} ({(ctx.Caller!.Position - fob.Position).magnitude}m away)".Colorize("ebd491"));
            else
                ctx.Reply($"You are not near a FOB.".Colorize("ebd491"));
        }
        else if (ctx.MatchParameter(0, "aatest", "aa"))
        {
            ctx.AssertGamemode<IVehicles>();

            ctx.AssertHelpCheck(1, "/dev <aatest|aa>. Spawns an AA target at a specified point.");

            if (ctx.TryGet(1, out VehicleAsset asset, out bool multipleResults, true, false))
            {
                InteractableVehicle? vehicle = VehicleBay.SpawnLockedVehicle(asset.GUID, ctx.Caller!.Player.transform.TransformPoint(new Vector3(0, 300, 200)), Quaternion.Euler(0, 0, 0), out _);
                ctx.Reply($"Successfully spawned AA target: {(vehicle == null ? asset.GUID.ToString("N") : vehicle.asset.vehicleName)}".Colorize("ebd491"));
            }
            else if (!ctx.HasArgs(2))
                throw ctx.Reply($"Please specify a vehicle name.".Colorize("ebd491"));
            else
                throw ctx.Reply($"A vehicle called '{ctx.Get(1)!} does not exist".Colorize("ebd491"));
        }
        else throw ctx.SendCorrectUsage(SYNTAX);
    }
}
