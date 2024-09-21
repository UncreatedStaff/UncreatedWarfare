using DanielWillett.ReflectionTools;
using System;
using System.Globalization;
using System.Linq;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Tweaks;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

[Command("teleport", "tp"), Priority(1)]
[MetadataFile(nameof(GetHelpMetadata))]
public class TeleportCommand : IExecutableCommand
{
    private readonly ChatService _chatService;
    private readonly TeleportCommandTranslations _translations;
    private const string Syntax = "/tp <x y z|player|location|wp|jump [dstance]> - or - /tp <player> <x y z|player|location|wp>";

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

    public TeleportCommand(ChatService chatService, TranslationInjection<TeleportCommandTranslations> translations)
    {
        _chatService = chatService;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertArgs(1, Syntax);

        if (Context.MatchParameter(0, "jump", "j"))
        {
            Context.AssertRanByPlayer();
            if (Context.MatchParameter(1, "start", "s", "begin"))
            {
                StartJumping(Context.Player);
                throw Context.Reply(_translations.TeleportStartJump);
            }
            
            if (Context.MatchParameter(1, "end", "e", "stop"))
            {
                StopJumping(Context.Player);
                throw Context.Reply(_translations.TeleportStopJump);
            }

            bool raycast = !Context.TryGet(1, out float distance);
            Context.Player.Component<PlayerJumpComponent>().Jump(raycast, distance);
            Vector3 castPt = Context.Player.Position;
            throw Context.Reply(_translations.TeleportSelfLocationSuccess, $"({castPt.x.ToString("0.##", Data.LocalLocale)}, {castPt.y.ToString("0.##", Data.LocalLocale)}, {castPt.z.ToString("0.##", Data.LocalLocale)})");
        }

        Vector3 pos;
        switch (Context.ArgumentCount)
        {
            case 1: // tp <player|location>
                Context.AssertRanByPlayer();

                if (Context.MatchParameter(0, "wp", "waypoint", "marker"))
                {
                    if (!Context.Player.UnturnedPlayer.quests.isMarkerPlaced)
                        throw Context.Reply(_translations.TeleportWaypointNotFound);
                    Vector3 waypoint = Context.Player.UnturnedPlayer.quests.markerPosition;
                    GridLocation loc = new GridLocation(in waypoint);
                    waypoint.y = TerrainUtility.GetHighestPoint(in waypoint, float.NaN);
                    if (Context.Player.UnturnedPlayer.teleportToLocation(waypoint, Context.Player.Yaw))
                        throw Context.Reply(_translations.TeleportSelfWaypointSuccess, loc);
                    throw Context.Reply(_translations.TeleportSelfWaypointObstructed, loc);
                }
                if (Context.TryGet(0, out _, out WarfarePlayer? onlinePlayer) && onlinePlayer is not null)
                {
                    if (onlinePlayer.UnturnedPlayer.life.isDead)
                        throw Context.Reply(_translations.TeleportTargetDead, onlinePlayer);
                    InteractableVehicle? veh = onlinePlayer.UnturnedPlayer.movement.getVehicle();
                    pos = onlinePlayer.Position;
                    if (veh != null && !veh.isExploded && !veh.isDead)
                    {
                        if (VehicleManager.ServerForcePassengerIntoVehicle(Context.Player.UnturnedPlayer, veh))
                            throw Context.Reply(_translations.TeleportSelfSuccessVehicle, onlinePlayer, veh);
                        pos.y += 5f;
                    }

                    if (Context.Player.UnturnedPlayer.teleportToLocation(pos, onlinePlayer.Yaw))
                        throw Context.Reply(_translations.TeleportSelfSuccessPlayer, onlinePlayer);
                    throw Context.Reply(_translations.TeleportSelfPlayerObstructed, onlinePlayer);
                }
                if (GridLocation.TryParse(Context.Get(0)!, out GridLocation location))
                {
                    Vector3 center = location.Center;
                    center.y = TerrainUtility.GetHighestPoint(in center, float.NaN);
                    if (Context.Player.UnturnedPlayer.teleportToLocation(center, Context.Player.Yaw))
                        throw Context.Reply(_translations.TeleportSelfWaypointSuccess, location);
                    throw Context.Reply(_translations.TeleportSelfWaypointObstructed, location);
                }
                
                string input = Context.GetRange(0)!;
                LocationDevkitNode? n = CollectionUtility.StringFind(LocationDevkitNodeSystem.Get().GetAllNodes().OrderBy(loc => loc.locationName.Length), loc => loc.locationName, input);
                if (n is null)
                    throw Context.Reply(_translations.TeleportLocationNotFound, input);
                pos = n.transform.position;
                if (Context.Player.UnturnedPlayer.teleportToLocation(new Vector3(pos.x, LevelGround.getHeight(pos), pos.z), Context.Player.Yaw))
                    throw Context.Reply(_translations.TeleportSelfLocationSuccess, n.locationName);
                throw Context.Reply(_translations.TeleportSelfLocationObstructed, n.locationName);

            case 2:
                if (Context.TryGet(0, out _, out WarfarePlayer? target) && target is not null)
                {
                    if (target.UnturnedPlayer.life.isDead)
                        throw Context.Reply(_translations.TeleportTargetDead, target);

                    if (Context.MatchParameter(1, "wp", "wayport", "marker"))
                    {
                        if (!Context.Player.UnturnedPlayer.quests.isMarkerPlaced)
                            throw Context.Reply(_translations.TeleportWaypointNotFound);
                        Vector3 waypoint = Context.Player.UnturnedPlayer.quests.markerPosition;
                        GridLocation loc = new GridLocation(in waypoint);
                        waypoint.y = TerrainUtility.GetHighestPoint(in waypoint, float.NaN);
                        if (target.UnturnedPlayer.teleportToLocation(waypoint, target.Yaw))
                        {
                            _chatService.Send(target, _translations.TeleportSelfWaypointSuccess, loc);
                            throw Context.Reply(_translations.TeleportOtherWaypointSuccess, target, loc);
                        }
                        throw Context.Reply(_translations.TeleportOtherWaypointObstructed, target, loc);
                    }
                    if (Context.TryGet(1, out _, out onlinePlayer) && onlinePlayer is not null)
                    {
                        if (onlinePlayer.UnturnedPlayer.life.isDead)
                            throw Context.Reply(_translations.TeleportTargetDead, onlinePlayer);
                        InteractableVehicle? veh = onlinePlayer.UnturnedPlayer.movement.getVehicle();
                        pos = onlinePlayer.Position;
                        if (veh != null && !veh.isExploded && !veh.isDead)
                        {
                            if (VehicleManager.ServerForcePassengerIntoVehicle(target.UnturnedPlayer, veh))
                            {
                                _chatService.Send(target, _translations.TeleportSelfSuccessVehicle, onlinePlayer, veh);
                                throw Context.Reply(_translations.TeleportOtherSuccessVehicle, target, onlinePlayer, veh);
                            }
                            pos.y += 5f;
                        }

                        if (target.UnturnedPlayer.teleportToLocation(pos, onlinePlayer.Yaw))
                        {
                            _chatService.Send(target,  _translations.TeleportSelfSuccessPlayer, onlinePlayer);
                            throw Context.Reply(_translations.TeleportOtherSuccessPlayer, target, onlinePlayer);
                        }
                        throw Context.Reply(_translations.TeleportOtherObstructedPlayer, target, onlinePlayer);
                    }
                    if (GridLocation.TryParse(Context.Get(1)!, out location))
                    {
                        Vector3 center = location.Center;
                        center.y = TerrainUtility.GetHighestPoint(in center, float.NaN);
                        if (target.UnturnedPlayer.teleportToLocation(center, target.Yaw))
                        {
                            _chatService.Send(target, _translations.TeleportSelfWaypointSuccess, location);
                            throw Context.Reply(_translations.TeleportOtherWaypointSuccess, target, location);
                        }
                        throw Context.Reply(_translations.TeleportOtherWaypointObstructed, target, location);
                    }
                    input = Context.GetRange(1)!;
                    n = CollectionUtility.StringFind(LocationDevkitNodeSystem.Get().GetAllNodes().OrderBy(loc => loc.locationName.Length), loc => loc.locationName, input);

                    if (n is null)
                        throw Context.Reply(_translations.TeleportLocationNotFound, input);
                    pos = n.transform.position;
                    if (target.UnturnedPlayer.teleportToLocation(new Vector3(pos.x, LevelGround.getHeight(pos), pos.z), target.Yaw))
                    {
                        _chatService.Send(target, _translations.TeleportSelfLocationSuccess, n.locationName);
                        throw Context.Reply(_translations.TeleportOtherSuccessLocation, target, n.locationName);
                    }
                    throw Context.Reply(_translations.TeleportOtherObstructedLocation, target, n.locationName);
                }
                throw Context.Reply(_translations.TeleportTargetNotFound, Context.Get(0)!);

            case 3:
                Context.AssertRanByPlayer();
                pos = Context.Player.Position;
                if (!Context.TryGet(2, out float z))
                {
                    string p = Context.Get(2)!;
                    if (p.StartsWith("~", StringComparison.Ordinal))
                        z = pos.z + GetOffset(p);
                    else
                        throw Context.Reply(_translations.TeleportInvalidCoordinates);
                }
                if (!Context.TryGet(1, out float y))
                {
                    string p = Context.Get(1)!;
                    if (p.StartsWith("~", StringComparison.Ordinal))
                        y = pos.y + GetOffset(p);
                    else if (p.Length > 0 && p[0] == '-')
                        y = float.NaN;
                    else
                        throw Context.Reply(_translations.TeleportInvalidCoordinates);
                }
                if (!Context.TryGet(0, out float x))
                {
                    string p = Context.Get(0)!;
                    if (p.StartsWith("~", StringComparison.Ordinal))
                        x = pos.x + GetOffset(p);
                    else
                        throw Context.Reply(_translations.TeleportInvalidCoordinates);
                }

                pos = new Vector3(x, y, z);
                if (float.IsNaN(y))
                    pos.y = LevelGround.getHeight(pos with { y = 0f });

                throw Context.Reply(Context.Player.UnturnedPlayer.teleportToLocation(pos, Context.Player.Yaw)
                        ? _translations.TeleportSelfLocationSuccess
                        : _translations.TeleportSelfLocationObstructed,
                    $"({pos.x.ToString("0.##", Data.LocalLocale)}, {pos.y.ToString("0.##", Data.LocalLocale)}, {pos.z.ToString("0.##", Data.LocalLocale)})");

            case 4:
                if (Context.TryGet(0, out _, out target) && target is not null)
                {
                    if (target.UnturnedPlayer.life.isDead)
                        throw Context.Reply(_translations.TeleportTargetDead, target);

                    pos = Context.Player.Position;
                    if (!Context.TryGet(3, out z))
                    {
                        string p = Context.Get(3)!;
                        if (p.StartsWith("~", StringComparison.Ordinal))
                            z = pos.z + GetOffset(p);
                        else
                            throw Context.Reply(_translations.TeleportInvalidCoordinates);
                    }
                    if (!Context.TryGet(2, out y))
                    {
                        string p = Context.Get(2)!;
                        if (p.StartsWith("~", StringComparison.Ordinal))
                            y = pos.y + GetOffset(p);
                        else if (p.Length > 0 && p[0] == '-')
                            y = float.NaN;
                        else
                            throw Context.Reply(_translations.TeleportInvalidCoordinates);
                    }
                    if (!Context.TryGet(1, out x))
                    {
                        string p = Context.Get(1)!;
                        if (p.StartsWith("~", StringComparison.Ordinal))
                            x = pos.x + GetOffset(p);
                        else
                            throw Context.Reply(_translations.TeleportInvalidCoordinates);
                    }

                    pos = new Vector3(x, y, z);
                    if (float.IsNaN(y))
                        pos.y = LevelGround.getHeight(pos with { y = 0f });

                    string loc = $"({pos.x.ToString("0.##", Data.LocalLocale)}, {pos.y.ToString("0.##", Data.LocalLocale)}, {pos.z.ToString("0.##", Data.LocalLocale)})";
                    if (target.UnturnedPlayer.teleportToLocation(pos, target.Yaw))
                    {
                        _chatService.Send(target, _translations.TeleportSelfLocationSuccess, loc);
                        throw Context.Reply(_translations.TeleportOtherSuccessLocation, target, loc);
                    }
                    throw Context.Reply(_translations.TeleportOtherObstructedLocation, target, loc);
                }
                throw Context.Reply(_translations.TeleportTargetNotFound, Context.Get(0)!);

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
    public static void StartJumping(WarfarePlayer player)
    {
        player.Component<PlayerJumpComponent>().IsActive = true;
    }
    public static void StopJumping(WarfarePlayer player)
    {
        player.Component<PlayerJumpComponent>().IsActive = false;
    }
}

public class TeleportCommandTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Commands/Teleport";

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<IPlayer> TeleportTargetDead = new Translation<IPlayer>("<#8f9494>{0} is not alive.", arg0Fmt: WarfarePlayer.FormatColoredCharacterName);

    [TranslationData]
    public readonly Translation<IPlayer, InteractableVehicle> TeleportSelfSuccessVehicle = new Translation<IPlayer, InteractableVehicle>("<#bfb9ac>You were put in {0}'s {1}.", arg0Fmt: WarfarePlayer.FormatColoredCharacterName, arg1Fmt: RarityColorAddon.Instance);

    [TranslationData]
    public readonly Translation<IPlayer> TeleportSelfSuccessPlayer = new Translation<IPlayer>("<#bfb9ac>You were teleported to {0}.", arg0Fmt: WarfarePlayer.FormatColoredCharacterName);

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<IPlayer> TeleportSelfPlayerObstructed = new Translation<IPlayer>("<#8f9494>Failed to teleport you to {0}, their position is obstructed.", arg0Fmt: WarfarePlayer.FormatColoredCharacterName);

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<string> TeleportLocationNotFound = new Translation<string>("<#8f9494>Failed to find a location similar to <#ddd>{0}</color>.");

    [TranslationData]
    public readonly Translation<string> TeleportSelfLocationSuccess = new Translation<string>("<#bfb9ac>You were teleported to <#ddd>{0}</color>.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<string> TeleportStopJump = new Translation<string>("<#bfb9ac>You will no longer jump on right punch.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<string> TeleportStartJump = new Translation<string>("<#bfb9ac>You will jump on right punch. Do <#ddd>/tp jump stop</color> to stop.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<string> TeleportSelfLocationObstructed = new Translation<string>("<#8f9494>Failed to teleport you to <#ddd>{0}</color>, it's position is obstructed.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation TeleportWaypointNotFound = new Translation("<#8f9494>You must have a waypoint placed on the map.");

    [TranslationData]
    public readonly Translation<GridLocation> TeleportSelfWaypointSuccess = new Translation<GridLocation>("<#bfb9ac>You were teleported to your waypoint in <#ddd>{0}</color>.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<GridLocation> TeleportSelfWaypointObstructed = new Translation<GridLocation>("<#8f9494>Failed to teleport you to your waypoint in <#ddd>{0}</color>, it's position is obstructed.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<GridLocation> TeleportGridLocationNotFound = new Translation<GridLocation>("<#8f9494>There is no terrain at <#ddd>{0}</color>.");

    [TranslationData]
    public readonly Translation<GridLocation> TeleportSelfGridLocationSuccess = new Translation<GridLocation>("<#bfb9ac>You were teleported to <#ddd>{0}</color>.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<GridLocation> TeleportSelfGridLocationObstructed = new Translation<GridLocation>("<#8f9494>Failed to teleport you to <#ddd>{0}</color>, it's position is obstructed.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<IPlayer, GridLocation> TeleportOtherWaypointSuccess = new Translation<IPlayer, GridLocation>("<#bfb9ac>{0} was teleported to your waypoint in <#ddd>{1}</color>.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<IPlayer, GridLocation> TeleportOtherWaypointObstructed = new Translation<IPlayer, GridLocation>("<#8f9494>Failed to teleport {0} to your waypoint in <#ddd>{1}</color>, it's position is obstructed.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<IPlayer, GridLocation> TeleportOtherGridLocationSuccess = new Translation<IPlayer, GridLocation>("<#bfb9ac>{0} was teleported to <#ddd>{1}</color>.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<IPlayer, GridLocation> TeleportOtherGridLocationObstructed = new Translation<IPlayer, GridLocation>("<#8f9494>Failed to teleport {0} to <#ddd>{1}</color>, it's position is obstructed.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<IPlayer, IPlayer, InteractableVehicle> TeleportOtherSuccessVehicle = new Translation<IPlayer, IPlayer, InteractableVehicle>("<#bfb9ac>{0} was put in {1}'s {2}.", arg0Fmt: WarfarePlayer.FormatColoredCharacterName, arg1Fmt: WarfarePlayer.FormatColoredCharacterName, arg2Fmt: RarityColorAddon.Instance);

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<IPlayer, IPlayer> TeleportOtherSuccessPlayer = new Translation<IPlayer, IPlayer>("<#bfb9ac>{0} was teleported to {1}.", arg0Fmt: WarfarePlayer.FormatColoredCharacterName, arg1Fmt: WarfarePlayer.FormatColoredCharacterName);

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<IPlayer, IPlayer> TeleportOtherObstructedPlayer = new Translation<IPlayer, IPlayer>("<#8f9494>Failed to teleport {0} to {1}, their position is obstructed.", arg0Fmt: WarfarePlayer.FormatColoredCharacterName, arg1Fmt: WarfarePlayer.FormatColoredCharacterName);

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<IPlayer, string> TeleportOtherSuccessLocation = new Translation<IPlayer, string>("<#bfb9ac>{0} was teleported to <#ddd>{1}</color>.", arg0Fmt: WarfarePlayer.FormatColoredCharacterName);

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<IPlayer, string> TeleportOtherObstructedLocation = new Translation<IPlayer, string>("<#8f9494>Failed to teleport {0} to <#ddd>{1}</color>, it's position is obstructed.", arg0Fmt: WarfarePlayer.FormatColoredCharacterName);

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<string> TeleportTargetNotFound = new Translation<string>("<#8f9494>Failed to find a player from <#ddd>{0}</color>.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation TeleportInvalidCoordinates = new Translation("<#8f9494>Use of coordinates should look like: <#eee>/tp [player] <x y z></color>.");
}