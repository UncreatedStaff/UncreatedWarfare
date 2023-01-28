using SDG.Unturned;
using System;
using System.Globalization;
using System.Linq;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Locations;
using UnityEngine;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands.VanillaRework;
public class TeleportCommand : Command
{
    private const string Syntax = "/tp <x y z|player|location|wp> - or - /tp <player> <x y z|player|location|wp>";
    private const string Help = "Teleport you or another player to a location.";

    public TeleportCommand() : base("teleport", EAdminType.TRIAL_ADMIN_ON_DUTY, 1)
    {
        AddAlias("tp");
    }

    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertHelpCheck(0, Syntax + " - " + Help);

        ctx.AssertArgs(1, Syntax);
        Vector3 pos;
        switch (ctx.ArgumentCount)
        {
            case 1: // tp <player|location>
                ctx.AssertRanByPlayer();

                if (ctx.MatchParameter(0, "wp", "wayport", "marker"))
                {
                    if (!ctx.Caller.Player.quests.isMarkerPlaced)
                        throw ctx.Reply(T.TeleportWaypointNotFound);
                    Vector3 waypoint = ctx.Caller.Player.quests.markerPosition;
                    GridLocation loc = new GridLocation(in waypoint);
                    if (F.TryGetHeight(waypoint.x, waypoint.z, out float height, 2f) &&
                        ctx.Caller.Player.teleportToLocation(new Vector3(waypoint.x, height, waypoint.z), ctx.Caller.Player.transform.rotation.eulerAngles.y))
                        throw ctx.Reply(T.TeleportSelfWaypointSuccess, loc);
                    throw ctx.Reply(T.TeleportSelfWaypointObstructed, loc);
                }
                if (ctx.TryGet(0, out _, out UCPlayer? onlinePlayer) && onlinePlayer is not null)
                {
                    if (onlinePlayer.Player.life.isDead)
                        throw ctx.Reply(T.TeleportTargetDead, onlinePlayer);
                    InteractableVehicle? veh = onlinePlayer.Player.movement.getVehicle();
                    pos = onlinePlayer.Position;
                    if (veh != null && !veh.isExploded && !veh.isDead)
                    {
                        if (VehicleManager.ServerForcePassengerIntoVehicle(ctx.Caller, veh))
                            throw ctx.Reply(T.TeleportSelfSuccessVehicle, onlinePlayer, veh);
                        pos.y += 5f;
                    }

                    if (ctx.Caller.Player.teleportToLocation(pos, onlinePlayer.Player.look.aim.transform.rotation.y))
                        throw ctx.Reply(T.TeleportSelfSuccessPlayer, onlinePlayer);
                    throw ctx.Reply(T.TeleportSelfPlayerObstructed, onlinePlayer);
                }
                if (GridLocation.TryParse(ctx.Get(0)!, out GridLocation location))
                {
                    Vector3 center = location.Center;
                    if (F.TryGetHeight(center.x, center.z, out float height, 2f) &&
                        ctx.Caller.Player.teleportToLocation(new Vector3(center.x, height, center.z), ctx.Caller.Player.transform.rotation.eulerAngles.y))
                        throw ctx.Reply(T.TeleportSelfWaypointSuccess, location);
                    throw ctx.Reply(T.TeleportSelfWaypointObstructed, location);
                }
                
                string input = ctx.GetRange(0)!;
                LocationDevkitNode? n = F.StringFind(LocationDevkitNodeSystem.Get().GetAllNodes(), loc => loc.locationName, loc => loc.locationName.Length, input);
                if (n is null)
                    throw ctx.Reply(T.TeleportLocationNotFound, input);
                pos = n.transform.position;
                if (ctx.Caller.Player.teleportToLocation(new Vector3(pos.x, F.GetTerrainHeightAt2DPoint(pos.x, pos.z, 1f), pos.z), 0f))
                    throw ctx.Reply(T.TeleportSelfLocationSuccess, n.name);
                throw ctx.Reply(T.TeleportSelfLocationObstructed, n.name);
            case 2:
                if (ctx.TryGet(0, out _, out UCPlayer? target) && target is not null)
                {
                    if (target.Player.life.isDead)
                        throw ctx.Reply(T.TeleportTargetDead, target);

                    if (ctx.MatchParameter(1, "wp", "wayport", "marker"))
                    {
                        if (!ctx.Caller.Player.quests.isMarkerPlaced)
                            throw ctx.Reply(T.TeleportWaypointNotFound);
                        Vector3 waypoint = ctx.Caller.Player.quests.markerPosition;
                        GridLocation loc = new GridLocation(in waypoint);
                        if (F.TryGetHeight(waypoint.x, waypoint.z, out float height, 2f) &&
                            ctx.Caller.Player.teleportToLocation(new Vector3(waypoint.x, height, waypoint.z), ctx.Caller.Player.transform.rotation.eulerAngles.y))
                            throw ctx.Reply(T.TeleportSelfWaypointSuccess, loc);
                        throw ctx.Reply(T.TeleportSelfWaypointObstructed, loc);
                    }
                    if (ctx.TryGet(1, out _, out onlinePlayer) && onlinePlayer is not null)
                    {
                        if (onlinePlayer.Player.life.isDead)
                            throw ctx.Reply(T.TeleportTargetDead, onlinePlayer);
                        InteractableVehicle? veh = onlinePlayer.Player.movement.getVehicle();
                        pos = onlinePlayer.Position;
                        if (veh != null && !veh.isExploded && !veh.isDead)
                        {
                            if (VehicleManager.ServerForcePassengerIntoVehicle(target, veh))
                            {
                                target.SendChat(T.TeleportSelfSuccessVehicle, onlinePlayer, veh);
                                throw ctx.Reply(T.TeleportOtherSuccessVehicle, target, onlinePlayer, veh);
                            }
                            pos.y += 5f;
                        }

                        if (target.Player.teleportToLocation(pos, onlinePlayer.Player.look.aim.transform.rotation.y))
                        {
                            target.SendChat(T.TeleportSelfSuccessPlayer, onlinePlayer);
                            throw ctx.Reply(T.TeleportOtherSuccessPlayer, target, onlinePlayer);
                        }
                        throw ctx.Reply(T.TeleportOtherObstructedPlayer, target, onlinePlayer);
                    }
                    if (GridLocation.TryParse(ctx.Get(1)!, out location))
                    {
                        Vector3 center = location.Center;
                        if (F.TryGetHeight(center.x, center.z, out float height, 2f) &&
                            ctx.Caller.Player.teleportToLocation(new Vector3(center.x, height, center.z), ctx.Caller.Player.transform.rotation.eulerAngles.y))
                            throw ctx.Reply(T.TeleportSelfWaypointSuccess, location);
                        throw ctx.Reply(T.TeleportSelfWaypointObstructed, location);
                    }
                    input = ctx.GetRange(1)!;
                    n = F.StringFind(LocationDevkitNodeSystem.Get().GetAllNodes(), loc => loc.locationName, loc => loc.locationName.Length, input);

                    if (n is null)
                        throw ctx.Reply(T.TeleportLocationNotFound, input);
                    pos = n.transform.position;
                    if (target.Player.teleportToLocation(new Vector3(pos.x, F.GetTerrainHeightAt2DPoint(pos.x, pos.z, 1f), pos.z), 0f))
                    {
                        target.SendChat(T.TeleportSelfLocationSuccess, n.name);
                        throw ctx.Reply(T.TeleportOtherSuccessLocation, target, n.name);
                    }
                    throw ctx.Reply(T.TeleportOtherObstructedLocation, target, n.name);
                }
                throw ctx.Reply(T.TeleportTargetNotFound, ctx.Get(0)!);
            case 3:
                ctx.AssertRanByPlayer();
                pos = ctx.Caller.Position;
                if (!ctx.TryGet(2, out float z))
                {
                    string p = ctx.Get(2)!;
                    if (p.StartsWith("~", StringComparison.Ordinal))
                        z = pos.z + GetOffset(p);
                    else
                        throw ctx.Reply(T.TeleportInvalidCoordinates);
                }
                if (!ctx.TryGet(1, out float y))
                {
                    string p = ctx.Get(1)!;
                    if (p.StartsWith("~", StringComparison.Ordinal))
                        y = pos.y + GetOffset(p);
                    else if (p.Length > 0 && p[0] == '-')
                        y = float.NaN;
                    else
                        throw ctx.Reply(T.TeleportInvalidCoordinates);
                }
                if (!ctx.TryGet(0, out float x))
                {
                    string p = ctx.Get(0)!;
                    if (p.StartsWith("~", StringComparison.Ordinal))
                        x = pos.x + GetOffset(p);
                    else
                        throw ctx.Reply(T.TeleportInvalidCoordinates);
                }

                if (float.IsNaN(y))
                    y = F.GetHeightAt2DPoint(x, z, pos.y, 2f);
                pos = new Vector3(x, y, z);
                throw ctx.Reply(ctx.Caller.Player.teleportToLocation(pos, ctx.Caller.Player.look.aim.transform.rotation.y)
                        ? T.TeleportSelfLocationSuccess
                        : T.TeleportSelfLocationObstructed,
                    $"({x.ToString("0.##", Data.LocalLocale)}, {y.ToString("0.##", Data.LocalLocale)}, {z.ToString("0.##", Data.LocalLocale)})");
            case 4:
                if (ctx.TryGet(0, out _, out target) && target is not null)
                {
                    if (target.Player.life.isDead)
                        throw ctx.Reply(T.TeleportTargetDead, target);

                    pos = ctx.Caller.Position;
                    if (!ctx.TryGet(3, out z))
                    {
                        string p = ctx.Get(3)!;
                        if (p.StartsWith("~", StringComparison.Ordinal))
                            z = pos.z + GetOffset(p);
                        else
                            throw ctx.Reply(T.TeleportInvalidCoordinates);
                    }
                    if (!ctx.TryGet(2, out y))
                    {
                        string p = ctx.Get(2)!;
                        if (p.StartsWith("~", StringComparison.Ordinal))
                            y = pos.y + GetOffset(p);
                        else if (p.Length > 0 && p[0] == '-')
                            y = float.NaN;
                        else
                            throw ctx.Reply(T.TeleportInvalidCoordinates);
                    }
                    if (!ctx.TryGet(1, out x))
                    {
                        string p = ctx.Get(1)!;
                        if (p.StartsWith("~", StringComparison.Ordinal))
                            x = pos.x + GetOffset(p);
                        else
                            throw ctx.Reply(T.TeleportInvalidCoordinates);
                    }

                    if (float.IsNaN(y))
                        y = F.GetHeightAt2DPoint(x, z, pos.y, 2f);
                    pos = new Vector3(x, y, z);
                    string loc = $"({x.ToString("0.##", Data.LocalLocale)}, {y.ToString("0.##", Data.LocalLocale)}, {z.ToString("0.##", Data.LocalLocale)})";
                    if (target.Player.teleportToLocation(pos, target.Player.look.aim.transform.rotation.y))
                    {
                        target.SendChat(T.TeleportSelfLocationSuccess, loc);
                        throw ctx.Reply(T.TeleportOtherSuccessLocation, target, loc);
                    }
                    throw ctx.Reply(T.TeleportOtherObstructedLocation, target, loc);
                }
                throw ctx.Reply(T.TeleportTargetNotFound, ctx.Get(0)!);
            default:
                throw ctx.SendCorrectUsage(Syntax);
        }
    }
    private static float GetOffset(string arg)
    {
        if (arg.Length < 2) return 0f;
        if (float.TryParse(arg.Substring(1), NumberStyles.Number, Data.AdminLocale, out float offset) || float.TryParse(arg.Substring(1), NumberStyles.Number, Data.LocalLocale, out offset))
            return offset;
        return 0;
    }
}