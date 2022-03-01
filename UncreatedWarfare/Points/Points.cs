﻿using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Point
{
    public class Points
    {
        private const int XPUI_KEY = 26969;
        private const int TWUI_KEY = 26970;
        private static readonly Config<XPConfig> _xpconfig = new Config<XPConfig>(Data.PointsStorage, "xp.json");
        private static readonly Config<TWConfig> _twconfig = new Config<TWConfig>(Data.PointsStorage, "tw.json");
        public static XPConfig XPConfig => _xpconfig.data;
        public static TWConfig TWConfig => _twconfig.data;

        public static OfficerStorage Officers;

        public static void Initialize()
        {
            Officers = new OfficerStorage();
        }
        public static void ReloadConfig()
        {
            _xpconfig.Reload();
            _twconfig.Reload();
        }
        public static void OnPlayerJoined(UCPlayer player, bool isnewGame)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (!isnewGame && (player.IsTeam1() || player.IsTeam2()))
            {
                UpdateXPUI(player);
                UpdateTWUI(player);
            }
        }
        public static void OnGroupChanged(UCPlayer player, ulong oldGroup, ulong newGroup)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (newGroup == 1 || newGroup == 2)
            {
                UpdateXPUI(player);
                UpdateTWUI(player);
            }
            else
            {
                EffectManager.askEffectClearByID(XPConfig.RankUI, player.Player.channel.owner.transportConnection);
                EffectManager.askEffectClearByID(TWConfig.MedalsUI, player.Player.channel.owner.transportConnection);
            }
        }
        /*
        public static void OnBranchChanged(UCPlayer player, EBranch oldBranch, EBranch newBranch)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            string rank;
            if (player.CurrentRank.Level > 0)
                rank = Translation.Translate("branch_changed", player, Translation.TranslateBranch(player.Branch, player), player.CurrentRank.Name, player.CurrentRank.Level.ToString(Data.Locale));
            else
                rank = Translation.Translate("branch_changed_recruit", player, Translation.TranslateBranch(player.Branch, player), player.CurrentRank.Name);

            if (!(oldBranch == EBranch.DEFAULT || player.Branch != EBranch.DEFAULT))
            {
                ToastMessage.QueueMessage(player, new ToastMessage(
                    "",
                    "",
                    rank,
                    EToastMessageSeverity.BIG));
            }

            UpdateXPUI(player);
        }*/

        private const float XP_STRETCH = 7883.735f;
        private const float XP_STRETCH_3 = XP_STRETCH * XP_STRETCH * XP_STRETCH;
        private const float XP_MULTIPLIER_SQR = 490000f;
        private const float ONE_THIRD = 1f / 3f;
        /// <summary>Get the current level given an amount of <paramref name="xp"/>.</summary>
        public static int GetLevel(int xp) => Mathf.FloorToInt(Mathf.Pow(xp * xp * XP_MULTIPLIER_SQR, ONE_THIRD) / XP_STRETCH);
        /// <summary>Get the given <paramref name="level"/>'s starting xp.</summary>
        public static int GetLevelXP(int level) => Mathf.RoundToInt(Mathf.Sqrt(XP_STRETCH_3 * level * level * level / XP_MULTIPLIER_SQR));
        /// <summary>Get the level after the given <paramref name="level"/>'s starting xp (or the given <paramref name="level"/>'s end xp.</summary>
        public static int GetNextLevelXP(int level) => Mathf.RoundToInt(Mathf.Sqrt(XP_STRETCH_3 * ++level * level * level / XP_MULTIPLIER_SQR));
        /// <summary>Get the percentage from 0-1 a player is through their current level at the given <paramref name="xp"/>.</summary>
        public static float GetLevelProgressXP(int xp)
        {
            int lvl = GetLevel(xp);
            int end = GetNextLevelXP(lvl);
            return (float)(end - GetLevelXP(lvl)) / (end - xp);
        }
        /// <summary>Get the percentage from 0-1 a player is through their current level at the given <paramref name="xp"/> and <paramref name="lvl"/>.</summary>
        public static float GetLevelProgressXP(int xp, int lvl)
        {
            int end = GetNextLevelXP(lvl);
            return (float)(end - GetLevelXP(lvl)) / (end - xp);
        }
        public static void AwardXP(UCPlayer player, int amount, string? message = null)
        {
            if (!Data.TrackStats || amount == 0 || _xpconfig.data.XPMultiplier == 0f) return;
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            amount = Mathf.RoundToInt(amount * _xpconfig.data.XPMultiplier);
            Task.Run(async () =>
            {
                int oldAmt = player.CachedXP;
                int currentAmount = await Data.DatabaseManager.AddXP(player.Steam64, amount);
                await UCWarfare.ToUpdate();

                player.CachedXP = currentAmount;

                if (!player.HasUIHidden && (Data.Gamemode is not IEndScreen lb || !lb.isScreenUp))
                {
                    string number = Translation.Translate(amount >= 0 ? "gain_xp" : "loss_xp", player, Math.Abs(amount).ToString(Data.Locale));

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

                int oldLvl = GetLevel(oldAmt);
                int newLvl = GetLevel(currentAmount);

                if (newLvl > oldLvl)
                {
                    ToastMessage.QueueMessage(player, new ToastMessage(Translation.Translate("level_up_xp_1", player), Translation.Translate("level_up_xp_2", player, newLvl.ToString(Data.Locale).ToUpper()), EToastMessageSeverity.BIG));
                    
                    for (int i = 0; i < VehicleSpawner.ActiveObjects.Count; i++)
                        VehicleSpawner.ActiveObjects[i].UpdateSign(player.SteamPlayer);
                    for (int i = 0; i < Kits.RequestSigns.ActiveObjects.Count; i++)
                        Kits.RequestSigns.ActiveObjects[i].InvokeUpdate(player.SteamPlayer);
                }
                else if (newLvl < oldLvl)
                {
                    ToastMessage.QueueMessage(player, new ToastMessage(Translation.Translate("level_down_xp", player), EToastMessageSeverity.BIG));
                    
                    for (int i = 0; i < VehicleSpawner.ActiveObjects.Count; i++)
                        VehicleSpawner.ActiveObjects[i].UpdateSign(player.SteamPlayer);
                    for (int i = 0; i < Kits.RequestSigns.ActiveObjects.Count; i++)
                        Kits.RequestSigns.ActiveObjects[i].InvokeUpdate(player.SteamPlayer);
                }
            });

        }
        /*
        [Obsolete]
        public static void AwardXPOld(UCPlayer player, int amount, string? message = null)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (!Data.TrackStats || amount == 0) return;

            int oldLevel = player.CurrentRank.Level;
            int oldTier = player.CurrentRank.RankTier;

            amount = Mathf.RoundToInt(amount * _xpconfig.data.XPMultiplier);

            int newBalance = Data.DatabaseManager.AddXP(player.Steam64, player.Branch, amount);
            player.UpdateRank(player.Branch, newBalance);


            if (!(Data.Gamemode is IEndScreen lb && lb.isScreenUp))
            {
                string number = Translation.Translate(amount >= 0 ? "gain_xp" : "loss_xp", player, Math.Abs(amount).ToString(Data.Locale));

                if (amount > 0)
                    number = number.Colorize("e3e3e3");
                else
                    number = number.Colorize("d69898");

                if (!string.IsNullOrEmpty(message))
                    ToastMessage.QueueMessage(player, new ToastMessage(number + "\n" + message!.Colorize("adadad"), EToastMessageSeverity.MINI));
                else
                    ToastMessage.QueueMessage(player, new ToastMessage(number, EToastMessageSeverity.MINI));
            }

            UpdateXPUI(player);

            RankData newRank = player.CurrentRank;

            if (newRank.Level > oldLevel)
            {
                ToastMessage.QueueMessage(player, new ToastMessage(Translation.Translate("level_up_xp_1", player), Translation.Translate("level_up_xp_2", player, Translation.TranslateBranch(newRank.Branch, player).ToUpper(), newRank.Level.ToString(Data.Locale).ToUpper()), EToastMessageSeverity.BIG));

                if (newRank.RankTier > oldTier)
                {
                    ToastMessage.QueueMessage(player, new ToastMessage(Translation.Translate("promoted_xp", player), newRank.Name.ToUpper(), EToastMessageSeverity.BIG));
                    Chat.BroadcastToAllExcept(new ulong[1] { player.CSteamID.m_SteamID }, "xp_announce_promoted", F.GetPlayerOriginalNames(player).CharacterName, newRank.Name);
                }

                for (int i = 0; i < VehicleSpawner.ActiveObjects.Count; i++)
                    VehicleSpawner.ActiveObjects[i].UpdateSign(player.SteamPlayer);
                for (int i = 0; i < Kits.RequestSigns.ActiveObjects.Count; i++)
                    Kits.RequestSigns.ActiveObjects[i].InvokeUpdate(player.SteamPlayer);
            }
            else if (newRank.Level < oldLevel)
            {
                ToastMessage.QueueMessage(player, new ToastMessage(Translation.Translate("level_down_xp", player), EToastMessageSeverity.BIG));

                if (newRank.RankTier < oldTier)
                {
                    ToastMessage.QueueMessage(player, new ToastMessage(Translation.Translate("demoted_xp", player), newRank.Name.ToUpper(), EToastMessageSeverity.BIG));
                    Chat.BroadcastToAllExcept(new ulong[1] { player.CSteamID.m_SteamID }, "xp_announce_demoted", F.GetPlayerOriginalNames(player).CharacterName, newRank.Name);
                }

                for (int i = 0; i < VehicleSpawner.ActiveObjects.Count; i++)
                    VehicleSpawner.ActiveObjects[i].UpdateSign(player.SteamPlayer);
                for (int i = 0; i < Kits.RequestSigns.ActiveObjects.Count; i++)
                    Kits.RequestSigns.ActiveObjects[i].InvokeUpdate(player.SteamPlayer);
            }

            if (player.Player.TryGetPlaytimeComponent(out PlaytimeComponent c) && c.stats is IExperienceStats ex)
            {
                ex.AddXP(amount);
            }
        }*/
        
        public static void AwardXP(Player player, int amount, string message = "")
        {
            UCPlayer? pl = UCPlayer.FromPlayer(player);
            if (pl != null)
                AwardXP(pl, amount, message);
            else
                L.LogWarning("Unable to find player.");
        }

        public static void AwardTW(UCPlayer player, int amount, string message = "")
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (!Data.TrackStats || amount == 0) return;

            MedalData oldMedals = player.Medals;

            amount = Mathf.RoundToInt(amount * _xpconfig.data.XPMultiplier);
            int newBalance = Data.DatabaseManager.AddTeamwork(player.Steam64, amount);

            player.UpdateMedals(newBalance);

            MedalData newMedals = player.Medals;

            if (!(Data.Gamemode is IEndScreen lb && lb.isScreenUp))
            {
                string number = Translation.Translate(amount >= 0 ? "gain_ofp" : "loss_ofp", player, Math.Abs(amount).ToString(Data.Locale));

                if (amount > 0)
                    number = number.Colorize("ffe392");
                else
                    number = number.Colorize("e0a98d");

                if (message != "")
                {
                    if (!(message.StartsWith("<") && message.EndsWith(">")))
                        message = message.Colorize("b8af95");

                    ToastMessage.QueueMessage(player, new ToastMessage(number + "\n" + message.Colorize("adadad"), EToastMessageSeverity.MINI));
                }
                else
                    ToastMessage.QueueMessage(player, new ToastMessage(number, EToastMessageSeverity.MINI));

            }

            UpdateTWUI(player);

            if (newMedals.NumberOfMedals > oldMedals.NumberOfMedals && !(Data.Gamemode is IEndScreen l && l.isScreenUp))
            {
                string startString = F.Colorize(Translation.Translate("officer_ui_stars", player, newMedals.NumberOfMedals.ToString(), newMedals.NumberOfMedals.S()).ToUpper(), UCWarfare.GetColorHex("star_color"));

                ToastMessage.QueueMessage(player, new ToastMessage(Translation.Translate("gain_star", player), startString, EToastMessageSeverity.BIG));

                Chat.BroadcastToAllExcept(new ulong[1] { player.CSteamID.m_SteamID }, "ofp_announce_gained", F.GetPlayerOriginalNames(player).CharacterName, startString);
            }

            if (player.Player.TryGetPlaytimeComponent(out PlaytimeComponent c) && c.stats is IExperienceStats ex)
            {
                ex.AddOfficerPoints(amount);
            }
        }
        public static void AwardTW(Player player, int amount, string message = "")
        {
            UCPlayer? pl = UCPlayer.FromPlayer(player);
            if (pl != null)
                AwardTW(pl, amount, message);
        }
        /*
        [Obsolete]
        public static void UpdateXPUI(UCPlayer player)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (player.HasUIHidden || (Data.Is(out IEndScreen lb) && lb.isScreenUp) || (Data.Is(out ITeams teams) && teams.JoinManager.IsInLobby(player)))
                return;

            RankData current = player.CurrentRank;

            EffectManager.sendUIEffect(XPConfig.RankUI, XPUI_KEY, player.connection, true);
            EffectManager.sendUIEffectText(XPUI_KEY, player.connection, true,
                "Rank", current.Name
            );
            EffectManager.sendUIEffectText(XPUI_KEY, player.connection, true,
                "Level", current.Level == 0 ? "" : Translation.Translate("ui_xp_level", player, current.Level.ToString(Data.Locale))
            );
            EffectManager.sendUIEffectText(XPUI_KEY, player.connection, true,
                "XP", current.CurrentXP + "/" + current.RequiredXP
            );
            EffectManager.sendUIEffectText(XPUI_KEY, player.connection, true,
                "Next", Translation.Translate("ui_xp_next_level", player, (current.Level + 1).ToString(Data.Locale))
            );
            EffectManager.sendUIEffectText(XPUI_KEY, player.connection, true,
                "Progress", GetProgressBar(current.CurrentXP, current.RequiredXP)
            );
            EffectManager.sendUIEffectText(XPUI_KEY, player.connection, true,
                "Division", Translation.TranslateBranch(player.Branch, player)
            );
        }*/
        public static void UpdateXPUI(UCPlayer player)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (player.HasUIHidden || (Data.Is(out IEndScreen lb) && lb.isScreenUp) || (Data.Is(out ITeams teams) && teams.JoinManager.IsInLobby(player)))
                return;

            ref Ranks.RankData rankdata = ref Ranks.RankManager.GetRank(player, out bool success);
            if (success)
            {
                int xp = player.CachedXP;
                int level = GetLevel(xp);
                int reqXp = GetNextLevelXP(level);

                EffectManager.sendUIEffect(XPConfig.RankUI, XPUI_KEY, player.connection, true);
                EffectManager.sendUIEffectText(XPUI_KEY, player.connection, true,
                    "Rank", rankdata.GetName(player.Steam64)
                );
                EffectManager.sendUIEffectText(XPUI_KEY, player.connection, true,
                    "Level", level == 0 ? string.Empty : Translation.Translate("ui_xp_level", player, level.ToString(Data.Locale))
                );
                EffectManager.sendUIEffectText(XPUI_KEY, player.connection, true,
                    "XP", xp + "/" + reqXp
                );
                EffectManager.sendUIEffectText(XPUI_KEY, player.connection, true,
                    "Next", Translation.Translate("ui_xp_next_level", player, (level + 1).ToString(Data.Locale))
                );
                EffectManager.sendUIEffectText(XPUI_KEY, player.connection, true,
                    "Progress", GetProgressBar(xp, reqXp)
                );
            }
        }
        public static void UpdateTWUI(UCPlayer player)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (player.HasUIHidden || Data.Is(out IEndScreen lb) && lb.isScreenUp || Data.Is(out ITeams teams) && teams.JoinManager.IsInLobby(player))
                return;

            EffectManager.sendUIEffect(TWConfig.MedalsUI, TWUI_KEY, player.connection, true);
            
            if (player.Medals.NumberOfMedals <= 0)
            {
                EffectManager.sendUIEffectVisibility(TWUI_KEY, player.connection, true, "Icon", false);
                EffectManager.sendUIEffectVisibility(TWUI_KEY, player.connection, true, "Icon_Grey", true);
            }
            else
            {
                EffectManager.sendUIEffectVisibility(TWUI_KEY, player.connection, true, "Icon", true);
                EffectManager.sendUIEffectVisibility(TWUI_KEY, player.connection, true, "Icon_Grey", false);
            }

            EffectManager.sendUIEffectText(TWUI_KEY, player.connection, true, "Count",
                player.Medals.NumberOfMedals < 2 ? string.Empty : player.Medals.NumberOfMedals.ToString()
            );
            EffectManager.sendUIEffectText(TWUI_KEY, player.connection, true, "Points",
                player.Medals.CurrentTW + "/" + player.Medals.RequiredTW
            );
            EffectManager.sendUIEffectText(TWUI_KEY, player.connection, true, "Progress",
                GetProgressBar(player.Medals.CurrentTW, player.Medals.RequiredTW)
            );
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
                    AwardXP(driver.player, amount, Translation.Translate("xp_driver_assist", gunner));
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
                AwardXP(creator, amount, Translation.Translate(translationKey, creator));
                AwardTW(creator, amount);
            }    

            if (fob.Placer != fob.Creator)
            {
                UCPlayer? placer = UCPlayer.FromID(fob.Placer);
                if (placer != null)
                    AwardXP(placer, amount, Translation.Translate(translationKey, placer));
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
            RepairVehicleXP = 20;
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
                {EVehicleType.HELI_ATTACK, 100},
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
}
