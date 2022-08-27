using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
using Uncreated.Warfare.Squads.Commander;
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
                throw ctx.Reply(T.DebugMultipleMatches, operation);
            }
            if (info == null)
                throw ctx.Reply(T.DebugNoMethod, operation);
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
                if (ex.InnerException is BaseCommandInteraction b)
                    throw b;
                L.LogError(ex.InnerException ?? ex);
                throw ctx.Reply(T.DebugErrorExecuting, info.Name, (ex.InnerException ?? ex).GetType().Name);
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
                                ctx.ReplyString($"Given <#fff>{amount}</color> <#ff9b01>XP</color> to {(ctx.IsConsole ? name.PlayerName : name.CharacterName)}.");
                            });
                            ctx.Defer();
                        }
                        else
                            ctx.SendCorrectUsage(GIVE_XP_SYNTAX);
                    }
                    else
                        ctx.Reply(T.PlayerNotFound);
                }
                else
                {
                    if (team < 1 || team > 2)
                        team = onlinePlayer.GetTeam();
                    Points.AwardXP(onlinePlayer, amount, ctx.IsConsole ? T.XPToastFromOperator : T.XPToastFromPlayer);
                    FPlayerName names = F.GetPlayerOriginalNames(onlinePlayer);
                    ctx.ReplyString($"Given <#fff>{amount}</color> <#ff9b01>XP</color> to {(ctx.IsConsole ? names.PlayerName : names.CharacterName)}.");
                }
            }
            else
                ctx.SendCorrectUsage(GIVE_XP_SYNTAX);
        }
        else
            ctx.ReplyString($"Couldn't parse {ctx.Get(1)!} as a number.");
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
                                ctx.ReplyString($"Given <#{UCWarfare.GetColorHex("credits")}>C</color> <#fff>{amount}</color> to {(ctx.IsConsole ? name.PlayerName : name.CharacterName)}.");
                            });
                            ctx.Defer();
                        }
                        else
                            ctx.SendCorrectUsage(GIVE_CREDITS_SYNTAX);
                    }
                    else
                        ctx.Reply(T.PlayerNotFound);
                }
                else
                {
                    if (team < 1 || team > 2)
                        team = onlinePlayer.GetTeam();
                    Points.AwardCredits(onlinePlayer, amount, ctx.IsConsole ? T.XPToastFromOperator : T.XPToastFromPlayer);
                    FPlayerName names = F.GetPlayerOriginalNames(onlinePlayer);
                    ctx.ReplyString($"Given <#{UCWarfare.GetColorHex("credits")}>C</color> <#fff>{amount}</color> to {(ctx.IsConsole ? names.PlayerName : names.CharacterName)}.");
                }
            }
            else
                ctx.SendCorrectUsage(GIVE_CREDITS_SYNTAX);
        }
        else
            ctx.ReplyString($"Couldn't parse {ctx.Get(1)!} as a number.");
    }
    private void quickcap(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.ADMIN);

        ctx.AssertRanByPlayer();
        ctx.AssertGamemode(out IFlagRotation fg);

        Flag flag = fg.Rotation.FirstOrDefault(f => f.PlayersOnFlag.Contains(ctx.Caller));
        if (flag == default)
        {
            ctx.Reply(T.ZoneNoResultsLocation);
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
        else ctx.SendGamemodeError();
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
            ctx.Reply(T.NotOnCaptureTeam);
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
        string txt = $"Position: <#ff9999>({pos.x.ToString("0.##", Data.Locale)}, {pos.y.ToString("0.##", Data.Locale)}, {pos.z.ToString("0.##", Data.Locale)})</color>. " +
                     $"Yaw: <#ff9999>{ctx.Caller.Player.transform.eulerAngles.y.ToString("0.##", Data.Locale)}</color>.";
        if (flag is null)
            ctx.ReplyString(txt);
        else
            ctx.ReplyString(txt + " Current Flag: <#{flag.TeamSpecificHexColor}>{flag.Name}</color>");
    }
    private void sign(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.STAFF);

        ctx.AssertRanByPlayer();

        InteractableSign? sign = UCBarricadeManager.GetInteractableFromLook<InteractableSign>(ctx.Caller.Player.look);
        if (sign == null) ctx.ReplyString($"You're not looking at a sign");
        else
        {
            if (!ctx.IsConsole)
                ctx.ReplyString("Sign text: \"" + sign.text + "\".");
            else
                ctx.Defer();
            L.Log("Sign Text: \n" + sign.text + "\nEND", ConsoleColor.Green);
        }
    }
    private void time(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);

        UCWarfare.I.CoroutineTiming = !UCWarfare.I.CoroutineTiming;
        ctx.ReplyString("Coroutine timing state: " + UCWarfare.I.CoroutineTiming.ToString());
    }
    // test zones: test zonearea all true false false true false
    private const string ZONEAREA_SYNTAX = "Syntax: /test zonearea [<selection: active|all> <extra-zones> <path> <range> <fill> <drawAngles>]";
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
            if (ctx.MatchParameter(0, "active", "false")) all = false;
            else if (!ctx.MatchParameter(0, "all", "true")) ctx.ReplyString(ZONEAREA_SYNTAX);
            if (ctx.MatchParameter(1, "false")) extra = false;
            else if (!ctx.MatchParameter(1, "true")) ctx.ReplyString(ZONEAREA_SYNTAX);
            if (ctx.MatchParameter(2, "false")) path = false;
            else if (!ctx.MatchParameter(2, "true")) ctx.ReplyString(ZONEAREA_SYNTAX);
            if (ctx.MatchParameter(3, "true")) range = true;
            else if (!ctx.MatchParameter(3, "false")) ctx.ReplyString(ZONEAREA_SYNTAX);
            if (ctx.MatchParameter(4, "false")) drawIn = false;
            else if (!ctx.MatchParameter(4, "true")) ctx.ReplyString(ZONEAREA_SYNTAX);
            if (ctx.MatchParameter(5, "true")) drawAngles = true;
            else if (!ctx.MatchParameter(5, "false")) ctx.ReplyString(ZONEAREA_SYNTAX);
        }
        else if (ctx.ArgumentCount != 1)
        {
            ctx.ReplyString(ZONEAREA_SYNTAX);
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
        ctx.ReplyString("Picture has to generate, wait a few seconds then check " + Path.Combine(Data.Paths.FlagStorage, "zonearea.png") + ".");
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
            ctx.Reply(T.ZoneNoResultsLocation);
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
        ctx.AssertGamemode(out IRevives revive);

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
        bool shouldAllow = true;
        DamagePlayerParameters p = new DamagePlayerParameters(player.Player)
        {
            cause = EDeathCause.KILL,
            applyGlobalArmorMultiplier = false,
            respectArmor = false,
            damage = 101,
            limb = ELimb.SPINE,
            direction = Vector3.down,
            killer = player == ctx.Caller ? Steamworks.CSteamID.Nil : player.CSteamID,
            times = 1f
        };
        revive.ReviveManager.InjurePlayer(ref shouldAllow, ref p, player == ctx.Caller ? null : player.SteamPlayer);
        ctx.ReplyString($"Injured {(player == ctx.Caller ? "you" : player.CharacterName)}.");
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
        if (Data.Gamemode is not null)
            ctx.ReplyString(Data.Gamemode.DumpState());
        else
            ctx.SendGamemodeError();
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
                    ESetFieldResult result = PlayerManager.SetProperty(save, ref property, value);
                    switch (result)
                    {
                        case ESetFieldResult.SUCCESS:
                            FPlayerName names = F.GetPlayerOriginalNames(player);
                            ctx.ReplyString($"Set {property} in {(ctx.IsConsole ? names.PlayerName : names.CharacterName)}'s playersave to {value}.");
                            break;
                        case ESetFieldResult.FIELD_NOT_FOUND:
                            ctx.ReplyString($"Couldn't find a field by the name {property.ToProperCase()} in PlayerSave.");
                            break;
                        case ESetFieldResult.FIELD_PROTECTED:
                            ctx.ReplyString($"Unable to set {property}, it's missing the JsonSettable attribute.");
                            break;
                        case ESetFieldResult.FIELD_NOT_SERIALIZABLE:
                        case ESetFieldResult.INVALID_INPUT:
                            ctx.ReplyString($"Couldn't parse {value} as a valid value for {property}.");
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
                ctx.ReplyString($"This player hasn't joined the server yet.");
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
                if (Data.Is(out IStagingPhase gm) && gm.State == EState.STAGING)
                {
                    ctx.ReplyString($"Skipped staging phase.");
                    gm.SkipStagingPhase();
                }
                if (newGamemode == Data.Gamemode.GetType())
                    Data.Singletons.ReloadSingleton(Gamemode.GAMEMODE_RELOAD_KEY);
                else if (Gamemode.TryLoadGamemode(newGamemode))
                {
                    ctx.ReplyString($"Successfully loaded {newGamemode.Name}.");
                }
                else
                {
                    ctx.ReplyString($"Failed to load {newGamemode.Name}.");
                }
            }
            else
                ctx.ReplyString($"Gamemode not found: {gamemodeName}.");
        }
        else
            ctx.SendCorrectUsage(GAMEMODE_USAGE);
    }
    private void trackstats(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);

        Data.TrackStats = !Data.TrackStats;
        if (Data.TrackStats)
            ctx.ReplyString($"Re-enabled stat tracking.");
        else
            ctx.ReplyString("Disabled stat tracking.");
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
            ctx.ReplyString("Couldn't find any zone blockers.");
        else
            ctx.ReplyString($"Destroyed {ct} zone blocked{ct.S()}");
    }
    private void skipstaging(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.MODERATOR);

        ctx.AssertGamemode(out IStagingPhase gm);

        if (gm.State == EState.STAGING)
        {
            gm.SkipStagingPhase();
            ctx.ReplyString("Skipped staging phase.");
        }
        else
        {
            ctx.ReplyString($"Staging phase is not active.");
        }
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
                ctx.ReplyString($"Reset lobby for {(ctx.IsConsole ? name.PlayerName : name.CharacterName)}.");
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
        if (t != null)
        {
            BarricadeDrop? bd = BarricadeManager.FindBarricadeByRootTransform(t);
            if (bd != null)
            {
                ctx.ReplyString($"Barricade {bd.asset.itemName}: #{bd.instanceID.ToString(Data.Locale)}");
                return;
            }
            StructureDrop? dp = StructureManager.FindStructureByRootTransform(t);
            if (dp != null)
            {
                ctx.ReplyString($"Structure {dp.asset.itemName}: #{dp.instanceID.ToString(Data.Locale)}");
                return;
            }
            for (int i = 0; i < VehicleManager.vehicles.Count; i++)
            {
                if (VehicleManager.vehicles[i].transform == t)
                {
                    ctx.ReplyString($"Vehicle {VehicleManager.vehicles[i].asset.vehicleName}: #{VehicleManager.vehicles[i].instanceID.ToString(Data.Locale)}");
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
                            ctx.ReplyString($"Vehicle {VehicleManager.vehicles[i].asset.vehicleName}: #{VehicleManager.vehicles[i].instanceID.ToString(Data.Locale)}");
                            return;
                        }
                    }
                }
            }
        }
        ctx.ReplyString($"You must be looking at a barricade, structure, vehicle, or object.");
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
        ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);

        ctx.AssertRanByPlayer();

        QuestManager.PrintAllQuests(ctx.Caller);
    }
    private void completequest(CommandInteraction ctx)
    {
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
            Signs.BroadcastSignUpdate(drop);
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
        ctx.ReplyString("Time: " + F.ParseTimespan(t).ToString("g"));
    }
    private void getperms(CommandInteraction ctx)
    {
        ctx.ReplyString("Permission: " + ctx.Caller.GetPermissions());
    }
#if DEBUG
    private static readonly InstanceSetter<InteractableVehicle, bool> SetEngineOn = F.GenerateInstanceSetter<InteractableVehicle, bool>("<isEngineOn>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
    private void drivetest(CommandInteraction ctx)
    {
        ctx.AssertRanByPlayer();
        VehicleAsset asset =
            (Assets.find(EAssetType.VEHICLE, ctx.TryGet(0, out ushort id) ? id : (ushort)38302) as VehicleAsset)!;
        ctx.ReplyString("Spawning " + asset.vehicleName);
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
            ctx.ReplyString("Reset debugger");
        }
        else throw ctx.ReplyString("Debugger is not active.");
    }

    private void translationtest(CommandInteraction ctx)
    {
        ctx.AssertRanByPlayer();
        ctx.Caller.SendChat(T.KitAlreadyHasAccess, ctx.Caller, ctx.Caller.Kit!);
    }
    private void quest(CommandInteraction ctx)
    {
        ctx.AssertRanByPlayer();
        ctx.AssertPermissions(EAdminType.ADMIN);
        if (ctx.MatchParameter(0, "add"))
        {
            if (ctx.TryGet(1, out QuestAsset asset, out _, true, -1, false))
            {
                ctx.Caller.Player.quests.sendAddQuest(asset.id);
                ctx.ReplyString("Added quest " + asset.questName + " <#ddd>(" + asset.id + ", " + asset.GUID.ToString("N") + ")</color>.");
            }
            else ctx.ReplyString("Quest not found.");
        }
        else if (ctx.MatchParameter(0, "track"))
        {
            if (ctx.TryGet(1, out QuestAsset asset, out _, true, -1, false))
            {
                ctx.Caller.Player.quests.sendTrackQuest(asset.id);
                ctx.ReplyString("Tracked quest " + asset.questName + " <#ddd>(" + asset.id + ", " + asset.GUID.ToString("N") + ")</color>.");
            }
            else ctx.ReplyString("Quest not found.");
        }
        else if (ctx.MatchParameter(0, "remove"))
        {
            if (ctx.TryGet(1, out QuestAsset asset, out _, true, -1, false))
            {
                ctx.Caller.Player.quests.sendRemoveQuest(asset.id);
                ctx.ReplyString("Removed quest " + asset.questName + " <#ddd>(" + asset.id + ", " + asset.GUID.ToString("N") + ")</color>.");
            }
            else ctx.ReplyString("Quest not found.");
        }
        else ctx.ReplyString("Syntax: /test quest <add|track|remove> <id>");
    }

    private void flag(CommandInteraction ctx)
    {
        ctx.AssertRanByPlayer();
        ctx.AssertPermissions(EAdminType.ADMIN);

        if (ctx.MatchParameter(0, "set"))
        {
            if (ctx.TryGet(2, out short value) && ctx.TryGet(1, out ushort flag))
            {
                bool f = ctx.Caller.Player.quests.getFlag(flag, out short old);
                ctx.Caller.Player.quests.sendSetFlag(flag, value);
                ctx.ReplyString("Set quest flag " + flag + " to " + value + " <#ddd>(from " +
                                (f ? old.ToString() : "<b>not set</b>") + ")</color>.");
                return;
            }
        }
        else if (ctx.MatchParameter(0, "get"))
        {
            if (ctx.TryGet(1, out ushort flag))
            {
                bool f = ctx.Caller.Player.quests.getFlag(flag, out short val);
                ctx.ReplyString("Quest flag " + flag + " is " + (f ? val.ToString() : "<b>not set</b>") + ")</color>.");
                return;
            }
        }
        else if (ctx.MatchParameter(0, "remove", "delete"))
        {
            if (ctx.TryGet(1, out ushort flag))
            {
                bool f = ctx.Caller.Player.quests.getFlag(flag, out short old);
                if (f)
                {
                    ctx.Caller.Player.quests.sendRemoveFlag(flag);
                    ctx.ReplyString("Quest flag " + flag + " was removed" + " <#ddd>(from " + old.ToString() + ")</color>.");
                }
                else ctx.ReplyString("Quest flag " + flag + " is not set.");
                return;
            }
        }
        ctx.ReplyString("Syntax: /test flag <set|get|remove> <flag> [value]");
    }

    private void findasset(CommandInteraction ctx)
    {
        if (ctx.TryGet(0, out Guid guid))
        {
            ctx.ReplyString("Asset: " + (Assets.find(guid)?.FriendlyName ?? "null"));
        }
        else if (ctx.TryGet(0, out ushort us) && ctx.TryGet(1, out EAssetType type))
        {
            ctx.ReplyString("Asset: " + (Assets.find(type, us)?.FriendlyName ?? "null"));
        }
        else
        {
            ctx.ReplyString("Please use a <GUID> or <uhort, type>");
        }
    }
    private void traits(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);

        ctx.AssertGamemode<ITraits>();

        ctx.AssertRanByPlayer();

        L.Log("Traits: ");
        using (IDisposable d = L.IndentLog(1))
        {
            for (int i = 0; i < ctx.Caller.ActiveTraits.Count; ++i)
            {
                Traits.Trait t = ctx.Caller.ActiveTraits[i];
                L.Log(t.Data.TypeName + " (" + (Time.realtimeSinceStartup - t.StartTime) + " seconds active)");
            }
        }

        L.Log("Buff UI:");
        L.Log("[ " + (ctx.Caller.ActiveBuffs[0]?.GetType().Name ?? "null") +
              ", " + (ctx.Caller.ActiveBuffs[1]?.GetType().Name ?? "null") +
              ", " + (ctx.Caller.ActiveBuffs[2]?.GetType().Name ?? "null") +
              ", " + (ctx.Caller.ActiveBuffs[3]?.GetType().Name ?? "null") +
              ", " + (ctx.Caller.ActiveBuffs[4]?.GetType().Name ?? "null") +
              ", " + (ctx.Caller.ActiveBuffs[5]?.GetType().Name ?? "null") + " ]");
    }

    private void sendui(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);

        ctx.AssertRanByPlayer();

        if (ctx.TryGet(0, out ushort id))
        {
            if (ctx.TryGet(1, out short key))
            {
                EffectManager.sendUIEffect(id, key, ctx.Caller.Connection, true);
            }
            else
            {
                EffectManager.sendUIEffect(id, unchecked((short)id), ctx.Caller.Connection, true);
            }
        }
        else
        {
            ctx.ReplyString("Syntax: /test sendui <id> [key]");
        }
    }

    private void advancedelays(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);

        if (ctx.TryGet(0, out float seconds))
        {
            Data.Gamemode.AdvanceDelays(seconds);
            ctx.ReplyString("Advanced delays by " + seconds.ToString("0.##", Data.Locale) + " seconds.");
        }
        else
            ctx.SendCorrectUsage("/test advancedelays <seconds>.");
    }

    private void listcooldowns(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);
        ctx.AssertRanByPlayer();
        L.Log("Cooldowns for " + ctx.Caller.Name.PlayerName + ":");
        using IDisposable d = L.IndentLog(1);
        foreach (Cooldown cooldown in CooldownManager.Singleton.cooldowns.Where(x => x.player.Steam64 == ctx.Caller.Steam64))
        {
            L.Log($"{cooldown.type}: {cooldown.Timeleft:hh\\:mm\\:ss}, {(cooldown.data is null || cooldown.data.Length == 0 ? "NO DATA" : string.Join(";", cooldown.data))}");
        }
    }


    private void giveuav(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);
        ctx.AssertRanByPlayer();
        bool isMarker = ctx.Caller.Player.quests.isMarkerPlaced;
        Vector3 pos = isMarker ? ctx.Caller.Player.quests.markerPosition : ctx.Caller.Player.transform.position;
        pos = pos with { y = Mathf.Min(Level.HEIGHT, F.GetHeight(pos, 0f) + UAV.GROUND_HEIGHT_OFFSET) };
        UAV.GiveUAV(ctx.Caller.GetTeam(), ctx.Caller, ctx.Caller, isMarker, pos);
        ctx.Defer();
    }

    private void requestuav(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);
        ctx.AssertRanByPlayer();

        UAV.RequestUAV(ctx.Caller);
        ctx.Defer();
    }
}