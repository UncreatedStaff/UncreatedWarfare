using Rocket.API;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Players;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Flags.Invasion;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.ReportSystem;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare.Commands;

#pragma warning disable IDE1006 // Naming Styles
internal class _DebugCommand : IRocketCommand
#pragma warning restore IDE1006 // Naming Styles
{
    public static int currentstep = 0;
    private readonly List<string> _aliases = new List<string>(0);
    public AllowedCaller AllowedCaller => AllowedCaller.Both;
    public string Name => "test";
    public string Help => "Collection of test commands.";
    public string Syntax => "/test <mode>";
    public List<string> Aliases => _aliases;
    private readonly List<string> _permissions = new List<string>(1) { "uc.test" };
		public List<string> Permissions => _permissions;
    private readonly Type type = typeof(_DebugCommand);
    public void Execute(IRocketPlayer caller, string[] command)
    {
        CommandContext ctx = new CommandContext(caller, command);
        if (ctx.TryGet(0, out string operation))
        {
            try
            {
                MethodInfo info = type.GetMethod(operation, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (info == null)
                {
                    ctx.Reply("test_no_method", operation);
                }
                else
                {
                    try
                    {
#if DEBUG
                        using IDisposable profiler = ProfilingUtils.StartTracking(info.Name + " Debug Command");
#endif
                        info.Invoke(this, new object[1] { ctx });
                    }
                    catch (Exception ex)
                    {
                        L.LogError(ex.InnerException ?? ex);
                        ctx.Reply("test_error_executing", info.Name, (ex.InnerException ?? ex).GetType().Name);
                    }
                }
            }
            catch (AmbiguousMatchException)
            {
                ctx.Reply("test_multiple_matches", operation);
            }
        }
        else
        {
            ctx.SendCorrectUsage("/test <operation> [parameters...]");
        }
    }
#pragma warning disable IDE1006
#pragma warning disable IDE0060
#pragma warning disable IDE0051
    private const string GIVE_XP_SYNTAX = "/test givexp <player> <amount> [team - required if offline]";
    private void givexp(CommandContext ctx)
    {
        if (!ctx.HasArgs(3))
        {
            ctx.SendCorrectUsage(GIVE_XP_SYNTAX);
            return;
        }
        if (ctx.TryGet(2, out int amount))
        {
            if (ctx.TryGet(1, out ulong player, out UCPlayer? onlinePlayer))
            {
                ctx.TryGet(3, out ulong team);
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
                        }
                        else
                        {
                            ctx.SendCorrectUsage(GIVE_XP_SYNTAX);
                        }
                    }
                    else
                    {
                        ctx.Reply("test_givexp_player_not_found", ctx.Get(1)!);
                    }
                }
                else
                {
                    if (team < 1 || team > 2)
                        team = onlinePlayer.GetTeam();
                    Points.AwardXP(onlinePlayer, amount, ctx.IsConsole ? Translation.Translate("xp_from_operator", player) :
                        Translation.Translate("xp_from_player", player, F.GetPlayerOriginalNames(ctx.CallerID).CharacterName.ToUpper()));
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
            ctx.Reply("test_givexp_invalid_amount", ctx.Get(2)!);
        }
    }
    private const string GIVE_CREDITS_SYNTAX = "/test givecredits <player> <amount> [team - required if offline]";
    private void givecredits(CommandContext ctx)
    {
        if (!ctx.HasArgs(3))
        {
            ctx.SendCorrectUsage(GIVE_CREDITS_SYNTAX);
            return;
        }
        if (ctx.TryGet(2, out int amount))
        {
            if (ctx.TryGet(1, out ulong player, out UCPlayer? onlinePlayer))
            {
                ctx.TryGet(3, out ulong team);
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
                        }
                        else
                        {
                            ctx.SendCorrectUsage(GIVE_CREDITS_SYNTAX);
                        }
                    }
                    else
                    {
                        ctx.Reply("test_givecredits_player_not_found", ctx.Get(1)!);
                    }
                }
                else
                {
                    if (team < 1 || team > 2)
                        team = onlinePlayer.GetTeam();
                    Points.AwardCredits(onlinePlayer, amount, ctx.IsConsole ? Translation.Translate("credits_from_operator", player) :
                        Translation.Translate("credits_from_player", player, F.GetPlayerOriginalNames(ctx.CallerID).CharacterName.ToUpper()));
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
            ctx.Reply("test_givecredits_invalid_amount", ctx.Get(2)!);
        }
    }
    private void quickcap(CommandContext ctx)
    {
        if (ctx.IsConsole || ctx.Caller is null)
        {
            ctx.SendPlayerOnlyError();
            return;
        }
        if (Data.Is(out IFlagRotation fg))
        {
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
        else ctx.Reply("gamemode_not_flag_gamemode", Data.Gamemode == null ? "null" : Data.Gamemode.Name);
    }
    private void quickwin(CommandContext ctx)
    {
        ulong team;
        if (ctx.TryGet(1, out ulong id))
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
    private void savemanyzones(CommandContext ctx)
    {
        if (Data.Is(out IFlagRotation fg))
        {
            if (!ctx.TryGet(1, out uint times))
                times = 1U;
            List<Zone> zones = new List<Zone>();
            if (!Directory.Exists(Data.FlagStorage + "ZoneExport\\"))
                Directory.CreateDirectory(Data.FlagStorage + "ZoneExport\\");
            for (int i = 0; i < times; i++)
            {
                zones.Clear();
                ReloadCommand.ReloadFlags();
                fg.Rotation.ForEach(x => zones.Add(x.ZoneData));
                ZoneDrawing.CreateFlagTestAreaOverlay(fg, ctx.Caller?.Player, zones, true, true, false, false, true, Data.FlagStorage + @"ZoneExport\zonearea_" + i.ToString(Data.Locale));
                L.Log("Done with " + (i + 1).ToString(Data.Locale) + '/' + times.ToString(Data.Locale));
            }
        }
        else
        {
            ctx.Reply("gamemode_not_flag_gamemode", Data.Gamemode == null ? "null" : Data.Gamemode.Name);
        }
    }
    private void savemanygraphs(CommandContext ctx)
    {
        if (Data.Is(out IFlagRotation fg))
        {
            if (!ctx.TryGet(1, out uint times))
                times = 1U;
            List<Zone> zones = new List<Zone>();
            if (!Directory.Exists(Data.FlagStorage + "GraphExport\\"))
                Directory.CreateDirectory(Data.FlagStorage + "GraphExport\\");
            for (int i = 0; i < times; i++)
            {
                zones.Clear();
                ReloadCommand.ReloadFlags();
                fg.Rotation.ForEach(x => zones.Add(x.ZoneData));
                ZoneDrawing.DrawZoneMap(fg, Data.FlagStorage + @"GraphExport\zonegraph_" + i.ToString(Data.Locale));
                L.Log("Done with " + (i + 1).ToString(Data.Locale) + '/' + times.ToString(Data.Locale));
            }
        }
        else
        {
            ctx.Reply("gamemode_not_flag_gamemode", Data.Gamemode == null ? "null" : Data.Gamemode.Name);
        }
    }
    private void zone(CommandContext ctx)
    {
        if (ctx.IsConsole || ctx.Caller is null)
        {
            ctx.SendPlayerOnlyError();
            return;
        }
        if (Data.Is(out IFlagRotation fg))
        {
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
        else ctx.Reply("gamemode_not_flag_gamemode", Data.Gamemode == null ? "null" : Data.Gamemode.Name);
    }
    private void sign(CommandContext ctx)
    {
        if (ctx.IsConsole || ctx.Caller is null)
        {
            ctx.SendPlayerOnlyError();
            return;
        }
        InteractableSign? sign = UCBarricadeManager.GetInteractableFromLook<InteractableSign>(ctx.Caller.Player.look);
        if (sign == null) ctx.Reply("test_sign_no_sign");
        else
        {
            ctx.Reply("test_sign_success", sign.text);
            L.Log(Translation.Translate("test_sign_success", 0, out _, sign.text), ConsoleColor.Green);
        }
    }
    private void time(CommandContext ctx)
    {
        UCWarfare.I.CoroutineTiming = !UCWarfare.I.CoroutineTiming;
        ctx.Reply("test_time_enabled_console");
    }
    // test zones: test zonearea all true false false true false
    private void zonearea(CommandContext ctx)
    {
        if (Data.Is(out IFlagRotation fg))
        {
            bool all = false;
            bool extra = false;
            bool path = true;
            bool range = false;
            bool drawIn = false;
            bool drawAngles = false;
            if (ctx.HasArgs(6))
            {
                if (ctx.MatchParameter(1, "all")) all = true;
                else if (!ctx.MatchParameter(1, "active")) ctx.Reply("test_zonearea_syntax");
                if (ctx.MatchParameter(2, "true")) extra = true;
                else if (!ctx.MatchParameter(2, "false")) ctx.Reply("test_zonearea_syntax");
                if (ctx.MatchParameter(3, "false")) path = false;
                else if (!ctx.MatchParameter(3, "true")) ctx.Reply("test_zonearea_syntax");
                if (ctx.MatchParameter(4, "true")) range = true;
                else if (!ctx.MatchParameter(4, "false")) ctx.Reply("test_zonearea_syntax");
                if (ctx.MatchParameter(5, "true")) drawIn = true;
                else if (!ctx.MatchParameter(5, "false")) ctx.Reply("test_zonearea_syntax");
                if (ctx.MatchParameter(6, "true")) drawAngles = true;
                else if (!ctx.MatchParameter(6, "false")) ctx.Reply("test_zonearea_syntax");
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
        else ctx.Reply("gamemode_not_flag_gamemode", Data.Gamemode == null ? "null" : Data.Gamemode.Name);
    }
    private void drawzone(CommandContext ctx)
    {
        if (ctx.IsConsole || ctx.Caller is null)
        {
            ctx.SendPlayerOnlyError();
            return;
        }
        Zone zone;
        string zoneName;
        string zoneColor;
        if (Data.Is(out IFlagRotation fg))
        {
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
            ZoneDrawing.CreateFlagTestAreaOverlay(fg, ctx.Caller?.Player, zones, false, true, false, true, true, Data.FlagStorage + "zonerange_" + zoneName);
        }
        else ctx.Reply("gamemode_not_flag_gamemode", Data.Gamemode == null ? "null" : Data.Gamemode.Name);
    }
    private void drawgraph(CommandContext ctx)
    {
        if (Data.Gamemode is FlagGamemode fg)
        {
            ctx.LogAction(EActionLogType.BUILD_ZONE_MAP, "DRAWGRAPH");
            ZoneDrawing.DrawZoneMap(fg, null);
        }
        else ctx.Reply("gamemode_not_flag_gamemode", Data.Gamemode == null ? "null" : Data.Gamemode.Name);
    }
    private void rotation(CommandContext ctx)
    {
        if (Data.Gamemode is FlagGamemode fg)
            fg.PrintFlagRotation();
    }
    private const byte DOWN_DAMAGE = 55;
    private void down(CommandContext ctx)
    {
        if (ctx.IsConsole || ctx.Caller is null)
        {
            ctx.SendPlayerOnlyError();
            return;
        }
        DamageTool.damage(ctx.Caller.Player, EDeathCause.KILL, ELimb.SPINE, ctx.Caller.CSteamID, Vector3.down, DOWN_DAMAGE, 1, out _, false, false);
        DamageTool.damage(ctx.Caller.Player, EDeathCause.KILL, ELimb.SPINE, ctx.Caller.CSteamID, Vector3.down, DOWN_DAMAGE, 1, out _, false, false);
        ctx.Reply("test_down_success", (DOWN_DAMAGE * 2).ToString(Data.Locale));
    }
    private void layer(CommandContext ctx)
    {
        if (ctx.IsConsole || ctx.Caller is null)
        {
            ctx.SendPlayerOnlyError();
            return;
        }
        L.Log(F.GetLayer(ctx.Caller.Player.look.aim.position, ctx.Caller.Player.look.aim.forward, RayMasks.BLOCK_COLLISION), ConsoleColor.DarkCyan); // so as to not hit player
    }
    private void clearui(CommandContext ctx)
    {
        if (ctx.IsConsole || ctx.Caller is null)
        {
            ctx.SendPlayerOnlyError();
            return;
        }
        if (ctx.Caller.HasUIHidden)
        {
            UCWarfare.I.UpdateLangs(ctx.Caller.SteamPlayer);
        }
        else
        {
            Data.SendEffectClearAll.InvokeAndLoopback(ENetReliability.Reliable, new ITransportConnection[] { ctx.Caller.Connection });
        }
        ctx.Caller.HasUIHidden = !ctx.Caller.HasUIHidden;
        
    }
    private void game(CommandContext ctx)
    {
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
    private void playersave(CommandContext ctx)
    {
        if (!ctx.HasArgs(4))
        {
            ctx.SendCorrectUsage(PLAYER_SAVE_USAGE);
            return;
        }
        if (ctx.TryGet(1, out ulong player, out UCPlayer? onlinePlayer))
        {
            if (PlayerManager.HasSave(player, out PlayerSave save))
            {
                if (ctx.TryGet(3, out string value) && ctx.TryGet(2, out string property))
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
    private void gamemode(CommandContext ctx)
    {
        if (!ctx.HasArgs(2))
        {
            ctx.SendCorrectUsage(GAMEMODE_USAGE);
            return;
        }
        if (ctx.TryGet(1, out string gamemodeName))
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
    private void trackstats(CommandContext ctx)
    {
        Data.TrackStats = !Data.TrackStats;
        if (Data.TrackStats)
            ctx.Reply("test_trackstats_enabled");
        else
            ctx.Reply("test_trackstats_disabled");
    }
    private void destroyblocker(CommandContext ctx)
    {
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
    private void skipstaging(CommandContext ctx)
    {
        if (Data.Is(out IStagingPhase gm))
        {
            ctx.Reply("test_gamemode_skipped_staging");
            gm.SkipStagingPhase();
        }
        else ctx.SendGamemodeError();
    }
    private void resetlobby(CommandContext ctx)
    {
        if (Data.Is(out ITeams t) && t.UseJoinUI)
        {
            if (!ctx.HasArgs(1))
            {
                ctx.SendCorrectUsage("/resetlobby <player>");
                return;
            }
            if (ctx.TryGet(1, out _, out UCPlayer? player) && player is not null)
            {
                t.JoinManager.OnPlayerDisconnected(player);
                t.JoinManager.CloseUI(player);
                t.JoinManager.OnPlayerConnected(player, true);
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
    private void clearcooldowns(CommandContext ctx)
    {
        if (ctx.IsConsole || ctx.Caller is null)
        {
            ctx.SendPlayerOnlyError();
            return;
        }
        if (!ctx.TryGet(1, out _, out UCPlayer? pl) || pl is null)
            pl = ctx.Caller;
        CooldownManager.RemoveCooldown(pl);
    }
    private void instid(CommandContext ctx)
    {
        if (ctx.IsConsole || ctx.Caller is null)
        {
            ctx.SendConsoleOnlyError();
            return;
        }
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
    private void fakereport(CommandContext ctx)
    {
        Report report = new ChatAbuseReport()
        {
            Message = string.Join(" ", ctx.Parameters),
            Reporter = 76561198267927009,
            Time = DateTime.Now,
            Violator = 76561198267927009,
            ChatRecords = new string[]
            {
                "%SPEAKER%: chat 1",
                "%SPEAKER%: chat 2",
                "%SPEAKER%: chat 3",
                "[2x] %SPEAKER%: chat 4",
            }
        };
        Reporter.NetCalls.SendReportInvocation.NetInvoke(report, false);
        L.Log("Sent chat abuse report.");
    }
    private void questdump(CommandContext ctx)
    {
        QuestManager.PrintAllQuests(ctx.Caller);
    }
    private void completequest(CommandContext ctx)
    {
        if (ctx.IsConsole || ctx.Caller is null)
        {
            ctx.SendPlayerOnlyError();
            return;
        }
        if (ctx.TryGet(1, out EQuestType type))
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
        else if (ctx.TryGet(1, out Guid key))
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

    private void setsign(CommandContext ctx)
    {
        if (ctx.IsConsole || ctx.Caller is null)
        {
            ctx.SendPlayerOnlyError();
            return;
        }
        if (ctx.TryGetRange(1, out string text) && ctx.TryGetTarget(out BarricadeDrop drop) && drop.interactable is InteractableSign sign)
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
    private void saveall(CommandContext ctx)
    {
        F.SaveProfilingData();
    }
    private void questtest(CommandContext ctx)
    {
        if (ctx.IsConsole || ctx.Caller is null)
        {
            ctx.SendPlayerOnlyError();
            return;
        }
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
}