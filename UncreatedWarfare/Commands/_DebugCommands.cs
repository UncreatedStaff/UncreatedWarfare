using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Networking.Async;
using Uncreated.Players;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.ReportSystem;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Structures;
using UnityEngine;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare.Commands;

#pragma warning disable IDE1006 // Naming Styles
internal class _DebugCommand : Command
#pragma warning restore IDE1006 // Naming Styles
{
    public static int currentstep = 0;
    private readonly Type type = typeof(_DebugCommand);
    public _DebugCommand() : base("test", EAdminType.MEMBER) { }
    public override void Execute(CommandInteraction ctx)
    {
        if (ctx.TryGet(0, out string operation))
        {
            MethodInfo info;
            try
            {
                info = type.GetMethod(operation, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
            }
            catch (AmbiguousMatchException)
            {
                throw ctx.Reply("test_multiple_matches", operation);
            }
            if (info == null)
                throw ctx.Reply("test_no_method", operation);
            try
            {
#if DEBUG
                using IDisposable profiler = ProfilingUtils.StartTracking(info.Name + " Debug Command");
#endif
                ctx.Offset = 1;
                info.Invoke(this, new object[1] { ctx });
                ctx.Offset = 0;
                ctx.Defer();
            }
            catch (Exception ex)
            {
                if (ex is BaseCommandInteraction b)
                    throw b;
                L.LogError(ex.InnerException ?? ex);
                throw ctx.Reply("test_error_executing", info.Name, (ex.InnerException ?? ex).GetType().Name);
            }
        }
        else throw ctx.SendCorrectUsage("/test <operation> [parameters...]");
    }
#pragma warning disable IDE1006
#pragma warning disable IDE0060
#pragma warning disable IDE0051
    private const string GIVE_XP_SYNTAX = "/test givexp <player> <amount> [team - required if offline]";
    private void givexp(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.MODERATOR);

        if (!ctx.HasArgs(2))
        {
            ctx.SendCorrectUsage(GIVE_XP_SYNTAX);
            return;
        }
        if (ctx.TryGet(1, out int amount))
        {
            if (ctx.TryGet(0, out ulong player, out UCPlayer? onlinePlayer))
            {
                ctx.TryGet(2, out ulong team);
                if (onlinePlayer is null)
                {
                    if (PlayerSave.HasPlayerSave(player))
                    {
                        if (team > 0 && team < 3)
                        {
                            Task.Run(async () =>
                            {
                                await Data.DatabaseManager.AddXP(player, team, amount);
                                FPlayerName name = await Data.DatabaseManager.GetUsernamesAsync(player);
                                await UCWarfare.ToUpdate();
                                ctx.Reply("test_givexp_success", amount.ToString(Data.Locale), ctx.IsConsole ? name.PlayerName : name.CharacterName);
                            });
                            ctx.Defer();
                        }
                        else
                        {
                            ctx.SendCorrectUsage(GIVE_XP_SYNTAX);
                        }
                    }
                    else
                    {
                        ctx.Reply("test_givexp_player_not_found", ctx.Get(0)!);
                    }
                }
                else
                {
                    if (team < 1 || team > 2)
                        team = onlinePlayer.GetTeam();
                    Points.AwardXP(onlinePlayer, amount, ctx.IsConsole ? Localization.Translate("xp_from_operator", player) :
                        Localization.Translate("xp_from_player", player, F.GetPlayerOriginalNames(ctx.CallerID).CharacterName.ToUpper()));
                    FPlayerName names = F.GetPlayerOriginalNames(onlinePlayer);
                    ctx.Reply("test_givexp_success", amount.ToString(Data.Locale), ctx.IsConsole ? names.PlayerName : names.CharacterName);
                }
            }
            else
            {
                ctx.SendCorrectUsage(GIVE_XP_SYNTAX);
            }
        }
        else
        {
            ctx.Reply("test_givexp_invalid_amount", ctx.Get(1)!);
        }
    }
    private const string GIVE_CREDITS_SYNTAX = "/test givecredits <player> <amount> [team - required if offline]";
    private void givecredits(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.MODERATOR);

        if (!ctx.HasArgs(2))
        {
            ctx.SendCorrectUsage(GIVE_CREDITS_SYNTAX);
            return;
        }
        if (ctx.TryGet(1, out int amount))
        {
            if (ctx.TryGet(0, out ulong player, out UCPlayer? onlinePlayer))
            {
                ctx.TryGet(2, out ulong team);
                if (onlinePlayer is null)
                {
                    if (PlayerSave.HasPlayerSave(player))
                    {
                        if (team > 0 && team < 3)
                        {
                            Task.Run(async () =>
                            {
                                await Data.DatabaseManager.AddCredits(player, team, amount);
                                FPlayerName name = await Data.DatabaseManager.GetUsernamesAsync(player);
                                await UCWarfare.ToUpdate();
                                ctx.Reply("test_givecredits_success", amount.ToString(Data.Locale), ctx.IsConsole ? name.PlayerName : name.CharacterName);
                            });
                            ctx.Defer();
                        }
                        else
                        {
                            ctx.SendCorrectUsage(GIVE_CREDITS_SYNTAX);
                        }
                    }
                    else
                    {
                        ctx.Reply("test_givecredits_player_not_found", ctx.Get(0)!);
                    }
                }
                else
                {
                    if (team < 1 || team > 2)
                        team = onlinePlayer.GetTeam();
                    Points.AwardCredits(onlinePlayer, amount, ctx.IsConsole ? Localization.Translate("credits_from_operator", player) :
                        Localization.Translate("credits_from_player", player, F.GetPlayerOriginalNames(ctx.CallerID).CharacterName.ToUpper()));
                    FPlayerName names = F.GetPlayerOriginalNames(onlinePlayer);
                    ctx.Reply("test_givecredits_success", amount.ToString(Data.Locale), ctx.IsConsole ? names.PlayerName : names.CharacterName);
                }
            }
            else
            {
                ctx.SendCorrectUsage(GIVE_CREDITS_SYNTAX);
            }
        }
        else
        {
            ctx.Reply("test_givecredits_invalid_amount", ctx.Get(1)!);
        }
    }
    private void quickcap(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.ADMIN);

        ctx.AssertRanByPlayer();
        ctx.AssertGamemode(out IFlagRotation fg);

        Flag flag = fg.Rotation.FirstOrDefault(f => f.PlayersOnFlag.Contains(ctx.Caller.Player));
        if (flag == default)
        {
            Vector3 pos = ctx.Caller.Position;
            ctx.Reply("test_zone_not_in_zone",
                pos.x.ToString(Data.Locale), pos.y.ToString(Data.Locale),
                pos.z.ToString(Data.Locale), fg.Rotation.Count.ToString(Data.Locale));
            return;
        }
        ulong team = ctx.Caller.GetTeam();
        if (team == 1)
        {
            if (flag.Points < 0)
            {
                flag.CapT1(Math.Abs(flag.Points));
            }
            else
            {
                flag.CapT1(Flag.MAX_POINTS - flag.Points - 1);
            }
        }
        else if (team == 2)
        {
            if (flag.Points > 0)
            {
                flag.CapT2(flag.Points);
            }
            else
            {
                flag.CapT2(Flag.MAX_POINTS - flag.Points - 2);
            }
        }
        else ctx.Reply("gamemode_flag_not_on_cap_team");
    }
    private void quickwin(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.ADMIN);

        ulong team;
        if (ctx.TryGet(0, out ulong id))
            team = id;
        else if (!ctx.IsConsole)
            team = ctx.Caller!.GetTeam();
        else
        {
            ctx.Reply("gamemode_flag_not_on_cap_team_console");
            return;
        }
        Data.Gamemode.DeclareWin(team);
    }
    private void savemanyzones(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);

        ctx.AssertGamemode(out IFlagRotation fg);

        if (!ctx.TryGet(0, out uint times))
            times = 1U;
        List<Zone> zones = new List<Zone>();
        string directory = Path.Combine(Data.Paths.FlagStorage, "ZoneExport", Path.DirectorySeparatorChar.ToString());
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
        for (int i = 0; i < times; i++)
        {
            zones.Clear();
            ReloadCommand.ReloadFlags();
            fg.Rotation.ForEach(x => zones.Add(x.ZoneData));
            ZoneDrawing.CreateFlagTestAreaOverlay(fg, ctx.Caller?.Player, zones, true, true, false, false, true, Path.Combine(directory, "zonearea_" + i.ToString(Data.Locale)));
            L.Log("Done with " + (i + 1).ToString(Data.Locale) + '/' + times.ToString(Data.Locale));
        }
    }
    private void savemanygraphs(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);

        ctx.AssertGamemode(out IFlagRotation fg);

        if (!ctx.TryGet(0, out uint times))
            times = 1U;
        List<Zone> zones = new List<Zone>();
        string directory = Path.Combine(Data.Paths.FlagStorage, "GraphExport", Path.DirectorySeparatorChar.ToString());
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
        List<Flag> rot = new List<Flag>();
        for (int i = 0; i < times; i++)
        {
            ObjectivePathing.TryPath(rot);
            zones.Clear();
            ZoneDrawing.DrawZoneMap(fg.LoadedFlags, rot, Path.Combine(directory, "zonegraph_" + i.ToString(Data.Locale)));
            L.Log("Done with " + (i + 1).ToString(Data.Locale) + '/' + times.ToString(Data.Locale));
            rot.Clear();
        }
    }
    private void zone(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.STAFF);

        ctx.AssertRanByPlayer();
        ctx.AssertGamemode(out IFlagRotation fg);

        Vector3 pos = ctx.Caller.Position;
        if (pos == Vector3.zero) return;
        Flag flag = fg.Rotation.FirstOrDefault(f => f.PlayerInRange(pos));
        if (flag == default(Flag))
        {
            ctx.Reply("test_zone_not_in_zone",
                pos.x.ToString(Data.Locale), pos.y.ToString(Data.Locale),
                pos.z.ToString(Data.Locale), ctx.Caller.Player.transform.eulerAngles.y.ToString(Data.Locale),
                fg.Rotation.Count.ToString(Data.Locale));
        }
        else
        {
            ctx.Reply("test_zone_current_zone", flag.Name,
                pos.x.ToString(Data.Locale), pos.y.ToString(Data.Locale),
                pos.z.ToString(Data.Locale));
        }
    }
    private void sign(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.STAFF);

        ctx.AssertRanByPlayer();

        InteractableSign? sign = UCBarricadeManager.GetInteractableFromLook<InteractableSign>(ctx.Caller.Player.look);
        if (sign == null) ctx.Reply("test_sign_no_sign");
        else
        {
            ctx.Reply("test_sign_success", sign.text);
            L.Log(Localization.Translate("test_sign_success", 0, out _, sign.text), ConsoleColor.Green);
        }
    }
    private void time(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);

        UCWarfare.I.CoroutineTiming = !UCWarfare.I.CoroutineTiming;
        ctx.Reply("test_time_enabled_console");
    }
    // test zones: test zonearea all true false false true false
    private void zonearea(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);

        ctx.AssertGamemode(out IFlagRotation fg);
        bool all = true;
        bool extra = true;
        bool path = true;
        bool range = false;
        bool drawIn = true;
        bool drawAngles = false;
        if (ctx.HasArgs(6))
        {
            if (ctx.MatchParameter(0, "active")) all = false;
            else if (!ctx.MatchParameter(0, "active")) ctx.Reply("test_zonearea_syntax");
            if (ctx.MatchParameter(1, "false")) extra = false;
            else if (!ctx.MatchParameter(1, "false")) ctx.Reply("test_zonearea_syntax");
            if (ctx.MatchParameter(2, "false")) path = false;
            else if (!ctx.MatchParameter(2, "true")) ctx.Reply("test_zonearea_syntax");
            if (ctx.MatchParameter(3, "true")) range = true;
            else if (!ctx.MatchParameter(3, "false")) ctx.Reply("test_zonearea_syntax");
            if (ctx.MatchParameter(4, "false")) drawIn = false;
            else if (!ctx.MatchParameter(4, "true")) ctx.Reply("test_zonearea_syntax");
            if (ctx.MatchParameter(5, "true")) drawAngles = true;
            else if (!ctx.MatchParameter(5, "false")) ctx.Reply("test_zonearea_syntax");
        }
        else if (ctx.ArgumentCount != 1)
        {
            ctx.Reply("test_zonearea_syntax");
            return;
        }
        List<Zone> zones = new List<Zone>();
        if (all)
        {
            if (extra)
                zones.AddRange(Data.ZoneProvider.Zones);
            else
                zones.AddRange(Data.ZoneProvider.Zones.Where(x => x.Data.UseCase == EZoneUseCase.FLAG));
        }
        else
            zones.AddRange(fg.Rotation.Select(x => x.ZoneData));
        ctx.Reply("test_zonearea_started");
        ctx.LogAction(EActionLogType.BUILD_ZONE_MAP, "ZONEAREA");
        ZoneDrawing.CreateFlagTestAreaOverlay(fg, ctx.Caller?.Player, zones, path, range, drawIn, drawAngles, true);
    }
    private void drawzone(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);

        ctx.AssertGamemode(out IFlagRotation fg);
        Zone zone;
        string zoneName;
        string zoneColor;
        Vector3 pos = ctx.Caller.Position;
        if (pos == Vector3.zero) return;
        Flag flag = fg.LoadedFlags.FirstOrDefault(f => f.PlayerInRange(pos));
        if (flag == default)
        {
            ctx.Reply("test_zone_test_zone_not_in_zone", pos.x.ToString(Data.Locale),
                pos.y.ToString(Data.Locale), pos.z.ToString(Data.Locale),
                fg.LoadedFlags.Count.ToString(Data.Locale));
            return;
        }
        else
        {
            zone = flag.ZoneData;
            zoneName = flag.Name;
            zoneColor = flag.TeamSpecificHexColor;
        }
        List<Zone> zones = new List<Zone>(1) { zone };
        ctx.LogAction(EActionLogType.BUILD_ZONE_MAP, "DRAWZONE");
        ZoneDrawing.CreateFlagTestAreaOverlay(fg, ctx.Caller?.Player, zones, false, true, false, true, true, Path.Combine(Data.Paths.FlagStorage, "zonerange_" + zoneName));
    }
    private void drawgraph(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);

        ctx.AssertGamemode(out IFlagRotation fg);

        ctx.LogAction(EActionLogType.BUILD_ZONE_MAP, "DRAWGRAPH");
        ZoneDrawing.DrawZoneMap(fg.LoadedFlags, fg.Rotation, null);
    }
    private void rotation(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);

        ctx.AssertGamemode(out FlagGamemode fg);
        fg.PrintFlagRotation();
    }
    private const byte DOWN_DAMAGE = 55;
    private void down(CommandInteraction ctx)
    {
        ctx.AssertGamemode<IRevives>();

        if (ctx.TryGet(0, out _, out UCPlayer? player))
        {
            if (player is null)
                throw ctx.SendPlayerNotFound();

            ctx.AssertPermissions(EAdminType.ADMIN);
        }
        else
        {
            ctx.AssertRanByPlayer();
            ctx.AssertPermissions(EAdminType.STAFF);
            player = ctx.Caller;
        }
        player.Player.life.askDamage(DOWN_DAMAGE, Vector3.down, EDeathCause.KILL, ELimb.SPINE, ctx.CallerCSteamID, out _, false, ERagdollEffect.ZERO_KELVIN, false, true);
        player.Player.life.askDamage(DOWN_DAMAGE, Vector3.down, EDeathCause.KILL, ELimb.SPINE, ctx.CallerCSteamID, out _, false, ERagdollEffect.ZERO_KELVIN, false, true);
        ctx.Reply("test_down_success", (DOWN_DAMAGE * 2).ToString(Data.Locale));
    }
    private void layer(CommandInteraction ctx)
    {
        ctx.AssertRanByPlayer();

        ctx.Reply("<color=#fdfdf>Layer: <b>" + F.GetLayer(ctx.Caller.Player.look.aim.position, ctx.Caller.Player.look.aim.forward, RayMasks.BLOCK_COLLISION) + "</b></color>"); // so as to not hit player
    }
    private void clearui(CommandInteraction ctx)
    {
        ctx.AssertRanByPlayer();

        if (ctx.Caller.HasUIHidden)
        {
            UCWarfare.I.UpdateLangs(ctx.Caller);
        }
        else
        {
            Data.SendEffectClearAll.InvokeAndLoopback(ENetReliability.Reliable, new ITransportConnection[] { ctx.Caller.Connection });
        }
        ctx.Caller.HasUIHidden = !ctx.Caller.HasUIHidden;
        
    }
    private void game(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);

        if (Data.Is(out IFlagRotation fg))
        {
            StringBuilder flags = new StringBuilder();
            for (int f = 0; f < fg.Rotation.Count; f++)
            {
                if (f == 0) flags.Append('\n');
                flags.Append(fg.Rotation[f].Name).Append("\nOwner: ").Append(fg.Rotation[f].Owner).Append(" Players: \n1: ")
                    .Append(string.Join(",", fg.Rotation[f].PlayersOnFlagTeam1.Select(x => F.GetPlayerOriginalNames(x).PlayerName))).Append("\n2: ")
                    .Append(string.Join(",", fg.Rotation[f].PlayersOnFlagTeam2.Select(x => F.GetPlayerOriginalNames(x).PlayerName)))
                    .Append("\nPoints: ").Append(fg.Rotation[f].Points).Append(" State: ").Append(fg.Rotation[f].LastDeltaPoints).Append('\n');
            }
        }
    }
    private const string PLAYER_SAVE_USAGE = "/test playersave <player> <property> <value>";
    private void playersave(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.MODERATOR);

        if (!ctx.HasArgs(3))
        {
            ctx.SendCorrectUsage(PLAYER_SAVE_USAGE);
            return;
        }
        if (ctx.TryGet(0, out ulong player, out UCPlayer? onlinePlayer))
        {
            if (PlayerManager.HasSave(player, out PlayerSave save))
            {
                if (ctx.TryGet(2, out string value) && ctx.TryGet(1, out string property))
                {
                    ESetFieldResult result = PlayerManager.SetProperty(save, property, value);
                    switch (result)
                    {
                        case ESetFieldResult.SUCCESS:
                            FPlayerName names = F.GetPlayerOriginalNames(player);
                            ctx.Reply("test_playersave_success", ctx.IsConsole ? names.PlayerName : names.CharacterName, property.ToProperCase(), value);
                            break;
                        case ESetFieldResult.FIELD_NOT_FOUND:
                            ctx.Reply("test_playersave_field_not_found", property.ToProperCase());
                            break;
                        case ESetFieldResult.FIELD_PROTECTED:
                            ctx.Reply("test_playersave_field_protected", property.ToProperCase());
                            break;
                        case ESetFieldResult.FIELD_NOT_SERIALIZABLE:
                        case ESetFieldResult.INVALID_INPUT:
                            ctx.Reply("test_playersave_couldnt_parse", value, property.ToProperCase());
                            break;
                        default:
                            ctx.SendUnknownError();
                            break;
                    }
                }
                else
                    ctx.SendCorrectUsage(PLAYER_SAVE_USAGE);
            }
            else
                ctx.Reply("test_playersave_not_found");
        }
        else
            ctx.SendCorrectUsage(PLAYER_SAVE_USAGE);
    }
    private const string GAMEMODE_USAGE = "/test gamemode <gamemode>";
    private void gamemode(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.ADMIN);

        if (!ctx.HasArgs(1))
        {
            ctx.SendCorrectUsage(GAMEMODE_USAGE);
            return;
        }
        if (ctx.TryGet(0, out string gamemodeName))
        {
            Type? newGamemode = Gamemode.FindGamemode(gamemodeName);
            if (newGamemode is not null)
            {
                if (Data.Is(out IStagingPhase gm))
                {
                    ctx.Reply("test_gamemode_skipped_staging");
                    gm.SkipStagingPhase();
                }
                if (newGamemode == Data.Gamemode.GetType())
                    Data.Singletons.ReloadSingleton(Gamemode.GAMEMODE_RELOAD_KEY);
                else if (Gamemode.TryLoadGamemode(newGamemode))
                {
                    ctx.Reply("test_gamemode_loaded_gamemode", newGamemode.Name);
                }
                else
                {
                    ctx.Reply("test_gamemode_failed_loading_gamemode", newGamemode.Name);
                }
            }
            else
                ctx.Reply("test_gamemode_type_not_found", gamemodeName);
        }
        else
            ctx.SendCorrectUsage(GAMEMODE_USAGE);
    }
    private void trackstats(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);

        Data.TrackStats = !Data.TrackStats;
        if (Data.TrackStats)
            ctx.Reply("test_trackstats_enabled");
        else
            ctx.Reply("test_trackstats_disabled");
    }
    private void destroyblocker(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.MODERATOR);

        int ct = 0;
        for (int x = 0; x < Regions.WORLD_SIZE; x++)
        {
            for (int y = 0; y < Regions.WORLD_SIZE; y++)
            {
                for (int i = 0; i < BarricadeManager.regions[x, y].drops.Count; i++)
                {
                    BarricadeDrop d = BarricadeManager.regions[x, y].drops[i];
                    if (d.asset.id == 36058 || d.asset.id == 36059)
                    {
                        BarricadeManager.destroyBarricade(d, (byte)x, (byte)y, ushort.MaxValue);
                        ++ct;
                    }
                }
            }
        }
        if (ct == 0)
            ctx.Reply("test_destroyblocker_failure");
        else
            ctx.Reply("test_destroyblocker_success", ct.ToString(Data.Locale), ct.S());
    }
    private void skipstaging(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.MODERATOR);

        ctx.AssertGamemode(out IStagingPhase gm);

        gm.SkipStagingPhase();

        ctx.Reply("test_gamemode_skipped_staging");
    }
    private void resetlobby(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.MODERATOR);

        ctx.AssertGamemode(out ITeams t);
        if (t.UseTeamSelector)
        {
            if (!ctx.HasArgs(1))
            {
                ctx.SendCorrectUsage("/resetlobby <player>");
                return;
            }
            if (ctx.TryGet(0, out _, out UCPlayer? player) && player is not null)
            {
                t.TeamSelector?.ResetState(player);
                FPlayerName name = F.GetPlayerOriginalNames(player);
                ctx.Reply("test_resetlobby_success", ctx.IsConsole ? name.PlayerName : name.CharacterName);
            }
            else
            {
                ctx.SendPlayerNotFound();
                return;
            }
        }
        else ctx.SendGamemodeError();
    }
    private void clearcooldowns(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.MODERATOR);

        ctx.AssertRanByPlayer();
        if (Data.Gamemode?.Cooldowns is null) throw ctx.SendNotImplemented();

        if (!ctx.TryGet(1, out _, out UCPlayer? pl) || pl is null)
            pl = ctx.Caller;
        CooldownManager.RemoveCooldown(pl);
    }
    private void instid(CommandInteraction ctx)
    {
        ctx.AssertRanByPlayer();

        Player player = ctx.Caller.Player;
        Transform? t = UCBarricadeManager.GetTransformFromLook(player.look, RayMasks.BARRICADE | RayMasks.STRUCTURE | RayMasks.LARGE | RayMasks.MEDIUM | RayMasks.SMALL | RayMasks.VEHICLE);
        if (t == null)
        {
            ctx.Reply("test_instid_not_found");
            return;
        }
        BarricadeDrop? bd = BarricadeManager.FindBarricadeByRootTransform(t);
        if (bd != null)
        {
            ctx.Reply("test_instid_found_barricade", bd.instanceID.ToString(Data.Locale));
            return;
        }
        StructureDrop? dp = StructureManager.FindStructureByRootTransform(t);
        if (dp != null)
        {
            ctx.Reply("test_instid_found_structure", dp.instanceID.ToString(Data.Locale));
            return;
        }
        for (int i = 0; i < VehicleManager.vehicles.Count; i++)
        {
            if (VehicleManager.vehicles[i].transform == t)
            {
                ctx.Reply("test_instid_found_vehicle", VehicleManager.vehicles[i].instanceID.ToString(Data.Locale));
                return;
            }
        }
        for (byte b = 0; b < Regions.WORLD_SIZE; b++)
        {
            for (byte b2 = 0; b2 < Regions.WORLD_SIZE; b2++)
            {
                for (int i = 0; i < LevelObjects.objects[b, b2].Count; i++)
                {
                    LevelObject obj = LevelObjects.objects[b, b2][i];
                    if (obj.transform == t)
                    {
                        ctx.Reply("test_instid_found_object", obj.instanceID.ToString(Data.Locale));
                        return;
                    }
                }
            }
        }
        ctx.Reply("test_instid_not_found");
    }
    private void fakereport(CommandInteraction ctx)
    {
        if (!UCWarfare.Config.EnableReporter) throw ctx.SendNotImplemented();

        ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);

        Report report = new ChatAbuseReport()
        {
            Message = ctx.GetRange(0) ?? "No message",
            Reporter = 76561198267927009,
            Time = DateTime.Now,
            Violator = ctx.CallerID == 0 ? 76561198267927009 : ctx.CallerID,
            ChatRecords = new string[]
            {
                "%SPEAKER%: chat 1",
                "%SPEAKER%: chat 2",
                "%SPEAKER%: chat 3",
                "[2x] %SPEAKER%: chat 4",
            }
        };
        Task.Run(async () =>
        {
            L.Log("Sending chat abuse report.");
            RequestResponse res = await Reporter.NetCalls.SendReportInvocation.Request(Reporter.NetCalls.ReceiveInvocationResponse, Data.NetClient!, report, false);
            L.Log(res.Responded && res.Parameters.Length > 1 && res.Parameters[1] is string url ? ("URL: " + url) : "No response.", ConsoleColor.DarkYellow);
        });
        ctx.Defer();
    }
    private void questdump(CommandInteraction ctx)
    {
        if (!UCWarfare.Config.EnableQuests) throw ctx.SendNotImplemented();

        ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);

        ctx.AssertRanByPlayer();

        QuestManager.PrintAllQuests(ctx.Caller);
    }
    private void completequest(CommandInteraction ctx)
    {
        if (!UCWarfare.Config.EnableQuests) throw ctx.SendNotImplemented();

        ctx.AssertPermissions(EAdminType.ADMIN);

        ctx.AssertRanByPlayer();

        if (ctx.TryGet(0, out EQuestType type))
        {
            for (int i = 0; i < QuestManager.RegisteredTrackers.Count; i++)
            {
                if (QuestManager.RegisteredTrackers[i].Player!.Steam64 == ctx.CallerID && 
                    QuestManager.RegisteredTrackers[i].QuestData?.QuestType == type)
                {
                    QuestManager.RegisteredTrackers[i].ManualComplete();
                    break;
                }
            }
        }
        else if (ctx.TryGet(0, out Guid key))
        {
            for (int i = 0; i < QuestManager.RegisteredTrackers.Count; i++)
            {
                if (QuestManager.RegisteredTrackers[i].Player!.Steam64 == ctx.CallerID &&
                    QuestManager.RegisteredTrackers[i].PresetKey == key)
                {
                    QuestManager.RegisteredTrackers[i].ManualComplete();
                    break;
                }
            }
        }
    }
    private void setsign(CommandInteraction ctx)
    {
        ctx.AssertRanByPlayer();

        ctx.AssertPermissions(EAdminType.STAFF);

        if (ctx.TryGetRange(0, out string text) && ctx.TryGetTarget(out BarricadeDrop drop) && drop.interactable is InteractableSign sign)
        {
            BarricadeManager.ServerSetSignText(sign, text.Replace("\\n", "\n"));
            if (RequestSigns.Loaded && RequestSigns.SignExists(sign, out RequestSign sign2))
            {
                sign2.SignText = text;
                RequestSigns.SaveSingleton();
            }
            if (StructureSaver.Loaded && StructureSaver.StructureExists(drop.instanceID, EStructType.BARRICADE, out Structures.Structure str))
            {
                str.state = Convert.ToBase64String(drop.GetServersideData().barricade.state);
                StructureSaver.SaveSingleton();
            }
            if (BarricadeManager.tryGetRegion(drop.model, out byte x, out byte y, out ushort plant, out _) && plant == ushort.MaxValue)
                F.InvokeSignUpdateForAll(sign, x, y, text);
        }
    }
#if DEBUG
    private void saveall(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);

        F.SaveProfilingData();
    }
#endif
    private void questtest(CommandInteraction ctx)
    {
        if (!UCWarfare.Config.EnableQuests) throw ctx.SendNotImplemented();

        ctx.AssertRanByPlayer();

        ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);

        foreach (BaseQuestData data in QuestManager.Quests)
        {
            try
            {
                QuestManager.CreateTracker(data, ctx.Caller);
            }
            catch (Exception ex)
            {
                L.LogError(ex);
            }
        }
    }

    private void gettime(CommandInteraction ctx)
    {
        ctx.AssertArgs(1, "/test gettime <timestr>");

        string t = ctx.GetRange(0)!;
        ctx.Reply("Time: " + F.ParseTimespan(t).ToString("g"));
    }
    private void getperms(CommandInteraction ctx)
    {
        ctx.Reply("Permission: " + ctx.Caller.GetPermissions());
    }
#if DEBUG
    private static InstanceSetter<InteractableVehicle, bool> SetEngineOn = F.GenerateInstanceSetter<InteractableVehicle, bool>("<isEngineOn>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
    private void drivetest(CommandInteraction ctx)
    {
        ctx.AssertRanByPlayer();
        VehicleAsset asset =
            (Assets.find(EAssetType.VEHICLE, ctx.TryGet(0, out ushort id) ? id : (ushort)38302) as VehicleAsset)!;
        ctx.Reply("Spawning " + asset.vehicleName);
        /*
        UCWarfare.I.StartCoroutine(_coroutine(ctx, asset, ctx.Caller.Position - new Vector3(0, 0, 100)));
        UCWarfare.I.StartCoroutine(_coroutine(ctx, asset, ctx.Caller.Position - new Vector3(0, 0, 90)));
        UCWarfare.I.StartCoroutine(_coroutine(ctx, asset, ctx.Caller.Position - new Vector3(0, 0, 80)));
        UCWarfare.I.StartCoroutine(_coroutine(ctx, asset, ctx.Caller.Position - new Vector3(0, 0, 70)));
        UCWarfare.I.StartCoroutine(_coroutine(ctx, asset, ctx.Caller.Position - new Vector3(0, 0, 60)));
        UCWarfare.I.StartCoroutine(_coroutine(ctx, asset, ctx.Caller.Position - new Vector3(0, 0, 50)));
        UCWarfare.I.StartCoroutine(_coroutine(ctx, asset, ctx.Caller.Position - new Vector3(0, 0, 40)));
        UCWarfare.I.StartCoroutine(_coroutine(ctx, asset, ctx.Caller.Position - new Vector3(0, 0, 30)));
        UCWarfare.I.StartCoroutine(_coroutine(ctx, asset, ctx.Caller.Position - new Vector3(0, 0, 20)));
        UCWarfare.I.StartCoroutine(_coroutine(ctx, asset, ctx.Caller.Position - new Vector3(0, 0, 10)));*/
        UCWarfare.I.StartCoroutine(_coroutine(ctx, asset, ctx.Caller.Position + ctx.Caller.Player.look.aim.forward with { y = 0 } * 5));/*
        UCWarfare.I.StartCoroutine(_coroutine(ctx, asset, ctx.Caller.Position + new Vector3(0, 0, 100)));
        UCWarfare.I.StartCoroutine(_coroutine(ctx, asset, ctx.Caller.Position + new Vector3(0, 0, 90)));
        UCWarfare.I.StartCoroutine(_coroutine(ctx, asset, ctx.Caller.Position + new Vector3(0, 0, 80)));
        UCWarfare.I.StartCoroutine(_coroutine(ctx, asset, ctx.Caller.Position + new Vector3(0, 0, 70)));
        UCWarfare.I.StartCoroutine(_coroutine(ctx, asset, ctx.Caller.Position + new Vector3(0, 0, 60)));
        UCWarfare.I.StartCoroutine(_coroutine(ctx, asset, ctx.Caller.Position + new Vector3(0, 0, 50)));
        UCWarfare.I.StartCoroutine(_coroutine(ctx, asset, ctx.Caller.Position + new Vector3(0, 0, 40)));
        UCWarfare.I.StartCoroutine(_coroutine(ctx, asset, ctx.Caller.Position + new Vector3(0, 0, 30)));
        UCWarfare.I.StartCoroutine(_coroutine(ctx, asset, ctx.Caller.Position + new Vector3(0, 0, 20)));
        UCWarfare.I.StartCoroutine(_coroutine(ctx, asset, ctx.Caller.Position + new Vector3(0, 0, 10)));*/
    }

    private IEnumerator _coroutine(CommandInteraction ctx, VehicleAsset asset, Vector3 pos)
    {
        Vector3 forward = ctx.Caller.Player.look.aim.forward;
        pos += (forward * 4);
        Quaternion angle = ctx.Caller.Player.transform.rotation;
        InteractableVehicle veh = VehicleManager.spawnLockedVehicleForPlayerV2(asset.id, pos, angle, ctx.Caller.Player);
        //uint sim = 1;
        SetEngineOn.Invoke(veh, true);
        RaycastHit[] results = new RaycastHit[16];
        yield return new WaitForSeconds(5f);
        //Array.ForEach(veh.transform.gameObject.GetComponentsInChildren<Collider>(), x => UnityEngine.Object.Destroy(x));
        while (veh != null)
        {
            const float TIME = 0.1f;
            yield return new WaitForSeconds(TIME);
            if (veh == null) yield break;
            //L.Log("\nSimulation #" + ++sim);
            Vector3 origin = veh.transform.position;
            Vector3 angle2 = veh.transform.forward;
            RaycastHit hit;
            int ct;
            if ((ct = Physics.RaycastNonAlloc(origin + new Vector3(0, 1, 0), angle2, results, 4f, RayMasks.BLOCK_COLLISION)) > 0)
            {
                hit = default;
                for (int j = ct - 1; j >= 0; --j)
                {
                    hit = results[j];
                    if (hit.transform == null || hit.transform == veh.transform || hit.transform.name == asset.id.ToString()) continue;
                    break;
                }

                //L.Log("Hits: " + ct + ", selected: " + (hit.transform == null ? "null" : hit.transform.name));
                if (hit.transform != null && hit.transform != veh.transform && hit.transform.name != asset.id.ToString())
                {
                    //L.Log("Normal: " + hit.normal.ToString("F3"));
                    if (hit.normal.y < 0.1)
                    {
                        // hit a wall
                        //L.Log("Hitwal: " + (hit.transform == null ? "null" : hit.transform.name));
                        yield return new WaitForSeconds(3f);
                        continue;
                    }
                    angle2.y += 1 - hit.normal.y;
                }
            }
            Rigidbody body = veh.GetComponent<Rigidbody>();
            //L.Log("Origin: " + origin.ToString("F3"));
            //L.Log("Angle : " + angle2.ToString("F3"));
            Vector3 vel = angle2 * 30f;
            if (veh.asset.engine is EEngine.PLANE or EEngine.HELICOPTER)
            {
                vel += new Vector3(0,
                    Physics.Raycast(body.position,
                        (Vector3.down + (body.transform.forward with { y = 0 })).normalized, out hit, 1024f,
                        RayMasks.BLOCK_COLLISION & ~RayMasks.VEHICLE) &&
                    hit.distance < 200
                        ? 10f
                        : -5f, 0f);
                body.useGravity = true;
            }
            body.velocity = vel;
            //veh.simulate(sim, 100, false, origin += angle2, veh.transform.rotation, 10000f, 10000f, 0, TIME); 
            //veh.simulate(sim, -1, 0, 1, 0, Mathf.Clamp(Level.HEIGHT / origin.y, -1, 1), false, false, TIME);
            veh.updatePhysics();
            veh.updateVehicle();
            //for (int j = 0; j < veh.tires.Length; ++j)
            //    veh.tires[j].simulate(0, 1, false, TIME);*/
        }
    }
#endif
    private void resetdebug(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);
        if (UCWarfare.I.Debugger != null)
        {
            UCWarfare.I.Debugger.Reset();
            ctx.Reply("Reset debugger");
        }
        else throw ctx.Reply("Debugger is not active.");
    }

    private void translationtest(CommandInteraction ctx)
    {
        ctx.AssertRanByPlayer();

        ctx.Caller.SendChat(T.KitAlreadyHasAccess, ctx.Caller, ctx.Caller.Kit!);
    }
}