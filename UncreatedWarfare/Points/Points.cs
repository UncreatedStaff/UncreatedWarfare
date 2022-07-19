using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

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
    public static void AwardCredits(UCPlayer player, int amount, string? message = null, bool redmessage = false, bool isPurchase = false)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (amount == 0 || _xpconfig.Data.XPMultiplier == 0f) return;

        amount = Mathf.RoundToInt(amount * _xpconfig.Data.XPMultiplier);
        Task.Run(async () =>
        {
            int currentAmount = await Data.DatabaseManager.AddCredits(player.Steam64, player.GetTeam(), amount);
            int oldamt = currentAmount - amount;
            await UCWarfare.ToUpdate();

            player.CachedCredits = currentAmount;

            ActionLog.Add(EActionLogType.CREDITS_CHANGED, oldamt + " >> " + currentAmount, player);

            if (!player.HasUIHidden && (Data.Gamemode is not IEndScreen lb || !lb.isScreenUp))
            {
                string key = "gain_credits";
                if (amount < 0)
                {
                    if (redmessage)
                        key = "loss_credits";
                    else
                        key = "subtract_credits";
                }

                string number = Localization.Translate(key, player, Math.Abs(amount).ToString(Data.Locale));
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
        });
    }
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

            if (!player.HasUIHidden && (Data.Gamemode is not IEndScreen lb || !lb.isScreenUp))
            {
                string number = Localization.Translate(amount >= 0 ? "gain_xp" : "loss_xp", player, Math.Abs(amount).ToString(Data.Locale));

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

            ActionLog.Add(EActionLogType.XP_CHANGED, oldRank.TotalXP + " >> " + currentAmount, player);

            if (awardCredits)
                AwardCredits(player, Mathf.RoundToInt(0.15f * amount), null, true);

            if (player.Rank.Level > oldRank.Level)
            {
                ToastMessage.QueueMessage(player, new ToastMessage(Localization.Translate("promoted_xp_1", player), Localization.Translate("promoted_xp_2", player, player.Rank.Name.ToUpper()), EToastMessageSeverity.BIG));

                if (VehicleSpawner.Loaded)
                {
                    VehicleSpawner.UpdateSigns(player);
                }
                if (RequestSigns.Loaded)
                {
                    RequestSigns.UpdateAllSigns(player.SteamPlayer);
                }
            }
            else if (player.Rank.Level < oldRank.Level)
            {
                ToastMessage.QueueMessage(player, new ToastMessage(Localization.Translate("demoted_xp_1", player), Localization.Translate("demoted_xp_2", player, player.Rank.Name.ToUpper()), EToastMessageSeverity.BIG));

                if (VehicleSpawner.Loaded)
                {
                    foreach (Vehicles.VehicleSpawn spawn in VehicleSpawner.Spawners)
                        spawn.UpdateSign(player.SteamPlayer);
                }
                if (RequestSigns.Loaded)
                {
                    RequestSigns.UpdateAllSigns(player.SteamPlayer);
                }
            }
        });
    }
    
    public static void AwardXP(Player player, int amount, string message = "", bool awardCredits = true)
    {
        UCPlayer? pl = UCPlayer.FromPlayer(player);
        if (pl != null)
            AwardXP(pl, amount, message, awardCredits);
        else
            L.LogWarning("Unable to find player.");
    }
    [Obsolete]
    public static void AwardTW(UCPlayer player, int amount, string message = "")
    {
    }
    [Obsolete]
    public static void AwardTW(Player player, int amount, string message = "")
    {
    }
    public static void UpdateXPUI(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (player.HasUIHidden || (Data.Is(out IEndScreen lb) && lb.isScreenUp) || (Data.Is(out ITeams teams) && teams.JoinManager.IsInLobby(player)))
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

        if (player.HasUIHidden || (Data.Is(out IEndScreen lb) && lb.isScreenUp) || (Data.Is(out ITeams teams) && teams.JoinManager.IsInLobby(player)))
            return;

        EffectManager.sendUIEffect(CreditsConfig.CreditsUI, CREDITSUI_KEY, player.Connection, true);
        EffectManager.sendUIEffectText(CREDITSUI_KEY, player.Connection, true,  
            "Credits", "<color=#b8ffc1>C</color>  " + player.CachedCredits
        );
    }
    [Obsolete]
    public static void UpdateTWUI(UCPlayer player)
    {

    }
    public static string GetProgressBar(int currentPoints, int totalPoints, int barLength = 50)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float ratio = currentPoints / (float)totalPoints;

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
                AwardXP(driver.player, amount, Localization.Translate("xp_driver_assist", gunner));
            }

            //if (vehicle.transform.TryGetComponent(out VehicleComponent component))
            //{
            //    component.Quota += quota;
            //}
        }
    }
    public static void TryAwardFOBCreatorXP(FOB fob, int amount, string translationKey)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayer? creator = UCPlayer.FromID(fob.Creator);

        if (creator != null)
        {
            AwardXP(creator, amount, Localization.Translate(translationKey, creator));
        }

        if (fob.Placer != fob.Creator)
        {
            UCPlayer? placer = UCPlayer.FromID(fob.Placer);
            if (placer != null)
                AwardXP(placer, amount, Localization.Translate(translationKey, placer));
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
