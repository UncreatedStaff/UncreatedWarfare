using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DanielWillett.ReflectionTools;
using Microsoft.EntityFrameworkCore;
using Uncreated.Players;
using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Configuration;
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
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Models.Users;
using Uncreated.Warfare.Teams;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;
using XPReward = Uncreated.Warfare.Levels.XPReward;
#if DEBUG
using Uncreated.Warfare.Squads.Commander;
#endif

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace Uncreated.Warfare.Commands;

[SynchronizedCommand, Command("test", aliases: "tests")]
[HelpMetadata(nameof(GetHelpMetadata))]
public class DebugCommand : IExecutableCommand
{
    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    [Ignore]
    public static CommandStructure GetHelpMetadata()
    {
        MethodInfo[] methods = typeof(DebugCommand)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        List<CommandParameter> parameters = new List<CommandParameter>();

        CommandParameter[] args = [ new CommandParameter("Arguments", typeof(object)) { IsRemainder = true } ];

        foreach (MethodInfo method in methods)
        {
            if (method.IsIgnored())
                continue;

            ParameterInfo[] p = method.GetParameters();
            if (p.Length > 1)
                continue;

            if (p.Length == 1 && p[0].ParameterType != typeof(CancellationToken))
                continue;

            if (method.ReturnType != typeof(void) && method.ReturnType != typeof(UniTask))
                continue;

            parameters.Add(new CommandParameter(method.Name)
            {
                Parameters = args
            });
        }

        return new CommandStructure
        {
            Description = "Commands used for development and basic actions not worth a full command.",
            Parameters = parameters.ToArray()
        };
    }

    /// <inheritdoc />
    [Ignore]
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        if (!Context.TryGet(0, out string operation))
            throw Context.SendCorrectUsage("/test <operation> [parameters...]");

        MethodInfo? testFunction;
        try
        {
            testFunction = typeof(DebugCommand).GetMethod(operation,
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
        }
        catch (AmbiguousMatchException)
        {
            throw Context.Reply(T.DebugMultipleMatches, operation);
        }

        if (testFunction == null || testFunction.IsIgnored())
        {
            throw Context.Reply(T.DebugNoMethod, operation);
        }

        ParameterInfo[] parameters = testFunction.GetParameters();

        if (parameters.Length != 0 && (parameters.Length != 1 || parameters[0].ParameterType != typeof(CancellationToken)))
            throw Context.Reply(T.DebugNoMethod, operation);

        try
        {
            Context.ArgumentOffset = 1;

            await Context.AssertPermissions(new PermissionLeaf($"commands.test.{testFunction.Name}", unturned: false, warfare: true));

            await UniTask.SwitchToMainThread();

            object obj = testFunction.Invoke(this, parameters.Length == 1 ? [ token ] : Array.Empty<object>());
            if (obj is UniTask task)
            {
                await task;
                await UniTask.SwitchToMainThread();
            }

            Context.ArgumentOffset = 0;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is CommandContext)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        }
        catch (CommandContext)
        {
            throw;
        }
        catch (Exception ex)
        {
            L.LogError(ex.InnerException ?? ex);
            throw Context.Reply(T.DebugErrorExecuting, testFunction.Name, (ex.InnerException ?? ex).GetType().Name);
        }
    }
#pragma warning disable IDE1006
#pragma warning disable IDE0051
    private const string UsageGiveXp = "/test givexp <player> <amount> [team - required if offline]";
    private async UniTask givexp(CancellationToken token)
    {
        if (!Context.HasArgs(2))
        {
            Context.SendCorrectUsage(UsageGiveXp);
            return;
        }
        if (Context.TryGet(1, out int amount))
        {
            if (Context.TryGet(0, out ulong player, out UCPlayer? onlinePlayer))
            {
                Context.TryGet(2, out ulong team);
                if (onlinePlayer is null)
                {
                    if (PlayerSave.HasPlayerSave(player))
                    {
                        if (team is > 0 and < 3)
                        {
                            await Data.DatabaseManager.AddXP(player, team, amount, token).ConfigureAwait(false);
                            PlayerNames name = await F.GetPlayerOriginalNamesAsync(player, token).ConfigureAwait(false);
                            await UniTask.SwitchToMainThread(token);

                            Context.ReplyString($"Given <#fff>{amount}</color> <#ff9b01>XP</color> to {(Context.IsConsole ? name.PlayerName : name.CharacterName)}.");
                        }
                        else
                            Context.SendCorrectUsage(UsageGiveXp);
                    }
                    else
                        Context.Reply(T.PlayerNotFound);
                }
                else
                {
                    if (team is < 1 or > 2)
                        team = onlinePlayer.GetTeam();
                    await XPParameters
                        .WithTranslation(onlinePlayer, team, Context.IsConsole ? T.XPToastFromOperator : T.XPToastFromPlayer, XPReward.Custom, amount)
                        .Award(token)
                        .ConfigureAwait(false);
                    await UniTask.SwitchToMainThread(token);
                    Context.ReplyString($"Given <#fff>{amount}</color> <#ff9b01>XP</color> " +
                                    $"to {(Context.IsConsole ? onlinePlayer.Name.PlayerName : onlinePlayer.Name.CharacterName)}.");
                }
            }
            else
                Context.SendCorrectUsage(UsageGiveXp);
        }
        else
            Context.ReplyString($"Couldn't parse {Context.Get(1)!} as a number.");
    }
    private const string UsageGiveCredits = "/test givecredits <player> <amount> [team - required if offline]";
    private async UniTask givecredits(CancellationToken token)
    {
        if (!Context.HasArgs(2))
        {
            Context.SendCorrectUsage(UsageGiveCredits);
            return;
        }
        if (Context.TryGet(1, out int amount))
        {
            if (Context.TryGet(0, out ulong player, out UCPlayer? onlinePlayer))
            {
                Context.TryGet(2, out ulong team);
                if (onlinePlayer is null)
                {
                    if (PlayerSave.HasPlayerSave(player))
                    {
                        if (team is > 0 and < 3)
                        {
                            await Data.DatabaseManager.AddCredits(player, team, amount, token).ConfigureAwait(false);
                            PlayerNames name = await Data.DatabaseManager.GetUsernamesAsync(player, token).ConfigureAwait(false);
                            await UniTask.SwitchToMainThread(token);
                            Context.ReplyString($"Given <#{UCWarfare.GetColorHex("credits")}>C</color> <#fff>{amount}</color> to {(Context.IsConsole ? name.PlayerName : name.CharacterName)}.");
                        }
                        else
                            Context.SendCorrectUsage(UsageGiveCredits);
                    }
                    else
                        Context.Reply(T.PlayerNotFound);
                }
                else
                {
                    if (team is < 1 or > 2)
                        team = onlinePlayer.GetTeam();
                    await Points.AwardCreditsAsync(onlinePlayer, amount, Context.IsConsole ? T.XPToastFromOperator : T.XPToastFromPlayer, token: token).ConfigureAwait(false);
                    await UniTask.SwitchToMainThread(token);
                    Context.ReplyString($"Given <#{UCWarfare.GetColorHex("credits")}>C</color> <#fff>{amount}</color> " +
                                    $"to {(Context.IsConsole ? onlinePlayer.Name.PlayerName : onlinePlayer.Name.CharacterName)}.");
                }
            }
            else
                Context.SendCorrectUsage(UsageGiveCredits);
        }
        else
            Context.ReplyString($"Couldn't parse {Context.Get(1)!} as a number.");
    }

    private void quickcap()
    {
        Context.AssertRanByPlayer();
        Context.AssertGamemode(out IFlagRotation fg);

        Flag? flag = fg.Rotation.FirstOrDefault(f => f.PlayersOnFlag.Contains(Context.Caller));
        if (flag == default)
        {
            Context.Reply(T.ZoneNoResultsLocation);
            return;
        }

        ulong team = Context.Player.GetTeam();
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
            Context.ReplyString("Flag quick-capped.");
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
            Context.ReplyString("Flag quick-capped.");
        }
        else Context.SendGamemodeError();
    }

    private void quickwin()
    {
        ulong team;
        if (Context.TryGet(0, out ulong id))
            team = id;
        else if (!Context.IsConsole)
            team = Context.Player.GetTeam();
        else
        {
            Context.Reply(T.NotOnCaptureTeam);
            return;
        }
        _ = UCWarfare.RunTask(Data.Gamemode.DeclareWin, team, ctx: "/test quickwin executed for " + team + ".");
        Context.Defer();
    }

    private void zone()
    {
        Context.AssertRanByPlayer();

        Vector3 pos = Context.Player.Position;
        if (pos == Vector3.zero) return;
        Flag? flag = Data.Is(out IFlagRotation fg) ? fg.Rotation.FirstOrDefault(f => f.PlayerInRange(pos)) : null;
        string txt = $"Position: <#ff9999>({pos.x.ToString("0.##", Context.Culture)}, {pos.y.ToString("0.##", Context.Culture)}, {pos.z.ToString("0.##", Context.Culture)}) @ {new GridLocation(Context.Player.Position)}</color>. " +
                     $"Yaw: <#ff9999>{Context.Player.Player.transform.eulerAngles.y.ToString("0.##", Context.Culture)}°</color>.";
        if (flag is null)
            Context.ReplyString(txt);
        else
            Context.ReplyString(txt + " Current Flag: <#{flag.TeamSpecificHexColor}>{flag.Name}</color>");
    }

    private void sign()
    {
        Context.AssertRanByPlayer();

        InteractableSign? sign = UCBarricadeManager.GetInteractableFromLook<InteractableSign>(Context.Player.Player.look);
        if (sign == null) Context.ReplyString("You're not looking at a sign");
        else
        {
            if (!Context.IsConsole)
                Context.ReplyString("Sign text: \"" + sign.text + "\".");
            else
                Context.Defer();
            L.Log("Sign Text: \n" + sign.text + "\nEND", ConsoleColor.Green);
        }
    }

    private void time()
    {
        UCWarfare.I.CoroutineTiming = !UCWarfare.I.CoroutineTiming;
        Context.ReplyString("Coroutine timing state: " + UCWarfare.I.CoroutineTiming.ToString());
    }

    // test zones: test zonearea all true false false true false
    private const string UsageZoneArea = "Syntax: /test zonearea [<selection: active|all> <extra-zones> <path> <range> <fill> <drawAngles>]";
    private void zonearea()
    {
        Context.AssertGamemode<IFlagRotation>();

        UCWarfare.I.StartCoroutine(ZoneDrawing.CreateFlagOverlay(Context, openOutput: true));
        Context.Defer();
    }

    private void rotation()
    {
        Context.AssertGamemode(out FlagGamemode fg);
        fg.PrintFlagRotation();
    }
    
    private void down()
    {
        Context.AssertGamemode(out IRevives revive);

        if (Context.TryGet(0, out _, out UCPlayer? player))
        {
            if (player is null)
                throw Context.SendPlayerNotFound();

            Context.AssertPermissions(new PermissionLeaf("warfare::commands.test.down.others"));
        }
        else
        {
            Context.AssertRanByPlayer();
            Context.AssertPermissions(new PermissionLeaf("warfare::commands.test.down.self"));
            player = Context.Player;
        }

        DamagePlayerParameters p = new DamagePlayerParameters(player.Player)
        {
            cause = EDeathCause.KILL,
            applyGlobalArmorMultiplier = false,
            respectArmor = false,
            damage = 101,
            limb = ELimb.SPINE,
            direction = Vector3.down,
            killer = player == Context.Player ? Steamworks.CSteamID.Nil : player.CSteamID,
            times = 1f
        };

        revive.ReviveManager.InjurePlayer(in p, player == Context.Caller ? null : player);
        Context.ReplyString($"Injured {(player == Context.Caller ? "you" : player.CharacterName)}.");
    }

    private void clearui()
    {
        Context.AssertRanByPlayer();

        if (Context.Player.HasUIHidden)
        {
            UCWarfare.I.UpdateLangs(Context.Player, true);
        }
        else
        {
            Data.HideAllUI(Context.Player);
        }
        Context.Player.HasUIHidden = !Context.Player.HasUIHidden;
        Context.ReplyString("<#a4a5b3>UI " + (Context.Player.HasUIHidden ? "hidden." : "visible."));
    }

    private void game()
    {
        if (Data.Gamemode is not null)
            Context.ReplyString(Data.Gamemode.DumpState());
        else
            Context.SendGamemodeError();
    }
    
    private const string UsagePlayerSave = "/test playersave <player> <property> <value>";
    private async UniTask playersave(CancellationToken token)
    {
        if (!Context.HasArgs(3))
        {
            Context.SendCorrectUsage(UsagePlayerSave);
            return;
        }

        if (!Context.TryGet(0, out ulong player, out _))
        {
            throw Context.SendCorrectUsage(UsagePlayerSave);
        }

        if (!PlayerManager.HasSave(player, out PlayerSave save))
        {
            throw Context.ReplyString("This player hasn't joined the server yet.");
        }

        if (!Context.TryGet(2, out string value) || !Context.TryGet(1, out string property))
        {
            throw Context.SendCorrectUsage(UsagePlayerSave);
        }

        SetPropertyResult result = Context.SetProperty(save, property, value, out property, out Type propertyType);
        switch (result)
        {
            case SetPropertyResult.Success:
                PlayerNames names = await F.GetPlayerOriginalNamesAsync(player, token).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(token);
                Context.ReplyString($"Set {property} in {(Context.IsConsole ? names.PlayerName : names.CharacterName)}'s playersave to {value}.");
                break;

            case SetPropertyResult.PropertyNotFound:
                Context.ReplyString($"Couldn't find a field by the name {property} in PlayerSave.");
                break;

            case SetPropertyResult.PropertyProtected:
                Context.ReplyString($"Unable to set {property}, it's missing the JsonSettable attribute.");
                break;

            case SetPropertyResult.TypeNotSettable:
            case SetPropertyResult.ParseFailure:
                Context.ReplyString($"Couldn't parse {value} as a valid value for {property} ({Accessor.ExceptionFormatter.Format(propertyType)}).");
                break;

            default:
                Context.SendUnknownError();
                break;
        }
    }

    private const string UsageGamemode = "/test gamemode <gamemode>";
    private void gamemode(CancellationToken token)
    {
        if (!Context.HasArgs(1))
        {
            Context.SendCorrectUsage(UsageGamemode);
            return;
        }

        if (!Context.TryGet(0, out string gamemodeName))
        {
            throw Context.SendCorrectUsage(UsageGamemode);
        }

        Type? newGamemode = Gamemode.FindGamemode(gamemodeName);
        if (newGamemode is not null)
        {
            if (Data.Is(out IStagingPhase gm) && gm.State == State.Staging)
            {
                Context.ReplyString("Skipped staging phase.");
                gm.SkipStagingPhase();
            }
            else
                Context.Defer();

            UCWarfare.RunTask(async token =>
            {
                await UniTask.SwitchToMainThread(token);
                if (newGamemode == Data.Gamemode?.GetType())
                {
                    await Data.Singletons.ReloadSingletonAsync(Gamemode.GamemodeReloadKey, token);
                    await UniTask.SwitchToMainThread(token);
                    Context.ReplyString($"Successfully reloaded {newGamemode.Name}.");
                }
                else if (await Gamemode.TryLoadGamemode(newGamemode, false, default))
                {
                    Context.LogAction(ActionLogType.ChangeGamemodeCommand, Data.Gamemode!.DisplayName);
                    await UniTask.SwitchToMainThread(token);
                    Context.ReplyString($"Successfully loaded {newGamemode.Name}.");
                }
                else
                {
                    await UniTask.SwitchToMainThread(token);
                    Context.ReplyString($"Failed to load {newGamemode.Name}.");
                }
            }, token, ctx: "/test gamemode executed for type " + newGamemode.Name + ".");
        }
        else
            Context.ReplyString($"Gamemode not found: {gamemodeName}.");
    }

    private void skipstaging()
    {
        Context.AssertGamemode(out IStagingPhase gm);

        if (gm.State == State.Staging)
        {
            gm.SkipStagingPhase();
            Context.ReplyString("Skipped staging phase.");
        }
        else
        {
            Context.ReplyString("Staging phase is not active.");
        }
    }

    private void resetlobby()
    {
        Context.AssertGamemode(out ITeams t);
        if (t.UseTeamSelector)
        {
            if (!Context.HasArgs(1))
            {
                Context.SendCorrectUsage("/resetlobby <player>");
                return;
            }
            if (Context.TryGet(0, out _, out UCPlayer? player) && player is not null)
            {
                t.TeamSelector?.ResetState(player);
                PlayerNames name = player.Name;
                Context.ReplyString($"Reset lobby for {(Context.IsConsole ? name.PlayerName : name.CharacterName)}.");
            }
            else
            {
                Context.SendPlayerNotFound();
            }
        }
        else Context.SendGamemodeError();
    }

    private void clearcooldowns()
    {
        Context.AssertRanByPlayer();
        if (Data.Gamemode?.Cooldowns is null) throw Context.SendNotImplemented();

        if (!Context.TryGet(1, out _, out UCPlayer? pl) || pl is null)
            pl = Context.Player;
        CooldownManager.RemoveCooldown(pl);
    }

    private void instid()
    {
        Context.AssertRanByPlayer();

        Player player = Context.Player.Player;
        if (!Context.TryGetTargetInfo(out RaycastInfo? raycast, RayMasks.BARRICADE | RayMasks.STRUCTURE | RayMasks.LARGE | RayMasks.MEDIUM | RayMasks.SMALL | RayMasks.VEHICLE))
        {
            throw Context.ReplyString("You must be looking at a barricade, structure, vehicle, or object.");
        }

        if (raycast.vehicle != null)
        {
            Context.ReplyString($"Vehicle {raycast.vehicle.asset.vehicleName}: #{raycast.vehicle.instanceID.ToString(Context.Culture)}");
            return;
        }

        if (raycast.player != null)
        {
            Context.ReplyString($"Player {raycast.player.channel.owner.playerID.playerName}: #{raycast.player.channel.owner.playerID.steamID.m_SteamID.ToString(Context.Culture)} (@ {raycast.limb})");
            return;
        }

        BarricadeDrop? bd = BarricadeManager.FindBarricadeByRootTransform(raycast.transform);
        if (bd != null)
        {
            Context.ReplyString($"Barricade {bd.asset.itemName}: #{bd.instanceID.ToString(Context.Culture)}");
            return;
        }

        StructureDrop? dp = StructureManager.FindStructureByRootTransform(raycast.transform);
        if (dp != null)
        {
            Context.ReplyString($"Structure {dp.asset.itemName}: #{dp.instanceID.ToString(Context.Culture)}");
            return;
        }

        if (!ObjectManager.tryGetRegion(raycast.transform, out byte x, out byte y, out ushort index))
            return;
        
        LevelObject obj = LevelObjects.objects[x, y][index];
        Context.ReplyString($"Level object {obj.asset.objectName} ({obj.asset.name}, {obj.asset.GUID:N}): #{obj.instanceID.ToString(Context.Culture)}");
    }

    private void questdump()
    {
        Context.AssertRanByPlayer();

        QuestManager.PrintAllQuests(Context.Player);
    }

    private void completequest()
    {
        Context.AssertRanByPlayer();

        if (Context.TryGet(0, out QuestType type))
        {
            for (int i = 0; i < QuestManager.RegisteredTrackers.Count; i++)
            {
                BaseQuestTracker tracker = QuestManager.RegisteredTrackers[i];
                if (tracker.Player!.Steam64 != Context.CallerId.m_SteamID || tracker.QuestData?.QuestType != type)
                    continue;
                
                tracker.ManualComplete();
                break;
            }
        }
        else if (Context.TryGet(0, out Guid key))
        {
            for (int i = 0; i < QuestManager.RegisteredTrackers.Count; i++)
            {
                BaseQuestTracker tracker = QuestManager.RegisteredTrackers[i];
                if (tracker.Player!.Steam64 != Context.CallerId.m_SteamID || tracker.PresetKey != key)
                    continue;

                tracker.ManualComplete();
                break;
            }
        }
        else
        {
            Context.SendCorrectUsage("/test completequest <QuestType or Guid>");
        }
    }

    private void setsign()
    {
        Context.AssertRanByPlayer();

        if (!Context.TryGetRange(0, out string? text) || !Context.TryGetTargetTransform(out BarricadeDrop? drop) || drop.interactable is not InteractableSign sign)
        {
            throw Context.ReplyString("You must be looking at a sign.");
        }
        
        BarricadeManager.ServerSetSignText(sign, text.Replace("\\n", "\n"));
        Signs.BroadcastSignUpdate(drop);
        // todo save structure
    }

    private void resetdebug()
    {
        if (UCWarfare.I.Debugger != null)
        {
            UCWarfare.I.Debugger.Reset();
            Context.ReplyString("Reset debugger");
        }
        else
        {
            Context.ReplyString("Debugger is not active.");
        }
    }

    private void quest()
    {
        Context.AssertRanByPlayer();

        if (Context.MatchParameter(0, "add"))
        {
            if (Context.TryGet(1, out QuestAsset? asset, out _, true, -1, false))
            {
                QuestManager.TryAddQuest(Context.Player, asset);
                Context.ReplyString("<#9fa1a6>Added quest " + asset.questName + " <#ddd>(" + asset.id + ", " + asset.GUID.ToString("N") + ")</color>.");
            }
            else Context.ReplyString("<#ff8c69>Quest not found.");
        }
        else if (Context.MatchParameter(0, "track"))
        {
            if (Context.TryGet(1, out QuestAsset? asset, out _, true, -1, false))
            {
                Context.Player.ServerTrackQuest(asset);
                Context.ReplyString("<#9fa1a6>Tracked quest " + asset.questName + " <#ddd>(" + asset.id + ", " + asset.GUID.ToString("N") + ")</color>.");
            }
            else Context.ReplyString("<#ff8c69>Quest not found.");
        }
        else if (Context.MatchParameter(0, "remove"))
        {
            if (Context.TryGet(1, out QuestAsset? asset, out _, true, -1, false))
            {
                Context.Player.Player.quests.ServerRemoveQuest(asset);
                Context.ReplyString("<#9fa1a6>Removed quest " + asset.questName + " <#ddd>(" + asset.id + ", " + asset.GUID.ToString("N") + ")</color>.");
            }
            else Context.ReplyString("<#ff8c69>Quest not found.");
        }
        else Context.SendCorrectUsage("/test quest <add|track|remove> <id>");
    }

    private void flag()
    {
        Context.AssertRanByPlayer();

        if (Context.MatchParameter(0, "set"))
        {
            if (Context.TryGet(2, out short value) && Context.TryGet(1, out ushort flag))
            {
                bool hasFlag = Context.Player.Player.quests.getFlag(flag, out short old);
                Context.Player.Player.quests.sendSetFlag(flag, value);
                Context.ReplyString($"Set quest flag {flag} to {value} <#ddd>(from {(hasFlag ? old.ToString() : "<b>not set</b>")})</color>.");
                return;
            }
        }
        else if (Context.MatchParameter(0, "get"))
        {
            if (Context.TryGet(1, out ushort flag))
            {
                bool hasFlag = Context.Player.Player.quests.getFlag(flag, out short val);
                Context.ReplyString($"Quest flag {flag} is {(hasFlag ? val.ToString() : "<b>not set</b>")})</color>.");
                return;
            }
        }
        else if (Context.MatchParameter(0, "remove", "delete"))
        {
            if (Context.TryGet(1, out ushort flag))
            {
                bool hasFlag = Context.Player.Player.quests.getFlag(flag, out short old);
                if (hasFlag)
                {
                    Context.Player.Player.quests.sendRemoveFlag(flag);
                    Context.ReplyString($"Quest flag {flag} was removed <#ddd>(from {old})</color>.");
                }
                else Context.ReplyString($"Quest flag {flag} is not set.");
                return;
            }
        }

        Context.ReplyString("Syntax: /test flag <set|get|remove> <flag> [value]");
    }

    private void findasset()
    {
        if (Context.TryGet(0, out Guid guid))
        {
            Context.ReplyString("<#9fa1a6>Asset: " + (Assets.find(guid)?.FriendlyName ?? "null"));
        }
        else if (Context.TryGet(0, out ushort us) && Context.TryGet(1, out EAssetType type))
        {
            Context.ReplyString("<#9fa1a6>Asset: " + (Assets.find(type, us)?.FriendlyName ?? "null"));
        }
        else
        {
            Context.ReplyString("<#ff8c69>Please use a <GUID> or <uhort, type>");
        }
    }

    private void traits()
    {
        Context.AssertGamemode<ITraits>();

        Context.AssertRanByPlayer();

        L.Log("Traits: ");
        using (L.IndentLog(1))
        {
            for (int i = 0; i < Context.Player.ActiveTraits.Count; ++i)
            {
                Traits.Trait t = Context.Player.ActiveTraits[i];
                L.Log(t.Data.TypeName + " (" + (Time.realtimeSinceStartup - t.StartTime) + " seconds active)");
            }
        }

        L.Log("Buff UI:");
        L.Log("[ " + (Context.Player.ActiveBuffs[0]?.GetType().Name ?? "null") +
              ", " + (Context.Player.ActiveBuffs[1]?.GetType().Name ?? "null") +
              ", " + (Context.Player.ActiveBuffs[2]?.GetType().Name ?? "null") +
              ", " + (Context.Player.ActiveBuffs[3]?.GetType().Name ?? "null") +
              ", " + (Context.Player.ActiveBuffs[4]?.GetType().Name ?? "null") +
              ", " + (Context.Player.ActiveBuffs[5]?.GetType().Name ?? "null") +
              ", " + (Context.Player.ActiveBuffs[6]?.GetType().Name ?? "null") +
              ", " + (Context.Player.ActiveBuffs[7]?.GetType().Name ?? "null") +
              " ]");
    }

    private void advancedelays()
    {
        if (Context.TryGet(0, out float seconds))
        {
            Data.Gamemode.AdvanceDelays(seconds);
            Context.ReplyString($"Advanced delays by {seconds.ToString("0.##", Context.Culture)} seconds.");
        }
        else
            Context.SendCorrectUsage("/test advancedelays <seconds>.");
    }

    private void listcooldowns()
    {
        Context.AssertRanByPlayer();
        L.Log("Cooldowns for " + Context.Player.Name.PlayerName + ":");
        using IDisposable indent = L.IndentLog(1);
        foreach (Cooldown cooldown in CooldownManager.Singleton.Cooldowns.Where(x => x.Player.Steam64 == Context.Player.Steam64))
        {
            L.Log($"{cooldown.CooldownType}: {cooldown.Timeleft.ToString(@"hh\:mm\:ss", Context.Culture)}, {(cooldown.Parameters is null || cooldown.Parameters.Length == 0 ? "NO DATA" : string.Join(";", cooldown.Parameters))}");
        }
    }

#if DEBUG
    private void giveuav()
    {
        Context.AssertRanByPlayer();

        bool isMarker = Context.Player.Player.quests.isMarkerPlaced;
        Vector3 pos = isMarker ? Context.Player.Player.quests.markerPosition : Context.Player.Player.transform.position;
        pos = pos with { y = Mathf.Min(Level.HEIGHT, F.GetHeight(pos, 0f) + UAV.GroundHeightOffset) };
        UAV.GiveUAV(Context.Player.GetTeam(), Context.Player, Context.Player, isMarker, pos);
        Context.Defer();
    }

    private async UniTask requestuav(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        await UAV.RequestUAV(Context.Player, token);
        Context.Defer();
    }

    private async UniTask testfield(CancellationToken token)
    {
        Context.AssertRanByPlayer();
        Context.AssertGamemode(out IVehicles vehicles);

        ulong other = TeamManager.Other(Context.Player.GetTeam());
        //FactionInfo? other = TeamManager.GetFactionSafe(oteam);
        if (other is not 1ul and not 2ul)
            throw Context.Reply(T.NotOnCaptureTeam);
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

        Vector3 st = Context.Player.Position;
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
                    await UniTask.SwitchToMainThread(token);
                }

                await UniTask.Delay(25, cancellationToken: token);
            }
        }

        Context.ReplyString("Spawned " + (sections * sections * 4) + " vehicles.");
    }
#endif

    private async UniTask squad(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        KitManager? manager = KitManager.GetSingletonQuick();
        ulong team = Context.Player.GetTeam();
        if (manager != null)
        {
            Kit? sql = await manager.GetRecommendedSquadleaderKit(team, token).ConfigureAwait(false);
            if (sql == null)
                Context.SendUnknownError();
            else
            {
                sql = await manager.GetKit(sql.PrimaryKey, token, x => KitManager.RequestableSet(x, false));
                await manager.Requests.GiveKit(Context.Player, sql, true, false, token).ConfigureAwait(false);
            }
        }

        await UniTask.SwitchToMainThread(token);
        if (Context.Player.Squad is null || Context.Player.Squad.Leader != Context.Player)
        {
            SquadManager.CreateSquad(Context.Player, team);
        }

        if (Data.Gamemode.State == State.Staging)
        {
            Data.Gamemode.SkipStagingPhase();
        }

        if (Data.Is(out IVehicles vehicleGm))
        {
            SqlItem<VehicleSpawn>? vehicle = VehicleBayCommand.GetBayTarget(Context, vehicleGm.VehicleSpawner);
            if (vehicle?.Item is null)
            {
                Context.ReplyString("No logistics vehicle nearby to request.", Color.red);
            }
            else if (vehicle.Item.HasLinkedVehicle(out InteractableVehicle veh))
            {
                SqlItem<VehicleData>? data = vehicle.Item.Vehicle;
                if (data?.Item == null)
                {
                    Context.ReplyString("Failed to get a logistics vehicle.", Color.red);
                }
                else
                {
                    await data.Enter(token).ConfigureAwait(false);
                    try
                    {
                        await UniTask.SwitchToMainThread(token);
                        RequestCommand.GiveVehicle(Context.Player, veh, data.Item);
                    }
                    finally
                    {
                        data.Release();
                    }
                }
            }
            else
                Context.ReplyString("Logistics vehicle not available.", Color.red);
        }

        Context.Defer();
    }

    private void effect()
    {
        Context.AssertRanByPlayer();

        const string usage = "/test effect [clear] <name/id/guid> (for UI only - [key : int16] [arg0 : str] [arg1 : str] [arg2 : str] [arg3 : str] )";
        Context.AssertHelpCheck(0, usage);
        Context.AssertArgs(0, usage);
        EffectAsset? asset;
        if (Context.MatchParameter(0, "clear", "remove", "delete"))
        {
            Context.AssertArgs(1, usage);
            if (Context.MatchParameter(1, "all", "*", "any"))
                asset = null;
            else if (!Context.TryGet(1, out asset, out _, allowMultipleResults: true))
                throw Context.ReplyString($"<#ff8c69>Can't find an effect with the term: <#ddd>{Context.Get(1)}</color>.");
            if (asset == null)
            {
                Data.HideAllUI(Context.Player);
                throw Context.ReplyString("<#9fa1a6>Cleared all effects.");
            }

            EffectManager.ClearEffectByGuid(asset.GUID, Context.Player.Connection);
            throw Context.ReplyString($"<#9fa1a6>Cleared all {asset.name} effects.");
        }

        if (!Context.TryGet(0, out asset, out _, allowMultipleResults: true))
            throw Context.ReplyString($"<#ff8c69>Can't find an effect with the term: <#ddd>{Context.Get(0)}</color>.");

        short key = Context.MatchParameter(1, "-", "_") || !Context.TryGet(1, out short s) ? (short)0 : s;
        if (asset?.effect == null)
        {
            throw Context.ReplyString($"<#ff8c69>{asset?.name}'s effect property hasn't been set. Possibly the effect was set up incorrectly.");
        }

        if (asset.effect.CompareTag("UI"))
        {
            switch (Context.ArgumentCount)
            {
                case 3:
                    EffectManager.sendUIEffect(asset.id, key, Context.Player.Connection, true, Context.Get(2));
                    throw Context.ReplyString($"<#9fa1a6>Sent {asset.name} to you with {{0}} = \"{Context.Get(2)}\".");
                case 4:
                    EffectManager.sendUIEffect(asset.id, key, Context.Player.Connection, true, Context.Get(2), Context.Get(3));
                    throw Context.ReplyString($"<#9fa1a6>Sent {asset.name} to you with {{0}} = \"{Context.Get(2)}\", {{1}} = \"{Context.Get(3)}\".");
                case 5:
                    EffectManager.sendUIEffect(asset.id, key, Context.Player.Connection, true, Context.Get(2), Context.Get(3), Context.Get(4));
                    throw Context.ReplyString($"<#9fa1a6>Sent {asset.name} to you with {{0}} = \"{Context.Get(2)}\", {{1}} = \"{Context.Get(3)}\", {{2}} = \"{Context.Get(4)}\".");
                default:
                    if (Context.ArgumentCount < 3)
                    {
                        EffectManager.sendUIEffect(asset.id, key, Context.Player.Connection, true);
                        throw Context.ReplyString($"<#9fa1a6>Sent {asset.name} to you with no arguments.");
                    }
                    EffectManager.sendUIEffect(asset.id, key, Context.Player.Connection, true, Context.Get(2), Context.Get(3), Context.Get(4), Context.Get(5));
                    throw Context.ReplyString($"<#9fa1a6>Sent {asset.name} to you with {{0}} = \"{Context.Get(2)}\", {{1}} = \"{Context.Get(3)}\", {{2}} = \"{Context.Get(4)}\", {{3}} = \"{Context.Get(5)}\".");
            }
        }

        F.TriggerEffectReliable(asset, Context.Player.Connection, Context.Player.Position);
        throw Context.ReplyString($"<#9fa1a6>Sent {asset.name} to you at {Context.Player.Position.ToString("0.##", Context.Culture)}." + (Context.HasArgs(2) ? " To spawn as UI instead, the effect must have the \"UI\" tag in unity." : string.Empty));
    }

    private void translate()
    {
        if (!Context.TryGet(0, out string name))
            return;

        Context.TryGet(1, out bool imgui);

        if (T.Translations.FirstOrDefault(x => x.Key.Equals(name, StringComparison.Ordinal)) is { } translation)
        {
            string val = translation.Translate(Context.Caller?.Locale.LanguageInfo, out Color color, Context.HasArg(1) ? imgui : (Context.Caller?.Save.IMGUI ?? false));
            L.Log($"Translation: {translation.Id}... {name}.");
            L.Log($"Type: {translation.GetType()}");
            L.Log($"Args: {string.Join(", ", translation.GetType().GetGenericArguments().Select(x => x.Name))}");
            L.Log( "Color: " + color.ToString("F2"));
            L.Log( "Value: \"" + val + "\".");
            L.Log("Dump:");
            using IDisposable indent = L.IndentLog(1);
            translation.Dump();
            Context.ReplyString(val, color);
        }
        else Context.ReplyString(name + " not found.");
    }
    private void nerd()
    {
        if (!Context.TryGet(0, out ulong s64, out UCPlayer? onlinePlayer))
        {
            throw Context.SendCorrectUsage("/test nerd <player>");
        }

        if (s64 == 76561198267927009ul)
        {
            s64 = Context.CallerId.m_SteamID;
            onlinePlayer = Context.Player;
        }

        if (!UCWarfare.Config.Nerds.Contains(s64))
        {
            UCWarfare.Config.Nerds.Add(s64);
            UCWarfare.SaveSystemConfig();
        }

        Context.ReplyString($"{onlinePlayer?.ToString() ?? s64.ToString()} is now a nerd.");
    }
    private void unnerd()
    {
        if (!Context.TryGet(0, out ulong s64, out UCPlayer? onlinePlayer))
            throw Context.SendCorrectUsage("/test unnerd <player>");
        
        if (UCWarfare.Config.Nerds.RemoveAll(x => x == s64) > 0)
        {
            UCWarfare.SaveSystemConfig();
            Context.ReplyString($"{onlinePlayer?.ToString() ?? s64.ToString()} is not a nerd.");
        }
        else
        {
            Context.ReplyString($"{onlinePlayer?.ToString() ?? s64.ToString()} was not a nerd.");
        }
    }
    private void findassets()
    {
        Context.AssertRanByConsole();
        Context.AssertHelpCheck(0, "/test findassets <type>.<field/property> <value> - List assets by their value");
        Context.AssertArgs(2, "/test findassets <type>.<field/property> <value>");

        string propertyName = Context.Get(0)!;
        string valueStr = Context.GetRange(1)!;

        Type type = typeof(Asset);
        int typeSeparatorIndex = propertyName.IndexOf('.');
        if (typeSeparatorIndex != -1)
        {
            string tStr = propertyName.Substring(0, typeSeparatorIndex);
            type = typeof(Provider).Assembly.GetType("SDG.Unturned." + tStr, false, true)!;
            if (type == null || !typeof(Asset).IsAssignableFrom(type))
            {
                L.LogWarning("Unable to find type: SDG.Unturned." + tStr + ".");
                type = typeof(Asset);
            }
            else
            {
                propertyName = propertyName.Substring(typeSeparatorIndex + 1);
            }
        } 

        if (!Variables.TryFind(type, propertyName, out IVariable? variable, true, Accessor.Active))
        {
            throw Context.ReplyString($"<#ff8c69>Variable not found: <#fff>{type.Name}</color>.<#fff>{propertyName}</color>.", ConsoleColor.DarkYellow);
        }

        Type fldType = variable!.MemberType;

        if (!FormattingUtility.TryParseAny(valueStr, Context.Culture, fldType, out object? val))
        {
            throw Context.ReplyString($"<#ff8c69>Failed to parse <#fff>{valueStr}</color> as <#fff>{fldType.Name}</color>.", ConsoleColor.DarkYellow);
        }

        MethodInfo? findAssetsFunction = typeof(Assets).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                                        .FirstOrDefault(x => x.Name.Equals("find", StringComparison.Ordinal) &&
                                                        x.IsGenericMethod &&
                                                        x.GetParameters().FirstOrDefault()?.ParameterType is { IsGenericType: true } ptype &&
                                                        ptype.GetGenericTypeDefinition() == typeof(List<>))?.MakeGenericMethod(type);

        IList allAssets = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(type));
        findAssetsFunction!.Invoke(null, [ allAssets ]);

        int c = 0;
        foreach (Asset asset in allAssets.Cast<Asset>().Where(val is null ? x => variable.GetValue(x) == null : x => val.Equals(variable.GetValue(x))))
        {
            L.Log(ActionLog.AsAsset(asset), ConsoleColor.DarkCyan);
            ++c;
        }

        Context.ReplyString($"Found {c} asset{(c == 1 ? string.Empty : "s")}.", ConsoleColor.DarkCyan);
    }

    private void hardpointadv()
    {
        Context.AssertGamemode(out Hardpoint hardpoint);
        Context.AssertOnDuty();

        hardpoint.ForceNextObjective();
        Context.Defer();
    }

    private void setholiday()
    {
        Context.AssertRanByConsole();

        if (!Context.TryGet(0, out ENPCHoliday holiday))
            throw Context.ReplyString("Invalid holiday. Must be field in ENPCHoliday.");
        
        FieldInfo? field = typeof(HolidayUtil).GetField("holidayOverride", BindingFlags.Static | BindingFlags.NonPublic);
        if (field == null)
            throw Context.ReplyString("Unable to find 'HolidayUtil.holidayOverride' field.");

        field.SetValue(null, holiday);
        Context.ReplyString("Set active holiday to " + Localization.TranslateEnum(holiday, Context.Language));

        field = typeof(Provider).GetField("authorityHoliday", BindingFlags.Static | BindingFlags.NonPublic);
        if (holiday == ENPCHoliday.NONE)
        {
            MethodInfo? method = typeof(HolidayUtil).GetMethod("BackendGetActiveHoliday", BindingFlags.Static | BindingFlags.NonPublic);
            if (method != null)
                holiday = (ENPCHoliday)method.Invoke(null, Array.Empty<object>());
        }
                
        field?.SetValue(null, holiday);
    }
    private void startloadout()
    {
        Context.AssertRanByPlayer();

        Context.AssertOnDuty();
        
        if (!Context.TryGet(0, out Class @class))
            throw Context.SendCorrectUsage("/test startloadout <class>");

        IKitItem[] items = KitDefaults<WarfareDbContext>.GetDefaultLoadoutItems(@class);
        L.LogDebug($"Found {items.Length} item{(items.Length == 1 ? string.Empty : "s")}.");
        
        UCInventoryManager.GiveItems(Context.Player, items, true);

        Context.ReplyString($"Given {items.Length} default item{(items.Length == 1 ? string.Empty : "s")} for a {Localization.TranslateEnum(@class, Context.Language)} loadout.");
    }

    private async UniTask viewlens(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        Context.AssertOnDuty();

        if (Context.MatchParameter(0, "clear", "none", "me"))
        {
            Context.Player.ViewLens = null;
            Context.ReplyString("Removed view lens.");

            UCWarfare.I.UpdateLangs(Context.Player, false);
        }
        else if (Context.TryGet(0, out ulong s64, out UCPlayer? onlinePlayer, remainder: true))
        {
            Context.Player.ViewLens = s64 == Context.CallerId.m_SteamID ? null : s64;
            if (s64 == Context.CallerId.m_SteamID)
            {
                Context.ReplyString("Removed view lens.");
            }
            else if (onlinePlayer != null)
            {
                Context.ReplyString("Set view lens to " + onlinePlayer.Translate(Context, UCPlayer.FormatColoredPlayerName) +
                                " (" + onlinePlayer.Translate(Context, UCPlayer.FormatColoredSteam64) + ")'s perspective. Clear with <#fff>/test viewlens clear</color>.");
            }
            else
            {
                PlayerNames names = await F.GetPlayerOriginalNamesAsync(s64, token).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(token);
                Context.ReplyString("Set view lens to " + names.PlayerName + " (" + s64.ToString(Context.Culture) + ")'s perspective. Clear with <#fff>/test viewlens clear</color>.");
            }

            UCWarfare.I.UpdateLangs(Context.Player, false);
        }
        else
        {
            Context.SendCorrectUsage("/test viewlens <player ...> - Simulates UI from another player's perspective.");
        }
    }

    private void dumpfob()
    {
        Context.AssertRanByPlayer();

        FOBManager? fobs = Data.Singletons.GetSingleton<FOBManager>();
        if (fobs == null)
            throw Context.SendGamemodeError();

        IFOB? fob = null;
        if (Context.TryGetBarricadeTarget(out BarricadeDrop? barricade))
        {
            fob = fobs.FindFob(new UCBarricade(barricade));
        }

        if (fob == null && Context.TryGetStructureTarget(out StructureDrop? structure))
        {
            fob = fobs.FindFob(new UCStructure(structure));
        }

        if (fob == null && Context.TryGetVehicleTarget(out InteractableVehicle? vehicle))
        {
            fob = fobs.FindFob(vehicle);
        }

        if (fob == null)
        {
            throw Context.ReplyString("<#ff8c69>Look at a FOB item to use this command.");
        }

        fob.Dump(Context.Player);
        Context.ReplyString("Check console.");
    }

    private async UniTask exportlang(CancellationToken token)
    {
        Context.AssertRanByConsole();
        Context.AssertHelpCheck(0, "/text exportlang [lang]");
        string? input = Context.GetRange(0);
        LanguageInfo lang = (input == null ? null : Data.LanguageDataStore.GetInfoCached(input, true)) ?? Localization.GetDefaultLanguage();
        await Translation.ExportLanguage(lang, false, true, token).ConfigureAwait(false);
        Context.ReplyString(lang + " exported.");
    }

    private void damage()
    {
        Context.AssertRanByPlayer();

        Context.AssertArgs(1, "/test damage <amount>[%] [-r]");
        string arg = Context.Get(0)!;
        bool percent = arg.Length > 0 && arg[^1] == '%';
        bool remaining = percent && Context.MatchFlag("r");
        if (percent)
            arg = arg.Substring(0, arg.Length - 1);
        if (!float.TryParse(arg, NumberStyles.Number, Context.Culture, out float damage))
            throw Context.SendCorrectUsage("/test damage <amount>[%] [-r]");

        RaycastInfo cast = DamageTool.raycast(new Ray(Context.Player.Player.look.aim.position, Context.Player.Player.look.aim.forward), 4f, RayMasks.BARRICADE | RayMasks.STRUCTURE | RayMasks.VEHICLE, Context.Player.Player);
        if (cast.transform == null)
            throw Context.SendCorrectUsage("/test damage <amount>[%] while looking at a barricade, structure, or vehicle.");

        if (BarricadeManager.FindBarricadeByRootTransform(cast.transform) is { } barricade)
        {
            damage = percent ? (remaining ? barricade.GetServersideData().barricade.health : barricade.asset.health) * (damage / 100f) : damage;
            BarricadeManager.damage(barricade.model, damage, 1f, false, Context.CallerId, EDamageOrigin.Unknown);
            Context.ReplyString($"Damaged barricade {barricade.asset.FriendlyName} by {damage.ToString(Context.Culture)} points.");
        }
        else if (StructureManager.FindStructureByRootTransform(cast.transform) is { } structure)
        {
            damage = percent ? (remaining ? structure.GetServersideData().structure.health : structure.asset.health) * (damage / 100f) : damage;
            StructureManager.damage(structure.model, Context.Player.Player.look.aim.forward, damage, 1f, false, Context.CallerId, EDamageOrigin.Unknown);
            Context.ReplyString($"Damaged structure {structure.asset.FriendlyName} by {damage.ToString(Context.Culture)} points.");
        }
        else if (cast.vehicle != null)
        {
            damage = percent ? (remaining ? cast.vehicle.health : cast.vehicle.asset.health) * (damage / 100f) : damage;
            VehicleManager.damage(cast.vehicle, damage, 1f, false, Context.CallerId, EDamageOrigin.Unknown);
            Context.ReplyString($"Damaged vehicle {cast.vehicle.asset.FriendlyName} by {damage.ToString(Context.Culture)} points.");
        }
        else
            throw Context.SendCorrectUsage("/test damage <amount>[%] while looking at a barricade, structure, or vehicle.");
    }

    private async UniTask migratebans(CancellationToken token)
    {
        Context.AssertRanByConsole();
        await Migration.MigrateBans(Data.ModerationSql, token).ConfigureAwait(false);
        Context.ReplyString("Done.");
    }

    private async UniTask migratebe(CancellationToken token)
    {
        Context.AssertRanByConsole();
        await Migration.MigrateBattlEyeKicks(Data.ModerationSql, token).ConfigureAwait(false);
        Context.ReplyString("Done.");
    }

    private async UniTask migratekicks(CancellationToken token)
    {
        Context.AssertRanByConsole();
        await Migration.MigrateKicks(Data.ModerationSql, token).ConfigureAwait(false);
        Context.ReplyString("Done.");
    }

    private async UniTask migratemutes(CancellationToken token)
    {
        Context.AssertRanByConsole();
        await Migration.MigrateMutes(Data.ModerationSql, token).ConfigureAwait(false);
        Context.ReplyString("Done.");
    }

    private async UniTask migratetks(CancellationToken token)
    {
        Context.AssertRanByConsole();
        await Migration.MigrateTeamkills(Data.ModerationSql, token).ConfigureAwait(false);
        Context.ReplyString("Done.");
    }

    private async UniTask migratewarns(CancellationToken token)
    {
        Context.AssertRanByConsole();
        await Migration.MigrateWarnings(Data.ModerationSql, token).ConfigureAwait(false);
        Context.ReplyString("Done.");
    }

    private void fobcooldown()
    {
        if (Context.TryGet(0, out int playerCount))
        {
            Context.ReplyString($"Cooldown: {FormattingUtility.ToTimeString(TimeSpan.FromSeconds(CooldownManager.GetFOBDeployCooldown(playerCount)))} for {playerCount} player(s).");
        }
        else
        {
            playerCount = Provider.clients.Count(x => x.GetTeam() is 1 or 2);
            Context.ReplyString($"Current cooldown: {FormattingUtility.ToTimeString(TimeSpan.FromSeconds(CooldownManager.GetFOBDeployCooldown(playerCount)))} for {playerCount} player(s)..");
        }
    }

    private async UniTask migrateusers(CancellationToken token)
    {
        Context.AssertRanByConsole();

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

            WarfareUserData data = new WarfareUserData
            {
                FirstJoined = firstJoined,
                LastJoined = lastJoined,
                Steam64 = steam64,
                CharacterName = username.CharacterName.MaxLength(30),
                DisplayName = null,
                NickName = username.NickName.MaxLength(30),
                PlayerName = username.PlayerName.MaxLength(48),
                DiscordId = await Data.AdminSql.GetDiscordID(steam64, token)
            };

            dbContext.UserData.Add(data);
        }

        await dbContext.SaveChangesAsync(CancellationToken.None);
    }

    private async Task dumpkit(CancellationToken token)
    {
        Context.AssertRanByConsole();

        KitManager? km = KitManager.GetSingletonQuick();
        if (km == null)
            throw Context.SendGamemodeError();

        Context.AssertArgs(1, "/test dumpkit <name or id ...>");

        Kit? kit;
        if (Context.TryGet(0, out uint id))
            kit = await km.GetKit(id, token, KitManager.FullSet);
        else
        {
            kit = await km.FindKit(Context.Get(0)!, token, false);
            if (kit != null)
                kit = await km.GetKit(kit.PrimaryKey, token, KitManager.FullSet);
        }

        if (kit == null)
            throw Context.ReplyString($"Kit not found: {Context.Get(0)}.");

        Context.ReplyString(Environment.NewLine + JsonSerializer.Serialize(kit, JsonSettings.SerializerSettings));
    }

    private void dumpfactions()
    {
        Context.AssertRanByConsole();

        Context.ReplyString(Environment.NewLine + JsonSerializer.Serialize(TeamManager.Factions, JsonSettings.SerializerSettings));
    }
}