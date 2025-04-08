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

[Command("teleport", "tp"), Priority(1), MetadataFile]
internal sealed class TeleportCommand : IExecutableCommand
{
    private readonly ChatService _chatService;
    private readonly TeleportCommandTranslations _translations;
    private const string Syntax = "/tp <x y z|player|location|wp|jump [dstance]> - or - /tp <player> <x y z|player|location|wp>";

    /// <inheritdoc />
    public required CommandContext Context { get; init; }
    
    public TeleportCommand(ChatService chatService, TranslationInjection<TeleportCommandTranslations> translations)
    {
        _chatService = chatService;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
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
            throw Context.Reply(_translations.TeleportSelfLocationSuccess, $"({castPt.x.ToString("0.##", Context.Culture)}, {castPt.y.ToString("0.##", Context.Culture)}, {castPt.z.ToString("0.##", Context.Culture)})");
        }

        const float heightOffset = 0.325f;

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
                    waypoint.y = TerrainUtility.GetHighestPoint(in waypoint, float.NaN) + heightOffset;
                    if (Context.Player.UnturnedPlayer.teleportToLocation(waypoint, Context.Player.Yaw))
                        throw Context.Reply(_translations.TeleportSelfWaypointSuccess, loc);
                    throw Context.Reply(_translations.TeleportSelfWaypointObstructed, loc);
                }
                string input = Context.GetRange(0)!;
                LocationDevkitNode? n = CollectionUtility.StringFind(LocationDevkitNodeSystem.Get().GetAllNodes().OrderBy(loc => loc.locationName.Length), loc => loc.locationName, input);
                if (n != null)
                {
                    pos = n.transform.position;
                    if (Context.Player.UnturnedPlayer.teleportToLocation(new Vector3(pos.x, TerrainUtility.GetHighestPoint(in pos, float.NaN) + heightOffset, pos.z), Context.Player.Yaw))
                        throw Context.Reply(_translations.TeleportSelfLocationSuccess, n.locationName);
                    throw Context.Reply(_translations.TeleportSelfLocationObstructed, n.locationName);
                }

                (_, WarfarePlayer? onlinePlayer) = await Context.TryGetPlayer(0).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(token);

                if (onlinePlayer is not null)
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
                    Vector2 center = location.Center;
                    float height = TerrainUtility.GetHighestPoint(in center, float.NaN) + heightOffset;
                    if (Context.Player.UnturnedPlayer.teleportToLocation(new Vector3(center.x, height, center.y), Context.Player.Yaw))
                        throw Context.Reply(_translations.TeleportSelfWaypointSuccess, location);
                    throw Context.Reply(_translations.TeleportSelfWaypointObstructed, location);
                }

                throw Context.Reply(_translations.TeleportLocationNotFound, input);

            case 2:
                (_, WarfarePlayer? target) = await Context.TryGetPlayer(0).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(token);

                if (target is null)
                    throw Context.Reply(_translations.TeleportTargetNotFound, Context.Get(0)!);

                if (target.UnturnedPlayer.life.isDead)
                    throw Context.Reply(_translations.TeleportTargetDead, target);

                if (Context.MatchParameter(1, "wp", "wayport", "marker"))
                {
                    if (!Context.Player.UnturnedPlayer.quests.isMarkerPlaced)
                        throw Context.Reply(_translations.TeleportWaypointNotFound);
                    Vector3 waypoint = Context.Player.UnturnedPlayer.quests.markerPosition;
                    GridLocation loc = new GridLocation(in waypoint);
                    waypoint.y = TerrainUtility.GetHighestPoint(in waypoint, float.NaN) + heightOffset;
                    if (target.UnturnedPlayer.teleportToLocation(waypoint, target.Yaw))
                    {
                        _chatService.Send(target, _translations.TeleportSelfWaypointSuccess, loc);
                        throw Context.Reply(_translations.TeleportOtherWaypointSuccess, target, loc);
                    }
                    throw Context.Reply(_translations.TeleportOtherWaypointObstructed, target, loc);
                }

                input = Context.GetRange(1)!;
                n = CollectionUtility.StringFind(LocationDevkitNodeSystem.Get().GetAllNodes().OrderBy(loc => loc.locationName.Length), loc => loc.locationName, input);
                if (n != null && !Context.MatchParameter(1, "me"))
                {
                    pos = n.transform.position;

                    if (!target.UnturnedPlayer.teleportToLocation(new Vector3(pos.x, TerrainUtility.GetHighestPoint(in pos, float.NaN) + heightOffset, pos.z), target.Yaw))
                    {
                        throw Context.Reply(_translations.TeleportOtherObstructedLocation, target, n.locationName);
                    }

                    _chatService.Send(target, _translations.TeleportSelfLocationSuccess, n.locationName);
                    throw Context.Reply(_translations.TeleportOtherSuccessLocation, target, n.locationName);
                }

                (_, onlinePlayer) = await Context.TryGetPlayer(1).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(token);

                if (onlinePlayer is not null)
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
                        _chatService.Send<IPlayer>(target,  _translations.TeleportSelfSuccessPlayer, onlinePlayer);
                        throw Context.Reply(_translations.TeleportOtherSuccessPlayer, target, onlinePlayer);
                    }
                    throw Context.Reply(_translations.TeleportOtherObstructedPlayer, target, onlinePlayer);
                }

                if (GridLocation.TryParse(Context.Get(1)!, out location))
                {
                    Vector2 center = location.Center;
                    float height = TerrainUtility.GetHighestPoint(in center, float.NaN) + heightOffset;
                    if (!target.UnturnedPlayer.teleportToLocation(new Vector3(center.x, height, center.y), target.Yaw))
                        throw Context.Reply(_translations.TeleportOtherWaypointObstructed, target, location);

                    _chatService.Send(target, _translations.TeleportSelfWaypointSuccess, location);
                    throw Context.Reply(_translations.TeleportOtherWaypointSuccess, target, location);
                }

                throw Context.Reply(_translations.TeleportLocationNotFound, input);

            case 3:
                Context.AssertRanByPlayer();
                pos = ParseCoordiantes(0, Context.Player.Position);
                if (float.IsNaN(pos.y))
                    pos.y = TerrainUtility.GetHighestPoint(in pos, float.NaN) + heightOffset;

                throw Context.Reply(Context.Player.UnturnedPlayer.teleportToLocation(pos, Context.Player.Yaw)
                        ? _translations.TeleportSelfLocationSuccess
                        : _translations.TeleportSelfLocationObstructed,
                    $"({pos.x.ToString("0.##", Context.Culture)}, {pos.y.ToString("0.##", Context.Culture)}, {pos.z.ToString("0.##", Context.Culture)})");

            case 4:
                (_, target) = await Context.TryGetPlayer(0).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(token);

                if (target is null)
                    throw Context.Reply(_translations.TeleportTargetNotFound, Context.Get(0)!);

                if (target.UnturnedPlayer.life.isDead)
                    throw Context.Reply(_translations.TeleportTargetDead, target);

                pos = ParseCoordiantes(1, Context.Player.Position);
                if (float.IsNaN(pos.y))
                    pos.y = TerrainUtility.GetHighestPoint(in pos, float.NaN) + heightOffset;

                string locName = $"({pos.x.ToString("0.##", Context.Culture)}, {pos.y.ToString("0.##", Context.Culture)}, {pos.z.ToString("0.##", Context.Culture)})";

                if (!target.UnturnedPlayer.teleportToLocation(pos, target.Yaw))
                    throw Context.Reply(_translations.TeleportOtherObstructedLocation, target, locName);
                    
                _chatService.Send(target, _translations.TeleportSelfLocationSuccess, locName);
                throw Context.Reply(_translations.TeleportOtherSuccessLocation, target, locName);

            default:
                throw Context.SendCorrectUsage(Syntax);
        }
    }

    private Vector3 ParseCoordiantes(int offset, Vector3 pos)
    {
        Vector3 v3 = default;

        string? xStr = Context.Get(offset);
        string? yStr = Context.Get(offset + 1);
        string? zStr = Context.Get(offset + 2);

        if (xStr != null && xStr.StartsWith('('))
            xStr = xStr[1..];
        if (xStr != null && xStr.EndsWith(','))
            xStr = xStr[..^1];
        if (yStr != null && yStr.EndsWith(','))
            yStr = yStr[..^1];
        if (zStr != null && zStr.EndsWith(')'))
            zStr = zStr[..^1];

        if (!float.TryParse(xStr, NumberStyles.Number, CultureInfo.InvariantCulture, out v3.x))
        {
            if (xStr != null && xStr.StartsWith("~", StringComparison.Ordinal))
                v3.x = pos.x + GetOffset(xStr);
            else
                throw Context.Reply(_translations.TeleportInvalidCoordinates);
        }

        if (!float.TryParse(yStr, NumberStyles.Number, CultureInfo.InvariantCulture, out v3.y))
        {
            if (yStr != null && yStr.StartsWith("~", StringComparison.Ordinal))
                v3.y = pos.y + GetOffset(yStr);
            else if (yStr == "-")
                v3.y = float.NaN;
            else
                throw Context.Reply(_translations.TeleportInvalidCoordinates);
        }

        if (!float.TryParse(zStr, NumberStyles.Number, CultureInfo.InvariantCulture, out v3.z))
        {
            if (zStr != null && zStr.StartsWith("~", StringComparison.Ordinal))
                v3.z = pos.z + GetOffset(zStr);
            else
                throw Context.Reply(_translations.TeleportInvalidCoordinates);
        }

        return v3;
    }

    private float GetOffset(string arg)
    {
        if (arg.Length < 2 || !float.TryParse(arg.AsSpan(1), NumberStyles.Number, Context.ParseFormat, out float offset))
        {
            return 0f;
        }

        return offset;
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
    public readonly Translation TeleportStopJump = new Translation("<#bfb9ac>You will no longer jump on right punch.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation TeleportStartJump = new Translation("<#bfb9ac>You will jump on right punch. Do <#ddd>/tp jump stop</color> to stop.");

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