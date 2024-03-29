﻿using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DanielWillett.ReflectionTools;
using Microsoft.EntityFrameworkCore;
using Uncreated.Framework;
using Uncreated.Json;
using Uncreated.Networking.Async;
using Uncreated.Players;
using Uncreated.SQL;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Flags.Hardpoint;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.ReportSystem;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;
using VehicleSpawn = Uncreated.Warfare.Vehicles.VehicleSpawn;
using XPReward = Uncreated.Warfare.Levels.XPReward;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Models.Users;
using Uncreated.Warfare.Permissions;
using Uncreated.Warfare.Teams;
#if DEBUG
using HarmonyLib;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Squads.Commander;
#endif

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace Uncreated.Warfare.Commands;
public class DebugCommand : AsyncCommand
{
    public DebugCommand() : base("test", EAdminType.MEMBER, sync: true)
    {
        AddAlias("dev");
        AddAlias("tests");
        List<MethodInfo> methods = typeof(DebugCommand)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .ToList();
        List<CommandParameter> parameters = new List<CommandParameter>();
        CommandParameter[] args =
        {
            new CommandParameter("Arguments", typeof(object))
            {
                IsRemainder = true
            }
        };
        foreach (MethodInfo method in methods)
        {
            ParameterInfo[] p = method.GetParameters();
            if (p.Length is not 1 and not 2)
                continue;
            if (!p[0].ParameterType.IsAssignableFrom(typeof(CommandInteraction)))
                continue;
            if (p.Length == 2 && (method.ReturnType != typeof(Task) || p[1].ParameterType.IsAssignableFrom(typeof(CancellationToken))))
                continue;
            parameters.Add(new CommandParameter(method.Name.ToProperCase())
            {
                Parameters = args
            });
        }

        Structure = new CommandStructure
        {
            Description = "Commands used for development and basic actions not worth a full command.",
            Parameters = parameters.ToArray()
        };
    }
    public override async Task Execute(CommandInteraction ctx, CancellationToken token)
    {
        if (ctx.TryGet(0, out string operation))
        {
            MethodInfo? info;
            try
            {
                info = typeof(DebugCommand).GetMethod(operation, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
            }
            catch (AmbiguousMatchException)
            {
                throw ctx.Reply(T.DebugMultipleMatches, operation);
            }
            if (info == null)
                throw ctx.Reply(T.DebugNoMethod, operation);
            ParameterInfo[] parameters = info.GetParameters();
            int len = 1;
            if (parameters.Length == 0 || parameters[0].ParameterType != typeof(CommandInteraction))
                throw ctx.Reply(T.DebugNoMethod, operation);
            if (parameters.Length > 2 || (parameters.Length == 2 && parameters[1].ParameterType != typeof(CancellationToken)))
                throw ctx.Reply(T.DebugNoMethod, operation);
            if (typeof(Task).IsAssignableFrom(info.ReturnType) && parameters.Length == 2)
                len = 2;
            try
            {
#if DEBUG
                using IDisposable profiler = ProfilingUtils.StartTracking(info.Name + " Debug Command");
#endif
                ctx.Offset = 1;
                object obj = info.Invoke(this, len == 2 ? new object[] { ctx, token } : new object[] { ctx });
                if (obj is Task task)
                {
                    await task.ConfigureAwait(false);
#if DEBUG
                    profiler.Dispose();
#endif
                    await UCWarfare.ToUpdate();
                }
                ctx.Offset = 0;
            }
            catch (Exception ex)
            {
                if (ex is BaseCommandInteraction b2)
                    throw b2;
                if (ex.InnerException is BaseCommandInteraction b)
                    throw b;
                L.LogError(ex.InnerException ?? ex);
                throw ctx.Reply(T.DebugErrorExecuting, info.Name, (ex.InnerException ?? ex).GetType().Name);
            }
        }
        else throw ctx.SendCorrectUsage("/test <operation> [parameters...]");
    }
#pragma warning disable IDE1006
#pragma warning disable IDE0051
    private const string GIVE_XP_SYNTAX = "/test givexp <player> <amount> [team - required if offline]";
    private async Task givexp(CommandInteraction ctx, CancellationToken token)
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
                        if (team is > 0 and < 3)
                        {
                            await Data.DatabaseManager.AddXP(player, team, amount, token).ConfigureAwait(false);
                            PlayerNames name = await F.GetPlayerOriginalNamesAsync(player, token).ConfigureAwait(false);
                            await UCWarfare.ToUpdate(token);

                            ctx.ReplyString($"Given <#fff>{amount}</color> <#ff9b01>XP</color> to {(ctx.IsConsole ? name.PlayerName : name.CharacterName)}.");
                        }
                        else
                            ctx.SendCorrectUsage(GIVE_XP_SYNTAX);
                    }
                    else
                        ctx.Reply(T.PlayerNotFound);
                }
                else
                {
                    if (team is < 1 or > 2)
                        team = onlinePlayer.GetTeam();
                    await XPParameters
                        .WithTranslation(onlinePlayer, team, ctx.IsConsole ? T.XPToastFromOperator : T.XPToastFromPlayer, XPReward.Custom, amount)
                        .Award(token)
                        .ConfigureAwait(false);
                    await UCWarfare.ToUpdate(token);
                    ctx.ReplyString($"Given <#fff>{amount}</color> <#ff9b01>XP</color> " +
                                    $"to {(ctx.IsConsole ? onlinePlayer.Name.PlayerName : onlinePlayer.Name.CharacterName)}.");
                }
            }
            else
                ctx.SendCorrectUsage(GIVE_XP_SYNTAX);
        }
        else
            ctx.ReplyString($"Couldn't parse {ctx.Get(1)!} as a number.");
    }
    private const string GIVE_CREDITS_SYNTAX = "/test givecredits <player> <amount> [team - required if offline]";
    private async Task givecredits(CommandInteraction ctx, CancellationToken token)
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
                        if (team is > 0 and < 3)
                        {
                            await Data.DatabaseManager.AddCredits(player, team, amount, token).ConfigureAwait(false);
                            PlayerNames name = await Data.DatabaseManager.GetUsernamesAsync(player, token).ConfigureAwait(false);
                            await UCWarfare.ToUpdate(token);
                            ctx.ReplyString($"Given <#{UCWarfare.GetColorHex("credits")}>C</color> <#fff>{amount}</color> to {(ctx.IsConsole ? name.PlayerName : name.CharacterName)}.");
                        }
                        else
                            ctx.SendCorrectUsage(GIVE_CREDITS_SYNTAX);
                    }
                    else
                        ctx.Reply(T.PlayerNotFound);
                }
                else
                {
                    if (team is < 1 or > 2)
                        team = onlinePlayer.GetTeam();
                    await Points.AwardCreditsAsync(onlinePlayer, amount, ctx.IsConsole ? T.XPToastFromOperator : T.XPToastFromPlayer, token: token).ConfigureAwait(false);
                    await UCWarfare.ToUpdate(token);
                    ctx.ReplyString($"Given <#{UCWarfare.GetColorHex("credits")}>C</color> <#fff>{amount}</color> " +
                                    $"to {(ctx.IsConsole ? onlinePlayer.Name.PlayerName : onlinePlayer.Name.CharacterName)}.");
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

        Flag? flag = fg.Rotation.FirstOrDefault(f => f.PlayersOnFlag.Contains(ctx.Caller));
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
                flag.CapT1(Flag.MaxPoints - flag.Points - 1);
            }
            ctx.ReplyString("Flag quick-capped.");
        }
        else if (team == 2)
        {
            if (flag.Points > 0)
            {
                flag.CapT2(flag.Points);
            }
            else
            {
                flag.CapT2(Flag.MaxPoints - flag.Points - 2);
            }
            ctx.ReplyString("Flag quick-capped.");
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
            team = ctx.Caller.GetTeam();
        else
        {
            ctx.Reply(T.NotOnCaptureTeam);
            return;
        }
        UCWarfare.RunTask(Data.Gamemode.DeclareWin, team, ctx: "/test quickwin executed for " + team + ".");
        ctx.Defer();
    }
    private void zone(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.STAFF);

        ctx.AssertRanByPlayer();

        Vector3 pos = ctx.Caller.Position;
        if (pos == Vector3.zero) return;
        Flag? flag = Data.Is(out IFlagRotation fg) ? fg.Rotation.FirstOrDefault(f => f.PlayerInRange(pos)) : null;
        string txt = $"Position: <#ff9999>({pos.x.ToString("0.##", Data.LocalLocale)}, {pos.y.ToString("0.##", Data.LocalLocale)}, {pos.z.ToString("0.##", Data.LocalLocale)}) @ {new GridLocation(ctx.Caller.Position)}</color>. " +
                     $"Yaw: <#ff9999>{ctx.Caller.Player.transform.eulerAngles.y.ToString("0.##", Data.LocalLocale)}°</color>.";
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
        if (sign == null) ctx.ReplyString("You're not looking at a sign");
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

        ctx.AssertGamemode<IFlagRotation>();

        UCWarfare.I.StartCoroutine(ZoneDrawing.CreateFlagOverlay(ctx, openOutput: true));
        ctx.Defer();
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
        revive.ReviveManager.InjurePlayer(in p, player == ctx.Caller ? null : player);
        ctx.ReplyString($"Injured {(player == ctx.Caller ? "you" : player.CharacterName)}.");
    }
    private void clearui(CommandInteraction ctx)
    {
        ctx.AssertRanByPlayer();

        if (ctx.Caller.HasUIHidden)
        {
            UCWarfare.I.UpdateLangs(ctx.Caller, true);
        }
        else
        {
            Data.HideAllUI(ctx.Caller);
        }
        ctx.Caller.HasUIHidden = !ctx.Caller.HasUIHidden;
        ctx.ReplyString("<#a4a5b3>UI " + (ctx.Caller.HasUIHidden ? "hidden." : "visible."));
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
    private async Task playersave(CommandInteraction ctx, CancellationToken token)
    {
        ctx.AssertPermissions(EAdminType.MODERATOR);

        if (!ctx.HasArgs(3))
        {
            ctx.SendCorrectUsage(PLAYER_SAVE_USAGE);
            return;
        }
        if (ctx.TryGet(0, out ulong player, out _))
        {
            if (PlayerManager.HasSave(player, out PlayerSave save))
            {
                if (ctx.TryGet(2, out string value) && ctx.TryGet(1, out string property))
                {
                    SetPropertyResult result = SettableUtil<PlayerSave>.SetProperty(save, property, value, out MemberInfo? info);
                    if (info?.Name != null)
                        property = info.Name;
                    switch (result)
                    {
                        case SetPropertyResult.Success:
                            PlayerNames names = await F.GetPlayerOriginalNamesAsync(player, token).ConfigureAwait(false);
                            await UCWarfare.ToUpdate(token);
                            ctx.ReplyString($"Set {property} in {(ctx.IsConsole ? names.PlayerName : names.CharacterName)}'s playersave to {value}.");
                            break;
                        case SetPropertyResult.PropertyNotFound:
                            ctx.ReplyString($"Couldn't find a field by the name {property} in PlayerSave.");
                            break;
                        case SetPropertyResult.PropertyProtected:
                            ctx.ReplyString($"Unable to set {property}, it's missing the JsonSettable attribute.");
                            break;
                        case SetPropertyResult.TypeNotSettable:
                        case SetPropertyResult.ParseFailure:
                            ctx.ReplyString($"Couldn't parse {value} as a valid value for {property} (" + (info?.GetMemberType() is { } type ? type.Name : "String") + ").");
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
                ctx.ReplyString("This player hasn't joined the server yet.");
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
                if (Data.Is(out IStagingPhase gm) && gm.State == State.Staging)
                {
                    ctx.ReplyString("Skipped staging phase.");
                    gm.SkipStagingPhase();
                }
                else
                    ctx.Defer();
                UCWarfare.RunTask(async () =>
                {
                    await UCWarfare.ToUpdate();
                    if (newGamemode == Data.Gamemode?.GetType())
                    {
                        await Data.Singletons.ReloadSingletonAsync(Gamemode.GamemodeReloadKey);
                        await UCWarfare.ToUpdate();
                        ctx.ReplyString($"Successfully reloaded {newGamemode.Name}.");
                    }
                    else if (await Gamemode.TryLoadGamemode(newGamemode, false, default))
                    {
                        ctx.LogAction(ActionLogType.ChangeGamemodeCommand, Data.Gamemode!.DisplayName);
                        await UCWarfare.ToUpdate();
                        ctx.ReplyString($"Successfully loaded {newGamemode.Name}.");
                    }
                    else
                    {
                        await UCWarfare.ToUpdate();
                        ctx.ReplyString($"Failed to load {newGamemode.Name}.");
                    }
                }, ctx: "/test gamemode executed for type " + newGamemode.Name + ".");
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
        ctx.ReplyString(Data.TrackStats ? "Re-enabled stat tracking." : "Disabled stat tracking.");
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

        ctx.ReplyString(ct == 0 ? "Couldn't find any zone blockers." : $"Destroyed {ct} zone blocked{ct.S()}");
    }
    private void skipstaging(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.MODERATOR);

        ctx.AssertGamemode(out IStagingPhase gm);

        if (gm.State == State.Staging)
        {
            gm.SkipStagingPhase();
            ctx.ReplyString("Skipped staging phase.");
        }
        else
        {
            ctx.ReplyString("Staging phase is not active.");
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
                PlayerNames name = player.Name;
                ctx.ReplyString($"Reset lobby for {(ctx.IsConsole ? name.PlayerName : name.CharacterName)}.");
            }
            else
            {
                ctx.SendPlayerNotFound();
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
                ctx.ReplyString($"Barricade {bd.asset.itemName}: #{bd.instanceID.ToString(Data.LocalLocale)}");
                return;
            }
            StructureDrop? dp = StructureManager.FindStructureByRootTransform(t);
            if (dp != null)
            {
                ctx.ReplyString($"Structure {dp.asset.itemName}: #{dp.instanceID.ToString(Data.LocalLocale)}");
                return;
            }
            for (int i = 0; i < VehicleManager.vehicles.Count; i++)
            {
                if (VehicleManager.vehicles[i].transform == t)
                {
                    ctx.ReplyString($"Vehicle {VehicleManager.vehicles[i].asset.vehicleName}: #{VehicleManager.vehicles[i].instanceID.ToString(Data.LocalLocale)}");
                    return;
                }
            }
            for (byte b = 0; b < Regions.WORLD_SIZE; b++)
            {
                for (byte b2 = 0; b2 < Regions.WORLD_SIZE; b2++)
                {
                    List<LevelObject> objs = LevelObjects.objects[b, b2];
                    for (int i = 0; i < objs.Count; i++)
                    {
                        LevelObject obj = objs[i];
                        if (obj.transform == t)
                        {
                            ctx.ReplyString($"Vehicle {obj.asset.objectName} ({obj.asset.name}, {obj.asset.GUID:N}): #{obj.instanceID.ToString(Data.LocalLocale)}");
                            return;
                        }
                    }
                }
            }
        }
        ctx.ReplyString("You must be looking at a barricade, structure, vehicle, or object.");
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
            RequestResponse res = await Reporter.NetCalls.SendReportInvocation.Request(Reporter.NetCalls.ReceiveInvocationResponse, UCWarfare.I.NetClient!, report, false);
            L.Log(res is { Responded: true, Parameters.Length: > 1 } && res.Parameters[1] is string url ? ("URL: " + url) : "No response.", ConsoleColor.DarkYellow);
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

        if (ctx.TryGet(0, out QuestType type))
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
        ctx.ReplyString("Time: " + Util.ParseTimespan(t).ToString("g"));
    }
    private void getperms(CommandInteraction ctx)
    {
        ctx.ReplyString("Permission: " + ctx.Caller.GetPermissions());
    }
#if DEBUG
    private static readonly InstanceSetter<InteractableVehicle, bool>? SetEngineOn = Accessor.GenerateInstanceSetter<InteractableVehicle, bool>("<isEngineOn>k__BackingField");
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
        SetEngineOn?.Invoke(veh, true);
        RaycastHit[] results = new RaycastHit[16];
        yield return new WaitForSeconds(5f);

        while (veh.isActiveAndEnabled)
        {
            const float TIME = 0.1f;
            yield return new WaitForSeconds(TIME);
            if (veh == null) yield break;
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

                if (hit.transform != null && hit.transform != veh.transform && hit.transform.name != asset.id.ToString())
                {
                    if (hit.normal.y < 0.1)
                    {
                        // hit a wall
                        yield return new WaitForSeconds(3f);
                        continue;
                    }
                    angle2.y += 1 - hit.normal.y;
                }
            }
            Rigidbody body = veh.GetComponent<Rigidbody>();
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
            veh.updatePhysics();
            veh.updateVehicle();
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
    private void quest(CommandInteraction ctx)
    {
        ctx.AssertRanByPlayer();
        ctx.AssertPermissions(EAdminType.ADMIN);
        if (ctx.MatchParameter(0, "add"))
        {
            if (ctx.TryGet(1, out QuestAsset asset, out _, true, -1, false))
            {
                QuestManager.TryAddQuest(ctx.Caller, asset);
                ctx.ReplyString("<#9fa1a6>Added quest " + asset.questName + " <#ddd>(" + asset.id + ", " + asset.GUID.ToString("N") + ")</color>.");
            }
            else ctx.ReplyString("<#ff8c69>Quest not found.");
        }
        else if (ctx.MatchParameter(0, "track"))
        {
            if (ctx.TryGet(1, out QuestAsset asset, out _, true, -1, false))
            {
                ctx.Caller.ServerTrackQuest(asset);
                ctx.ReplyString("<#9fa1a6>Tracked quest " + asset.questName + " <#ddd>(" + asset.id + ", " + asset.GUID.ToString("N") + ")</color>.");
            }
            else ctx.ReplyString("<#ff8c69>Quest not found.");
        }
        else if (ctx.MatchParameter(0, "remove"))
        {
            if (ctx.TryGet(1, out QuestAsset asset, out _, true, -1, false))
            {
                ctx.Caller.Player.quests.ServerRemoveQuest(asset);
                ctx.ReplyString("<#9fa1a6>Removed quest " + asset.questName + " <#ddd>(" + asset.id + ", " + asset.GUID.ToString("N") + ")</color>.");
            }
            else ctx.ReplyString("<#ff8c69>Quest not found.");
        }
        else ctx.SendCorrectUsage("/test quest <add|track|remove> <id>");
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
            ctx.ReplyString("<#9fa1a6>Asset: " + (Assets.find(guid)?.FriendlyName ?? "null"));
        }
        else if (ctx.TryGet(0, out ushort us) && ctx.TryGet(1, out EAssetType type))
        {
            ctx.ReplyString("<#9fa1a6>Asset: " + (Assets.find(type, us)?.FriendlyName ?? "null"));
        }
        else
        {
            ctx.ReplyString("<#ff8c69>Please use a <GUID> or <uhort, type>");
        }
    }
    private void traits(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);

        ctx.AssertGamemode<ITraits>();

        ctx.AssertRanByPlayer();

        L.Log("Traits: ");
        using (L.IndentLog(1))
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
            ctx.ReplyString("Advanced delays by " + seconds.ToString("0.##", Data.LocalLocale) + " seconds.");
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
        foreach (Cooldown cooldown in CooldownManager.Singleton.Cooldowns.Where(x => x.Player.Steam64 == ctx.Caller.Steam64))
        {
            L.Log($"{cooldown.CooldownType}: {cooldown.Timeleft:hh\\:mm\\:ss}, {(cooldown.Parameters is null || cooldown.Parameters.Length == 0 ? "NO DATA" : string.Join(";", cooldown.Parameters))}");
        }
    }
#if DEBUG
    private void giveuav(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);
        ctx.AssertRanByPlayer();
        bool isMarker = ctx.Caller.Player.quests.isMarkerPlaced;
        Vector3 pos = isMarker ? ctx.Caller.Player.quests.markerPosition : ctx.Caller.Player.transform.position;
        pos = pos with { y = Mathf.Min(Level.HEIGHT, F.GetHeight(pos, 0f) + UAV.GroundHeightOffset) };
        UAV.GiveUAV(ctx.Caller.GetTeam(), ctx.Caller, ctx.Caller, isMarker, pos);
        ctx.Defer();
    }
    private async Task requestuav(CommandInteraction ctx, CancellationToken token)
    {
        ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);
        ctx.AssertRanByPlayer();

        await UAV.RequestUAV(ctx.Caller, token);
        ctx.Defer();
    }
    private async Task testfield(CommandInteraction ctx, CancellationToken token)
    {
        ctx.AssertRanByPlayer();
        ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);
        ctx.AssertGamemode(out IVehicles vehicles);
        ulong other = TeamManager.Other(ctx.Caller.GetTeam());
        //FactionInfo? other = TeamManager.GetFactionSafe(oteam);
        if (other is not 1ul and not 2ul)
            throw ctx.Reply(T.NotOnCaptureTeam);
        VehicleData[] data;
        vehicles.VehicleBay.WriteWait();
        try
        {
            data = vehicles.VehicleBay.Items
                .Select(x => x.Item)
                .Where(x => x is { Branch: Branch.Default or Branch.Infantry or Branch.Armor }).ToArray()!;
        }
        finally
        {
            vehicles.VehicleBay.WriteRelease();
        }

        Vector3 st = ctx.Caller.Position;
        Quaternion rot = Quaternion.Euler(Vector3.zero);
        const int size = 480;
        const int offset = 24;
        const int sections = size / 2 / offset;
        for (int x = -sections; x <= sections; ++x)
        {
            for (int z = -sections; z <= sections; ++z)
            {
                Vector3 pos = new Vector3(st.x + x * offset, st.y, st.z + z * offset);
                float h = LevelGround.getHeight(pos);
                if (h <= 0f)
                    continue;
                pos = pos with { y = h + 10f };
                VehicleData veh = data[UnityEngine.Random.Range(0, data.Length)];
                if (VehicleData.IsEmplacement(veh.Type) && Assets.find(veh.VehicleID) is VehicleAsset asset)
                {
                    FOBManager.SpawnEmplacement(asset, pos, Quaternion.identity, 0ul, other);
                }
                else
                {
                    await VehicleSpawner.SpawnLockedVehicle(veh.VehicleID, pos, rot, groupOwner: other, token: token);
                }
                await Task.Delay(25, token);
                await UCWarfare.ToUpdate(token);
            }
        }

        ctx.ReplyString("Spawned " + (sections * sections * 4) + " vehicles.");
    }
#endif
    private async Task squad(CommandInteraction ctx, CancellationToken token)
    {
        ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);
        ctx.AssertRanByPlayer();

        KitManager? manager = KitManager.GetSingletonQuick();
        ulong team = ctx.Caller.GetTeam();
        if (manager != null)
        {
            Kit? sql = await manager.GetRecommendedSquadleaderKit(team, token).ConfigureAwait(false);
            if (sql == null)
                ctx.SendUnknownError();
            else
            {
                sql = await manager.GetKit(sql.PrimaryKey, token, x => KitManager.RequestableSet(x, false));
                await manager.Requests.GiveKit(ctx.Caller, sql, true, false, token).ConfigureAwait(false);
            }
        }

        await UCWarfare.ToUpdate(token);
        if (ctx.Caller.Squad is null || ctx.Caller.Squad.Leader != ctx.Caller)
        {
            SquadManager.CreateSquad(ctx.Caller, team);
        }

        if (Data.Gamemode.State == State.Staging)
        {
            Data.Gamemode.SkipStagingPhase();
        }

        if (Data.Is(out IVehicles vehicleGm))
        {
            SqlItem<VehicleSpawn>? vehicle = VehicleBayCommand.GetBayTarget(ctx, vehicleGm.VehicleSpawner);
            if (vehicle?.Item is null)
            {
                ctx.ReplyString("No logistics vehicle nearby to request.", Color.red);
            }
            else if (vehicle.Item.HasLinkedVehicle(out InteractableVehicle veh))
            {
                SqlItem<VehicleData>? data = vehicle.Item.Vehicle;
                if (data?.Item == null)
                {
                    ctx.ReplyString("Failed to get a logistics vehicle.", Color.red);
                }
                else
                {
                    await data.Enter(token).ConfigureAwait(false);
                    try
                    {
                        await UCWarfare.ToUpdate(token);
                        RequestCommand.GiveVehicle(ctx.Caller, veh, data.Item);
                    }
                    finally
                    {
                        data.Release();
                    }
                }
            }
            else
                ctx.ReplyString("Logistics vehicle not available.", Color.red);
        }

        ctx.Defer();
    }
    private void effect(CommandInteraction ctx)
    {
        ctx.AssertRanByPlayer();
        ctx.AssertPermissions(EAdminType.MODERATOR);
        const string usage = "/test effect [clear] <name/id/guid> (for UI only - [key : int16] [arg0 : str] [arg1 : str] [arg2 : str] [arg3 : str] )";
        ctx.AssertHelpCheck(0, usage);
        ctx.AssertArgs(0, usage);
        EffectAsset asset;
        if (ctx.MatchParameter(0, "clear", "remove", "delete"))
        {
            ctx.AssertArgs(1, usage);
            if (ctx.MatchParameter(1, "all", "*", "any"))
                asset = null!;
            else if (!ctx.TryGet(1, out asset, out _, allowMultipleResults: true))
                throw ctx.ReplyString($"<#ff8c69>Can't find an effect with the term: <#ddd>{ctx.Get(1)}</color>.");
            if (asset == null)
            {
                Data.HideAllUI(ctx.Caller);
                throw ctx.ReplyString("<#9fa1a6>Cleared all effects.");
            }
            EffectManager.ClearEffectByGuid(asset.GUID, ctx.Caller.Connection);
            throw ctx.ReplyString($"<#9fa1a6>Cleared all {asset.name} effects.");
        }
        if (!ctx.TryGet(0, out asset, out _, allowMultipleResults: true))
            throw ctx.ReplyString($"<#ff8c69>Can't find an effect with the term: <#ddd>{ctx.Get(0)}</color>.");
        short key = ctx.MatchParameter(1, "-", "_") || !ctx.TryGet(1, out short s) ? (short)0 : s;
        if (asset.effect != null)
        {
            if (asset.effect.CompareTag("UI"))
            {
                switch (ctx.ArgumentCount)
                {
                    case 3:
                        EffectManager.sendUIEffect(asset.id, key, ctx.Caller.Connection, true, ctx.Get(2));
                        throw ctx.ReplyString($"<#9fa1a6>Sent {asset.name} to you with {{0}} = \"{ctx.Get(2)}\".");
                    case 4:
                        EffectManager.sendUIEffect(asset.id, key, ctx.Caller.Connection, true, ctx.Get(2), ctx.Get(3));
                        throw ctx.ReplyString($"<#9fa1a6>Sent {asset.name} to you with {{0}} = \"{ctx.Get(2)}\", {{1}} = \"{ctx.Get(3)}\".");
                    case 5:
                        EffectManager.sendUIEffect(asset.id, key, ctx.Caller.Connection, true, ctx.Get(2), ctx.Get(3), ctx.Get(4));
                        throw ctx.ReplyString($"<#9fa1a6>Sent {asset.name} to you with {{0}} = \"{ctx.Get(2)}\", {{1}} = \"{ctx.Get(3)}\", {{2}} = \"{ctx.Get(4)}\".");
                    default:
                        if (ctx.ArgumentCount < 3)
                        {
                            EffectManager.sendUIEffect(asset.id, key, ctx.Caller.Connection, true);
                            throw ctx.ReplyString($"<#9fa1a6>Sent {asset.name} to you with no arguments.");
                        }
                        EffectManager.sendUIEffect(asset.id, key, ctx.Caller.Connection, true, ctx.Get(2), ctx.Get(3), ctx.Get(4), ctx.Get(5));
                        throw ctx.ReplyString($"<#9fa1a6>Sent {asset.name} to you with {{0}} = \"{ctx.Get(2)}\", {{1}} = \"{ctx.Get(3)}\", {{2}} = \"{ctx.Get(4)}\", {{3}} = \"{ctx.Get(5)}\".");
                }
            }
            F.TriggerEffectReliable(asset, ctx.Caller.Connection, ctx.Caller.Position);
            throw ctx.ReplyString($"<#9fa1a6>Sent {asset.name} to you at {ctx.Caller.Position.ToString("0.##", ctx.CultureInfo)}." + (ctx.HasArg(1) ? " To spawn as UI instead, the effect must have the \"UI\" tag in unity." : string.Empty));
        }
        throw ctx.ReplyString($"<#ff8c69>{asset.name}'s effect property hasn't been set. Possibly the effect was set up incorrectly.");
    }
    private void watchdogtest(CommandInteraction ctx)
    {
        ctx.AssertRanByConsole();
        ctx.ReplyString("Starting...");
        UCWarfare.RunTask(async () =>
        {
            ctx.ReplyString("0 sec / 240 sec");
            for (int i = 0; i < 24; ++i)
            {
                await Task.Delay(10000);
                ctx.ReplyString(((i + 1) * 10).ToString(Data.AdminLocale) + " sec / 240 sec");
            }
        });
    }

#if DEBUG
    private void runtests(CommandInteraction ctx)
    {
        ctx.AssertRanByConsole();

        string? typeName = ctx.Get(0);
        string? methodName = ctx.Get(1);
        ctx.AssertHelpCheck(0, "/test [type : str] [method : str]");
        ctx.ReplyString("<#9fa1a6>Running tests (type: " + (typeName ?? "[any]") + ", method: " + (methodName ?? "[any]") + ").");
        Task.Run(async () =>
        {
            try
            {
                foreach (Type type in Accessor.GetTypesSafe())
                {
                    if (!string.IsNullOrEmpty(typeName) && !type.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase))
                        continue;
                    MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (methods.Length < 0) continue;
                    bool one = false;
                    bool isSingleton = false;
                    foreach (MethodInfo method in methods)
                    {
                        if (!string.IsNullOrEmpty(methodName) && !method.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase))
                            continue;
                        OperationTestAttribute[] attrs = Attribute.GetCustomAttributes(method, typeof(OperationTestAttribute)).Cast<OperationTestAttribute>().ToArray();
                        if (attrs.Length < 1) continue;
                        object? target = null;
                        if (!method.IsStatic)
                        {
                            if (!one)
                            {
                                isSingleton = typeof(IUncreatedSingleton).IsAssignableFrom(type);
                                one = true;
                            }
                            if (!isSingleton)
                            {
                                L.LogWarning("Can't run an instance method on a non-singleton object: " + method.FullDescription() + ".");
                                continue;
                            }

                            IUncreatedSingleton? singleton = Data.Singletons.GetSingleton(type);
                            if (singleton is null)
                            {
                                L.LogWarning("Can't find a singleton of type " + type.Name + " to run an instance method on: " + method.FullDescription() + ".");
                                continue;
                            }

                            target = singleton;
                        }
                        string? disp2 = null;
                        ParameterInfo[] parameters = method.GetParameters();
                        for (int a = 0; a < attrs.Length; ++a)
                        {
                            OperationTestAttribute attr = attrs[a];
                            string? disp = attr.DisplayName;
                            if (disp is null)
                            {
                                disp = disp2 ?? method.Name;
                                if (disp2 is null)
                                    L.LogDebug("\n[INFO]  " + disp, ConsoleColor.DarkCyan);
                            }
                            else if (disp2 is null)
                            {
                                L.LogDebug("\n[INFO]  " + disp, ConsoleColor.DarkCyan);
                                disp2 = disp;
                            }
                            object[] @params;
                            bool cancel = false;
                            if (parameters.Length != 1)
                            {
                                if (parameters.Length == 0)
                                {
                                    @params = Array.Empty<object>();
                                    goto exe;
                                }
                                if (parameters.Length == 2)
                                    cancel = parameters[1].ParameterType == typeof(CancellationToken);
                                if (!cancel)
                                {
                                    L.LogWarning(" 🗴 Test method " + disp + " \"" + method.FullDescription() + "\" does not have a valid signature for test #" + a + ".");
                                    continue;
                                }
                            }
                            else
                            {
                                cancel = parameters[0].ParameterType == typeof(CancellationToken);
                            }
                            if (attr.ArgumentInt64.HasValue) @params = new object[] { attr.ArgumentInt64.Value };
                            else if (attr.ArgumentUInt64.HasValue) @params = new object[] { attr.ArgumentUInt64.Value };
                            else if (attr.ArgumentInt32.HasValue) @params = new object[] { attr.ArgumentInt32.Value };
                            else if (attr.ArgumentUInt32.HasValue) @params = new object[] { attr.ArgumentUInt32.Value };
                            else if (attr.ArgumentInt16.HasValue) @params = new object[] { attr.ArgumentInt16.Value };
                            else if (attr.ArgumentUInt16.HasValue) @params = new object[] { attr.ArgumentUInt16.Value };
                            else if (attr.ArgumentInt8.HasValue) @params = new object[] { attr.ArgumentInt8.Value };
                            else if (attr.ArgumentUInt8.HasValue) @params = new object[] { attr.ArgumentUInt8.Value };
                            else if (attr.ArgumentSingle.HasValue) @params = new object[] { attr.ArgumentSingle.Value };
                            else if (attr.ArgumentDouble.HasValue) @params = new object[] { attr.ArgumentDouble.Value };
                            else if (attr.ArgumentDecimal.HasValue) @params = new object[] { attr.ArgumentDecimal.Value };
                            else if (attr.ArgumentBoolean.HasValue) @params = new object[] { attr.ArgumentBoolean.Value };
                            else if (attr.ArgumentString != null) @params = new object[] { attr.ArgumentString };
                            else if (attr.ArgumentType != null) @params = new object[] { attr.ArgumentType };
                            else
                            {
                                @params = Array.Empty<object>();
                                goto exe;
                            }

                            if (cancel)
                                Util.AddToArray(ref @params!, CancellationToken.None);

                            Type type1 = @params[0].GetType();
                            if (!type1.IsAssignableFrom(parameters[0].ParameterType))
                            {
                                L.LogWarning(" 🗴 Test method " + disp + " \"" + method.FullDescription() + "\" does not have a valid signature for test #" + a +
                                             ": " + method.ReturnType.Name + " (" + type1.Name + ").");
                                continue;
                            }
                            exe:
                            try
                            {
                                object? result = method.Invoke(target, @params);
                                if (result is Task task && !task.IsCompleted)
                                {
                                    await task.ConfigureAwait(false);
                                    Type taskType = task.GetType();
                                    if (taskType.IsGenericType)
                                    {
                                        result = taskType.GetProperty("Result", BindingFlags.Public | BindingFlags.Instance)?.GetValue(task);
                                    }
                                    if (result != null)
                                        L.Log(" ✓ " + disp + " test " + a + " completed async with result \"" + result + "\".", ConsoleColor.Green);
                                    else
                                        L.Log(" ✓ " + disp + " test " + a + " completed async.", ConsoleColor.Green);
                                }
                                else if (result != null)
                                {
                                    L.Log(" ✓ " + disp + " test " + a + " completed with result \"" + result + "\".", ConsoleColor.Green);
                                }
                                else
                                {
                                    L.Log(" ✓ " + disp + " test " + a + " completed.", ConsoleColor.Green);
                                }
                            }
                            catch (Exception ex)
                            {
                                if (ex is OperationCanceledException)
                                    L.LogWarning(" " + disp + " " + method.FullDescription() + " test " + a + " was cancelled.");
                                else
                                {
                                    L.LogError(" " + disp + " " + method.FullDescription() + " test " + a + " failed with exception:");
                                    L.LogError(ex);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                L.LogError("Error running tests:");
                L.LogError(ex);
            }
        });
    }
#endif
    private void translate(CommandInteraction ctx)
    {
        if (ctx.TryGet(0, out string name))
        {
            ctx.TryGet(1, out bool imgui);

            if (T.Translations.FirstOrDefault(x => x.Key.Equals(name, StringComparison.Ordinal)) is { } translation)
            {
                string val = translation.Translate(ctx.Caller?.Locale.LanguageInfo, out Color color, ctx.HasArg(1) ? imgui : (ctx.Caller?.Save.IMGUI ?? false));
                L.Log($"Translation: {translation.Id}... {name}.");
                L.Log($"Type: {translation.GetType()}");
                L.Log($"Args: {string.Join(", ", translation.GetType().GetGenericArguments().Select(x => x.Name))}");
                L.Log( "Color: " + color.ToString("F2"));
                L.Log( "Value: \"" + val + "\".");
                L.Log("Dump:");
                using IDisposable indent = L.IndentLog(1);
                translation.Dump();
                ctx.ReplyString(val, color);
            }
            else ctx.ReplyString(name + " not found.");
        }
    }
    private void nerd(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.STAFF);
        if (ctx.TryGet(0, out ulong s64, out UCPlayer? onlinePlayer))
        {
            if (s64 == 76561198267927009ul)
            {
                s64 = ctx.CallerID;
                onlinePlayer = ctx.Caller;
            }
            if (!UCWarfare.Config.Nerds.Contains(s64))
            {
                UCWarfare.Config.Nerds.Add(s64);
                UCWarfare.SaveSystemConfig();
            }
            ctx.ReplyString($"{onlinePlayer?.ToString() ?? s64.ToString()} is now a nerd.");
        }
        else ctx.SendCorrectUsage("/test nerd <player>");
    }
    private void unnerd(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.STAFF);
        if (ctx.TryGet(0, out ulong s64, out UCPlayer? onlinePlayer))
        {
            if (UCWarfare.Config.Nerds.RemoveAll(x => x == s64) > 0)
            {
                UCWarfare.SaveSystemConfig();
                ctx.ReplyString($"{onlinePlayer?.ToString() ?? s64.ToString()} is not a nerd.");
            }
            else ctx.ReplyString($"{onlinePlayer?.ToString() ?? s64.ToString()} was not a nerd.");
        }
        else ctx.SendCorrectUsage("/test unnerd <player>");
    }
    private void findassets(CommandInteraction ctx)
    {
        ctx.AssertRanByConsole();
        ctx.AssertHelpCheck(0, "/test findassets <type>.<field/property> <value> - List assets by their value");
        ctx.AssertArgs(2, "/test findassets <type>.<field/property> <value>");

        string str1 = ctx.Get(0)!;
        string str2 = ctx.GetRange(1)!;

        Type type = typeof(Asset);
        int ind1 = str1.IndexOf('.');
        if (ind1 != -1)
        {
            string tStr = str1.Substring(0, ind1);
            type = typeof(Provider).Assembly.GetType("SDG.Unturned." + tStr, false, true)!;
            if (type == null || !typeof(Asset).IsAssignableFrom(type))
            {
                L.LogWarning("Unable to find type: SDG.Unturned." + tStr + ".");
                type = typeof(Asset);
            }
            else
                str1 = str1.Substring(ind1 + 1);
        } 

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy;
        MemberInfo? field = (MemberInfo?)type.GetProperty(str1, flags) ?? type.GetField(str1, flags);
        if (field == null || field is PropertyInfo p && p.GetMethod == null)
            throw ctx.ReplyString("<#ff8c69>Field or property not found: <#fff>" + type.Name + "</color>.<#fff>" + str1 + "</color>.", ConsoleColor.DarkYellow);
        Type fldType = field is FieldInfo f ? f.FieldType : ((PropertyInfo)field).PropertyType;

        if (!Util.TryParseAny(str2, fldType, out object val))
            throw ctx.ReplyString("<#ff8c69>Failed to parse <#fff>" + str2 + "</color> as <#fff>" + fldType.Name + "</color>.", ConsoleColor.DarkYellow);
        MethodInfo? assetsFind = typeof(Assets).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                                        .FirstOrDefault(x => x.Name.Equals("find", StringComparison.Ordinal) &&
                                                        x.IsGenericMethod &&
                                                        x.GetParameters().FirstOrDefault()?.ParameterType is { IsGenericType: true } ptype &&
                                                        ptype.GetGenericTypeDefinition() == typeof(List<>))?.MakeGenericMethod(type);
        IList allassets = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(type));
        assetsFind!.Invoke(null, new object[] { allassets });

        int c = 0;
        foreach (Asset asset in allassets.Cast<Asset>().Where(val is null ? x => GetValue(x) == null : x => val.Equals(GetValue(x))))
        {
            L.Log(ActionLog.AsAsset(asset), ConsoleColor.DarkCyan);
            ++c;
        }

        ctx.ReplyString("Found " + c + " assets.", ConsoleColor.DarkCyan);

        object GetValue(Asset asset)
        {
            if (field is FieldInfo f)
                return f.GetValue(asset);

            return ((PropertyInfo)field).GetMethod.Invoke(asset, Array.Empty<object>());
        }
    }
    private void hardpointadv(CommandInteraction ctx)
    {
        ctx.AssertGamemode(out Hardpoint hardpoint);
        ctx.AssertOnDuty();

        hardpoint.ForceNextObjective();
        ctx.Defer();
    }
    private void setholiday(CommandInteraction ctx)
    {
        ctx.AssertRanByConsole();
        
        if (ctx.TryGet(0, out ENPCHoliday holiday))
        {
            FieldInfo? field = typeof(HolidayUtil).GetField("holidayOverride", BindingFlags.Static | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(null, holiday);
                ctx.ReplyString("Set active holiday to " + Localization.TranslateEnum(holiday, ctx.LanguageInfo));
                field = typeof(Provider).GetField("authorityHoliday", BindingFlags.Static | BindingFlags.NonPublic);
                if (holiday == ENPCHoliday.NONE)
                {
                    MethodInfo? method = typeof(HolidayUtil).GetMethod("BackendGetActiveHoliday", BindingFlags.Static | BindingFlags.NonPublic);
                    if (method != null)
                        holiday = (ENPCHoliday)method.Invoke(null, Array.Empty<object>());
                }
                
                field?.SetValue(null, holiday);
                return;
            }

            throw ctx.ReplyString("Unable to find 'HolidayUtil.holidayOverride' field.");
        }
        throw ctx.ReplyString("Invalid holiday: " + SqlTypes.Enum<ENPCHoliday>());
    }
    private void startloadout(CommandInteraction ctx)
    {
        ctx.AssertRanByPlayer();

        ctx.AssertOnDuty();
        
        if (!ctx.TryGet(0, out Class @class))
            throw ctx.SendCorrectUsage("/test startloadout <class>");

        IKitItem[] items = KitDefaults<WarfareDbContext>.GetDefaultLoadoutItems(@class);
        L.LogDebug("Found " + items.Length + " item" + items.Length.S() + ".");
        
        UCInventoryManager.GiveItems(ctx.Caller, items, true);

        ctx.ReplyString($"Given {items.Length} default item{items.Length.S()} for a {Localization.TranslateEnum(@class, ctx.LanguageInfo)} loadout.");
    }

    private async Task viewlens(CommandInteraction ctx, CancellationToken token)
    {
        ctx.AssertRanByPlayer();

        ctx.AssertOnDuty();

        if (ctx.MatchParameter(0, "clear", "none", "me"))
        {
            ctx.Caller.ViewLens = null;
            ctx.ReplyString("Removed view lens.");

            UCWarfare.I.UpdateLangs(ctx.Caller, false);
        }
        else if (ctx.TryGet(0, out ulong s64, out UCPlayer? onlinePlayer, remainder: true))
        {
            ctx.Caller.ViewLens = s64 == ctx.CallerID ? null : s64;
            if (s64 == ctx.CallerID)
            {
                ctx.ReplyString("Removed view lens.");
            }
            else if (onlinePlayer != null)
            {
                ctx.ReplyString("Set view lens to " + onlinePlayer.Translate(ctx, UCPlayer.FormatColoredPlayerName) +
                                " (" + onlinePlayer.Translate(ctx, UCPlayer.FormatColoredSteam64) + ")'s perspective. Clear with <#fff>/test viewlens clear</color>.");
            }
            else
            {
                PlayerNames names = await F.GetPlayerOriginalNamesAsync(s64, token).ConfigureAwait(false);
                await UCWarfare.ToUpdate(token);
                ctx.ReplyString("Set view lens to " + names.PlayerName + " (" + s64.ToString(ctx.CultureInfo) + ")'s perspective. Clear with <#fff>/test viewlens clear</color>.");
            }

            UCWarfare.I.UpdateLangs(ctx.Caller, false);
        }
        else ctx.SendCorrectUsage("/test viewlens <player ...> - Simulates UI from another player's perspective.");
    }

    private void dumpfob(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.HELPER);
        
        ctx.AssertRanByPlayer();

        FOBManager? fobs = Data.Singletons.GetSingleton<FOBManager>();
        if (fobs == null)
            throw ctx.SendGamemodeError();
        IFOB? fob = null;
        if (ctx.TryGetTarget(out BarricadeDrop barricade))
            fob = fobs.FindFob(new UCBarricade(barricade));
        if (fob == null && ctx.TryGetTarget(out StructureDrop structure))
            fob = fobs.FindFob(new UCStructure(structure));
        if (fob == null && ctx.TryGetTarget(out InteractableVehicle vehicle))
            fob = fobs.FindFob(vehicle);

        if (fob == null)
            throw ctx.ReplyString("<#ff8c69>Look at a FOB item to use this command.");

        fob.Dump(ctx.Caller);
        ctx.ReplyString("Check console.");
    }

    private async Task exportlang(CommandInteraction ctx, CancellationToken token)
    {
        ctx.AssertRanByConsole();
        ctx.AssertHelpCheck(0, "/text exportlang [lang]");
        string? input = ctx.GetRange(0);
        LanguageInfo lang = (input == null ? null : Data.LanguageDataStore.GetInfoCached(input, true)) ?? Localization.GetDefaultLanguage();
        await Translation.ExportLanguage(lang, false, true, token).ConfigureAwait(false);
        ctx.ReplyString(lang + " exported.");
    }

    private void zonespeedtest(CommandInteraction ctx)
    {
        ctx.AssertRanByConsole();
        ZoneList zl = Data.Singletons.GetSingleton<ZoneList>()!;
        zl.WriteWait();
        try
        {
            Flag[] zones = zl.Items.Where(x => x.Item != null).Select(x => new Flag(x, null!)).ToArray();
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < zones.Length; ++i)
            {
                zones[i].GetUpdatedPlayers().Release();
            }

            stopwatch.Stop();
            ctx.ReplyString($"Time to scan {zl.Items.Count} flags * {Provider.clients.Count} players: {stopwatch.GetElapsedMilliseconds():F4} ms.");
        }
        finally
        {
            zl.WriteRelease();
        }
    }
    private async Task getpfp(CommandInteraction ctx)
    {
        ctx.AssertRanByConsole();
        if (!ctx.TryGet(0, out ulong steam64, out _, true))
        {
            ctx.AssertRanByPlayer();
            steam64 = ctx.CallerID;
        }

        IModerationActor actor = Actors.GetActor(steam64);
        string? pfp = await actor.GetProfilePictureURL(Data.ModerationSql, AvatarSize.Full, UCWarfare.UnloadCancel);
        ctx.ReplyString(pfp ?? "NULL");
        if (!ctx.IsConsole)
            L.Log("PFP URL: " + (pfp ?? "NULL"));
    }

    private void damage(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.ADMIN_ON_DUTY | EAdminType.VANILLA_ADMIN);
        ctx.AssertRanByPlayer();

        ctx.AssertArgs(1, "/test damage <amount>[%] [-r]");
        string arg = ctx.Get(0)!;
        bool percent = arg.Length > 0 && arg[arg.Length - 1] == '%';
        bool remaining = percent && ctx.MatchFlag("r");
        if (percent)
            arg = arg.Substring(0, arg.Length - 1);
        if (!float.TryParse(arg, NumberStyles.Number, ctx.CultureInfo, out float damage))
            throw ctx.SendCorrectUsage("/test damage <amount>[%] [-r]");

        RaycastInfo cast = DamageTool.raycast(new Ray(ctx.Caller.Player.look.aim.position, ctx.Caller.Player.look.aim.forward), 4f, RayMasks.BARRICADE | RayMasks.STRUCTURE | RayMasks.VEHICLE, ctx.Caller.Player);
        if (cast.transform == null)
            throw ctx.SendCorrectUsage("/test damage <amount>[%] while looking at a barricade, structure, or vehicle.");

        if (BarricadeManager.FindBarricadeByRootTransform(cast.transform) is { } barricade)
        {
            damage = percent ? (remaining ? barricade.GetServersideData().barricade.health : barricade.asset.health) * (damage / 100f) : damage;
            BarricadeManager.damage(barricade.model, damage, 1f, false, ctx.CallerCSteamID, EDamageOrigin.Unknown);
            ctx.ReplyString($"Damaged barricade {barricade.asset.FriendlyName} by {damage.ToString(ctx.CultureInfo)} points.");
        }
        else if (StructureManager.FindStructureByRootTransform(cast.transform) is { } structure)
        {
            damage = percent ? (remaining ? structure.GetServersideData().structure.health : structure.asset.health) * (damage / 100f) : damage;
            StructureManager.damage(structure.model, ctx.Caller.Player.look.aim.forward, damage, 1f, false, ctx.CallerCSteamID, EDamageOrigin.Unknown);
            ctx.ReplyString($"Damaged structure {structure.asset.FriendlyName} by {damage.ToString(ctx.CultureInfo)} points.");
        }
        else if (cast.vehicle != null)
        {
            damage = percent ? (remaining ? cast.vehicle.health : cast.vehicle.asset.health) * (damage / 100f) : damage;
            VehicleManager.damage(cast.vehicle, damage, 1f, false, ctx.CallerCSteamID, EDamageOrigin.Unknown);
            ctx.ReplyString($"Damaged vehicle {cast.vehicle.asset.FriendlyName} by {damage.ToString(ctx.CultureInfo)} points.");
        }
        else
            throw ctx.SendCorrectUsage("/test damage <amount>[%] while looking at a barricade, structure, or vehicle.");
    }

    private async Task migratebans(CommandInteraction ctx, CancellationToken token)
    {
        ctx.AssertRanByConsole();
        await Migration.MigrateBans(Data.ModerationSql, token).ConfigureAwait(false);
        ctx.ReplyString("Done.");
    }
    private async Task migratebe(CommandInteraction ctx, CancellationToken token)
    {
        ctx.AssertRanByConsole();
        await Migration.MigrateBattlEyeKicks(Data.ModerationSql, token).ConfigureAwait(false);
        ctx.ReplyString("Done.");
    }
    private async Task migratekicks(CommandInteraction ctx, CancellationToken token)
    {
        ctx.AssertRanByConsole();
        await Migration.MigrateKicks(Data.ModerationSql, token).ConfigureAwait(false);
        ctx.ReplyString("Done.");
    }
    private async Task migratemutes(CommandInteraction ctx, CancellationToken token)
    {
        ctx.AssertRanByConsole();
        await Migration.MigrateMutes(Data.ModerationSql, token).ConfigureAwait(false);
        ctx.ReplyString("Done.");
    }
    private async Task migratetks(CommandInteraction ctx, CancellationToken token)
    {
        ctx.AssertRanByConsole();
        await Migration.MigrateTeamkills(Data.ModerationSql, token).ConfigureAwait(false);
        ctx.ReplyString("Done.");
    }
    private async Task migratewarns(CommandInteraction ctx, CancellationToken token)
    {
        ctx.AssertRanByConsole();
        await Migration.MigrateWarnings(Data.ModerationSql, token).ConfigureAwait(false);
        ctx.ReplyString("Done.");
    }

    private void geticons(CommandInteraction ctx)
    {
        ctx.AssertRanByConsole();
        ctx.ReplyString(string.Join(string.Empty, ItemIconProvider.Defaults.Select(x => x.Character).Where(x => x.HasValue).Select(x => x!.Value.ToString())));
    }
    private void fobcooldown(CommandInteraction ctx)
    {
        if (ctx.TryGet(0, out int playerCount))
        {
            ctx.ReplyString($"Cooldown: {Util.ToTimeString(TimeSpan.FromSeconds(CooldownManager.GetFOBDeployCooldown(playerCount)))} for {playerCount} player(s).");
        }
        else
        {
            playerCount = Provider.clients.Count(x => x.GetTeam() is 1 or 2);
            ctx.ReplyString($"Current cooldown: {Util.ToTimeString(TimeSpan.FromSeconds(CooldownManager.GetFOBDeployCooldown(playerCount)))} for {playerCount} player(s)..");
        }
    }
    private void amcdamage(CommandInteraction ctx)
    {
        ctx.AssertRanByPlayer();

        ctx.ReplyString($"Your multiplier is currently {ctx.Caller.GetAMCDamageMultiplier().ToString("F4", ctx.CultureInfo)}.");
    }
    private void nearpos(CommandInteraction ctx)
    {
        ctx.AssertRanByPlayer();
        ctx.AssertPermissions(EAdminType.STAFF);

        ZoneList? zl = Data.Singletons.GetSingleton<ZoneList>();
        if (zl == null)
            throw ctx.SendGamemodeError();

        Zone? targetZone = null;
        if (ctx.TryGetRange(0, out string str))
        {
            SqlItem<Zone>? proxy = zl.SearchZone(str);

            zl.WriteWait();
            try
            {
                if (proxy is { Item: { } zone })
                {
                    targetZone = zone;
                }
            }
            finally
            {
                zl.WriteRelease();
            }
        }

        zl.WriteWait();
        try
        {
            if (targetZone == null)
            {
                Vector2 pos = new Vector2(ctx.Caller.Position.x, ctx.Caller.Position.z);
                SqlItem<Zone>? closest = zl.Items.OrderBy(x => Util.SqrDistance2D(x.Item!.Center, pos)).FirstOrDefault();
                targetZone = closest?.Item;

                if (targetZone == null)
                    throw ctx.ReplyString("No closest zone.");
            }

            Vector3 closestPosition = targetZone.GetClosestPointOnBorder(ctx.Caller.Position);
            F.TriggerEffectReliable(Assets.find<EffectAsset>(new Guid("effb5901fc934c4eaaf0e80ba4c642e3")), ctx.Caller.Connection, closestPosition);
            ctx.ReplyString($"Found nearest point to <b>{targetZone.Name}</b> at: {closestPosition.ToString("F2", ctx.CultureInfo)}.");
        }
        finally
        {
            zl.WriteRelease();
        }
    }

    private async Task migrateusers(CommandInteraction ctx, CancellationToken token)
    {
        ctx.AssertRanByConsole();

        await using WarfareDbContext dbContext = new WarfareDbContext();

        HashSet<ulong> s64s = new HashSet<ulong>(8192);

        foreach (PlayerIPAddress data in await dbContext.IPAddresses.ToListAsync(token))
            s64s.Add(data.Steam64);

        foreach (PlayerHWID data in await dbContext.HWIDs.ToListAsync(token))
            s64s.Add(data.Steam64);

        await Data.AdminSql.QueryAsync($"SELECT `{WarfareSQL.ColumnUsernamesSteam64}` FROM `{WarfareSQL.TableUsernames}` GROUP BY `{WarfareSQL.ColumnUsernamesSteam64}`;", null,
            reader =>
            {
                s64s.Add(reader.GetUInt64(0));
            }, token);

        foreach (KitAccess access in await dbContext.KitAccess.ToListAsync(token))
            s64s.Add(access.Steam64);

        foreach (WarfareUserData data in await dbContext.UserData.ToListAsync(token))
            s64s.Remove(data.Steam64);

        int c = 0;
        foreach (ulong steam64 in s64s)
        {
            ++c;
            if (c % 50 == 0 || c == s64s.Count || c == 1)
                L.LogDebug($"{c} / {s64s.Count}.");
            PlayerNames username = await Data.AdminSql.GetUsernamesAsync(steam64, token).ConfigureAwait(false);
            DateTimeOffset? firstJoined = null;
            DateTimeOffset? lastJoined = null;
            await Data.AdminSql.QueryAsync($"SELECT `{WarfareSQL.ColumnLoginDataFirstLoggedIn}`, `{WarfareSQL.ColumnLoginDataLastLoggedIn}` FROM" +
                                           $"`{WarfareSQL.TableLoginData}` WHERE `{WarfareSQL.ColumnLoginDataSteam64}` = {steam64};", null,
                reader =>
                {
                    firstJoined = reader.IsDBNull(0) ? null : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(0), DateTimeKind.Utc));
                    lastJoined = reader.IsDBNull(1) ? null : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(1), DateTimeKind.Utc));
                }, token);

            EAdminType adminType = Permissions.PermissionSaver.Instance.GetPlayerPermissionLevel(steam64);

            PermissionLevel level = PermissionLevel.Member;
            if ((adminType & EAdminType.ADMIN) != 0)
                level = PermissionLevel.Admin;
            if ((adminType & EAdminType.TRIAL_ADMIN) != 0)
                level = PermissionLevel.TrialAdmin;
            if ((adminType & EAdminType.HELPER) != 0)
                level = PermissionLevel.Helper;
            if ((adminType & (EAdminType.VANILLA_ADMIN | EAdminType.CONSOLE)) != 0)
                level = PermissionLevel.Superuser;

            WarfareUserData data = new WarfareUserData
            {
                PermissionLevel = level,
                FirstJoined = firstJoined,
                LastJoined = lastJoined,
                Steam64 = steam64,
                CharacterName = username.CharacterName.MaxLength(30)!,
                DisplayName = null,
                NickName = username.NickName.MaxLength(30)!,
                PlayerName = username.PlayerName.MaxLength(48)!,
                DiscordId = await Data.AdminSql.GetDiscordID(steam64, token)
            };

            dbContext.UserData.Add(data);
        }

        await dbContext.SaveChangesAsync(CancellationToken.None);
    }

    private async Task dumpkit(CommandInteraction ctx, CancellationToken token)
    {
        ctx.AssertRanByConsole();

        KitManager? km = KitManager.GetSingletonQuick();
        if (km == null)
            throw ctx.SendGamemodeError();

        ctx.AssertArgs(1, "/test dumpkit <name or id ...>");

        Kit? kit;
        if (ctx.TryGet(0, out uint id))
            kit = await km.GetKit(id, token, KitManager.FullSet);
        else
        {
            kit = await km.FindKit(ctx.Get(0)!, token, false);
            if (kit != null)
                kit = await km.GetKit(kit.PrimaryKey, token, KitManager.FullSet);
        }

        if (kit == null)
            throw ctx.ReplyString($"Kit not found: {ctx.Get(0)}.");

        ctx.ReplyString(Environment.NewLine + JsonSerializer.Serialize(kit, JsonEx.serializerSettings));
    }


    private void dumpfactions(CommandInteraction ctx)
    {
        ctx.AssertRanByConsole();

        ctx.ReplyString(Environment.NewLine + JsonSerializer.Serialize(TeamManager.Factions, JsonEx.serializerSettings));
    }
}