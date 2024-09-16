using DanielWillett.ReflectionTools;
using System;
using System.Globalization;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Commands;

[Command("teleport", "tp"), Priority(1)]
[MetadataFile(nameof(GetHelpMetadata))]
public class TeleportCommand : IExecutableCommand
{
    private const string Syntax = "/tp <x y z|player|location|wp|jump [dstance]> - or - /tp <player> <x y z|player|location|wp>";
    private const string Help = "Teleport you or another player to a location.";

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Teleport you or another player to a location.",
            Parameters =
            [
                new CommandParameter("Player", typeof(IPlayer))
                {
                    Description = "Teleport another player to a location.",
                    Parameters =
                    [
                        new CommandParameter("X", typeof(float))
                        {
                            Description = "Teleport another player to a set of coordinates.",
                            ChainDisplayCount = 3,
                            Parameters =
                            [
                                new CommandParameter("Y", typeof(float))
                                {
                                    Description = "Teleport another player to a set of coordinates.",
                                    Parameters =
                                    [
                                        new CommandParameter("Z", typeof(float))
                                        {
                                            Description = "Teleport another player to a set of coordinates.",
                                        }
                                    ]
                                }
                            ]
                        },
                        new CommandParameter("Location", typeof(string), typeof(GridLocation))
                        {
                            Description = "Teleport another player to a location or grid location.",
                        },
                        new CommandParameter("WP")
                        {
                            Aliases = [ "waypoint", "marker" ],
                            Description = "Teleport another player to your waypoint.",
                        }
                    ]
                },
                new CommandParameter("X", typeof(float))
                {
                    Description = "Teleport yourself to a set of coordinates.",
                    ChainDisplayCount = 3,
                    Parameters =
                    [
                        new CommandParameter("Y", typeof(float))
                        {
                            Description = "Teleport yourself to a set of coordinates.",
                            Parameters =
                            [
                                new CommandParameter("Z", typeof(float))
                                {
                                    Description = "Teleport yourself to a set of coordinates.",
                                }
                            ]
                        }
                    ]
                },
                new CommandParameter("Location", typeof(string), typeof(GridLocation))
                {
                    Description = "Teleport yourself to a location or grid location.",
                },
                new CommandParameter("WP")
                {
                    Aliases = [ "waypoint", "marker" ],
                    Description = "Teleport yourself to your waypoint.",
                },
                new CommandParameter("Jump")
                {
                    Description = "Teleport to where you're looking.",
                    Aliases = [ "j" ],
                    Parameters =
                    [
                        new CommandParameter("Distance", typeof(float))
                        {
                            Description = "Teleport yourself a certain distance in the direction you're looking.",
                            IsOptional = true
                        }
                    ]
                }
            ]
        };
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertHelpCheck(0, Syntax + " - " + Help);

        Context.AssertArgs(1, Syntax);

        if (Context.MatchParameter(0, "jump", "j"))
        {
            Context.AssertRanByPlayer();
            if (Context.MatchParameter(1, "start", "s", "begin"))
            {
                StartJumping(Context.Player);
                throw Context.Reply(T.TeleportStartJump);
            }
            
            if (Context.MatchParameter(1, "end", "e", "stop"))
            {
                StopJumping(Context.Player);
                throw Context.Reply(T.TeleportStopJump);
            }

            bool raycast = !Context.TryGet(1, out float distance);
            Jump(raycast, distance, Context.Player);
            Vector3 castPt = Context.Player.Position;
            throw Context.Reply(T.TeleportSelfLocationSuccess, $"({castPt.x.ToString("0.##", Data.LocalLocale)}, {castPt.y.ToString("0.##", Data.LocalLocale)}, {castPt.z.ToString("0.##", Data.LocalLocale)})");
        }

        Vector3 pos;
        switch (Context.ArgumentCount)
        {
            case 1: // tp <player|location>
                Context.AssertRanByPlayer();

                if (Context.MatchParameter(0, "wp", "waypoint", "marker"))
                {
                    if (!Context.Player.UnturnedPlayer.quests.isMarkerPlaced)
                        throw Context.Reply(T.TeleportWaypointNotFound);
                    Vector3 waypoint = Context.Player.UnturnedPlayer.quests.markerPosition;
                    GridLocation loc = new GridLocation(in waypoint);
                    if (F.TryGetHeight(waypoint.x, waypoint.z, out float height, 2f) &&
                        Context.Player.UnturnedPlayer.teleportToLocation(new Vector3(waypoint.x, height, waypoint.z), Context.Player.Yaw))
                        throw Context.Reply(T.TeleportSelfWaypointSuccess, loc);
                    throw Context.Reply(T.TeleportSelfWaypointObstructed, loc);
                }
                if (Context.TryGet(0, out _, out UCPlayer? onlinePlayer) && onlinePlayer is not null)
                {
                    if (onlinePlayer.Player.life.isDead)
                        throw Context.Reply(T.TeleportTargetDead, onlinePlayer);
                    InteractableVehicle? veh = onlinePlayer.Player.movement.getVehicle();
                    pos = onlinePlayer.Position;
                    if (veh != null && !veh.isExploded && !veh.isDead)
                    {
                        if (VehicleManager.ServerForcePassengerIntoVehicle(Context.Player, veh))
                            throw Context.Reply(T.TeleportSelfSuccessVehicle, onlinePlayer, veh);
                        pos.y += 5f;
                    }

                    if (Context.Player.UnturnedPlayer.teleportToLocation(pos, onlinePlayer.Yaw))
                        throw Context.Reply(T.TeleportSelfSuccessPlayer, onlinePlayer);
                    throw Context.Reply(T.TeleportSelfPlayerObstructed, onlinePlayer);
                }
                if (GridLocation.TryParse(Context.Get(0)!, out GridLocation location))
                {
                    Vector3 center = location.Center;
                    if (F.TryGetHeight(center.x, center.z, out float height, 2f) &&
                        Context.Player.UnturnedPlayer.teleportToLocation(new Vector3(center.x, height, center.z), Context.Player.Yaw))
                        throw Context.Reply(T.TeleportSelfWaypointSuccess, location);
                    throw Context.Reply(T.TeleportSelfWaypointObstructed, location);
                }
                
                string input = Context.GetRange(0)!;
                LocationDevkitNode? n = F.StringFind(LocationDevkitNodeSystem.Get().GetAllNodes(), loc => loc.locationName, loc => loc.locationName.Length, input);
                if (n is null)
                    throw Context.Reply(T.TeleportLocationNotFound, input);
                pos = n.transform.position;
                if (Context.Player.UnturnedPlayer.teleportToLocation(new Vector3(pos.x, F.GetTerrainHeightAt2DPoint(pos.x, pos.z, 1f), pos.z), Context.Player.Yaw))
                    throw Context.Reply(T.TeleportSelfLocationSuccess, n.locationName);
                throw Context.Reply(T.TeleportSelfLocationObstructed, n.locationName);

            case 2:
                if (Context.TryGet(0, out _, out UCPlayer? target) && target is not null)
                {
                    if (target.Player.life.isDead)
                        throw Context.Reply(T.TeleportTargetDead, target);

                    if (Context.MatchParameter(1, "wp", "wayport", "marker"))
                    {
                        if (!Context.Player.UnturnedPlayer.quests.isMarkerPlaced)
                            throw Context.Reply(T.TeleportWaypointNotFound);
                        Vector3 waypoint = Context.Player.UnturnedPlayer.quests.markerPosition;
                        GridLocation loc = new GridLocation(in waypoint);
                        if (F.TryGetHeight(waypoint.x, waypoint.z, out float height, 2f) &&
                            target.Player.teleportToLocation(new Vector3(waypoint.x, height, waypoint.z), target.Yaw))
                        {
                            target.SendChat(T.TeleportSelfWaypointSuccess, loc);
                            throw Context.Reply(T.TeleportOtherWaypointSuccess, target, loc);
                        }
                        throw Context.Reply(T.TeleportOtherWaypointObstructed, target, loc);
                    }
                    if (Context.TryGet(1, out _, out onlinePlayer) && onlinePlayer is not null)
                    {
                        if (onlinePlayer.Player.life.isDead)
                            throw Context.Reply(T.TeleportTargetDead, onlinePlayer);
                        InteractableVehicle? veh = onlinePlayer.Player.movement.getVehicle();
                        pos = onlinePlayer.Position;
                        if (veh != null && !veh.isExploded && !veh.isDead)
                        {
                            if (VehicleManager.ServerForcePassengerIntoVehicle(target, veh))
                            {
                                target.SendChat(T.TeleportSelfSuccessVehicle, onlinePlayer, veh);
                                throw Context.Reply(T.TeleportOtherSuccessVehicle, target, onlinePlayer, veh);
                            }
                            pos.y += 5f;
                        }

                        if (target.Player.teleportToLocation(pos, onlinePlayer.Yaw))
                        {
                            target.SendChat(T.TeleportSelfSuccessPlayer, onlinePlayer);
                            throw Context.Reply(T.TeleportOtherSuccessPlayer, target, onlinePlayer);
                        }
                        throw Context.Reply(T.TeleportOtherObstructedPlayer, target, onlinePlayer);
                    }
                    if (GridLocation.TryParse(Context.Get(1)!, out location))
                    {
                        Vector3 center = location.Center;
                        if (F.TryGetHeight(center.x, center.z, out float height, 2f) &&
                            target.Player.teleportToLocation(new Vector3(center.x, height, center.z), target.Yaw))
                        {
                            target.SendChat(T.TeleportSelfWaypointSuccess, location);
                            throw Context.Reply(T.TeleportOtherWaypointSuccess, target, location);
                        }
                        throw Context.Reply(T.TeleportOtherWaypointObstructed, target, location);
                    }
                    input = Context.GetRange(1)!;
                    n = F.StringFind(LocationDevkitNodeSystem.Get().GetAllNodes(), loc => loc.locationName, loc => loc.locationName.Length, input);

                    if (n is null)
                        throw Context.Reply(T.TeleportLocationNotFound, input);
                    pos = n.transform.position;
                    if (target.Player.teleportToLocation(new Vector3(pos.x, F.GetTerrainHeightAt2DPoint(pos.x, pos.z, 1f), pos.z), target.Yaw))
                    {
                        target.SendChat(T.TeleportSelfLocationSuccess, n.locationName);
                        throw Context.Reply(T.TeleportOtherSuccessLocation, target, n.locationName);
                    }
                    throw Context.Reply(T.TeleportOtherObstructedLocation, target, n.locationName);
                }
                throw Context.Reply(T.TeleportTargetNotFound, Context.Get(0)!);

            case 3:
                Context.AssertRanByPlayer();
                pos = Context.Player.Position;
                if (!Context.TryGet(2, out float z))
                {
                    string p = Context.Get(2)!;
                    if (p.StartsWith("~", StringComparison.Ordinal))
                        z = pos.z + GetOffset(p);
                    else
                        throw Context.Reply(T.TeleportInvalidCoordinates);
                }
                if (!Context.TryGet(1, out float y))
                {
                    string p = Context.Get(1)!;
                    if (p.StartsWith("~", StringComparison.Ordinal))
                        y = pos.y + GetOffset(p);
                    else if (p.Length > 0 && p[0] == '-')
                        y = float.NaN;
                    else
                        throw Context.Reply(T.TeleportInvalidCoordinates);
                }
                if (!Context.TryGet(0, out float x))
                {
                    string p = Context.Get(0)!;
                    if (p.StartsWith("~", StringComparison.Ordinal))
                        x = pos.x + GetOffset(p);
                    else
                        throw Context.Reply(T.TeleportInvalidCoordinates);
                }

                if (float.IsNaN(y))
                    y = F.GetHeightAt2DPoint(x, z, pos.y, 2f);
                pos = new Vector3(x, y, z);
                throw Context.Reply(Context.Player.UnturnedPlayer.teleportToLocation(pos, Context.Player.Yaw)
                        ? T.TeleportSelfLocationSuccess
                        : T.TeleportSelfLocationObstructed,
                    $"({x.ToString("0.##", Data.LocalLocale)}, {y.ToString("0.##", Data.LocalLocale)}, {z.ToString("0.##", Data.LocalLocale)})");

            case 4:
                if (Context.TryGet(0, out _, out target) && target is not null)
                {
                    if (target.Player.life.isDead)
                        throw Context.Reply(T.TeleportTargetDead, target);

                    pos = Context.Player.Position;
                    if (!Context.TryGet(3, out z))
                    {
                        string p = Context.Get(3)!;
                        if (p.StartsWith("~", StringComparison.Ordinal))
                            z = pos.z + GetOffset(p);
                        else
                            throw Context.Reply(T.TeleportInvalidCoordinates);
                    }
                    if (!Context.TryGet(2, out y))
                    {
                        string p = Context.Get(2)!;
                        if (p.StartsWith("~", StringComparison.Ordinal))
                            y = pos.y + GetOffset(p);
                        else if (p.Length > 0 && p[0] == '-')
                            y = float.NaN;
                        else
                            throw Context.Reply(T.TeleportInvalidCoordinates);
                    }
                    if (!Context.TryGet(1, out x))
                    {
                        string p = Context.Get(1)!;
                        if (p.StartsWith("~", StringComparison.Ordinal))
                            x = pos.x + GetOffset(p);
                        else
                            throw Context.Reply(T.TeleportInvalidCoordinates);
                    }

                    if (float.IsNaN(y))
                        y = F.GetHeightAt2DPoint(x, z, pos.y, 2f);
                    pos = new Vector3(x, y, z);
                    string loc = $"({x.ToString("0.##", Data.LocalLocale)}, {y.ToString("0.##", Data.LocalLocale)}, {z.ToString("0.##", Data.LocalLocale)})";
                    if (target.Player.teleportToLocation(pos, target.Yaw))
                    {
                        target.SendChat(T.TeleportSelfLocationSuccess, loc);
                        throw Context.Reply(T.TeleportOtherSuccessLocation, target, loc);
                    }
                    throw Context.Reply(T.TeleportOtherObstructedLocation, target, loc);
                }
                throw Context.Reply(T.TeleportTargetNotFound, Context.Get(0)!);

            default:
                throw Context.SendCorrectUsage(Syntax);
        }
    }
    private static float GetOffset(string arg)
    {
        if (arg.Length < 2) return 0f;
        if (float.TryParse(arg.Substring(1), NumberStyles.Number, CultureInfo.InvariantCulture, out float offset) || float.TryParse(arg.Substring(1), NumberStyles.Number, Data.LocalLocale, out offset))
            return offset;
        return 0;
    }
    public static void StartJumping(UCPlayer player)
    {
        player.JumpOnPunch = true;
    }
    public static void StopJumping(UCPlayer player)
    {
        player.JumpOnPunch = false;
    }
    public static void Jump(bool raycast, float distance, UCPlayer player)
    {
        Vector3 castPt = default;
        if (raycast)
        {
            distance = 10f;
            raycast = Physics.Raycast(new Ray(player.Player.look.aim.position, player.Player.look.aim.forward), out RaycastHit hit,
                1024, RayMasks.BLOCK_COLLISION);
            if (raycast)
                castPt = hit.point;
        }
        if (!raycast)
            castPt = player.Position + player.Player.look.aim.forward * distance;

        int c = 0;
        while (!PlayerStance.hasStandingHeightClearanceAtPosition(castPt) && ++c < 12)
            castPt += new Vector3(0, 1f, 0);

        player.Player.teleportToLocationUnsafe(castPt, player.Yaw);
    }
}