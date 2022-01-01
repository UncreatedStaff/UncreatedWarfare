using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uncreated.Players;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Point
{
    public class Points
    {
        private static Config<XPConfig> _xpconfig = new Config<XPConfig>(Data.PointsStorage, "xp.json");
        private static Config<TWConfig> _twconfig = new Config<TWConfig>(Data.PointsStorage, "tw.json");
        public static XPConfig XPConfig => _xpconfig.Data;
        public static TWConfig TWConfig => _twconfig.Data;

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
                
            }
        }
        public static void OnBranchChanged(UCPlayer player, EBranch oldBranch, EBranch newBranch)
        {
            if (oldBranch != EBranch.DEFAULT)
            {
                string rank = "";
                if (player.CurrentRank.Level > 0)
                    rank = Translation.Translate("branch_changed", player, Translation.TranslateBranch(player.Branch, player), player.CurrentRank.Name, player.CurrentRank.Level.ToString(Data.Locale));
                else
                    rank = Translation.Translate("branch_changed_recruit", player, Translation.TranslateBranch(player.Branch, player), player.CurrentRank.Name);

                ToastMessage.QueueMessage(player, new ToastMessage(
                "",
                "",
                rank,
                EToastMessageSeverity.BIG));

                UpdateXPUI(player);
            }
        }
        public static void AwardXP(UCPlayer player, int amount, string message = "")
        {
            if (!Data.TrackStats) return;

            RankData oldRank = player.CurrentRank;

            amount = Mathf.RoundToInt(amount * _xpconfig.Data.XPMultiplier);
            int newBalance = Data.DatabaseManager.AddXP(player.Steam64, player.Branch, amount);

            player.UpdateRank(player.Branch, newBalance);

            if (amount != 0 && !(Data.Gamemode is IEndScreen lb && lb.isScreenUp))
            {
                string number = Translation.Translate(amount >= 0 ? "gain_xp" : "loss_xp", player, Math.Abs(amount).ToString(Data.Locale));

                if (amount >= 0)
                    number = number.Colorize("e3e3e3");
                else
                    number = number.Colorize("d69898");

                ToastMessage.QueueMessage(player, new ToastMessage(number, EToastMessageSeverity.MINI));

                if (message != "")
                {
                    message = message.Colorize("adadad");

                    ToastMessage.QueueMessage(player, new ToastMessage(message, EToastMessageSeverity.MINI));
                }
            }

            UpdateXPUI(player);

            RankData newRank = player.CurrentRank;

            if (newRank.Level > oldRank?.Level)
            {
                ToastMessage.QueueMessage(player, new ToastMessage(Translation.Translate("promoted_xp", player), newRank.Name.ToUpper(), EToastMessageSeverity.BIG));
                Chat.BroadcastToAllExcept(new List<CSteamID>() { player.CSteamID }, "xp_announce_promoted", F.GetPlayerOriginalNames(player).CharacterName, newRank.Name);
                for (int i = 0; i < VehicleSpawner.ActiveObjects.Count; i++)
                    VehicleSpawner.ActiveObjects[i].UpdateSign(player.SteamPlayer);
                for (int i = 0; i < Kits.RequestSigns.ActiveObjects.Count; i++)
                    Kits.RequestSigns.ActiveObjects[i].InvokeUpdate(player.SteamPlayer);
            }
            else if (newRank.Level < oldRank?.Level)
            {
                ToastMessage.QueueMessage(player, new ToastMessage(Translation.Translate("demoted_xp", player), newRank.Name.ToUpper(), EToastMessageSeverity.BIG));
                Chat.BroadcastToAllExcept(new List<CSteamID>() { player.CSteamID }, "xp_announce_demoted", F.GetPlayerOriginalNames(player).CharacterName, newRank.Name);
                for (int i = 0; i < VehicleSpawner.ActiveObjects.Count; i++)
                    VehicleSpawner.ActiveObjects[i].UpdateSign(player.SteamPlayer);
                for (int i = 0; i < Kits.RequestSigns.ActiveObjects.Count; i++)
                    Kits.RequestSigns.ActiveObjects[i].InvokeUpdate(player.SteamPlayer);
            }
            if (player.Player.TryGetPlaytimeComponent(out Components.PlaytimeComponent c) && c.stats is IExperienceStats ex)
            {
                ex.AddXP(amount);
            }
        }
        public static void AwardXP(Player player, int amount, string message = "") => AwardXP(UCPlayer.FromPlayer(player), amount, message);
        public static void AwardTW(UCPlayer player, int amount, string message = "")
        {
            if (!Data.TrackStats) return;

            MedalData oldMedals = player.Medals;

            amount = Mathf.RoundToInt(amount * _xpconfig.Data.XPMultiplier);
            int newBalance = Data.DatabaseManager.AddTeamwork(player.Steam64, amount);

            player.UpdateMedals(newBalance);

            MedalData newMedals = player.Medals;

            if (amount != 0 && !(Data.Gamemode is IEndScreen lb && lb.isScreenUp))
            {
                string number = Translation.Translate(amount >= 0 ? "gain_ofp" : "loss_ofp", player, Math.Abs(amount).ToString(Data.Locale));

                if (amount >= 0)
                    number = number.Colorize("ffe392");
                else
                    number = number.Colorize("e0b08d");

                ToastMessage.QueueMessage(player, new ToastMessage(number, EToastMessageSeverity.MINI));

                if (message != "")
                {
                    if (!(message.StartsWith("<") && message.EndsWith(">")))
                    {
                        message = message.Colorize("adadad");
                    }
                    ToastMessage.QueueMessage(player, new ToastMessage(message, EToastMessageSeverity.MINI));
                }
            }

            UpdateTWUI(player);

            if (newMedals.NumberOfMedals > oldMedals.NumberOfMedals && !(Data.Gamemode is IEndScreen l && l.isScreenUp))
            {
                string startString = F.Colorize(Translation.Translate("officer_ui_stars", player, newMedals.NumberOfMedals.ToString(), newMedals.NumberOfMedals.S()).ToUpper(), UCWarfare.GetColorHex("star_color"));

                ToastMessage.QueueMessage(player, new ToastMessage(Translation.Translate("gain_star", player), startString, EToastMessageSeverity.BIG));

                Chat.BroadcastToAllExcept(new List<CSteamID>() { player.CSteamID }, "ofp_announce_gained", F.GetPlayerOriginalNames(player).CharacterName, startString);
            }

            if (player.Player.TryGetPlaytimeComponent(out Components.PlaytimeComponent c) && c.stats is IExperienceStats ex)
            {
                ex.AddOfficerPoints(amount);
            }
        }
        public static void AwardTW(Player player, int amount, string message = "") => AwardTW(UCPlayer.FromPlayer(player), amount, message);
        public static void UpdateXPUI(UCPlayer player)
        {
            short key = (short)XPConfig.RankUI;
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
            short key = (short)TWConfig.MedalsUI;
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

            int progress = UnityEngine.Mathf.RoundToInt(ratio * barLength);

            StringBuilder bars = new StringBuilder();
            for (int i = 0; i < progress; i++)
            {
                bars.Append(XPConfig.ProgressBlockCharacter);
            }
            return bars.ToString();
        }
    }
    
    public class XPConfig : ConfigData
    {
        public char ProgressBlockCharacter;
        public int EnemyKilledXP;
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
        public float TimeBetweenXpAndOfpAwardForTransport;
        public int ShovelXP;
        public int BuiltFOBXP;
        public int BuiltAmmoCrateXP;
        public int BuiltRepairStationXP;
        public int BuiltEmplacementXP;
        public int BuiltBarricadeXP;
        public int OnDutyXP;
        public int RessupplyFriendlyXP;
        public int UnloadSuppliesXP;
        public Dictionary<EVehicleType, int> VehicleDestroyedXP;
        public Dictionary<int, int> InfantryRankValues;
        public Dictionary<int, int> ArmorRankValues;
        public Dictionary<int, int> AirforceRankValues;

        public float XPMultiplier;

        public ushort RankUI;

        public override void SetDefaults()
        {
            ProgressBlockCharacter = '█';
            EnemyKilledXP = 10;
            FriendlyKilledXP = -50;
            FriendlyRevivedXP = 25;
            FOBKilledXP = 150;
            FOBTeamkilledXP = -1500;
            FOBBunkerKilledXP = 100;
            FOBBunkerTeamkilledXP = -800;
            FOBDeployedXP = 10;
            FlagCapturedXP = 120;
            FlagAttackXP = 5;
            FlagDefendXP = 5;
            FlagNeutralizedXP = 40;
            TransportPlayerXP = 10;
            TimeBetweenXpAndOfpAwardForTransport = 10f;
            ShovelXP = 3;
            BuiltFOBXP = 50;
            BuiltAmmoCrateXP = 10;
            BuiltRepairStationXP = 25;
            BuiltEmplacementXP = 15;
            BuiltBarricadeXP = 5;
            OnDutyXP = 5;
            UnloadSuppliesXP = 50;


            VehicleDestroyedXP = new Dictionary<EVehicleType, int>()
            {
                {EVehicleType.HUMVEE, 40},
                {EVehicleType.TRANSPORT, 30},
                {EVehicleType.LOGISTICS, 50},
                {EVehicleType.SCOUT_CAR, 60},
                {EVehicleType.APC, 80},
                {EVehicleType.IFV, 100},
                {EVehicleType.MBT, 200},
                {EVehicleType.HELI_TRANSPORT, 60},
                {EVehicleType.EMPLACEMENT, 30},
            };

            XPMultiplier = 1f;

            RankUI = 36031;

            InfantryRankValues = new Dictionary<int, int>()
            {
                {0, 1500 },
                {1, 3800 },
                {2, 5300 },
                {3, 7200 },
                {4, 12500 },
                {5, 15000 },
                {6, 20000 },
                {7, 24000 },
                {8, 28000 },
                {9, 32000 },
                {10, 40000 }
            };
            ArmorRankValues = new Dictionary<int, int>()
            {
                {0, 1500 },
                {1, 3800 },
                {2, 5300 },
                {3, 7200 },
                {4, 12500 },
                {5, 15000 },
                {6, 20000 },
                {7, 24000 },
                {8, 28000 },
                {9, 32000 },
                {10, 40000 }
            };
            AirforceRankValues = new Dictionary<int, int>()
            {
                {0, 1500 },
                {1, 3800 },
                {2, 5300 },
                {3, 7200 },
                {4, 12500 },
                {5, 15000 },
                {6, 20000 },
                {7, 24000 },
                {8, 28000 },
                {9, 32000 },
                {10, 40000 }
            };
        }
        public XPConfig()
        { }
    }
    public class TWConfig : ConfigData
    {
        public ushort MedalsUI;
        public int FirstStarPoints;
        public int PointsIncreasePerStar;
        public int RallyUsedPoints;
        public int MemberFlagCapturePoints;
        public int ResupplyFriendlyPoints;
        public int UnloadSuppliesPoints;

        public override void SetDefaults()
        {
            MedalsUI = 36033;
            FirstStarPoints = 1000;
            PointsIncreasePerStar = 400;
            RallyUsedPoints = 30;
            MemberFlagCapturePoints = 15;
            ResupplyFriendlyPoints = 20;
            UnloadSuppliesPoints = 20;
        }
        public TWConfig()
        { }
    }
}
