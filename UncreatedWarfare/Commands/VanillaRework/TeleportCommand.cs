using SDG.Unturned;
using System;
using System.Globalization;
using System.Linq;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using UnityEngine;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands.VanillaRework;
public class TeleportCommand : Command
{
    private const string SYNTAX = "/tp <x y z|player|location> - or - /tp <player> <x y z|player|location>";
    private const string HELP = "Teleport you or another player to a location.";

    public TeleportCommand() : base("teleport", EAdminType.TRIAL_ADMIN_ON_DUTY, 1)
    {
        AddAlias("tp");
    }

    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        ctx.AssertArgs(1, SYNTAX);
        Vector3 pos;
        switch (ctx.ArgumentCount)
        {
            case 1: // tp <player|location>
                ctx.AssertRanByPlayer();
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
                    else
                        throw ctx.Reply(T.TeleportSelfPlayerObstructed, onlinePlayer);
                }
                else
                {
                    LocationNode? n = null;
                    string input = ctx.GetRange(0)!;
                    foreach (LocationNode node in LevelNodes.nodes.OfType<LocationNode>().OrderBy(x => x.name.Length))
                    {
                        if (node.name.IndexOf(input, StringComparison.OrdinalIgnoreCase) != -1)
                        {
                            n = node;
                            break;
                        }
                    }

                    if (n is null)
                        throw ctx.Reply(T.TeleportLocationNotFound, input);
                    if (ctx.Caller.Player.teleportToLocation(n.point, 0f))
                        throw ctx.Reply(T.TeleportSelfLocationSuccess, n.name);
                    else
                        throw ctx.Reply(T.TeleportSelfLocationObstructed, n.name);
                }
            case 2:
                if (ctx.TryGet(0, out _, out UCPlayer? target) && target is not null)
                {
                    if (target.Player.life.isDead)
                        throw ctx.Reply(T.TeleportTargetDead, target);

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
                        else
                            throw ctx.Reply(T.TeleportOtherObstructedPlayer, target, onlinePlayer);
                    }
                    else
                    {
                        LocationNode? n = null;
                        string input = ctx.GetRange(0)!;
                        foreach (LocationNode node in LevelNodes.nodes.OfType<LocationNode>().OrderBy(x => x.name.Length))
                        {
                            if (node.name.IndexOf(input, StringComparison.OrdinalIgnoreCase) != -1)
                            {
                                n = node;
                                break;
                            }
                        }

                        if (n is null)
                            throw ctx.Reply(T.TeleportLocationNotFound, input);
                        if (target.Player.teleportToLocation(n.point, 0f))
                        {
                            target.SendChat(T.TeleportSelfLocationSuccess, n.name);
                            throw ctx.Reply(T.TeleportOtherSuccessLocation, target, n.name);
                        }
                        else
                            throw ctx.Reply(T.TeleportOtherObstructedLocation, target, n.name);
                    }
                }
                else
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

                pos = new Vector3(x, y, z);
                throw ctx.Reply(ctx.Caller.Player.teleportToLocation(pos, ctx.Caller.Player.look.aim.transform.rotation.y)
                        ? T.TeleportSelfLocationSuccess
                        : T.TeleportSelfLocationObstructed,
                    $"({x.ToString("0.##", Data.Locale)}, {y.ToString("0.##", Data.Locale)}, {z.ToString("0.##", Data.Locale)})");
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

                    pos = new Vector3(x, y, z);
                    string loc = $"({x.ToString("0.##", Data.Locale)}, {y.ToString("0.##", Data.Locale)}, {z.ToString("0.##", Data.Locale)})";
                    if (target.Player.teleportToLocation(pos, target.Player.look.aim.transform.rotation.y))
                    {
                        target.SendChat(T.TeleportSelfLocationSuccess, loc);
                        throw ctx.Reply(T.TeleportOtherSuccessLocation, target, loc);
                    }
                    else
                        throw ctx.Reply(T.TeleportOtherObstructedLocation, target, loc);
                }
                else
                    throw ctx.Reply(T.TeleportTargetNotFound, ctx.Get(0)!);
            default:
                throw ctx.SendCorrectUsage(SYNTAX);
        }
    }
    private static float GetOffset(string arg)
    {
        if (arg.Length < 2) return 0f;
        if (float.TryParse(arg.Substring(1), NumberStyles.Number, Data.Locale, out float offset))
            return offset;
        return 0;
    }
}