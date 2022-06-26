using SDG.Unturned;
using System;
using System.Linq;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Teams;
using UnityEngine;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands.VanillaRework;
public class TeleportCommand : Command
{
    private const string SYNTAX = "/tp <x y z|player|location> - or - /tp <player> <x y z|player|location>";
    private const string HELP = "Teleport you or another player to a location.";

    public TeleportCommand() : base("teleport", EAdminType.TRIAL_ADMIN, 1)
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
                        throw ctx.Reply("tp_target_dead", onlinePlayer.CharacterName, TeamManager.GetTeamHexColor(onlinePlayer.GetTeam()));
                    InteractableVehicle? veh = onlinePlayer.Player.movement.getVehicle();
                    pos = onlinePlayer.Position;
                    if (veh != null && !veh.isExploded && !veh.isDead)
                    {
                        if (VehicleManager.ServerForcePassengerIntoVehicle(ctx.Caller, veh))
                            throw ctx.Reply("tp_entered_vehicle", veh.asset.vehicleName, onlinePlayer.CharacterName, TeamManager.GetTeamHexColor(onlinePlayer.GetTeam()));
                        pos.y += 5f;
                    }

                    if (ctx.Caller.Player.teleportToLocation(pos, onlinePlayer.Player.look.aim.transform.rotation.y))
                        throw ctx.Reply("tp_teleported_player", onlinePlayer.CharacterName, TeamManager.GetTeamHexColor(onlinePlayer.GetTeam()));
                    else
                        throw ctx.Reply("tp_obstructed_player", onlinePlayer.CharacterName, TeamManager.GetTeamHexColor(onlinePlayer.GetTeam()));
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
                        throw ctx.Reply("tp_location_not_found", input);
                    if (ctx.Caller.Player.teleportToLocation(n.point, 0f))
                        throw ctx.Reply("tp_teleported_location", n.name);
                    else
                        throw ctx.Reply("tp_obstructed_location", n.name);
                }
            case 2:
                if (ctx.TryGet(0, out _, out UCPlayer? target) && target is not null)
                {
                    if (target.Player.life.isDead)
                        throw ctx.Reply("tp_target_dead", target.CharacterName, TeamManager.GetTeamHexColor(target.GetTeam()));

                    if (ctx.TryGet(1, out _, out onlinePlayer) && onlinePlayer is not null)
                    {
                        if (onlinePlayer.Player.life.isDead)
                            throw ctx.Reply("tp_target_dead", onlinePlayer.CharacterName, TeamManager.GetTeamHexColor(onlinePlayer.GetTeam()));
                        InteractableVehicle? veh = onlinePlayer.Player.movement.getVehicle();
                        pos = onlinePlayer.Position;
                        if (veh != null && !veh.isExploded && !veh.isDead)
                        {
                            if (VehicleManager.ServerForcePassengerIntoVehicle(target, veh))
                            {
                                string team = TeamManager.GetTeamHexColor(onlinePlayer.GetTeam());
                                target.SendChat("tp_entered_vehicle", veh.asset.vehicleName, onlinePlayer.CharacterName, team);
                                throw ctx.Reply("tp_entered_vehicle_other", veh.asset.vehicleName, onlinePlayer.CharacterName, team, target.CharacterName, TeamManager.GetTeamHexColor(target.GetTeam()));
                            }
                            pos.y += 5f;
                        }

                        if (target.Player.teleportToLocation(pos, onlinePlayer.Player.look.aim.transform.rotation.y))
                        {
                            string team = TeamManager.GetTeamHexColor(onlinePlayer.GetTeam());
                            target.SendChat("tp_teleported_player", onlinePlayer.CharacterName, team);
                            throw ctx.Reply("tp_teleported_player_other", onlinePlayer.CharacterName, team, target.CharacterName, TeamManager.GetTeamHexColor(target.GetTeam()));
                        }
                        else
                            throw ctx.Reply("tp_obstructed_player_other", onlinePlayer.CharacterName, TeamManager.GetTeamHexColor(onlinePlayer.GetTeam()), target.CharacterName, TeamManager.GetTeamHexColor(target.GetTeam()));
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
                            throw ctx.Reply("tp_location_not_found", input);
                        if (target.Player.teleportToLocation(n.point, 0f))
                        {
                            target.SendChat("tp_teleported_location", n.name);
                            throw ctx.Reply("tp_teleported_location_other", n.name, target.CharacterName, TeamManager.GetTeamHexColor(target.GetTeam()));
                        }
                        else
                            throw ctx.Reply("tp_obstructed_location_other", n.name, target.CharacterName, TeamManager.GetTeamHexColor(target.GetTeam()));
                    }
                }
                else
                    throw ctx.Reply("tp_target_not_found", ctx.Get(0)!);
            case 3:
                ctx.AssertRanByPlayer();
                pos = ctx.Caller.Position;
                if (!ctx.TryGet(2, out float z))
                {
                    if (ctx.MatchParameter(2, "~"))
                        z = pos.z;
                    else
                        throw ctx.Reply("tp_invalid_coordinates");
                }
                if (!ctx.TryGet(1, out float y))
                {
                    if (ctx.MatchParameter(1, "~"))
                        y = pos.y;
                    else
                        throw ctx.Reply("tp_invalid_coordinates");
                }
                if (!ctx.TryGet(0, out float x))
                {
                    if (ctx.MatchParameter(0, "~"))
                        x = pos.x;
                    else
                        throw ctx.Reply("tp_invalid_coordinates");
                }

                pos = new Vector3(x, y, z);
                throw ctx.Reply(ctx.Caller.Player.teleportToLocation(pos, ctx.Caller.Player.look.aim.transform.rotation.y) ? "tp_teleported_player_location" : "tp_obstructed_player_location",
                                $"({x.ToString("F2", Data.Locale)}, {y.ToString("F2", Data.Locale)}, {z.ToString("F2", Data.Locale)})");
            case 4:
                if (ctx.TryGet(0, out _, out target) && target is not null)
                {
                    if (target.Player.life.isDead)
                        throw ctx.Reply("tp_target_dead", target.CharacterName, TeamManager.GetTeamHexColor(target.GetTeam()));

                    pos = ctx.Caller.Position;
                    if (!ctx.TryGet(3, out z))
                    {
                        if (ctx.MatchParameter(3, "~"))
                            z = pos.z;
                        else
                            throw ctx.Reply("tp_invalid_coordinates");
                    }
                    if (!ctx.TryGet(2, out y))
                    {
                        if (ctx.MatchParameter(2, "~"))
                            y = pos.y;
                        else
                            throw ctx.Reply("tp_invalid_coordinates");
                    }
                    if (!ctx.TryGet(1, out x))
                    {
                        if (ctx.MatchParameter(1, "~"))
                            x = pos.x;
                        else
                            throw ctx.Reply("tp_invalid_coordinates");
                    }

                    pos = new Vector3(x, y, z);
                    throw ctx.Reply(
                        target.Player.teleportToLocation(pos, target.Player.look.aim.transform.rotation.y)
                            ? "tp_teleported_player_location_other"
                            : "tp_obstructed_player_location_other",
                        $"({x.ToString("F2", Data.Locale)}, {y.ToString("F2", Data.Locale)}, {z.ToString("F2", Data.Locale)})", target.CharacterName, TeamManager.GetTeamHexColor(target.GetTeam()));
                }
                else
                    throw ctx.Reply("tp_target_not_found", ctx.Get(0)!);
            default:
                throw ctx.SendCorrectUsage(SYNTAX);
        }
    }
}