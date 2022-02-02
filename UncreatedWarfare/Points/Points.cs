﻿using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Point
{
    public class Points
    {
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
            if (!isnewGame && (player.IsTeam1() || player.IsTeam2()))
            {
                UpdateXPUI(player);
                UpdateTWUI(player);
            }
        }
        public static void OnGroupChanged(UCPlayer player, ulong oldGroup, ulong newGroup)
        {
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
        public static void OnBranchChanged(UCPlayer player, EBranch oldBranch, EBranch newBranch)
        {
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
        }
        public static void AwardXP(UCPlayer player, int amount, string message = "")
        {
            if (!Data.TrackStats || amount == 0) return;

            RankData oldRank = player.CurrentRank;

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

                if (message != "")
                    ToastMessage.QueueMessage(player, new ToastMessage(number + "\n" + message.Colorize("adadad"), EToastMessageSeverity.MINI));
                else
                    ToastMessage.QueueMessage(player, new ToastMessage(number, EToastMessageSeverity.MINI));
            }

            UpdateXPUI(player);

            RankData newRank = player.CurrentRank;

            if (newRank.Level > oldRank.Level)
            {
                if (newRank.RankTier > oldRank.RankTier)
                {
                    ToastMessage.QueueMessage(player, new ToastMessage(Translation.Translate("promoted_xp", player), newRank.Name.ToUpper(), EToastMessageSeverity.BIG));
                    Chat.BroadcastToAllExcept(new ulong[1] { player.CSteamID.m_SteamID }, "xp_announce_promoted", F.GetPlayerOriginalNames(player).CharacterName, newRank.Name);
                }
                else
                    ToastMessage.QueueMessage(player, new ToastMessage(Translation.Translate("level_up_xp_1", player), Translation.Translate("level_up_xp_2", player, Translation.TranslateBranch(newRank.Branch, player).ToUpper(), newRank.Level.ToString(Data.Locale).ToUpper()), EToastMessageSeverity.BIG));

                for (int i = 0; i < VehicleSpawner.ActiveObjects.Count; i++)
                    VehicleSpawner.ActiveObjects[i].UpdateSign(player.SteamPlayer);
                for (int i = 0; i < Kits.RequestSigns.ActiveObjects.Count; i++)
                    Kits.RequestSigns.ActiveObjects[i].InvokeUpdate(player.SteamPlayer);
            }
            else if (newRank.Level < oldRank.Level)
            {
                if (newRank.RankTier < oldRank.RankTier)
                {
                    ToastMessage.QueueMessage(player, new ToastMessage(Translation.Translate("demoted_xp", player), newRank.Name.ToUpper(), EToastMessageSeverity.BIG));
                    Chat.BroadcastToAllExcept(new ulong[1] { player.CSteamID.m_SteamID }, "xp_announce_demoted", F.GetPlayerOriginalNames(player).CharacterName, newRank.Name);
                }
                else
                    ToastMessage.QueueMessage(player, new ToastMessage(Translation.Translate("level_down_xp", player), EToastMessageSeverity.BIG));

                for (int i = 0; i < VehicleSpawner.ActiveObjects.Count; i++)
                    VehicleSpawner.ActiveObjects[i].UpdateSign(player.SteamPlayer);
                for (int i = 0; i < Kits.RequestSigns.ActiveObjects.Count; i++)
                    Kits.RequestSigns.ActiveObjects[i].InvokeUpdate(player.SteamPlayer);
            }

            if (player.Player.TryGetPlaytimeComponent(out PlaytimeComponent c) && c.stats is IExperienceStats ex)
            {
                ex.AddXP(amount);
            }
        }
        public static void AwardXP(Player player, int amount, string message = "") => AwardXP(UCPlayer.FromPlayer(player), amount, message);
        public static void AwardTW(UCPlayer player, int amount, string message = "")
        {
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
        public static void AwardTW(Player player, int amount, string message = "") => AwardTW(UCPlayer.FromPlayer(player), amount, message);
        public static void UpdateXPUI(UCPlayer player)
        {
            if ((Data.Is(out IEndScreen lb) && lb.isScreenUp) || (Data.Is(out ITeams teams) && teams.JoinManager.IsInLobby(player)))
            {
                if (UCWarfare.Config.Debug)
                {
                    L.Log("UpdateXPUI returned early");

                    bool islb = Data.Is(out IEndScreen lb2);
                    L.Log($"     Is IEndScreen: {islb}");
                    if (islb)
                        L.Log($"        Is screen up: {lb2.isScreenUp}");

                    bool isteams = Data.Is(out ITeams t);
                    L.Log($"     Is ITeams: {isteams}");
                    if (isteams)
                        L.Log($"        Is player in lobby: {t.JoinManager.IsInLobby(player)}");
                }
                return;
            }

            short key = 26969;
            RankData current = player.CurrentRank;

            EffectManager.sendUIEffect(XPConfig.RankUI, key, player.connection, true);
            EffectManager.sendUIEffectText(key, player.connection, true,
                "Rank", current.Name
            );
            EffectManager.sendUIEffectText(key, player.connection, true,
                "Level", current.Level == 0 ? "" : Translation.Translate("ui_xp_level", player, current.Level.ToString(Data.Locale))
            );
            EffectManager.sendUIEffectText(key, player.connection, true,
                "XP", current.CurrentXP + "/" + current.RequiredXP
            );
            EffectManager.sendUIEffectText(key, player.connection, true,
                "Next", Translation.Translate("ui_xp_next_level", player, (current.Level + 1).ToString(Data.Locale))
            );
            EffectManager.sendUIEffectText(key, player.connection, true,
                "Progress", GetProgressBar(current.CurrentXP, current.RequiredXP)
            );
            EffectManager.sendUIEffectText(key, player.connection, true,
                "Division", Translation.TranslateBranch(player.Branch, player)
            );
        }
        public static void UpdateTWUI(UCPlayer player)
        {
            if (Data.Is(out IEndScreen lb) && lb.isScreenUp || Data.Is(out ITeams teams) && teams.JoinManager.IsInLobby(player))
                return;

            short key = 26970;
            EffectManager.sendUIEffect(TWConfig.MedalsUI, key, player.connection, true);
            
            if (player.Medals.NumberOfMedals <= 0)
            {
                EffectManager.sendUIEffectVisibility(key, player.connection, true, "Icon", false);
                EffectManager.sendUIEffectVisibility(key, player.connection, true, "Icon_Grey", true);
            }
            else
            {
                EffectManager.sendUIEffectVisibility(key, player.connection, true, "Icon", true);
                EffectManager.sendUIEffectVisibility(key, player.connection, true, "Icon_Grey", false);
            }

            EffectManager.sendUIEffectText(key, player.connection, true, "Count",
                player.Medals.NumberOfMedals < 2 ? string.Empty : player.Medals.NumberOfMedals.ToString()
            );
            EffectManager.sendUIEffectText(key, player.connection, true, "Points",
                player.Medals.CurrentTW + "/" + player.Medals.RequiredTW
            );
            EffectManager.sendUIEffectText(key, player.connection, true, "Progress",
                GetProgressBar(player.Medals.CurrentTW, player.Medals.RequiredTW)
            );
        }
        public static string GetProgressBar(int currentPoints, int totalPoints, int barLength = 50)
        {
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
            UCPlayer creator = UCPlayer.FromID(fob.Creator);

            if (creator != null)
            {
                AwardXP(creator, amount, Translation.Translate(translationKey, creator));
                AwardTW(creator, amount);
            }    

            if (fob.Placer != fob.Creator)
            {
                UCPlayer placer = UCPlayer.FromID(fob.Placer);
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
            FriendlyKilledXP = -50;
            FriendlyRevivedXP = 30;
            FOBKilledXP = 80;
            FOBTeamkilledXP = -1500;
            FOBBunkerKilledXP = 60;
            FOBBunkerTeamkilledXP = -1000;
            FOBDeployedXP = 10;
            FlagCapturedXP = 30;
            FlagAttackXP = 5;
            FlagDefendXP = 5;
            FlagNeutralizedXP = 50;
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
            FirstMedalPoints = 1000;
            PointsIncreasePerMedal = 400;
            RallyUsedPoints = 30;
            MemberFlagCapturePoints = 15;
            ResupplyFriendlyPoints = 20;
            RepairVehiclePoints = 5;
            ReviveFriendlyTW = 20;
            UnloadSuppliesPoints = 10;
        }
        public TWConfig()
        { }
    }
}