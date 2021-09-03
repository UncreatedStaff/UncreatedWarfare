using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Players;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.XP;
using UnityEngine;

namespace Uncreated.Warfare.Officers
{
    public class OfficerManager : JSONSaver<Officer>
    {
        public static Config<OfficerConfigData> config;

        public OfficerManager()
            : base(Data.OfficerStorage + "officers.json")
        {
            config = new Config<OfficerConfigData>(Data.OfficerStorage, "config.json");
            Reload();
        }
        public static void OnPlayerJoined(UCPlayer player)
        {
            if (player.IsTeam1() || player.IsTeam2())
            {
                int points = GetOfficerPoints(player.Player, true);

                if (IsOfficer(player.CSteamID.m_SteamID, out Officer officer))
                {
                    player.OfficerRank = GetOfficerRank(officer.officerLevel);
                }
                UpdateUI(player.Player, points, out _);
            }
        }
        public static void OnGroupChanged(SteamPlayer player, ulong oldGroup, ulong newGroup)
        {
            int op = GetOfficerPoints(player.player, true);
            UpdateUI(player.player, op, out _);
        }
        public static int GetOfficerPoints(Player player, bool important)
        {
            UCPlayer ucplayer = UCPlayer.FromPlayer(player);
            if (ucplayer == default || important || ucplayer.CachedOfp == -1)
            {
                int newofp = Data.DatabaseManager.GetOfficerPoints(player.channel.owner.playerID.steamID.m_SteamID);
                if (ucplayer != null)
                    ucplayer.CachedOfp = newofp;
                return newofp;
            }
            else return ucplayer.CachedOfp;

        }
        public static int GetOfficerPoints(ulong player, bool important)
        {
            UCPlayer ucplayer = UCPlayer.FromID(player);
            if (ucplayer == default || important || ucplayer.CachedOfp == -1)
            {
                int newofp = Data.DatabaseManager.GetOfficerPoints(player);
                if (ucplayer != default)
                    ucplayer.CachedOfp = newofp;
                return newofp;
            }
            else return ucplayer.CachedOfp;
        }
        public static void AddOfficerPoints(Player player, int amount, string message = "")
        {
            UCPlayer ucplayer = UCPlayer.FromPlayer(player);

            int oldStars = int.MaxValue;
            if (ucplayer != null)
                oldStars = GetStars(ucplayer.CachedOfp);

            int newBalance = Data.DatabaseManager.AddOfficerPoints(player.channel.owner.playerID.steamID.m_SteamID, Mathf.RoundToInt(amount * config.Data.PointsMultiplier));
            if (ucplayer != null)
                ucplayer.CachedOfp = newBalance;

            if (message != "" && amount != 0)
                ToastMessage.QueueMessage(player, F.Translate(amount >= 0 ? "gain_ofp" : "loss_ofp", player, Math.Abs(amount).ToString(Data.Locale)), message, ToastMessageSeverity.MINIOFFICERPTS);

            UpdateUI(player, newBalance, out int stars);

            if (stars > oldStars)
            {
                string startString = F.Colorize(F.Translate("officer_ui_stars", player, stars.ToString(), stars.S()).ToUpper(), UCWarfare.GetColorHex("star_color"));

                ToastMessage.QueueMessage(player, F.Translate("gain_star", player), startString, ToastMessageSeverity.BIG);

                F.BroadcastToAllExcept(new List<CSteamID>() { ucplayer.CSteamID }, "ofp_announce_gained", F.GetPlayerOriginalNames(ucplayer).CharacterName, startString);
            }

            if (player.TryGetPlaytimeComponent(out Components.PlaytimeComponent c))
            {
                c.stats.AddOfficerPoints(amount);
            }
        }
        public static Rank GetOfficerRank(int officerRankLevel)
        {
            return config.Data.OfficerRanks.Where(r => r.level == officerRankLevel).FirstOrDefault();
        }
        public static void ChangeOfficerRank(UCPlayer player, Rank newRank, EBranch branch)
        {
            if (ObjectExists(o => o.steamID == player.Steam64, out var officer))
            {
                if (newRank.level == officer.officerLevel && branch == officer.branch)
                    return;

                UpdateObjectsWhere(o => o.steamID == player.CSteamID.m_SteamID, o => o.officerLevel = newRank.level);

                if (branch != officer.branch || newRank.level >= officer.officerLevel)
                {
                    player.Message("officer_promoted", newRank.TranslateName(player.Steam64), F.TranslateBranch(branch, player));

                    FPlayerName names = F.GetPlayerOriginalNames(player);
                    for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
                    {
                        if (PlayerManager.OnlinePlayers[i].Steam64 != player.Steam64)
                        {
                            PlayerManager.OnlinePlayers[i].Message("officer_announce_promoted", names.CharacterName, newRank.TranslateName(PlayerManager.OnlinePlayers[i].Steam64), F.TranslateBranch(branch, PlayerManager.OnlinePlayers[i]));
                        }
                    }
                }
                else
                {
                    player.Message("officer_demoted", newRank.TranslateName(player.Steam64));

                    FPlayerName names = F.GetPlayerOriginalNames(player);
                    for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
                    {
                        if (PlayerManager.OnlinePlayers[i].Steam64 != player.Steam64)
                        {
                            PlayerManager.OnlinePlayers[i].Message("officer_announce_demoted", names.CharacterName);
                        }
                    }
                }
            }
            else
            {
                AddObjectToSave(new Officer(player.CSteamID.m_SteamID, newRank.level, branch));

                player.Message("officer_promoted", newRank.TranslateName(player.Steam64), F.TranslateBranch(branch, player));

                FPlayerName names = F.GetPlayerOriginalNames(player);
                for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
                {
                    if (PlayerManager.OnlinePlayers[i].Steam64 != player.Steam64)
                    {
                        PlayerManager.OnlinePlayers[i].Message("officer_announce_promoted", names.CharacterName, newRank.TranslateName(PlayerManager.OnlinePlayers[i].Steam64), F.TranslateBranch(branch, PlayerManager.OnlinePlayers[i]));
                    }
                }
            }
        }
        public static void ChangeOfficerRank(ulong player, Rank newRank, EBranch branch)
        {
            UCPlayer ucplayer = UCPlayer.FromID(player);
            if (ObjectExists(o => o.steamID == player, out Officer officer))
            {
                if (newRank.level == officer.officerLevel && branch == officer.branch)
                    return;

                UpdateObjectsWhere(o => o.steamID == player, o => o.officerLevel = newRank.level);

                if (branch != officer.branch || newRank.level >= officer.officerLevel)
                {
                    ucplayer?.Message("officer_promoted", newRank.TranslateName(player), F.TranslateBranch(branch, player));
                    FPlayerName names = F.GetPlayerOriginalNames(player);
                    for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
                    {
                        if (PlayerManager.OnlinePlayers[i].Steam64 != player)
                        {
                            PlayerManager.OnlinePlayers[i].Message("officer_announce_promoted", names.CharacterName, newRank.TranslateName(PlayerManager.OnlinePlayers[i].Steam64), F.TranslateBranch(branch, PlayerManager.OnlinePlayers[i]));
                        }
                    }
                }
                else
                {
                    ucplayer?.Message("officer_demoted", newRank.TranslateName(player));
                    FPlayerName names = F.GetPlayerOriginalNames(player);
                    for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
                    {
                        if (PlayerManager.OnlinePlayers[i].Steam64 != player)
                        {
                            PlayerManager.OnlinePlayers[i].Message("officer_announce_demoted", names.CharacterName);
                        }
                    }
                }
            }
            else
            {
                AddObjectToSave(new Officer(player, newRank.level, branch));

                ucplayer?.Message("officer_promoted", newRank.TranslateName(player), F.TranslateBranch(branch, player));
                FPlayerName names = F.GetPlayerOriginalNames(player);
                for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
                {
                    if (PlayerManager.OnlinePlayers[i].Steam64 != player)
                    {
                        PlayerManager.OnlinePlayers[i].Message("officer_announce_promoted", names.CharacterName, newRank.TranslateName(PlayerManager.OnlinePlayers[i].Steam64), F.TranslateBranch(branch, PlayerManager.OnlinePlayers[i]));
                    }
                }
            }
        }
        public static void DischargeOfficer(UCPlayer player, Rank currentRank)
        {
            RemoveWhere(o => o.steamID == player.CSteamID.m_SteamID);

            player.Message("officer_discharged", currentRank.TranslateName(player.Steam64));
            FPlayerName names = F.GetPlayerOriginalNames(player);
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                if (PlayerManager.OnlinePlayers[i].Steam64 != player.Steam64)
                {
                    PlayerManager.OnlinePlayers[i].Message("officer_announce_discharged", names.CharacterName, currentRank.TranslateName(PlayerManager.OnlinePlayers[i].Steam64));
                }
            }
        }
        public static void DischargeOfficer(ulong player, Rank currentRank)
        {
            UCPlayer ucplayer = UCPlayer.FromID(player);
            RemoveWhere(o => o.steamID == player);

            ucplayer?.Message("officer_discharged", currentRank.TranslateName(player));
            FPlayerName names = F.GetPlayerOriginalNames(player);
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                if (PlayerManager.OnlinePlayers[i].Steam64 != player)
                {
                    PlayerManager.OnlinePlayers[i].Message("officer_announce_discharged", names.CharacterName, currentRank.TranslateName(PlayerManager.OnlinePlayers[i].Steam64));
                }
            }
        }
        public static bool IsOfficer(ulong playerID, out Officer officer)
        {
            officer = GetObject(o => o.steamID == playerID);
            return officer != null;
        }
        public static void UpdateUI(Player player, int balance, out int stars)
        {
            int currentPoints = GetCurrentLevelPoints(balance);
            int requiredPoints = GetRequiredLevelPoints(balance);

            stars = GetStars(balance);

            EffectManager.sendUIEffect(config.Data.StarsUI, (short)config.Data.StarsUI, player.channel.owner.transportConnection, true);
            EffectManager.sendUIEffectText((short)config.Data.StarsUI, player.channel.owner.transportConnection, true, "Icon",
                stars == 0 ? $"<color=#797979>{config.Data.StarCharacter}</color>" : $"<color=#{UCWarfare.GetColorHex("star_color")}>{config.Data.StarCharacter}</color>"
            );
            EffectManager.sendUIEffectText((short)config.Data.StarsUI, player.channel.owner.transportConnection, true, "Count",
                stars < 2 ? string.Empty : stars.ToString()
            );
            EffectManager.sendUIEffectText((short)config.Data.StarsUI, player.channel.owner.transportConnection, true, "Info",
                stars == 0 ? $"<color=#{UCWarfare.GetColorHex("officer_ui_no_stars")}>{F.Translate("officer_ui_no_stars", player)}</color>" : F.Translate("officer_ui_stars", player, stars.ToString(Data.Locale), stars.S())
            );
            EffectManager.sendUIEffectText((short)config.Data.StarsUI, player.channel.owner.transportConnection, true, "Points",
                currentPoints + "/" + requiredPoints
            );
            EffectManager.sendUIEffectText((short)config.Data.StarsUI, player.channel.owner.transportConnection, true, "Progress",
                GetProgress(currentPoints, requiredPoints)
            );
        }
        private static string GetProgress(int currentPoints, int totalPoints, uint barLength = 40)
        {
            float ratio = currentPoints / (float)totalPoints;

            int progress = Mathf.RoundToInt(ratio * barLength);

            string bars = string.Empty;
            for (int i = 0; i < progress; i++)
            {
                bars += config.Data.FullBlock;
            }
            return bars;
        }
        public static int GetRequiredLevelPoints(int totalPoints)
        {
            int a = config.Data.FirstStarPoints;
            int d = config.Data.PointsIncreasePerStar;

            int stars = Mathf.RoundToInt(Mathf.Floor(1f + ((0.5f * d) - a + Mathf.Sqrt(Mathf.Pow(a - 0.5f * d, 2) + (2f * d * totalPoints))) / d));

            return Mathf.RoundToInt(stars / 2.0f * ((2f * a) + ((stars - 1f) * d)) - (stars - 1f) / 2.0f * ((2f * a) + ((stars - 2f) * d)));
        }
        public static int GetCurrentLevelPoints(int totalPoints)
        {
            int a = config.Data.FirstStarPoints;
            int d = config.Data.PointsIncreasePerStar;

            int stars = Mathf.RoundToInt(Mathf.Floor(1f + ((0.5f * d) - a + Mathf.Sqrt(Mathf.Pow(a - 0.5f * d, 2f) + (2f * d * totalPoints))) / d));

            return Mathf.RoundToInt(GetRequiredLevelPoints(totalPoints) - ((stars / 2.0f * ((2f * a) + ((stars - 1f) * d))) - totalPoints));
        }
        public static int GetStars(int totalPoints)
        {
            int a = config.Data.FirstStarPoints;
            int d = config.Data.PointsIncreasePerStar;

            return Mathf.RoundToInt(Mathf.Floor(((0.5f * d) - a + Mathf.Sqrt(Mathf.Pow(a - 0.5f * d, 2f) + (2f * d * totalPoints))) / d));
        }
        public static Rank GetRankFromLevel(int level)
        {
            if (level <= 0)
                if (config.Data.OfficerRanks.Length > 0)
                    return config.Data.OfficerRanks[0];
                else return null;
            if (config.Data.OfficerRanks.Length > level - 1)
                return config.Data.OfficerRanks[level - 1];
            return config.Data.OfficerRanks[config.Data.OfficerRanks.Length - 1];
        }
        protected override string LoadDefaults() => "[]";
    }

    public class Officer
    {
        public ulong steamID;
        public int officerLevel;
        public EBranch branch;
        public Officer(ulong steamID, int officerLevel, EBranch branch)
        {
            this.steamID = steamID;
            this.officerLevel = officerLevel;
            this.branch = branch;
        }
        public Officer()
        {
            this.steamID = 0;
            this.officerLevel = 0;
            this.branch = EBranch.DEFAULT;
        }
    }

    public class OfficerConfigData : ConfigData
    {
        public int FriendlyKilledPoints;
        public int MemberEnemyKilledPoints;
        public int MemberFlagTickPoints;
        public int MemberFlagCapturePoints;
        public int MemberFlagNeutralizedPoints;
        public int TransportPlayerPoints;
        public int SpawnOnRallyPoints;
        public int BuiltFOBPoints;
        public int BuiltAmmoCratePoints;
        public int BuiltRepairStationPoints;
        public int BuiltEmplacementPoints;
        public int BuiltBarricadePoints;
        public int RallyDeployPoints;
        public Dictionary<EVehicleType, int> VehicleDestroyedPoints;
        public int FirstStarPoints;
        public int PointsIncreasePerStar;
        public float PointsMultiplier;
        public ushort StarsUI;
        public Rank[] OfficerRanks;
        public char FullBlock;
        public char StarCharacter;
        public override void SetDefaults()
        {
            FriendlyKilledPoints = -1;
            MemberEnemyKilledPoints = 1;
            MemberFlagTickPoints = 1;
            MemberFlagCapturePoints = 30;
            MemberFlagNeutralizedPoints = 10;
            TransportPlayerPoints = 1;
            SpawnOnRallyPoints = 1;
            BuiltFOBPoints = 50;
            BuiltAmmoCratePoints = 10;
            BuiltRepairStationPoints = 40;
            BuiltEmplacementPoints = 5;
            BuiltBarricadePoints = 1;
            RallyDeployPoints = 10;
            VehicleDestroyedPoints = new Dictionary<EVehicleType, int>()
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



            FirstStarPoints = 2000;
            PointsIncreasePerStar = 400;
            PointsMultiplier = 1;

            StarsUI = 36033;

            OfficerRanks = new Rank[]
            {
                new Rank(1, "Captain", "Cpt.", 50000),
                new Rank(2, "Major", "Maj.", 60000),
                new Rank(3, "Lieutenant", "Lt.", 70000),
                new Rank(4, "Colonel", "Col.", 80000),
                new Rank(5, "General", "Gen.", 100000)
            };

            FullBlock = '█';
            StarCharacter = '¼';
        }
        public OfficerConfigData() { }
    }
}
