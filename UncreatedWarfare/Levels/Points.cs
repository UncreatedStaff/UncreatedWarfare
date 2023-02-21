using MySqlConnector;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.SQL;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Events.Vehicles;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using VehicleSpawn = Uncreated.Warfare.Vehicles.VehicleSpawn;

namespace Uncreated.Warfare.Levels;

public sealed class Points : BaseSingletonComponent, IUIListener
{
    private const string UpdateAllPointsQuery = "SELECT `Steam64`, `Team`, `Experience`, `Credits` FROM `s2_levels` WHERE `Steam64` in (";

    public static readonly XPUI XPUI;
    public static readonly CreditsUI CreditsUI;
    public static List<Task> Transactions;

    private static readonly Config<XPConfig> XPConfigObj;
    private static readonly Config<CreditsConfig> CreditsConfigObj;
    private static bool _first = true;

    public static XPConfig XPConfig => XPConfigObj.Data;
    public static CreditsConfig CreditsConfig => CreditsConfigObj.Data;
    static Points()
    {
        if (UCWarfare.IsLoaded) // allows external access to this class without initializing everything
        {
            XPConfigObj = new Config<XPConfig>(Data.Paths.PointsStorage, "xp.json");
            CreditsConfigObj = new Config<CreditsConfig>(Data.Paths.PointsStorage, "credits.json");
            Transactions = new List<Task>(16);
            XPUI = new XPUI();
            CreditsUI = new CreditsUI();
        }
    }

    public override void Load()
    {
        EventDispatcher.GroupChanged += OnGroupChanged;
        EventDispatcher.VehicleDestroyed += OnVehicleDestoryed;
        KitManager.OnKitChanged += OnKitChanged;
        EventDispatcher.PlayerLeaving += OnPlayerLeft;
        if (!_first) ReloadConfig();
        else _first = false;
    }
    public override void Unload()
    {
        EventDispatcher.PlayerLeaving -= OnPlayerLeft;
        KitManager.OnKitChanged -= OnKitChanged;
        EventDispatcher.VehicleDestroyed -= OnVehicleDestoryed;
        EventDispatcher.GroupChanged -= OnGroupChanged;
    }
    public static void ReloadConfig()
    {
        XPConfigObj.Reload();
        CreditsConfigObj.Reload();
    }

    public void HideUI(UCPlayer player)
    {
        XPUI.Clear(player);
        CreditsUI.Clear(player);
    }
    public void ShowUI(UCPlayer player)
    {
        XPUI.SendTo(player);
        CreditsUI.SendTo(player);
    }
    public void UpdateUI(UCPlayer player)
    {
        XPUI.Update(player, true);
        CreditsUI.Update(player, true);
    }

    private static void OnGroupChanged(GroupChanged e)
    {
        UCPlayer player = e.Player;
        ulong newGroup = e.NewGroup;
        player.PointsDirtyMask |= 0b00111111;
        UCWarfare.RunTask(async () =>
        {
            if (e.NewTeam is 1 or 2)
            {
                (int xp, int credits) = await Data.DatabaseManager.GetCreditsAndXP(e.Steam64, e.NewTeam).ConfigureAwait(false);
                await UCWarfare.ToUpdate();
                player.CachedXP = xp;
                player.CachedCredits = credits;
                XPUI.Update(player, true);
                CreditsUI.Update(player, true);
            }
            else
            {
                XPUI.Clear(player);
                CreditsUI.Clear(player);
            }

            if (newGroup is 1 or 2)
            {
                XPUI.Update(player, true);
                CreditsUI.Update(player, true);
            }
            else
            {
                XPUI.ClearFromPlayer(player.Connection);
                CreditsUI.ClearFromPlayer(player.Connection);
            }
        }, ctx: "Reload xp on group change.");
    }
    public static readonly int[] Levels =
    {
        1000,
        4000,
        8000,
        13000,
        20000,
        29000,
        40000,
        55000
    };
    /// <summary>Get the current level given an amount of <paramref name="xp"/>.</summary>
    public static int GetLevel(int xp)
    {
        for (int i = 0; i < Levels.Length; i++)
        {
            if (xp < Levels[i])
                return i;
        }
        return Levels.Length;
    }
    /// <summary>Get the given <paramref name="level"/>'s starting xp.</summary>
    public static int GetLevelXP(int level)
    {
        if (level >= Levels.Length)
            return Levels[Levels.Length - 1];

        if (level > 0)
            return Levels[level - 1];

        return 0;
    }
    /// <summary>Get the level after the given <paramref name="level"/>'s starting xp (or the given <paramref name="level"/>'s end xp.</summary>
    public static int GetNextLevelXP(int level)
    {
        if (level >= Levels.Length)
            return 100000;

        if (level >= 0)
            return Levels[level];

        return 0;
    }
    /// <summary>Get the percentage from 0-1 a player is through their current level at the given <paramref name="xp"/>.</summary>
    public static float GetLevelProgressXP(int xp)
    {
        int lvl = GetLevel(xp);
        int start = GetLevelXP(lvl);
        xp -= start;
        return xp / (float)(GetNextLevelXP(lvl) - start);
    }
    /// <summary>Get the percentage from 0-1 a player is through their current level at the given <paramref name="xp"/> and <paramref name="lvl"/>.</summary>
    public static float GetLevelProgressXP(int xp, int lvl)
    {
        int strt = GetLevelXP(lvl);
        return (float)(xp - strt) / (GetNextLevelXP(lvl) - strt);
    }
    public static void AwardCredits(UCPlayer player, int amount, Translation message, bool redmessage = false, bool isPurchase = false, bool @lock = true) =>
        AwardCredits(player, amount, Localization.Translate(message, player), redmessage, isPurchase, @lock);
    public static void AwardCredits<T>(UCPlayer player, int amount, Translation<T> message, T arg, bool redmessage = false, bool isPurchase = false, bool @lock = true) =>
        AwardCredits(player, amount, Localization.Translate(message, player, arg), redmessage, isPurchase, @lock);
    public static void AwardCredits<T1, T2>(UCPlayer player, int amount, Translation<T1, T2> message, T1 arg1, T2 arg2, bool redmessage = false, bool isPurchase = false, bool @lock = true) =>
        AwardCredits(player, amount, Localization.Translate(message, player, arg1, arg2), redmessage, isPurchase, @lock);
    public static Task AwardCreditsAsync(UCPlayer player, int amount, Translation message, bool redmessage = false, bool isPurchase = false, bool @lock = true, CancellationToken token = default) =>
        AwardCreditsAsync(player, amount, Localization.Translate(message, player), redmessage, isPurchase, @lock, token);
    public static async Task AwardCreditsAsync(CreditsParameters parameters, CancellationToken token = default, bool @lock = true)
    {
        if (!token.CanBeCanceled)
            token = UCWarfare.UnloadCancel;
        UCPlayer? player = parameters.Player;
        Task? remote = null;
        try
        {
            ulong team = parameters.Team;
            if (team is < 1 or > 2)
            {
                if (player == null || !player.IsOnline)
                {
                    PlayerSave? save = PlayerManager.GetSave(parameters.Steam64);
                    if (save != null)
                        team = save.Team;
                }
                else team = player.GetTeam();

                if (team is < 1 or > 2)
                    return;
            }

            if (@lock && player != null)
                await player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
            if (parameters.Amount == 0 || XPConfigObj.Data.XPMultiplier <= 0f || parameters.StartingMultiplier <= 0f)
                return;
            int amount = Mathf.RoundToInt(parameters.Amount * XPConfigObj.Data.XPMultiplier * parameters.StartingMultiplier);

            bool redmessage = amount < 0 && parameters.IsPunishment;
            int currentAmount = await Data.DatabaseManager.AddCredits(parameters.Steam64, team, amount, token).ConfigureAwait(false);
            if (Data.RemoteSQL != null)
            {
                remote = Data.RemoteSQL.AddCredits(parameters.Steam64, team, amount, token);
            }
            int oldamt = currentAmount - amount;
            await UCWarfare.ToUpdate(token);

            if (player != null)
                player.CachedCredits = currentAmount;

            ActionLog.Add(ActionLogType.CreditsChanged, oldamt + " >> " + currentAmount, parameters.Steam64);

            if (player != null && player.IsOnline && !player.HasUIHidden && !Data.Gamemode.LeaderboardUp())
            {
                Translation<int> key = T.XPToastGainCredits;
                if (amount < 0)
                {
                    key = redmessage ? T.XPToastLoseCredits : T.XPToastPurchaseCredits;
                }

                string number = Localization.Translate(key, player, Math.Abs(amount));
                ToastMessage.QueueMessage(player,
                    !string.IsNullOrEmpty(parameters.Message)
                        ? new ToastMessage(number + "\n" + parameters.Message!.Colorize("adadad"), ToastMessageSeverity.Mini)
                        : new ToastMessage(number, ToastMessageSeverity.Mini));

                if (!parameters.IsPurchase && player.Player.TryGetPlayerData(out UCPlayerData c))
                {
                    if (c.Stats is IExperienceStats kd)
                        kd.AddCredits(amount);
                }

                player.PointsDirtyMask |= 0b00001000;
                CreditsUI.Update(player, false);
            }
        }
        catch (Exception ex)
        {
            L.LogError("Error giving credits to " + (player?.Name.PlayerName ?? string.Empty) + " (" + parameters.Steam64 + ")");
            L.LogError(ex);
        }
        finally
        {
            if (@lock && player != null)
                player.PurchaseSync.Release();
        }
        if (remote != null && !remote.IsCompleted)
            await remote.ConfigureAwait(false);
    }
    public static Task AwardCreditsAsync<T>(UCPlayer player, int amount, Translation<T> message, T arg, bool redmessage = false, bool isPurchase = false, bool @lock = true, CancellationToken token = default) =>
        AwardCreditsAsync(player, amount, Localization.Translate(message, player, arg), redmessage, isPurchase, @lock, token);
    public static Task AwardCreditsAsync<T1, T2>(UCPlayer player, int amount, Translation<T1, T2> message, T1 arg1, T2 arg2, bool redmessage = false, bool isPurchase = false, bool @lock = true, CancellationToken token = default) =>
        AwardCreditsAsync(player, amount, Localization.Translate(message, player, arg1, arg2), redmessage, isPurchase, @lock, token);
    public static void AwardCredits(UCPlayer player, int amount, string? message = null, bool redmessage = false, bool isPurchase = false, bool @lock = true)
    {
        CreditsParameters parameters = new CreditsParameters(player, player.GetTeam(), amount, message, redmessage)
        {
            IsPurchase = isPurchase
        };
        AwardCredits(in parameters, @lock);
    }
    public static void AwardCredits(in CreditsParameters parameters, bool @lock = true)
    {
        UCWarfare.RunTask(AwardCreditsAsync, parameters, UCWarfare.UnloadCancel, @lock, ctx: "Award " + parameters.Amount + " credits to " + parameters.Steam64 + ".");
    }
    public static Task AwardCreditsAsync(UCPlayer player, int amount, string? message = null, bool redmessage = false, bool isPurchase = false, bool @lock = true, CancellationToken token = default)
    {
        CreditsParameters parameters = new CreditsParameters(player, player.GetTeam(), amount, message, redmessage)
        {
            IsPurchase = isPurchase
        };
        return AwardCreditsAsync(parameters, token, @lock);
    }
    public static void AwardXP(UCPlayer player, int amount, Translation message, bool awardCredits = true, ulong team = 0) =>
        AwardXP(player, amount, Localization.Translate(message, player), awardCredits, team);
    public static void AwardXP<T>(UCPlayer player, int amount, Translation<T> message, T arg, bool awardCredits = true, ulong team = 0) =>
        AwardXP(player, amount, Localization.Translate(message, player, arg), awardCredits, team);
    public static void AwardXP<T1, T2>(UCPlayer player, int amount, Translation<T1, T2> message, T1 arg1, T2 arg2, bool awardCredits = true, ulong team = 0) =>
        AwardXP(player, amount, Localization.Translate(message, player, arg1, arg2), awardCredits, team);
    public static void AwardXP(UCPlayer player, int amount, string? message = null, bool awardCredits = true, ulong team = 0)
    {
        if (team is < 1 or > 2)
            team = player.GetTeam();
        if (team is < 1 or > 2)
            return;
        XPParameters parameters = new XPParameters(player, team, amount, message, awardCredits);
        AwardXP(in parameters);
    }
    public static void AwardXP(in XPParameters parameters)
    {
        UCWarfare.RunTask(AwardXPAsync, parameters, UCWarfare.UnloadCancel, ctx: "Award " + parameters.Amount + " xp to " + parameters.Steam64 + ".");
    }
    public static async Task AwardXPAsync(XPParameters parameters, CancellationToken token = default)
    {
        try
        {
            if (!token.CanBeCanceled)
                token = UCWarfare.UnloadCancel;
            Task? remote = null;
            await UCWarfare.ToUpdate(token);
            if (!Data.TrackStats || parameters.Amount == 0 || XPConfigObj.Data.XPMultiplier == 0f) return;
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            float multiplier = -1;
            UCPlayer? player = parameters.Player;
            if (player != null)
            {
                for (int i = 0; i < player.ActiveBuffs.Length; ++i)
                {
                    if (player.ActiveBuffs[i] is IXPBoostBuff buff)
                    {
                        if (buff.Multiplier > multiplier)
                            multiplier = buff.Multiplier;
                    }
                }
            }

            if (multiplier < 0f)
                multiplier = 1f;
            else if (multiplier == 0f)
                return;
            float amt = parameters.Amount * XPConfigObj.Data.XPMultiplier * multiplier;
            if (amt <= 0)
                return;
            ulong team = parameters.Team;
            if (team is < 1 or > 2)
            {
                if (parameters.Player == null)
                {
                    PlayerSave? save = PlayerManager.GetSave(parameters.Steam64);
                    if (save == null)
                        return;
                    team = save.Team;
                }
                else team = parameters.Player.GetTeam();
            }

            if (team is < 1 or > 2)
                return;
            LevelData oldLevel = default;
            int amount = Mathf.RoundToInt(amt);
            int credits = parameters.AwardCredits ? Mathf.RoundToInt((parameters.OverrideCreditPercentage ?? 0.15f) * amt) : 0;
            try
            {
                if (player != null)
                {
                    await player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
                    oldLevel = player.Level;
                }

                int currentAmount;
                if (player != null)
                {
                    currentAmount = await Data.DatabaseManager.AddXP(parameters.Steam64, team, amount, token).ConfigureAwait(false);
                    if (Data.RemoteSQL != null && Data.RemoteSQL.Opened)
                    {
                        remote = Data.RemoteSQL.AddCredits(parameters.Steam64, team, amount, token);
                    }
                    if (parameters.AwardCredits)
                    {
                        await AwardCreditsAsync(
                                new CreditsParameters(player, team, credits, null, isPunishment: true), token, false)
                            .ConfigureAwait(false);
                    }
                    await UCWarfare.ToUpdate(token);

                    player.CachedXP = currentAmount;

                    if (player.Player.TryGetPlayerData(out UCPlayerData c))
                    {
                        if (c.Stats is IExperienceStats kd)
                            kd.AddXP(amount);
                    }

                    if (player.IsOnline && !player.HasUIHidden && (Data.Gamemode is not IEndScreen lb || !lb.IsScreenUp))
                    {
                        string number = Localization.Translate(amount >= 0 ? T.XPToastGainXP : T.XPToastLoseXP, player,
                            Math.Abs(amount));

                        number = number.Colorize(amount > 0 ? "e3e3e3" : "d69898");

                        ToastMessage.QueueMessage(player,
                            !string.IsNullOrEmpty(parameters.Message)
                                ? new ToastMessage(number + "\n" + parameters.Message!.Colorize("adadad"), ToastMessageSeverity.Mini)
                                : new ToastMessage(number, ToastMessageSeverity.Mini));
                    }
                }
                else if (parameters.AwardCredits)
                {
                    if (Data.RemoteSQL != null)
                    {
                        (int credits, int xp)[] arr = await Task.WhenAll(
                                Data.DatabaseManager.AddCreditsAndXP(parameters.Steam64, team, credits, amount, token),
                                Data.RemoteSQL.AddCreditsAndXP(parameters.Steam64, team, credits, amount, token))
                            .ConfigureAwait(false);
                        currentAmount = arr.Length > 0 ? arr[0].xp : amount;
                    }
                    else
                        currentAmount = (await Data.DatabaseManager.AddCreditsAndXP(parameters.Steam64, team, credits, amount, token).ConfigureAwait(false)).Item2;
                    await UCWarfare.ToUpdate();
                }
                else
                {
                    currentAmount = await Data.DatabaseManager.AddXP(parameters.Steam64, team, amount, token).ConfigureAwait(false);
                    await UCWarfare.ToUpdate(token);
                }

                if (player == null)
                    oldLevel = new LevelData(Mathf.RoundToInt(currentAmount - amt));

                ActionLog.Add(ActionLogType.XPChanged, oldLevel.TotalXP + " >> " + currentAmount, parameters.Steam64);
            }
            finally
            {
                player?.PurchaseSync.Release();
            }
            if (player != null && player.IsOnline)
            {
                player.PointsDirtyMask |= 0b00000001;
                if (player.Level.Level > oldLevel.Level)
                {
                    ToastMessage.QueueMessage(player,
                        new ToastMessage(Localization.Translate(T.ToastPromoted, player), player.Level.Name.ToUpper(),
                            ToastMessageSeverity.Big));
                    player.PointsDirtyMask |= 0b00000010;
                }
                else if (player.Level.Level < oldLevel.Level)
                {
                    ToastMessage.QueueMessage(player,
                        new ToastMessage(Localization.Translate(T.ToastDemoted, player), player.Level.Name.ToUpper(),
                            ToastMessageSeverity.Big));
                    player.PointsDirtyMask |= 0b00000010;
                }
                else goto skipUpdates;
                Signs.UpdateAllSigns(player);

                skipUpdates:
                XPUI.Update(player, false);
                for (int i = 0; i < player.ActiveBuffs.Length; ++i)
                    if (player.ActiveBuffs[i] is IXPBoostBuff buff)
                        buff.OnXPBoostUsed(amount, parameters.AwardCredits);
            }

            if (remote != null && !remote.IsCompleted)
                await remote.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            L.LogError("Exception awarding " + parameters.Amount + " XP to " + parameters.Steam64 + ".");
            L.LogError(ex);
            throw;
        }
    }
    public static void AwardXP(Player player, int amount, string? message = null, bool awardCredits = true)
    {
        UCPlayer? pl = UCPlayer.FromPlayer(player);
        if (pl != null)
            AwardXP(pl, amount, message, awardCredits);
        else
            L.LogWarning("Unable to find player.");
    }
    public static string GetProgressBar(float currentPoints, int totalPoints, int barLength = 50)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float ratio = currentPoints / totalPoints;

        int progress = Mathf.RoundToInt(ratio * barLength);
        if (progress > barLength)
            progress = barLength;

        char[] bars = new char[barLength];
        for (int i = 0; i < progress; i++)
        {
            bars[i] = XPConfig.ProgressBlockCharacter;
        }
        return new string(bars);
    }
    public static void TryAwardDriverAssist(PlayerDied args, int amount, float quota = 0)
    {
        if (args.DriverAssist is null || !args.DriverAssist.IsOnline) return;

        AwardXP(args.DriverAssist, amount, Localization.Translate(T.XPToastKillDriverAssist, args.DriverAssist));
        /*
        if (quota != 0 && args.ActiveVehicle != null && args.ActiveVehicle.TryGetComponent(out VehicleComponent comp))
            comp.Quota += quota;*/
    }
    public static void TryAwardDriverAssist(Player gunner, int amount, float quota = 0)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        InteractableVehicle vehicle = gunner.movement.getVehicle();
        if (vehicle != null)
        {
            SteamPlayer driver = vehicle.passengers[0].player;
            if (driver != null && driver.playerID.steamID != gunner.channel.owner.playerID.steamID)
            {
                AwardXP(driver.player, amount, Localization.Translate(T.XPToastKillDriverAssist, driver.playerID.steamID.m_SteamID));
            }

            //if (vehicle.transform.TryGetComponent(out VehicleComponent component))
            //{
            //    component.Quota += quota;
            //}
        }
    }
    public static void TryAwardFOBCreatorXP(FOB fob, int amount, Translation translationKey)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayer? creator = UCPlayer.FromID(fob.Creator);

        if (creator != null)
        {
            AwardXP(creator, amount, translationKey);
        }

        if (fob.Placer != fob.Creator)
        {
            UCPlayer? placer = UCPlayer.FromID(fob.Placer);
            if (placer != null)
                AwardXP(placer, amount, translationKey);
        }
    }
    public static void OnPlayerDeath(PlayerDied e)
    {
        if (e.Killer is null || !e.Killer.IsOnline) return;
        if (e.WasTeamkill)
        {
            AwardXP(e.Killer, XPConfig.FriendlyKilledXP, T.XPToastFriendlyKilled);
            return;
        }
        AwardXP(e.Killer, XPConfig.EnemyKilledXP, T.XPToastEnemyKilled);

        if (e.Player.Player.TryGetPlayerData(out UCPlayerData component))
        {
            ulong killerID = e.Killer.Steam64;
            ulong victimID = e.Player.Steam64;

            UCPlayer? assister = UCPlayer.FromID(component.SecondLastAttacker.Key);
            if (assister != null && assister.Steam64 != killerID && assister.Steam64 != victimID && (DateTime.Now - component.SecondLastAttacker.Value).TotalSeconds <= 30)
            {
                AwardXP(assister, XPConfig.KillAssistXP, T.XPToastKillAssist);
            }

            if (e.Player.Player.TryGetComponent(out SpottedComponent spotted))
            {
                spotted.OnTargetKilled(XPConfig.EnemyKilledXP);
            }

            component.ResetAttackers();
        }
        TryAwardDriverAssist(e, XPConfig.EnemyKilledXP, 1);
    }
    private static void OnPlayerLeft(PlayerEvent e)
    {
        if (Data.RemoteSQL != null && Data.RemoteSQL.Opened)
        {
            UCPlayer caller = e.Player;
            Task.Run(async () =>
            {
                await caller.PurchaseSync.WaitAsync();
                try
                {
                    uint t1Xp = 0;
                    uint t2Xp = 0;
                    uint t1Cd = 0;
                    uint t2Cd = 0;
                    await Data.DatabaseManager.QueryAsync(
                        "SELECT `Experience`, `Credits`, `Team` FROM `s2_levels` WHERE `Steam64` = @0 LIMIT 2;",
                        new object[] { caller.Steam64 },
                        reader =>
                        {
                            ulong team = reader.GetUInt64(2);
                            if (team == 1)
                            {
                                t1Xp = reader.GetUInt32(0);
                                t1Cd = reader.GetUInt32(1);
                            }
                            else if (team == 2)
                            {
                                t2Xp = reader.GetUInt32(0);
                                t2Cd = reader.GetUInt32(1);
                            }
                        });
                    await Data.RemoteSQL.NonQueryAsync(
                        "INSERT INTO `s2_levels` (`Steam64`, `Team`, `Experience`, `Credits`) VALUES (@0, 1, @1, @2), (@0, 2, @3, @4) AS vals " +
                        "ON DUPLICATE KEY UPDATE `Experience` = vals.Experience, `Credits` = vals.Credits;",
                        new object[] { caller.Steam64, t1Xp, t1Cd, t2Xp, t2Cd }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    L.LogError("Error trying to sync player levels with remote server.");
                    L.LogError(ex);
                }
                finally
                {
                    caller.PurchaseSync.Release();
                }
            });
        }
    }
    public static async Task UpdateAllPointsAsync(CancellationToken token = default)
    {
        if (PlayerManager.OnlinePlayers.Count < 1)
            return;
        StringBuilder builder = new StringBuilder(UpdateAllPointsQuery.Length + PlayerManager.OnlinePlayers.Count * 18 + 1);
        builder.Append(UpdateAllPointsQuery);
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
        {
            PlayerManager.OnlinePlayers[i].IsDownloadingXP = true;
            if (i != 0)
                builder.Append(',');
            builder.Append(PlayerManager.OnlinePlayers[i].Steam64);
        }

        builder.Append(");");
        
        string query = builder.ToString();
        List<XPData> data = new List<XPData>(PlayerManager.OnlinePlayers.Count);
        void ReadLoop(MySqlDataReader reader)
        {
            data.Add(new XPData(reader.GetUInt64(0), reader.GetUInt64(1), reader.GetUInt32(2), reader.GetUInt32(3)));
        }

        if (Data.AdminSql.Opened)
        {
            await Data.AdminSql.QueryAsync(query, null, ReadLoop, token).ConfigureAwait(false);
        }
        else if (Data.AdminSql != Data.DatabaseManager && Data.DatabaseManager.Opened)
        {
            await Data.DatabaseManager.QueryAsync(query, null, ReadLoop, token).ConfigureAwait(false);
        }
        else
        {
            L.LogWarning("No SQL connections to download levels.");
            return;
        }
        
        foreach (UCPlayer player in PlayerManager.OnlinePlayers.ToList())
        {
            if (!player.IsOnline) continue;
            ulong id = player.Steam64;
            for (int j = 0; j < data.Count; ++j)
            {
                if (data[j].Steam64 == id)
                    goto c;
            }
            
            await UpdatePointsAsync(player, true, token.CanBeCanceled ? CancellationTokenSource.CreateLinkedTokenSource(player.DisconnectToken, token).Token : player.DisconnectToken).ConfigureAwait(false);
            c:;
        }

        for (int j = 0; j < data.Count; ++j)
        {
            XPData levels = data[j];
            UCPlayer? pl = UCPlayer.FromID(levels.Steam64);
            if (pl is null || pl.GetTeam() != levels.Team)
                continue;
            await pl.PurchaseSync.WaitAsync(token.CanBeCanceled ? CancellationTokenSource.CreateLinkedTokenSource(pl.DisconnectToken, token).Token : pl.DisconnectToken).ConfigureAwait(false);
            try
            {
                pl.UpdatePoints(levels.XP, levels.Credits);
            }
            finally
            {
                pl.PurchaseSync.Release();
            }
        }

        await UCWarfare.ToUpdate();
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            pl.IsDownloadingXP = false;
            pl.PointsDirtyMask |= 0b00001011;
            XPUI.Update(pl, false);
            CreditsUI.Update(pl, false);
        }
    }
    private record struct XPData(ulong Steam64, ulong Team, uint XP, uint Credits);
    private static void OnKitChanged(UCPlayer player, SqlItem<Kit>? kit, SqlItem<Kit>? oldkit)
    {
        Branch oldbranch = Branch.Default;
        if (oldkit is { Item: { } oldkit2 })
            oldbranch = oldkit2.Branch;
        if (player.Branch != oldbranch)
        {
            player.PointsDirtyMask |= 0b00000100;
            XPUI.Update(player, false);
        }
    }
    public static async Task UpdatePointsAsync(UCPlayer caller, bool @lock, CancellationToken token = default)
    {
        if (caller is null) throw new ArgumentNullException(nameof(caller));
        caller.IsDownloadingXP = true;
        if (@lock)
            await caller.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        try
        {
            ulong team = caller.GetTeam();
            if (team is 1 or 2)
            {
                bool found = false;
                bool found2 = false;
                uint cd = 1;
                uint xp = 1;
                uint cd2 = 1;
                uint xp2 = 1;
                Task? t2 = null;
                if (Data.RemoteSQL != null && Data.RemoteSQL.Opened)
                {
                    t2 = Data.RemoteSQL.QueryAsync(
                        "SELECT `Experience`, `Credits` FROM `s2_levels` WHERE `Steam64` = @0 AND `Team` = @1 LIMIT 1;",
                        new object[] { caller.Steam64, team },
                        reader =>
                        {
                            xp2 = reader.GetUInt32(0);
                            cd2 = reader.GetUInt32(1);
                            found2 = true;
                        }, token);
                }

                await Data.DatabaseManager.QueryAsync(
                    "SELECT `Experience`, `Credits` FROM `s2_levels` WHERE `Steam64` = @0 AND `Team` = @1 LIMIT 1;",
                    new object[] { caller.Steam64, team },
                    reader =>
                    {
                        xp = reader.GetUInt32(0);
                        cd = reader.GetUInt32(1);
                        found = true;
                    }, token).ConfigureAwait(false);
                if (found)
                    caller.UpdatePoints(xp, cd);
                if (t2 != null)
                    await t2.ConfigureAwait(false);
                if (!found && found2)
                {
                    cd = cd2;
                    xp = xp2;
                    L.LogWarning(caller.Steam64 +
                                 " Missing local levels, Remote: (XP: " + xp2 + ", Credits: " + cd2 + ").");
                }

                if (cd != cd2 || xp != xp2)
                {
                    WarfareSQL? target;
                    if (found2)
                    {
                        L.LogWarning("Inconsistancy between remote and local experience/credit values for " +
                                     caller.Steam64 +
                                     " Remote: (XP: " + xp2 + ", Credits: " + cd2 + "), Local: (" + xp + ", " + cd +
                                     ").");
                        if (xp2 > xp)
                        {
                            xp = xp2;
                            cd = cd2;
                            target = Data.DatabaseManager;
                        }
                        else
                            target = Data.RemoteSQL;
                    }
                    else target = found ? Data.RemoteSQL : null;

                    if (target != null && target.Opened)
                        await target.NonQueryAsync(
                            "INSERT INTO `s2_levels` (`Steam64`, `Team`, `Experience`, `Credits`) VALUES (@0, @1, @2, @3) ON DUPLICATE KEY UPDATE `Experience` = @2, `Credits` = @3;",
                            new object[] { caller.Steam64, team, xp, cd }, token).ConfigureAwait(false);
                }

                if (!found && !found2)
                    return;
                caller.UpdatePoints(xp, cd);
            }
        }
        catch (Exception ex)
        {
            L.LogError("Error downloading " + caller.Steam64 + " (" + caller.Name.PlayerName + ")'s XP and Credits.");
            L.LogError(ex);
            return;
        }
        finally
        {
            if (@lock)
                caller.PurchaseSync.Release();
            caller.IsDownloadingXP = false;
            caller.HasDownloadedXP = true;
        }
        await UCWarfare.ToUpdate(token);
        caller.PointsDirtyMask |= 0b00001011;
        XPUI.Update(caller, false);
        CreditsUI.Update(caller, false);
        
        Signs.UpdateAllSigns(caller);
    }
    /*
    public static void AwardSquadXP(UCPlayer ucplayer, float range, int xp, int ofp, string KeyplayerTranslationKey, string squadTranslationKey, float squadMultiplier)
    {
       string xpstr = Translation.Translate(KeyplayerTranslationKey, ucplayer.Steam64);
       string sqstr = Translation.Translate(squadTranslationKey, ucplayer.Steam64);
       Points.AwardXP(ucplayer.Player, xp, xpstr);

       if (ucplayer.Squad != null && ucplayer.Squad.Members.Count > 1)
       {
           if (ucplayer == ucplayer.Squad.Leader)
               OfficerManager.AddOfficerPoints(ucplayer.Player, ofp, sqstr);

           int squadxp = (int)Math.Round(xp * squadMultiplier);
           int squadofp = (int)Math.Round(ofp * squadMultiplier);

           if (squadxp > 0)
           {
               for (int i = 0; i < ucplayer.Squad.Members.Count; i++)
               {
                   UCPlayer member = ucplayer.Squad.Members[i];
                   if (member != ucplayer && ucplayer.IsNearOtherPlayer(member, range))
                   {
                       Points.AwardXP(member.Player, squadxp, sqstr);
                       if (member.IsSquadLeader())
                           OfficerManager.AddOfficerPoints(ucplayer.Player, squadofp, sqstr);
                   }
               }
           }
       }
    }
    */

    private static void OnVehicleDestoryed(VehicleDestroyed e)
    {
        if (e.Instigator is null || e.VehicleData == null || e.Component == null)
            return;
        if (XPConfig.VehicleDestroyedXP.ContainsKey(e.VehicleData.Type))
        {
            ulong dteam = e.Instigator.GetTeam();
            bool vehicleWasEnemy = (dteam == 1 && e.Team == 2) || (dteam == 2 && e.Team == 1);
            bool vehicleWasFriendly = dteam == e.Team;
            if (!vehicleWasFriendly)
                Stats.StatsManager.ModifyTeam(dteam, t => t.VehiclesDestroyed++, false);

            if (!Points.XPConfig.VehicleDestroyedXP.TryGetValue(e.VehicleData.Type, out int fullXP))
                fullXP = 0;


            float totalDamage = 0;
            int l = 0;
            foreach (KeyValuePair<ulong, KeyValuePair<ushort, DateTime>> entry in e.Component.DamageTable)
            {
                if ((DateTime.Now - entry.Value.Value).TotalSeconds < 60)
                {
                    totalDamage += entry.Value.Key;
                    ++l;
                }
            }

            if (vehicleWasEnemy)
            {
                Translation<VehicleType> message = e.Component.IsAircraft ? T.XPToastAircraftDestroyed : T.XPToastVehicleDestroyed;

                Asset asset = Assets.find(e.Component.LastItem);
                string reason = string.Empty;
                if (asset != null)
                {
                    if (asset is ItemAsset item)
                        reason = item.itemName;
                    else if (asset is VehicleAsset v)
                        reason = "suicide " + v.vehicleName;
                }

                int distance = Mathf.RoundToInt((e.Instigator.Position - e.Vehicle.transform.position).magnitude);

                string clr = TeamManager.GetTeamHexColor(e.Team);
                if (reason.Length == 0)
                    Chat.Broadcast(T.VehicleDestroyedUnknown, e.Instigator, e.Vehicle.asset, clr);
                else
                    Chat.Broadcast(T.VehicleDestroyed, e.Instigator, e.Vehicle.asset, reason, distance, clr);

                ActionLog.Add(ActionLogType.OwnedVehicleDied, $"{e.Vehicle.asset.vehicleName} / {e.Vehicle.id} / {e.Vehicle.asset.GUID:N} ID: {e.Vehicle.instanceID}" +
                                                                 $" - Destroyed by {e.Instigator.Steam64.ToString(Data.AdminLocale)}", e.OwnerId);

                QuestManager.OnVehicleDestroyed(e);

                float resMax = 0f;
                UCPlayer? resMaxPl = null;
                DateTime now = DateTime.Now;
                KeyValuePair<ulong, float>[] assists = new KeyValuePair<ulong, float>[l];
                foreach (KeyValuePair<ulong, KeyValuePair<ushort, DateTime>> entry in e.Component.DamageTable)
                {
                    if ((now - entry.Value.Value).TotalSeconds < 60)
                    {
                        float responsibleness = entry.Value.Key / totalDamage;
                        int reward = Mathf.RoundToInt(responsibleness * fullXP);
                        if (l > 1)
                            assists[--l] = new KeyValuePair<ulong, float>(entry.Key, responsibleness);
                        UCPlayer? attacker = UCPlayer.FromID(entry.Key);
                        if (attacker != null && attacker.GetTeam() != e.Team)
                        {
                            if (entry.Key == e.InstigatorId)
                            {
                                AwardXP(attacker, reward, message.Translate(e.Instigator, e.VehicleData.Type).ToUpperInvariant());
                                UCPlayer? pl = e.LastDriver ?? e.Owner;
                                if (pl is not null && pl.Steam64 != e.InstigatorId)
                                    TryAwardDriverAssist(pl, fullXP, e.VehicleData.TicketCost);

                                if (e.Spotter != null)
                                {
                                    e.Spotter.OnTargetKilled(reward);
                                    UnityEngine.Object.Destroy(e.Spotter);
                                }
                            }
                            else if (responsibleness > 0.1F)
                                AwardXP(attacker, reward, T.XPToastKillVehicleAssist);
                            if (responsibleness > resMax)
                            {
                                resMax = responsibleness;
                                resMaxPl = attacker;
                            }
                        }
                    }
                }

                Array.Sort(assists, (a, b) => b.Value.CompareTo(a.Value));
                e.Assists = assists.ToArray();
                if (resMaxPl != null && resMax > 0.4f && e.InstigatorId != resMaxPl.Steam64)
                    QuestManager.OnVehicleDestroyed(e);

                if (e.InstigatorId != 0)
                    Stats.StatsManager.ModifyStats(e.InstigatorId, s => s.VehiclesDestroyed++, false);
                Stats.StatsManager.ModifyVehicle(e.Vehicle.id, v => v.TimesDestroyed++);
            }
            else if (vehicleWasFriendly)
            {
                Translation<VehicleType> message = e.Component.IsAircraft ? T.XPToastFriendlyAircraftDestroyed : T.XPToastFriendlyVehicleDestroyed;
                Chat.Broadcast(T.VehicleTeamkilled, e.Instigator, e.Vehicle.asset, TeamManager.GetTeamHexColor(e.Team));

                ActionLog.Add(ActionLogType.OwnedVehicleDied, $"{e.Vehicle.asset.vehicleName} / {e.Vehicle.id} / {e.Vehicle.asset.GUID:N} ID: {e.Vehicle.instanceID}" +
                                                                 $" - Destroyed by {e.InstigatorId}", e.OwnerId);
                if (e.Instigator is not null)
                    AwardCredits(e.Instigator, Mathf.Clamp(e.VehicleData.CreditCost, 5, 1000), message, e.VehicleData.Type, true, @lock: false);
                OffenseManager.LogVehicleTeamkill(e.InstigatorId, e.Vehicle.id, e.Vehicle.asset.vehicleName, DateTime.Now);
            }
            /*
            float missingQuota = vc.Quota - vc.RequiredQuota;
            if (missingQuota < 0)
            {
                // give quota penalty
                if (vc.RequiredQuota != -1 && (vehicleWasEnemy || wasCrashed))
                {
                    for (byte i = 0; i < vehicle.passengers.Length; i++)
                    {
                        Passenger passenger = vehicle.passengers[i];

                        if (passenger.player is not null)
                        {
                            vc.EvaluateUsage(passenger.player);
                        }
                    }

                    double totalTime = 0;
                    foreach (KeyValuePair<ulong, double> entry in vc.UsageTable)
                        totalTime += entry.Value;

                    foreach (KeyValuePair<ulong, double> entry in vc.UsageTable)
                    {
                        float responsibleness = (float)(entry.Value / totalTime);
                        int penalty = Mathf.RoundToInt(responsibleness * missingQuota * 60F);

                        UCPlayer? assetWaster = UCPlayer.FromID(entry.Key);
                        if (assetWaster != null)
                            Points.AwardXP(assetWaster, penalty, Translation.Translate("xp_wasting_assets", assetWaster));
                    }
                }
            }
            */
            Data.Reporter?.OnVehicleDied(e.OwnerId, Data.Is(out IVehicles vgm) && vgm.VehicleSpawner.TryGetSpawn(e.Vehicle, out SqlItem<VehicleSpawn> spawn)
                        ? spawn.PrimaryKey : PrimaryKey.NotAssigned, e.InstigatorId, e.Vehicle.asset.GUID, e.Component.LastItem, e.Component.LastDamageOrigin, vehicleWasFriendly);
        }
    }
}

public class XPConfig : JSONConfigData
{
    public char ProgressBlockCharacter;
    public int EnemyKilledXP;
    public int KillAssistXP;
    public int FriendlyKilledXP;
    public int FriendlyRevivedXP;
    public int FOBKilledXP;
    public int FOBTeamkilledXP;
    public int FOBBunkerKilledXP;
    public int FOBBunkerTeamkilledXP;
    public int FOBDeployedXP;
    public int FlagCapturedXP;
    public int FlagAttackXP;
    public int FlagDefendXP;
    public int FlagNeutralizedXP;
    public int TransportPlayerXP;
    public int ShovelXP;
    public int BuiltFOBXP;
    public int OnDutyXP;
    public int ResupplyFriendlyXP;
    public int RepairVehicleXP;
    public int UnloadSuppliesXP;
    public Dictionary<VehicleType, int> VehicleDestroyedXP;

    public float XPMultiplier;
    public override void SetDefaults()
    {
        ProgressBlockCharacter = '█';
        EnemyKilledXP = 10;
        KillAssistXP = 5;
        FriendlyKilledXP = -30;
        FriendlyRevivedXP = 30;
        FOBKilledXP = 80;
        FOBTeamkilledXP = -1000;
        FOBBunkerKilledXP = 60;
        FOBBunkerTeamkilledXP = -800;
        FOBDeployedXP = 10;
        FlagCapturedXP = 50;
        FlagAttackXP = 7;
        FlagDefendXP = 7;
        FlagNeutralizedXP = 80;
        TransportPlayerXP = 10;
        ShovelXP = 2;
        BuiltFOBXP = 100;
        ResupplyFriendlyXP = 20;
        RepairVehicleXP = 3;
        OnDutyXP = 5;
        UnloadSuppliesXP = 20;

        VehicleDestroyedXP = new Dictionary<VehicleType, int>()
        {
            {VehicleType.Humvee, 25},
            {VehicleType.TransportGround, 20},
            {VehicleType.LogisticsGround, 25},
            {VehicleType.ScoutCar, 30},
            {VehicleType.APC, 60},
            {VehicleType.IFV, 70},
            {VehicleType.MBT, 100},
            {VehicleType.TransportAir, 30},
            {VehicleType.AA, 20},
            {VehicleType.HMG, 20},
            {VehicleType.ATGM, 20},
            {VehicleType.Mortar, 20},
            {VehicleType.AttackHeli, 150},
            {VehicleType.Jet, 200},
        };

        XPMultiplier = 1f;
    }
}
public class CreditsConfig : JSONConfigData
{
    public int StartingCredits;
    public override void SetDefaults()
    {
        StartingCredits = 500;
    }
}
public struct CreditsParameters
{
    public readonly UCPlayer? Player;
    public readonly ulong Steam64;
    public readonly int Amount;
    public readonly ulong Team;
    public bool IsPunishment = false;
    /// <summary>Prevents updating stats.</summary>
    public bool IsPurchase = false;
    public float StartingMultiplier = 1f;
    public string? Message;

    public static CreditsParameters WithTranslation(UCPlayer player, Translation translation, int amount) =>
        new CreditsParameters(player, player.GetTeam(), amount, translation.Translate(player));
    public static CreditsParameters WithTranslation<T>(UCPlayer player, Translation<T> translation, T arg, int amount) =>
        new CreditsParameters(player, player.GetTeam(), amount, translation.Translate(player, arg));
    public static CreditsParameters WithTranslation<T1, T2>(UCPlayer player, Translation<T1, T2> translation, T1 arg1, T2 arg2, int amount) =>
        new CreditsParameters(player, player.GetTeam(), amount, translation.Translate(player, arg1, arg2));
    public static CreditsParameters WithTranslation(UCPlayer player, ulong team, Translation translation, int amount) =>
        new CreditsParameters(player, team, amount, translation.Translate(player));
    public static CreditsParameters WithTranslation<T>(UCPlayer player, ulong team, Translation<T> translation, T arg, int amount) =>
        new CreditsParameters(player, team, amount, translation.Translate(player, arg));
    public static CreditsParameters WithTranslation<T1, T2>(UCPlayer player, ulong team, Translation<T1, T2> translation, T1 arg1, T2 arg2, int amount) =>
        new CreditsParameters(player, team, amount, translation.Translate(player, arg1, arg2));
    public CreditsParameters(ulong player, ulong team, int amount)
    {
        if (!Util.IsValidSteam64Id(player))
            throw new ArgumentException("Invalid Steam64 ID: " + player, nameof(player));
        Steam64 = player;
        Player = UCPlayer.FromID(player);
        Amount = amount;
        Team = team;
        Message = null;
        IsPunishment = amount < 0;
    }
    public CreditsParameters(ulong player, ulong team, int amount, string? message, bool isPunishment = true)
    {
        if (!Util.IsValidSteam64Id(player))
            throw new ArgumentException("Invalid Steam64 ID: " + player, nameof(player));
        Steam64 = player;
        Player = UCPlayer.FromID(player);
        Team = team;
        Amount = amount;
        Message = message;
        IsPunishment = amount < 0 && isPunishment;
    }
    public CreditsParameters(UCPlayer player, ulong team, int amount)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        Steam64 = player.Steam64;
        Amount = amount;
        Team = team;
        Message = null;
        IsPunishment = amount < 0;
    }
    public CreditsParameters(UCPlayer player, ulong team, int amount, string? message, bool isPunishment = true)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        Steam64 = player.Steam64;
        Team = team;
        Amount = amount;
        Message = message;
        IsPunishment = amount < 0 && isPunishment;
    }

    public Task Award() => Points.AwardCreditsAsync(this);
    public Task Award(CancellationToken token) => Points.AwardCreditsAsync(this, token);
}
public struct XPParameters
{
    public readonly UCPlayer? Player;
    public readonly ulong Steam64;
    public readonly int Amount;
    public readonly ulong Team;
    public string? Message;
    public bool AwardCredits;
    public float StartingMultiplier = 1f;
    public bool IgnoreXPBuff = false;
    public bool IgnoreConfigXPBoost = false;
    public float? OverrideCreditPercentage = null;
    public bool AnnounceRankChange = true;
    public static XPParameters WithTranslation(UCPlayer player, Translation translation, int amount, bool awardCredits = true) =>
        new XPParameters(player, player.GetTeam(), amount, translation.Translate(player), awardCredits);
    public static XPParameters WithTranslation<T>(UCPlayer player, Translation<T> translation, T arg, int amount, bool awardCredits = true) =>
        new XPParameters(player, player.GetTeam(), amount, translation.Translate(player, arg), awardCredits);
    public static XPParameters WithTranslation<T1, T2>(UCPlayer player, Translation<T1, T2> translation, T1 arg1, T2 arg2, int amount, bool awardCredits = true) =>
        new XPParameters(player, player.GetTeam(), amount, translation.Translate(player, arg1, arg2), awardCredits);
    public static XPParameters WithTranslation(UCPlayer player, ulong team, Translation translation, int amount, bool awardCredits = true) =>
        new XPParameters(player, team, amount, translation.Translate(player), awardCredits);
    public static XPParameters WithTranslation<T>(UCPlayer player, ulong team, Translation<T> translation, T arg, int amount, bool awardCredits = true) =>
        new XPParameters(player, team, amount, translation.Translate(player, arg), awardCredits);
    public static XPParameters WithTranslation<T1, T2>(UCPlayer player, ulong team, Translation<T1, T2> translation, T1 arg1, T2 arg2, int amount, bool awardCredits = true) =>
        new XPParameters(player, team, amount, translation.Translate(player, arg1, arg2), awardCredits);
    public XPParameters(ulong player, ulong team, int amount)
    {
        if (!Util.IsValidSteam64Id(player))
            throw new ArgumentException("Invalid Steam64 ID: " + player, nameof(player));
        Steam64 = player;
        Player = UCPlayer.FromID(player);
        Amount = amount;
        Team = team;
        AwardCredits = true;
        Message = null;
    }
    public XPParameters(ulong player, ulong team, int amount, string? message, bool awardCredits)
    {
        if (!Util.IsValidSteam64Id(player))
            throw new ArgumentException("Invalid Steam64 ID: " + player, nameof(player));
        Steam64 = player;
        Player = UCPlayer.FromID(player);
        Team = team;
        Amount = amount;
        Message = message;
        AwardCredits = awardCredits;
    }
    public XPParameters(UCPlayer player, ulong team, int amount)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        Steam64 = player.Steam64;
        Amount = amount;
        Team = team;
        AwardCredits = true;
        Message = null;
    }
    public XPParameters(UCPlayer player, ulong team, int amount, string? message, bool awardCredits)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        Steam64 = player.Steam64;
        Team = team;
        Amount = amount;
        Message = message;
        AwardCredits = awardCredits;
    }

    public Task Award() => Points.AwardXPAsync(this);
    public Task Award(CancellationToken token) => Points.AwardXPAsync(this, token);
}