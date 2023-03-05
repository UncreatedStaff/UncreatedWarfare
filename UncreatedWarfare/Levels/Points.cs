using MySqlConnector;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
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

    private static readonly Config<PointsConfig> PointsConfigObj;
    private static bool _first = true;

    public static PointsConfig PointsConfig => PointsConfigObj.Data;
    static Points()
    {
        if (UCWarfare.IsLoaded) // allows external access to this class without initializing everything
        {
            PointsConfigObj = new Config<PointsConfig>(Data.Paths.BaseDirectory, "points.json");
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
        PointsConfigObj.Reload();
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
        player.PointsDirtyMask = 0b00001111;
        XPUI.Update(player, true);
        CreditsUI.Update(player, true);
    }

    private static void OnGroupChanged(GroupChanged e)
    {
        UCPlayer player = e.Player;
        player.PointsDirtyMask = 0b00001111;
        UCWarfare.RunTask(async () =>
        {
            if (e.NewTeam is 1 or 2)
            {
                (int credits, int xp) = await Data.DatabaseManager.GetCreditsAndXP(e.Steam64, e.NewTeam).ConfigureAwait(false);
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
    public static void AwardCredits<T>(UCPlayer player, int amount, Translation<T> message, T arg, bool redmessage = false, bool isPurchase = false, bool @lock = true) =>
        AwardCredits(player, amount, Localization.Translate(message, player, arg), redmessage, isPurchase, @lock);
    public static void AwardCredits<T1, T2>(UCPlayer player, int amount, Translation<T1, T2> message, T1 arg1, T2 arg2, bool redmessage = false, bool isPurchase = false, bool @lock = true) =>
        AwardCredits(player, amount, Localization.Translate(message, player, arg1, arg2), redmessage, isPurchase, @lock);
    public static Task AwardCreditsAsync(UCPlayer player, int amount, Translation message, bool redmessage = false, bool isPurchase = false, bool @lock = true, CancellationToken token = default) =>
        AwardCreditsAsync(player, amount, Localization.Translate(message, player), redmessage, isPurchase, @lock, token);
    public static async Task AwardCreditsAsync(CreditsParameters parameters, CancellationToken token = default, bool @lock = true)
    {
        token.CombineIfNeeded(UCWarfare.UnloadCancel);
        UCPlayer? player = parameters.Player;
        Task? remote = null;
        bool locked = false;
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
            {
                await player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
                locked = true;
            }
            if (parameters.Amount == 0 || PointsConfigObj.Data.GlobalXPMultiplier <= 0f || parameters.StartingMultiplier <= 0f)
                return;
            int amount = Mathf.RoundToInt(parameters.Amount * PointsConfigObj.Data.GlobalXPMultiplier * parameters.StartingMultiplier);

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
            if (locked)
                player!.PurchaseSync.Release();
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
    public static void AwardXP(UCPlayer player, XPReward reward, int amount)
    {
        AwardXP(new XPParameters(player, 0, amount)
        {
            Reward = reward,
            Message = PointsConfig.GetDefaultTranslation(player.Language, reward)
        });
    }
    public static void AwardXP(UCPlayer player, XPReward reward, float multiplier = 1f)
    {
        AwardXP(new XPParameters(player, 0, reward)
        {
            Multiplier = multiplier,
            Message = PointsConfig.GetDefaultTranslation(player.Language, reward)
        });
    }
    public static void AwardXP(UCPlayer player, XPReward reward, Translation translation, float multiplier = 1f)
    {
        string? t;
        if (translation is Translation<VehicleType> vehicleTranslation &&
            PointsConfig.TryGetVehicleType(reward, out VehicleType type))
        {
            t = vehicleTranslation.Translate(player, type);
        }
        else t = translation.Translate(player);
        AwardXP(new XPParameters(player, 0, reward) { Multiplier = multiplier, Message = t });
    }
    public static void AwardXP(UCPlayer player, XPReward reward, Translation translation, int amount)
    {
        string? t;
        if (translation is Translation<VehicleType> vehicleTranslation && PointsConfig.TryGetVehicleType(reward, out VehicleType type))
        {
            t = vehicleTranslation.Translate(player, type);
        }
        else t = translation.Translate(player);
        AwardXP(new XPParameters(player, 0, amount) { Reward = reward, Message = t });
    }
    public static void AwardXP(UCPlayer player, XPReward reward, string message, float multiplier = 1f)
    {
        AwardXP(new XPParameters(player, 0, reward) { Multiplier = multiplier, Message = message });
    }
    public static void AwardXP(UCPlayer player, XPReward reward, string message, int amount)
    {
        AwardXP(new XPParameters(player, 0, amount) { Reward = reward, Message = message });
    }
    public static void AwardXP(in XPParameters parameters)
    {
        UCWarfare.RunTask(AwardXPAsync, parameters, UCWarfare.UnloadCancel, ctx: "Award xp to " + parameters.Steam64 + ".");
    }
    public static async Task AwardXPAsync(XPParameters parameters, CancellationToken token = default)
    {
        try
        {
            // cancel on unload
            token.CombineIfNeeded(UCWarfare.UnloadCancel);
            Task? remote = null;
            await UCWarfare.ToUpdate(token);

            int origAmt = parameters.Amount;
            PointsConfig.XPData.TryGetValue(parameters.Reward, out PointsConfig.XPRewardData? data);
            if (origAmt == 0)
            {
                if (data == null || data.Amount == 0)
                    return;
                origAmt = data.Amount;
            }

            if (!Data.TrackStats || PointsConfigObj.Data.GlobalXPMultiplier == 0f) return;
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            float multiplier = -1;
            UCPlayer? player = parameters.Player;

            if (player is { IsOnline: true } && data is not { IgnoresXPBoosts: true })
            {
                // checks for any xp boost buffs
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
            float amt = origAmt;
            if (data is not { IgnoresGlobalMultiplier: true })
                amt *= PointsConfig.GlobalXPMultiplier;

            if (data is not { IgnoresXPBoosts: true })
                amt *= multiplier;

            amt *= parameters.Multiplier;

            if (amt == 0)
                return;

            ulong team = parameters.Team;
            bool awardCredits = data != null ? data.CreditReward is { Amount: not 0 } or { Percentage: not 0 } : parameters.AwardCredits;

            // get current team if not specified
            if (team is < 1 or > 2)
            {
                if (player is not { IsOnline: true })
                {
                    PlayerSave? save = PlayerManager.GetSave(parameters.Steam64);
                    if (save == null)
                        return;
                    team = save.Team;
                }
                else team = player.GetTeam();
            }

            if (team is < 1 or > 2)
                return;
            LevelData oldLevel = default;
            int amtXp = Mathf.RoundToInt(amt);
            int credits = 0;

            // calculate number of credits
            if (awardCredits)
            {
                float mult;
                if (data != null)
                {
                    mult = data.CreditReward!.Percentage;
                    if (mult == 0)
                        mult = data.CreditReward.Amount / (float)origAmt;
                }
                else mult = (parameters.OverrideCreditPercentage ?? (PointsConfig.DefaultCreditPercentage / 100f));
                float cAmt = mult * origAmt;
                if (cAmt > 0)
                    credits = Mathf.CeilToInt(cAmt);
                else if (cAmt < 0)
                    credits = Mathf.FloorToInt(cAmt);
            }
            if (player != null)
            {
                await player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
                oldLevel = player.Level;
            }
            try
            {
                int currentAmount;
                int credsAmt = -1;
                if (player is { IsOnline: true })
                {
                    // Begin adding to remote database. Will not be awaited yet.
                    if (Data.RemoteSQL != null && Data.RemoteSQL.Opened)
                    {
                        if (credits != 0)
                            remote = Data.RemoteSQL.AddCreditsAndXP(parameters.Steam64, team, credits, amtXp, token);
                        else
                            remote = Data.RemoteSQL.AddXP(parameters.Steam64, team, amtXp, token);
                    }
                    if (credits != 0)
                    {
                        (credsAmt, currentAmount) = await Data.DatabaseManager.AddCreditsAndXP(parameters.Steam64, team, credits, amtXp, token).ConfigureAwait(false);
                    }
                    else
                    {
                        currentAmount = await Data.DatabaseManager.AddXP(parameters.Steam64, team, amtXp, token).ConfigureAwait(false);
                    }

                    // action logs
                    ActionLog.Add(ActionLogType.XPChanged, (currentAmount - amtXp).ToString(Data.AdminLocale) + " >> " +
                                                                currentAmount.ToString(Data.AdminLocale) +
                                                                " (" + (amtXp > 0 ? "+" : "-")
                                                                + Math.Abs(amtXp).ToString(Data.AdminLocale) +
                                                                ") (" + parameters.Reward + ")", parameters.Steam64);
                    if (credits != 0)
                    {
                        ActionLog.Add(ActionLogType.CreditsChanged, (credsAmt - credits).ToString(Data.AdminLocale) + " >> " +
                                                                    credsAmt.ToString(Data.AdminLocale) +
                                                                    " (" + (credits > 0 ? "+" : "-")
                                                                    + Math.Abs(credits).ToString(Data.AdminLocale) +
                                                                    ") (With XP: " + parameters.Reward + ")", parameters.Steam64);
                    }
                    await UCWarfare.ToUpdate(token);

                    if (player.IsOnline)
                    {
                        player.CachedXP = currentAmount;
                        if (credits != 0)
                            player.CachedCredits = credsAmt;

                        // leaderboard stats
                        if (data is not { ExcludeFromLeaderboard: true } && player.Player.TryGetPlayerData(out UCPlayerData c) && c.Stats is IExperienceStats kd)
                        {
                            kd.AddXP(amtXp);
                            if (credits != 0)
                                kd.AddCredits(credits);
                        }
                    }

                    // toasts
                    if (player.IsOnline && !player.HasUIHidden && !Data.Gamemode.LeaderboardUp())
                    {
                        string number = Localization.Translate(amtXp >= 0 ? T.XPToastGainXP : T.XPToastLoseXP, player, Math.Abs(amtXp));

                        number = number.Colorize(amtXp > 0 ? "e3e3e3" : "d69898");
                        
                        ToastMessage.QueueMessage(player,
                            !string.IsNullOrEmpty(parameters.Message)
                                ? new ToastMessage(number + "\n" + parameters.Message!.Colorize("adadad"), ToastMessageSeverity.Mini)
                                : new ToastMessage(number, ToastMessageSeverity.Mini));
                        if (credits != 0)
                        {
                            Translation<int> key = credits < 0 ? T.XPToastLoseCredits : T.XPToastGainCredits;

                            number = Localization.Translate(key, player, Math.Abs(credits));
                            ToastMessage.QueueMessage(player, !string.IsNullOrEmpty(parameters.Message)
                                    ? new ToastMessage(number + "\n" + parameters.Message!.Colorize("adadad"), ToastMessageSeverity.Mini)
                                    : new ToastMessage(number, ToastMessageSeverity.Mini));

                            player.PointsDirtyMask |= 0b00001000;
                            CreditsUI.Update(player, false);
                        }
                    }
                }
                else if (awardCredits)
                {
                    if (Data.RemoteSQL != null)
                    {
                        (int credits, int xp)[] arr = await Task.WhenAll(
                                Data.DatabaseManager.AddCreditsAndXP(parameters.Steam64, team, credits, amtXp, token),
                                Data.RemoteSQL.AddCreditsAndXP(parameters.Steam64, team, credits, amtXp, token))
                            .ConfigureAwait(false);
                        currentAmount = arr.Length > 0 ? arr[0].xp : amtXp;
                    }
                    else
                        currentAmount = (await Data.DatabaseManager.AddCreditsAndXP(parameters.Steam64, team, credits, amtXp, token).ConfigureAwait(false)).Item2;
                    await UCWarfare.ToUpdate(token);
                }
                else
                {
                    currentAmount = await Data.DatabaseManager.AddXP(parameters.Steam64, team, amtXp, token).ConfigureAwait(false);
                    await UCWarfare.ToUpdate(token);
                }

                if (player == null)
                    oldLevel = new LevelData(Mathf.RoundToInt(currentAmount - amt));
            }
            finally
            {
                player?.PurchaseSync.Release();
            }

            // check for promotions or demotions
            if (player is { IsOnline: true })
            {
                player.PointsDirtyMask |= 0b00000001;
                if (player.Level.Level > oldLevel.Level)
                {
                    ToastMessage.QueueMessage(player,
                        new ToastMessage(Localization.Translate(T.ToastPromoted, player), player.Level.Name.ToUpper(),
                            ToastMessageSeverity.Big));
                    player.PointsDirtyMask |= 0b00000010;
                    Signs.UpdateAllSigns(player);
                }
                else if (player.Level.Level < oldLevel.Level)
                {
                    ToastMessage.QueueMessage(player,
                        new ToastMessage(Localization.Translate(T.ToastDemoted, player), player.Level.Name.ToUpper(),
                            ToastMessageSeverity.Big));
                    player.PointsDirtyMask |= 0b00000010;
                    Signs.UpdateAllSigns(player);
                }

                XPUI.Update(player, false);
                for (int i = 0; i < player.ActiveBuffs.Length; ++i)
                    if (player.ActiveBuffs[i] is IXPBoostBuff buff)
                        buff.OnXPBoostUsed(amtXp, awardCredits);
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
            bars[i] = PointsConfig.ProgressBlockCharacter;
        }
        return new string(bars);
    }
    public static void TryAwardDriverAssist(PlayerDied args, XPReward reward, int amount = 0, float quota = 0)
    {
        if (args.DriverAssist is null || !args.DriverAssist.IsOnline) return;
        if (amount == 0 && PointsConfig.XPData.TryGetValue(reward, out PointsConfig.XPRewardData data))
            amount = data.Amount;

        AwardXP(args.DriverAssist, reward, T.XPToastKillDriverAssist, amount);
        /*
        if (quota != 0 && args.ActiveVehicle != null && args.ActiveVehicle.TryGetComponent(out VehicleComponent comp))
            comp.Quota += quota;*/
    }
    public static void TryAwardDriverAssist(Player gunner, XPReward reward, int amount = 0, float quota = 0)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        InteractableVehicle vehicle = gunner.movement.getVehicle();
        if (vehicle != null)
        {
            if (amount == 0 && PointsConfig.XPData.TryGetValue(reward, out PointsConfig.XPRewardData data))
                amount = data.Amount;
            SteamPlayer driver = vehicle.passengers[0].player;
            if (driver != null &&
                driver.playerID.steamID.m_SteamID != gunner.channel.owner.playerID.steamID.m_SteamID &&
                UCPlayer.FromSteamPlayer(driver) is { } ucplayer)
            {
                AwardXP(ucplayer, reward, T.XPToastKillDriverAssist, amount);
            }

            //if (vehicle.transform.TryGetComponent(out VehicleComponent component))
            //{
            //    component.Quota += quota;
            //}
        }
    }
    public static void TryAwardFOBCreatorXP(FOB fob, XPReward reward, float multiplier = 1f)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayer? creator = UCPlayer.FromID(fob.Creator);

        if (creator != null)
        {
            AwardXP(creator, reward, multiplier);
        }

        if (fob.Placer != fob.Creator)
        {
            UCPlayer? placer = UCPlayer.FromID(fob.Placer);
            if (placer != null)
                AwardXP(placer, reward, multiplier);
        }
    }
    public static void OnPlayerDeath(PlayerDied e)
    {
        if (e.Killer is null || !e.Killer.IsOnline) return;
        if (e.WasTeamkill)
        {
            AwardXP(e.Killer, XPReward.Teamkill);
            return;
        }
        AwardXP(e.Killer, XPReward.EnemyKilled);

        if (e.Player.Player.TryGetPlayerData(out UCPlayerData component))
        {
            ulong killerID = e.Killer.Steam64;
            ulong victimID = e.Player.Steam64;

            UCPlayer? assister = UCPlayer.FromID(component.SecondLastAttacker.Key);
            if (assister != null && assister.Steam64 != killerID && assister.Steam64 != victimID && (DateTime.Now - component.SecondLastAttacker.Value).TotalSeconds <= 30)
            {
                AwardXP(assister, XPReward.KillAssist);
            }

            if (e.Player.Player.TryGetComponent(out SpottedComponent spotted) &&
                PointsConfig.XPData.TryGetValue(XPReward.EnemyKilled, out PointsConfig.XPRewardData data))
            {
                spotted.OnTargetKilled(Mathf.RoundToInt(data.Amount * 0.75f));
            }

            component.ResetAttackers();
        }
        TryAwardDriverAssist(e, XPReward.EnemyKilled, quota: 1);
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
        XPReward xpreward = e.VehicleData.Type switch
        {
            VehicleType.Humvee => XPReward.VehicleHumvee,
            VehicleType.TransportGround => XPReward.VehicleTransportGround,
            VehicleType.ScoutCar => XPReward.VehicleScoutCar,
            VehicleType.LogisticsGround => XPReward.VehicleLogisticsGround,
            VehicleType.APC => XPReward.VehicleAPC,
            VehicleType.IFV => XPReward.VehicleIFV,
            VehicleType.MBT => XPReward.VehicleMBT,
            VehicleType.TransportAir => XPReward.VehicleTransportAir,
            VehicleType.AttackHeli => XPReward.VehicleAttackHeli,
            VehicleType.Jet => XPReward.VehicleJet,
            VehicleType.AA => XPReward.VehicleAA,
            VehicleType.HMG => XPReward.VehicleHMG,
            VehicleType.ATGM => XPReward.VehicleATGM,
            VehicleType.Mortar => XPReward.VehicleMortar,
            _ => XPReward.VehicleOther
        };
        if (xpreward != XPReward.VehicleOther)
        {
            ulong dteam = e.Instigator.GetTeam();
            bool vehicleWasEnemy = (dteam == 1 && e.Team == 2) || (dteam == 2 && e.Team == 1);
            bool vehicleWasFriendly = dteam == e.Team;
            if (!vehicleWasFriendly)
                Stats.StatsManager.ModifyTeam(dteam, t => t.VehiclesDestroyed++, false);
            int fullXP = 0;
            if (PointsConfig.XPData.TryGetValue(xpreward, out PointsConfig.XPRewardData data) && data.Amount > 0)
                fullXP = data.Amount;


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
                                AwardXP(attacker, xpreward, reward);
                                UCPlayer? pl = e.LastDriver ?? e.Owner;
                                if (pl is not null && pl.Steam64 != e.InstigatorId)
                                    TryAwardDriverAssist(pl, xpreward, reward, e.VehicleData.TicketCost);

                                if (e.Spotter != null)
                                {
                                    e.Spotter.OnTargetKilled(reward);
                                    Destroy(e.Spotter);
                                }
                            }
                            else if (responsibleness > 0.1F)
                                AwardXP(attacker, xpreward, T.XPToastKillVehicleAssist, reward);
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
public enum XPReward
{
    Custom = 0,
    OnDuty,
    EnemyKilled,
    KillAssist,
    Teamkill,
    Revive,
    RadioDestroyed,
    FriendlyRadioDestroyed,
    BunkerDestroyed,
    FriendlyBunkerDestroyed,
    BunkerDeployment,
    FortificationDestroyed,
    FriendlyFortificationDestroyed,
    BuildableDestroyed,
    FriendlyBuildableDestroyed,
    CacheDestroyed,
    FriendlyCacheDestroyed,
    FlagCaptured,
    FlagNeutralized,
    AttackingFlag,
    DefendingFlag,
    TransportingPlayer,
    Shoveling,
    BunkerBuilt,
    Resupply,
    RepairVehicle,
    UnloadSupplies,
    VehicleOther,
    VehicleHumvee,
    VehicleTransportGround,
    VehicleScoutCar,
    VehicleLogisticsGround,
    VehicleAPC,
    VehicleIFV,
    VehicleMBT,
    VehicleTransportAir,
    VehicleAttackHeli,
    VehicleJet,
    VehicleAA,
    VehicleHMG,
    VehicleATGM,
    VehicleMortar
}

public class PointsConfig : JSONConfigData
{
    [JsonIgnore]
    public const float DefaultCreditPercentage = 15f;

    [JsonPropertyName("player_starting_credits")]
    public int StartingCredits { get; set; }

    [JsonPropertyName("block_char")]
    public char ProgressBlockCharacter { get; set; }

    [JsonPropertyName("xp_data")]
    public Dictionary<XPReward, XPRewardData> XPData { get; set; }

    [JsonPropertyName("global_xp_multiplier")]
    public float GlobalXPMultiplier { get; set; }

    public override void SetDefaults()
    {
        ProgressBlockCharacter = '█';
        XPData = new Dictionary<XPReward, XPRewardData>(35)
        {
            { XPReward.Custom,
                new XPRewardData(0)
                {
                    IgnoresGlobalMultiplier = true,
                    IgnoresXPBoosts = true,
                    CreditReward = null,
                    ExcludeFromLeaderboard = true
                }
            },
            { XPReward.OnDuty,
                new XPRewardData(5)
                {
                    ExcludeFromLeaderboard = true,
                }
            },
            { XPReward.EnemyKilled, new XPRewardData(10, DefaultCreditPercentage) },
            { XPReward.CacheDestroyed, new XPRewardData(800, DefaultCreditPercentage) },
            { XPReward.FriendlyCacheDestroyed,
                new XPRewardData(-8000)
                {
                    CreditReward = new CreditRewardData(DefaultCreditPercentage)
                    {
                        IsPunishment = true
                    }
                }
            },
            { XPReward.KillAssist, new XPRewardData(5, DefaultCreditPercentage) },
            { XPReward.Teamkill,
                new XPRewardData(-30)
                {
                    CreditReward = new CreditRewardData(DefaultCreditPercentage)
                    {
                        IsPunishment = true
                    }
                }
            },
            { XPReward.Revive, new XPRewardData(30, DefaultCreditPercentage * 1.5f) },
            { XPReward.RadioDestroyed, new XPRewardData(80, DefaultCreditPercentage * 1.5f) },
            { XPReward.FriendlyRadioDestroyed,
                new XPRewardData(-1000)
                {
                    CreditReward = new CreditRewardData(DefaultCreditPercentage)
                    {
                        IsPunishment = true
                    }
                }
            },
            { XPReward.BunkerDestroyed, new XPRewardData(60, DefaultCreditPercentage * 1.5f) },
            { XPReward.FriendlyBunkerDestroyed,
                new XPRewardData(-800)
                {
                    CreditReward = new CreditRewardData(DefaultCreditPercentage)
                    {
                        IsPunishment = true
                    }
                }
            },
            { XPReward.BunkerDeployment, new XPRewardData(10, DefaultCreditPercentage) },
            { XPReward.FlagCaptured, new XPRewardData(50, DefaultCreditPercentage) },
            { XPReward.FlagNeutralized, new XPRewardData(80, DefaultCreditPercentage) },
            { XPReward.AttackingFlag, new XPRewardData(8, DefaultCreditPercentage) },
            { XPReward.DefendingFlag, new XPRewardData(6, DefaultCreditPercentage) },
            { XPReward.TransportingPlayer, new XPRewardData(10, DefaultCreditPercentage) },
            { XPReward.Shoveling, new XPRewardData(2, 50f) },
            { XPReward.BunkerBuilt, new XPRewardData(100, DefaultCreditPercentage * 1.5f) },
            { XPReward.Resupply, new XPRewardData(20, DefaultCreditPercentage) },
            { XPReward.RepairVehicle, new XPRewardData(3, DefaultCreditPercentage) },
            { XPReward.UnloadSupplies, new XPRewardData(20, DefaultCreditPercentage) },
            { XPReward.FriendlyFortificationDestroyed, new XPRewardData(0, DefaultCreditPercentage) }, // dependant amount
            { XPReward.FortificationDestroyed, new XPRewardData(0, DefaultCreditPercentage) }, // dependant amount
            { XPReward.FriendlyBuildableDestroyed, new XPRewardData(0, DefaultCreditPercentage) }, // dependant amount
            { XPReward.BuildableDestroyed, new XPRewardData(0, DefaultCreditPercentage) }, // dependant amount
            { XPReward.VehicleHumvee, new XPRewardData(25, DefaultCreditPercentage) },
            { XPReward.VehicleTransportGround, new XPRewardData(20, DefaultCreditPercentage) },
            { XPReward.VehicleScoutCar, new XPRewardData(30, DefaultCreditPercentage) },
            { XPReward.VehicleLogisticsGround, new XPRewardData(25, DefaultCreditPercentage) },
            { XPReward.VehicleAPC, new XPRewardData(60, DefaultCreditPercentage) },
            { XPReward.VehicleIFV, new XPRewardData(70, DefaultCreditPercentage) },
            { XPReward.VehicleMBT, new XPRewardData(100, DefaultCreditPercentage) },
            { XPReward.VehicleTransportAir, new XPRewardData(30, DefaultCreditPercentage) },
            { XPReward.VehicleAttackHeli, new XPRewardData(150, DefaultCreditPercentage) },
            { XPReward.VehicleJet, new XPRewardData(200, DefaultCreditPercentage) },
            { XPReward.VehicleAA, new XPRewardData(20, DefaultCreditPercentage) },
            { XPReward.VehicleHMG, new XPRewardData(20, DefaultCreditPercentage) },
            { XPReward.VehicleATGM, new XPRewardData(20, DefaultCreditPercentage) },
            { XPReward.VehicleMortar, new XPRewardData(20, DefaultCreditPercentage) }
        };

        GlobalXPMultiplier = 1f;
    }
    internal static bool TryGetVehicleType(XPReward reward, out VehicleType type)
    {
        switch (reward)
        {
            case XPReward.VehicleHumvee:
                type = VehicleType.Humvee;
                break;
            case XPReward.VehicleTransportGround:
                type = VehicleType.TransportGround;
                break;
            case XPReward.VehicleScoutCar:
                type = VehicleType.ScoutCar;
                break;
            case XPReward.VehicleLogisticsGround:
                type = VehicleType.LogisticsGround;
                break;
            case XPReward.VehicleAPC:
                type = VehicleType.APC;
                break;
            case XPReward.VehicleIFV:
                type = VehicleType.IFV;
                break;
            case XPReward.VehicleMBT:
                type = VehicleType.MBT;
                break;
            case XPReward.VehicleTransportAir:
                type = VehicleType.TransportAir;
                break;
            case XPReward.VehicleAttackHeli:
                type = VehicleType.AttackHeli;
                break;
            case XPReward.VehicleJet:
                type = VehicleType.Jet;
                break;
            case XPReward.VehicleAA:
                type = VehicleType.AA;
                break;
            case XPReward.VehicleHMG:
                type = VehicleType.HMG;
                break;
            case XPReward.VehicleATGM:
                type = VehicleType.ATGM;
                break;
            case XPReward.VehicleMortar:
                type = VehicleType.Mortar;
                break;
            default:
                type = VehicleType.None;
                return false;
        }

        return true;
    }
    internal static string GetDefaultTranslation(string language, XPReward reward)
    {
        if (TryGetVehicleType(reward, out VehicleType vtype))
        {
            return T.XPToastVehicleDestroyed.Translate(language, vtype);
        }
        Translation? t = reward switch
        {
            XPReward.UnloadSupplies => T.XPToastSuppliesUnloaded,
            XPReward.OnDuty => T.XPToastOnDuty,
            XPReward.EnemyKilled => T.XPToastEnemyKilled,
            XPReward.KillAssist => T.XPToastKillAssist,
            XPReward.Teamkill => T.XPToastFriendlyKilled,
            XPReward.Revive => T.XPToastHealedTeammate,
            XPReward.RadioDestroyed => T.XPToastFOBDestroyed,
            XPReward.FriendlyRadioDestroyed => T.XPToastFriendlyFOBDestroyed,
            XPReward.BunkerDestroyed => T.XPToastBunkerDestroyed,
            XPReward.FriendlyBunkerDestroyed => T.XPToastFriendlyBunkerDestroyed,
            XPReward.BunkerDeployment => T.XPToastFOBUsed,
            XPReward.FlagCaptured => T.XPToastFlagCaptured,
            XPReward.FlagNeutralized => T.XPToastFlagNeutralized,
            XPReward.AttackingFlag => T.XPToastFlagAttackTick,
            XPReward.DefendingFlag => T.XPToastFlagDefenseTick,
            XPReward.TransportingPlayer => T.XPToastTransportingPlayers,
            XPReward.Resupply => T.XPToastResuppliedTeammate,
            XPReward.RepairVehicle => T.XPToastRepairedVehicle,
            XPReward.CacheDestroyed => T.XPToastCacheDestroyed,
            XPReward.FriendlyCacheDestroyed => T.XPToastFriendlyCacheDestroyed,
            _ => null
        };
        if (t == null)
            return "{" + reward.ToString().ToUpperInvariant() + "}";
        return t.Translate(language);
    }

    public sealed class XPRewardData
    {
        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("ignores_global_multiplier")]
        public bool IgnoresGlobalMultiplier { get; set; }

        [JsonPropertyName("ignores_xp_boosts")]
        public bool IgnoresXPBoosts { get; set; }

        [JsonPropertyName("credit_reward")]
        public CreditRewardData? CreditReward { get; set; }

        [JsonPropertyName("exclude_from_leaderboard")]
        public bool ExcludeFromLeaderboard { get; set; }

        public XPRewardData() { }
        public XPRewardData(int amount)
        {
            Amount = amount;
        }
        public XPRewardData(int amount, float creditPercentage)
        {
            Amount = amount;
            CreditReward = new CreditRewardData(creditPercentage);
        }
    }

    public sealed class CreditRewardData
    {
        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("percentage")]
        public float Percentage { get; set; }

        [JsonPropertyName("is_punishment")]
        public bool IsPunishment { get; set; }

        [JsonPropertyName("is_purchase")]
        public bool IsPurchase { get; set; }

        public CreditRewardData() { }
        public CreditRewardData(int amount)
        {
            Amount = amount;
        }
        public CreditRewardData(float percentage)
        {
            Percentage = percentage / 100f;
        }
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
    public XPReward Reward;
    public string? Message;
    public bool AwardCredits;
    public float Multiplier = 1f;
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
    public static XPParameters WithTranslation(UCPlayer player, Translation translation, XPReward reward) =>
        new XPParameters(player, player.GetTeam(), reward, translation.Translate(player), true);
    public static XPParameters WithTranslation<T>(UCPlayer player, Translation<T> translation, T arg, XPReward reward) =>
        new XPParameters(player, player.GetTeam(), reward, translation.Translate(player, arg), true);
    public static XPParameters WithTranslation<T1, T2>(UCPlayer player, Translation<T1, T2> translation, T1 arg1, T2 arg2, XPReward reward) =>
        new XPParameters(player, player.GetTeam(), reward, translation.Translate(player, arg1, arg2), true);
    public static XPParameters WithTranslation(UCPlayer player, ulong team, Translation translation, XPReward reward) =>
        new XPParameters(player, team, reward, translation.Translate(player), true);
    public static XPParameters WithTranslation<T>(UCPlayer player, ulong team, Translation<T> translation, T arg, XPReward reward) =>
        new XPParameters(player, team, reward, translation.Translate(player, arg), true);
    public static XPParameters WithTranslation<T1, T2>(UCPlayer player, ulong team, Translation<T1, T2> translation, T1 arg1, T2 arg2, XPReward reward) =>
        new XPParameters(player, team, reward, translation.Translate(player, arg1, arg2), true);
    public static XPParameters WithTranslation(UCPlayer player, Translation translation, XPReward reward, int amount) =>
        new XPParameters(player, player.GetTeam(), amount, translation.Translate(player), true) { Reward = reward };
    public static XPParameters WithTranslation<T>(UCPlayer player, Translation<T> translation, T arg, XPReward reward, int amount) =>
        new XPParameters(player, player.GetTeam(), amount, translation.Translate(player, arg), true) { Reward = reward };
    public static XPParameters WithTranslation<T1, T2>(UCPlayer player, Translation<T1, T2> translation, T1 arg1, T2 arg2, XPReward reward, int amount) =>
        new XPParameters(player, player.GetTeam(), amount, translation.Translate(player, arg1, arg2), true) { Reward = reward };
    public static XPParameters WithTranslation(UCPlayer player, ulong team, Translation translation, XPReward reward, int amount) =>
        new XPParameters(player, team, amount, translation.Translate(player), true) { Reward = reward };
    public static XPParameters WithTranslation<T>(UCPlayer player, ulong team, Translation<T> translation, T arg, XPReward reward, int amount) =>
        new XPParameters(player, team, amount, translation.Translate(player, arg), true) { Reward = reward };
    public static XPParameters WithTranslation<T1, T2>(UCPlayer player, ulong team, Translation<T1, T2> translation, T1 arg1, T2 arg2, XPReward reward, int amount) =>
        new XPParameters(player, team, amount, translation.Translate(player, arg1, arg2), true) { Reward = reward };
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
        Reward = XPReward.Custom;
    }
    public XPParameters(ulong player, ulong team, XPReward reward)
    {
        if (!Util.IsValidSteam64Id(player))
            throw new ArgumentException("Invalid Steam64 ID: " + player, nameof(player));
        Steam64 = player;
        Player = UCPlayer.FromID(player);
        Amount = 0;
        Team = team;
        AwardCredits = true;
        Message = null;
        Reward = reward;
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
        Reward = XPReward.Custom;
    }
    public XPParameters(ulong player, ulong team, XPReward reward, string? message, bool awardCredits)
    {
        if (!Util.IsValidSteam64Id(player))
            throw new ArgumentException("Invalid Steam64 ID: " + player, nameof(player));
        Steam64 = player;
        Player = UCPlayer.FromID(player);
        Team = team;
        Amount = 0;
        Message = message;
        AwardCredits = awardCredits;
        Reward = reward;
    }
    public XPParameters(UCPlayer player, ulong team, int amount)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        Steam64 = player.Steam64;
        Amount = amount;
        Team = team;
        AwardCredits = true;
        Message = null;
        Reward = XPReward.Custom;
    }
    public XPParameters(UCPlayer player, ulong team, XPReward reward)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        Steam64 = player.Steam64;
        Amount = 0;
        Team = team;
        AwardCredits = true;
        Message = null;
        Reward = reward;
    }
    public XPParameters(UCPlayer player, ulong team, XPReward reward, string? message, bool awardCredits)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        Steam64 = player.Steam64;
        Team = team;
        Amount = 0;
        Message = message;
        AwardCredits = awardCredits;
        Reward = reward;
    }
    public XPParameters(UCPlayer player, ulong team, int amount, string? message, bool awardCredits)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        Steam64 = player.Steam64;
        Team = team;
        Amount = amount;
        Message = message;
        AwardCredits = awardCredits;
        Reward = XPReward.Custom;
    }

    public Task Award() => Points.AwardXPAsync(this);
    public Task Award(CancellationToken token) => Points.AwardXPAsync(this, token);
}