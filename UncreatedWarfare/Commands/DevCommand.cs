using Rocket.API;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Commands;

public class DevCommand : IRocketCommand
{
    private readonly List<string> _permissions = new List<string>(1) { "uc.dev" };
    private readonly List<string> _aliases = new List<string>(0);
    public AllowedCaller AllowedCaller => AllowedCaller.Player;
    public string Name => "dev";
    public string Help => "Dev command for various server setup features.";
    public string Syntax => "/dev <addcache|gencaches|addintel|quickbuild|logmeta|checkvehicle|getpos|onfob|aatest> [parameters...]";
    public List<string> Aliases => _aliases;
	public List<string> Permissions => _permissions;
    public void Execute(IRocketPlayer caller, string[] command)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        CommandContext ctx = new CommandContext(caller, command);

        if (ctx.MatchParameter(0, "addcache"))
        {
            if (Data.Is(out Insurgency insurgency))
            {
                if (ctx.TryGetTarget(out BarricadeDrop cache) && cache != null && cache.asset.GUID == Gamemode.Config.Barricades.InsurgencyCacheGUID)
                {
                    SerializableTransform transform = new SerializableTransform(cache.model);
                    Gamemode.Config.MapConfig.AddCacheSpawn(transform);
                    ctx.Reply("Added new cache spawn: " + transform.ToString().Colorize("ebd491"));
                    ctx.LogAction(EActionLogType.ADD_CACHE, "ADDED CACHE SPAWN AT " + transform.ToString());
                }
                else
                    ctx.Reply("You must be looking at a CACHE barricade.".Colorize("c7a29f"));
            }
            else
                ctx.Reply("Gamemode must be Insurgency in order to use this command.".Colorize("c7a29f"));
        }
        else if (ctx.MatchParameter(0, "gencaches"))
        {
            // DO NOT USE THIS COMMAND ON LINUX

            if (Data.Is(out Insurgency insurgency))
            {
                IEnumerable<BarricadeDrop> caches = UCBarricadeManager.AllBarricades.Where(b =>
                    b.asset.GUID == Gamemode.Config.Barricades.InsurgencyCacheGUID);

                FileStream writer = File.Create(Path.Combine("C:\\Users\\USER\\Desktop\\cachespanws.json"));

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
            else
                ctx.SendGamemodeError();
        }
        else if (ctx.MatchParameter(0, "addintel"))
        {
            if (Data.Is(out Insurgency insurgency))
            {
                if (ctx.TryGet(1, out int points))
                {
                    insurgency.AddIntelligencePoints(points);

                    ctx.LogAction(EActionLogType.ADD_INTEL, "ADDED " + points.ToString(Data.Locale) + " OF INTEL");
                    ctx.Reply($"Added {points} intelligence points.".Colorize("ebd491"));
                }
                else
                    ctx.Reply($"You must supply a valid number of intelligence points (negative or positive).".Colorize("c7a29f"));
            }
            else
                ctx.SendGamemodeError();
        }
        else if (ctx.MatchParameter(0, "quickbuild", "build", "qb"))
        {
            if (ctx.TryGetTarget(out BarricadeDrop drop))
            {
                if (drop.model.TryGetComponent<BuildableComponent>(out var buildable))
                {
                    buildable.Build();
                    ctx.Reply($"Successfully built {drop.asset.itemName}".Colorize("ebd491"));
                }
                else
                    ctx.Reply($"This barricade ({drop.asset.itemName}) is not buildable.".Colorize("c7a29f"));
            }
            else
                ctx.Reply($"You are not looking at a barricade.".Colorize("c7a29f"));
        }
        else if (ctx.MatchParameter(0, "logmeta", "logstate", "metadata"))
        {
            if (ctx.TryGetTarget(out BarricadeDrop drop))
            {
                var data = drop.GetServersideData();
                string state = Convert.ToBase64String(data.barricade.state);
                L.Log($"BARRICADE STATE: {state}");
                ctx.Reply($"Metadata state has been logged to console. State: {state}".Colorize("ebd491"));
            }
            else
                ctx.Reply($"You are not looking at a barricade.".Colorize("c7a29f"));
        }
        else if (ctx.MatchParameter(0, "checkvehicle", "cv"))
        {
            InteractableVehicle vehicle = ctx.Caller!.Player.movement.getVehicle();
            if (vehicle is null && !(ctx.TryGetTarget(out vehicle) && vehicle is not null))
            {
                ctx.Reply($"You are not inside or looking at a vehicle.".Colorize("c7a29f"));
            }
            else
            {
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
                    {
                        L.Log($"        {entry.Key}'s time in vehicle: {entry.Value} s");
                    }
                    L.Log($"    Transport Table:");
                    foreach (var entry in component.TransportTable)
                    {
                        L.Log($"        {entry.Key}'s starting position: {entry.Value}");
                    }
                    L.Log($"    Damage Table:");
                    foreach (var entry in component.DamageTable)
                    {
                        L.Log($"        {entry.Key}'s damage so far: {entry.Value.Key} ({(DateTime.Now - entry.Value.Value).TotalSeconds} seconds ago)");
                    }
                }
                else
                    ctx.Reply($"This vehicle does have a VehicleComponent".Colorize("c7a29f"));
            }
        }
        else if (ctx.MatchParameter(0, "getpos", "cvc"))
        {
            ctx.Reply($"Your position: {ctx.Caller!.Position} - Your rotation: {ctx.Caller!.Player.transform.eulerAngles.y}".Colorize("ebd491"));
        }
        else if (ctx.MatchParameter(0, "onfob", "fob"))
        {
            FOB? fob = FOB.GetNearestFOB(ctx.Caller!.Position, EFOBRadius.FULL_WITH_BUNKER_CHECK, ctx.Caller!.GetTeam());
            if (fob != null)
                ctx.Reply($"Your nearest FOB is: {fob.Name.Colorize(fob.UIColor)} ({(ctx.Caller!.Position - fob.Position).magnitude}m away)".Colorize("ebd491"));
            else
                ctx.Reply($"You are not near a FOB.".Colorize("ebd491"));
        }
        else if (ctx.MatchParameter(0, "aatest", "aa"))
        {
            if (ctx.TryGet(1, out VehicleAsset asset, out bool multipleResults, true, false))
            {
                InteractableVehicle? vehicle = VehicleBay.SpawnLockedVehicle(asset.GUID, ctx.Caller!.Player.transform.TransformPoint(new Vector3(0, 300, 200)), Quaternion.Euler(0, 0, 0), out _);
                ctx.Reply($"Successfully spawned AA target: {(vehicle == null ? asset.GUID.ToString("N") : vehicle.asset.vehicleName)}".Colorize("ebd491"));
            }
            else if (!ctx.HasArgs(2))
                ctx.Reply($"Please specify a vehicle name.".Colorize("ebd491"));
            else
                ctx.Reply($"A vehicle called '{ctx.Get(1)!} does not exist".Colorize("ebd491"));
        }
        else ctx.SendCorrectUsage(Syntax);
    }
}
