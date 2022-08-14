using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Events.Vehicles;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using static Uncreated.Warfare.Components.SpottedComponent;
using static Uncreated.Warfare.Gamemodes.Flags.ZoneModel;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare.Point;

public static class Points
{
    private const int XPUI_KEY = 26969;
    private const int CREDITSUI_KEY = 26971;
    private static readonly Config<XPConfig> _xpconfig = UCWarfare.IsLoaded ? new Config<XPConfig>(Data.Paths.PointsStorage, "xp.json") : null!;
    private static readonly Config<TWConfig> _twconfig = UCWarfare.IsLoaded ? new Config<TWConfig>(Data.Paths.PointsStorage, "tw.json") : null!;
    private static readonly Config<CreditsConfig> _creditsconfig = UCWarfare.IsLoaded ? new Config<CreditsConfig>(Data.Paths.PointsStorage, "credits.json") : null!;
    public static XPConfig XPConfig => _xpconfig.Data;
    public static TWConfig TWConfig => _twconfig.Data;
    public static CreditsConfig CreditsConfig => _creditsconfig.Data;

    public static OfficerStorage Officers;

    public static void Initialize()
    {
        Officers = new OfficerStorage();
        EventDispatcher.OnGroupChanged += OnGroupChanged;
        EventDispatcher.OnVehicleDestroyed += OnVehicleDestoryed;
    }
    public static void ReloadConfig()
    {
        _xpconfig.Reload();
        _twconfig.Reload();
        _creditsconfig.Reload();
    }
    public static void OnPlayerJoined(UCPlayer player, bool isnewGame)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!isnewGame && (player.IsTeam1() || player.IsTeam2()))
        {
            UpdateXPUI(player);
            UpdateCreditsUI(player);
        }
    }

    private static void OnGroupChanged(GroupChanged e)
    {
        UCPlayer player = e.Player;
        ulong newGroup = e.NewGroup;
        Task.Run(async () =>
        {
            Task<int> t1 = Data.DatabaseManager.GetXP(player.Steam64, player.GetTeam());
            Task<int> t2 = Data.DatabaseManager.GetCredits(player.Steam64, player.GetTeam());
            await UCWarfare.ToUpdate();

            player.CachedXP = await t1;
            player.CachedCredits = await t2;

            if (newGroup is > 0 and < 3)
            {
                UpdateXPUI(player);
                UpdateCreditsUI(player);
            }
            else
            {
                EffectManager.askEffectClearByID(XPConfig.RankUI, player.Player.channel.owner.transportConnection);
                EffectManager.askEffectClearByID(TWConfig.MedalsUI, player.Player.channel.owner.transportConnection);
            }
        }).ConfigureAwait(false);
    }
    public static readonly int[] LEVELS = new int[]
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
        for (int i = 0; i < LEVELS.Length; i++)
        {
            if (xp < LEVELS[i])
                return i;
        }
        return LEVELS.Length;
    }
    /// <summary>Get the given <paramref name="level"/>'s starting xp.</summary>
    public static int GetLevelXP(int level)
    {
        if (level >= LEVELS.Length)
            return LEVELS[LEVELS.Length - 1];

        if (level > 0)
            return LEVELS[level - 1];

        return 0;
    }
    /// <summary>Get the level after the given <paramref name="level"/>'s starting xp (or the given <paramref name="level"/>'s end xp.</summary>
    public static int GetNextLevelXP(int level)
    {
        if (level >= LEVELS.Length)
            return 100000;

        if (level >= 0)
            return LEVELS[level];

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
    public static void AwardCredits(UCPlayer player, int amount, Translation message, bool redmessage = false, bool isPurchase = false) =>
        AwardCredits(player, amount, Localization.Translate(message, player), redmessage, isPurchase);
    public static void AwardCredits<T>(UCPlayer player, int amount, Translation<T> message, T arg, bool redmessage = false, bool isPurchase = false) =>
        AwardCredits(player, amount, Localization.Translate(message, player, arg), redmessage, isPurchase);
    public static void AwardCredits<T1, T2>(UCPlayer player, int amount, Translation<T1, T2> message, T1 arg1, T2 arg2, bool redmessage = false, bool isPurchase = false) =>
        AwardCredits(player, amount, Localization.Translate(message, player, arg1, arg2), redmessage, isPurchase);
    public static Task AwardCreditsAsync(UCPlayer player, int amount, Translation message, bool redmessage = false, bool isPurchase = false) =>
        AwardCreditsAsync(player, amount, Localization.Translate(message, player), redmessage, isPurchase);
    public static Task AwardCreditsAsync<T>(UCPlayer player, int amount, Translation<T> message, T arg, bool redmessage = false, bool isPurchase = false) =>
        AwardCreditsAsync(player, amount, Localization.Translate(message, player, arg), redmessage, isPurchase);
    public static Task AwardCreditsAsync<T1, T2>(UCPlayer player, int amount, Translation<T1, T2> message, T1 arg1, T2 arg2, bool redmessage = false, bool isPurchase = false) =>
        AwardCreditsAsync(player, amount, Localization.Translate(message, player, arg1, arg2), redmessage, isPurchase);
    public static void AwardCredits(UCPlayer player, int amount, string? message = null, bool redmessage = false, bool isPurchase = false)
    {
        _ = AwardCreditsAsyncIntl(player, amount, message, redmessage, isPurchase).ConfigureAwait(false);
    }
    public static Task AwardCreditsAsync(UCPlayer player, int amount, string? message = null, bool redmessage = false, bool isPurchase = false)
    {
        return AwardCreditsAsyncIntl(player, amount, message, redmessage, isPurchase);
    }
    private static async Task AwardCreditsAsyncIntl(UCPlayer player, int amount, string? message = null, bool redmessage = false, bool isPurchase = false)
    {
        try
        {
            if (amount == 0 || _xpconfig.Data.XPMultiplier == 0f) return;
            amount = Mathf.RoundToInt(amount * _xpconfig.Data.XPMultiplier);

            int currentAmount = await Data.DatabaseManager.AddCredits(player.Steam64, player.GetTeam(), amount);
            int oldamt = currentAmount - amount;
            await UCWarfare.ToUpdate();

            player.CachedCredits = currentAmount;

            ActionLogger.Add(EActionLogType.CREDITS_CHANGED, oldamt + " >> " + currentAmount, player);

            if (!player.HasUIHidden && (Data.Gamemode is not IEndScreen lb || !lb.IsScreenUp))
            {
                Translation<int> key = T.XPToastGainCredits;
                if (amount < 0)
                {
                    if (redmessage)
                        key = T.XPToastLoseCredits;
                    else
                        key = T.XPToastPurchaseCredits;
                }

                string number = Localization.Translate(key, player, Math.Abs(amount));
                if (!string.IsNullOrEmpty(message))
                    ToastMessage.QueueMessage(player, new ToastMessage(number + "\n" + message!.Colorize("adadad"), EToastMessageSeverity.MINI));
                else
                    ToastMessage.QueueMessage(player, new ToastMessage(number, EToastMessageSeverity.MINI));

                if (!isPurchase && player.Player.TryGetPlayerData(out UCPlayerData c))
                {
                    if (c.stats is IExperienceStats kd)
                        kd.AddCredits(amount);
                }

                UpdateCreditsUI(player);
            }
        }
        catch (Exception ex)
        {
            L.LogError("Error giving credits to " + player.Name.PlayerName + " (" + player.Steam64 + ")", method: "AWARD CREDITS");
            L.LogError(ex, method: "AWARD CREDITS");
        }
    }
    public static void AwardXP(UCPlayer player, int amount, Translation message, bool awardCredits = true) =>
        AwardXP(player, amount, Localization.Translate(message, player), awardCredits);
    public static void AwardXP<T>(UCPlayer player, int amount, Translation<T> message, T arg, bool awardCredits = true) =>
        AwardXP(player, amount, Localization.Translate(message, player, arg), awardCredits);
    public static void AwardXP<T1, T2>(UCPlayer player, int amount, Translation<T1, T2> message, T1 arg1, T2 arg2, bool awardCredits = true) =>
        AwardXP(player, amount, Localization.Translate(message, player, arg1, arg2), awardCredits);
    public static void AwardXP(UCPlayer player, int amount, string? message = null, bool awardCredits = true)
    {
        if (!Data.TrackStats || amount == 0 || _xpconfig.Data.XPMultiplier == 0f) return;
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        amount = Mathf.RoundToInt(amount * _xpconfig.Data.XPMultiplier);
        Task.Run(async () =>
        {
            RankData oldRank = player.Rank;
            int currentAmount = await Data.DatabaseManager.AddXP(player.Steam64, player.GetTeam(), amount);
            await UCWarfare.ToUpdate();

            player.CachedXP = currentAmount;

            if (player.Player.TryGetPlayerData(out UCPlayerData c))
            {
                if (c.stats is IExperienceStats kd)
                    kd.AddXP(amount);
            }

            if (!player.HasUIHidden && (Data.Gamemode is not IEndScreen lb || !lb.IsScreenUp))
            {
                string number = Localization.Translate(amount >= 0 ? T.XPToastGainXP : T.XPToastLoseXP, player, Math.Abs(amount));

                if (amount > 0)
                    number = number.Colorize("e3e3e3");
                else
                    number = number.Colorize("d69898");

                if (!string.IsNullOrEmpty(message))
                    ToastMessage.QueueMessage(player, new ToastMessage(number + "\n" + message!.Colorize("adadad"), EToastMessageSeverity.MINI));
                else
                    ToastMessage.QueueMessage(player, new ToastMessage(number, EToastMessageSeverity.MINI));
                UpdateXPUI(player);
            }

            ActionLogger.Add(EActionLogType.XP_CHANGED, oldRank.TotalXP + " >> " + currentAmount, player);

            if (awardCredits)
                AwardCredits(player, Mathf.RoundToInt(0.15f * amount), redmessage: true);

            if (player.Rank.Level > oldRank.Level)
            {
                ToastMessage.QueueMessage(player, new ToastMessage(Localization.Translate(T.ToastPromoted, player), player.Rank.Name.ToUpper(), EToastMessageSeverity.BIG));

                if (VehicleSpawner.Loaded)
                {
                    VehicleSpawner.UpdateSigns(player);
                }
                if (RequestSigns.Loaded)
                {
                    RequestSigns.UpdateAllSigns(player);
                }
                if (TraitManager.Loaded)
                {
                    TraitSigns.SendAllTraitSigns(player);
                }
            }
            else if (player.Rank.Level < oldRank.Level)
            {
                ToastMessage.QueueMessage(player, new ToastMessage(Localization.Translate(T.ToastDemoted, player), player.Rank.Name.ToUpper(), EToastMessageSeverity.BIG));

                if (VehicleSpawner.Loaded)
                {
                    foreach (Vehicles.VehicleSpawn spawn in VehicleSpawner.Spawners)
                        spawn.UpdateSign(player.SteamPlayer);
                }
                if (RequestSigns.Loaded)
                {
                    RequestSigns.UpdateAllSigns(player);
                }
                if (TraitManager.Loaded)
                {
                    TraitSigns.SendAllTraitSigns(player);
                }
            }
        });
    }
    public static void AwardXP(Player player, int amount, string? message = null, bool awardCredits = true)
    {
        UCPlayer? pl = UCPlayer.FromPlayer(player);
        if (pl != null)
            AwardXP(pl, amount, message, awardCredits);
        else
            L.LogWarning("Unable to find player.");
    }
    public static void UpdateXPUI(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (player.HasUIHidden || (Data.Is(out IEndScreen lb) && lb.IsScreenUp))
            return;

        EffectManager.sendUIEffect(XPConfig.RankUI, XPUI_KEY, player.Connection, true);
        EffectManager.sendUIEffectText(XPUI_KEY, player.Connection, true,
            "Rank", player.Rank.Name
        );
        //EffectManager.sendUIEffectText(XPUI_KEY, player.connection, true,
        //    "Level", player.Rank.Level == 0 ? string.Empty : Translation.Translate("ui_xp_level", player, player.Rank.Level.ToString(Data.Locale))
        //);
        EffectManager.sendUIEffectText(XPUI_KEY, player.Connection, true,
            "XP", player.Rank.CurrentXP + "/" + player.Rank.RequiredXP
        );
        EffectManager.sendUIEffectText(XPUI_KEY, player.Connection, true,   
            "Next", player.Rank.NextAbbreviation
        );
        EffectManager.sendUIEffectText(XPUI_KEY, player.Connection, true,
            "Progress", player.Rank.ProgressBar
        );
    }
    public static void UpdateCreditsUI(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif

        if (player.HasUIHidden || (Data.Is(out IEndScreen lb) && lb.IsScreenUp))
            return;

        EffectManager.sendUIEffect(CreditsConfig.CreditsUI, CREDITSUI_KEY, player.Connection, true);
        EffectManager.sendUIEffectText(CREDITSUI_KEY, player.Connection, true,  
            "Credits", "<color=#b8ffc1>C</color>  " + player.CachedCredits
        );
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
    public static void FlagNeutralized(Flag flag, ulong team)
    {
        foreach (Player nelsonplayer in flag.PlayersOnFlag.Where(p => p.GetTeam() == team))
        {
            UCPlayer? player = UCPlayer.FromPlayer(nelsonplayer);

            if (player == null) continue;
            int xp = XPConfig.FlagNeutralizedXP;

            AwardXP(player, xp, T.XPToastFlagNeutralized);
            xp = player.NearbyMemberBonus(xp, 60) - xp;
            if (xp > 0)
                AwardXP(player, xp, T.XPToastSquadBonus);
        }
    }
    public static void FlagCaptured(Flag flag, ulong team)
    {
        Dictionary<Squad, int> alreadyUpdated = new Dictionary<Squad, int>();

        foreach (Player nelsonplayer in flag.PlayersOnFlag.Where(p => p.GetTeam() == team))
        {
            UCPlayer? player = UCPlayer.FromPlayer(nelsonplayer);

            if (player == null) continue;

            AwardXP(player, player.NearbyMemberBonus(XPConfig.FlagCapturedXP, 60), T.XPToastFlagCaptured);
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

            UCPlayer? assister = UCPlayer.FromID(component.secondLastAttacker.Key);
            if (assister != null && assister.Steam64 != killerID && assister.Steam64 != victimID && (DateTime.Now - component.secondLastAttacker.Value).TotalSeconds <= 30)
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

    public static async Task UpdatePointsAsync(UCPlayer caller)
    {
        if (caller is null) throw new ArgumentNullException(nameof(caller));
        ulong team = caller.GetTeam();
        if (team is 1 or 2)
        {
            bool found = false;
            uint cd = 1;
            uint xp = 1;
            await Data.DatabaseManager.QueryAsync(
                "SELECT `Experience`, `Credits` FROM `s2_levels` WHERE `Steam64` = @0 AND `Team` = @1 LIMIT 1;",
                new object[] { caller.Steam64, team },
                R =>
                {
                    xp = R.GetUInt32(0);
                    cd = R.GetUInt32(1);
                    found = true;
                });
            if (!found)
                return;
            caller.UpdatePoints(xp, cd);
            await UCWarfare.ToUpdate();

            if (TraitManager.Loaded)
                TraitSigns.SendAllTraitSigns(caller);

            if (VehicleBay.Loaded && VehicleSpawner.Loaded && VehicleSigns.Loaded)
                VehicleSpawner.UpdateSigns(caller);

            if (RequestSigns.Loaded)
                RequestSigns.UpdateAllSigns(caller);
        }
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
                Translation<EVehicleType> message;

                if (e.Component.IsAircraft)
                    message = T.XPToastAircraftDestroyed;
                else
                    message = T.XPToastVehicleDestroyed;

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

                if (reason.Length == 0)
                    Chat.Broadcast(T.VehicleDestroyedUnknown, e.Instigator, e.Vehicle.asset);
                else
                    Chat.Broadcast(T.VehicleDestroyed, e.Instigator, e.Vehicle.asset, reason, distance);

                ActionLogger.Add(EActionLogType.OWNED_VEHICLE_DIED, $"{e.Vehicle.asset.vehicleName} / {e.Vehicle.id} / {e.Vehicle.asset.GUID:N} ID: {e.Vehicle.instanceID}" +
                                                                 $" - Destroyed by {e.Instigator.Steam64.ToString(Data.Locale)}", e.OwnerId);

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
                                AwardXP(attacker, reward, message.Translate(Data.Languages.TryGetValue(e.InstigatorId, out string lang) ? lang : L.DEFAULT, e.VehicleData.Type).ToUpperInvariant());
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
                Translation<EVehicleType> message;

                if (e.Component.IsAircraft)
                    message = T.XPToastFriendlyAircraftDestroyed;
                else
                    message = T.XPToastFriendlyVehicleDestroyed;
                Chat.Broadcast(T.VehicleTeamkilled, e.Instigator, e.Vehicle.asset);

                ActionLogger.Add(EActionLogType.OWNED_VEHICLE_DIED, $"{e.Vehicle.asset.vehicleName} / {e.Vehicle.id} / {e.Vehicle.asset.GUID:N} ID: {e.Vehicle.instanceID}" +
                                                                 $" - Destroyed by {e.InstigatorId}", e.OwnerId);
                if (e.Instigator is not null)
                    AwardCredits(e.Instigator, Mathf.Clamp(e.VehicleData.CreditCost, 5, 1000), message, e.VehicleData.Type, true);
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

            if (Data.Reporter is not null)
            {
                Data.Reporter.OnVehicleDied(e.OwnerId,
                    VehicleSpawner.HasLinkedSpawn(e.Vehicle.instanceID, out Vehicles.VehicleSpawn spawn)
                        ? spawn.SpawnPadInstanceID
                        : uint.MaxValue, e.InstigatorId, e.Vehicle.asset.GUID, e.Component.LastItem,
                    e.Component.LastDamageOrigin, vehicleWasFriendly);
            }
        }
    }
}

public class XPConfig : ConfigData
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
    public Dictionary<EVehicleType, int> VehicleDestroyedXP;

    public float XPMultiplier;

    public ushort RankUI;

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


        VehicleDestroyedXP = new Dictionary<EVehicleType, int>()
        {
            {EVehicleType.HUMVEE, 25},
            {EVehicleType.TRANSPORT, 20},
            {EVehicleType.LOGISTICS, 25},
            {EVehicleType.SCOUT_CAR, 30},
            {EVehicleType.APC, 60},
            {EVehicleType.IFV, 70},
            {EVehicleType.MBT, 100},
            {EVehicleType.HELI_TRANSPORT, 30},
            {EVehicleType.EMPLACEMENT, 20},
            {EVehicleType.HELI_ATTACK, 150},
            {EVehicleType.JET, 200},
        };

        XPMultiplier = 1f;

        RankUI = 36031;
    }
    public XPConfig()
    { }
}
public class TWConfig : ConfigData
{
    public ushort MedalsUI;
    public int FirstMedalPoints;
    public int PointsIncreasePerMedal;
    public int RallyUsedPoints;
    public int MemberFlagCapturePoints;
    public int ResupplyFriendlyPoints;
    public int RepairVehiclePoints;
    public int ReviveFriendlyTW;
    public int UnloadSuppliesPoints;

    public override void SetDefaults()
    {
        MedalsUI = 36033;
        FirstMedalPoints = 2000;
        PointsIncreasePerMedal = 500;
        RallyUsedPoints = 30;
        MemberFlagCapturePoints = 10;
        ResupplyFriendlyPoints = 20;
        RepairVehiclePoints = 5;
        ReviveFriendlyTW = 20;
        UnloadSuppliesPoints = 10;
    }
    public TWConfig()
    { }
}
public class CreditsConfig : ConfigData
{
    public ushort CreditsUI;
    public int StartingCredits;

    public override void SetDefaults()
    {
        CreditsUI = 36070;
        StartingCredits = 500;
    }
    public CreditsConfig()
    { }
}
