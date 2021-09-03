using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Players;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.XP;
using UnityEngine;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare.Officers
{
    public class OfficerManager :JSONSaver<Officer>
    {
        public static Config<OfficerConfigData> config;

        public OfficerManager()
            :base(Data.OfficerStorage + "officers.json")
        {
            config = new Config<OfficerConfigData>(Data.OfficerStorage, "config.json");
            Reload();
        }
        public static void OnPlayerJoined(UCPlayer player)
        {
            if (player.IsTeam1() || player.IsTeam2())
            {
                int points = GetOfficerPoints(player.Player, player.GetTeam(), true);

                if (IsOfficer(player.CSteamID, out var officer) && player.GetTeam() == officer.team)
                {
                    player.OfficerRank = GetOfficerRank(officer.officerLevel);
                }
                UpdateUI(player.Player, points, out _);
            }
        }
        public static void OnGroupChanged(SteamPlayer player, ulong oldGroup, ulong newGroup)
        {
            int op = GetOfficerPoints(player.player, newGroup, true);
            UpdateUI(player.player, op, out _);
        }
        public static int GetOfficerPoints(Player player, ulong team, bool important)
        {
            if (team < 1 || team > 2) return 0;
            UCPlayer ucplayer = UCPlayer.FromPlayer(player);
            if (ucplayer == default || important || ucplayer.cachedOfp == -1)
            {
                int newofp = Data.DatabaseManager.GetOfficerPoints(player.channel.owner.playerID.steamID.m_SteamID, team);
                if (ucplayer != null)
                    ucplayer.cachedOfp = newofp;
                return newofp;
            }
            else return ucplayer.cachedOfp;
            
        }
        public static int GetOfficerPoints(ulong player, ulong team, bool important)
        {
            if (team < 1 || team > 2) return 0;
            UCPlayer ucplayer = UCPlayer.FromID(player);
            if (ucplayer == default || important || ucplayer.cachedOfp == -1)
            {
                int newofp = Data.DatabaseManager.GetOfficerPoints(player, team);
                if (ucplayer != default)
                    ucplayer.cachedOfp = newofp;
                return newofp;
            }
            else return ucplayer.cachedOfp;
        }
        public static void AddOfficerPoints(Player player, ulong team, int amount, string message ="")
        {
            if (team < 1 || team > 2) return;
            UCPlayer ucplayer = UCPlayer.FromPlayer(player);

            int oldStars = int.MaxValue;
            if (ucplayer != null)
                oldStars = GetStars(ucplayer.cachedOfp);

            int newBalance = Data.DatabaseManager.AddOfficerPoints(player.channel.owner.playerID.steamID.m_SteamID, team, Mathf.RoundToInt(amount * config.Data.PointsMultiplier));
            if (ucplayer != null)
                ucplayer.cachedOfp = newBalance;

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
                c.UCPlayerStats.warfare_stats.AddOfficerPoints(amount);
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

                    for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
                    {
                        if (PlayerManager.OnlinePlayers[i].Steam64 != player.Steam64)
                        {
                            player.Message("officer_announce_promoted", F.GetPlayerOriginalNames(player.Steam64).CharacterName, newRank.TranslateName(PlayerManager.OnlinePlayers[i].Steam64), F.TranslateBranch(branch, PlayerManager.OnlinePlayers[i]));
                        }
                    }
                }
                else
                {
                    player.Message("officer_demoted", newRank.TranslateName(player.Steam64));

                    for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
                    {
                        if (PlayerManager.OnlinePlayers[i].Steam64 != player.Steam64)
                        {
                            player.Message("officer_announce_demoted", F.GetPlayerOriginalNames(player.Steam64).CharacterName);
                        }
                    }
                }
            }
            else
            {
                AddObjectToSave(new Officer(player.CSteamID.m_SteamID, player.GetTeam(), newRank.level, branch));

                player.Message("officer_promoted", newRank.TranslateName(player.Steam64), F.TranslateBranch(branch, player));

                for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
                {
                    if (PlayerManager.OnlinePlayers[i].Steam64 != player.Steam64)
                    {
                        player.Message("officer_announce_promoted", F.GetPlayerOriginalNames(player.Steam64).CharacterName, newRank.TranslateName(PlayerManager.OnlinePlayers[i].Steam64), F.TranslateBranch(branch, PlayerManager.OnlinePlayers[i]));
                    }
                }
            }
        }
        public static void DischargeOfficer(UCPlayer player, Rank currentRank)
        {
            RemoveWhere(o => o.steamID == player.CSteamID.m_SteamID);

            player.Message("officer_discharged", currentRank.TranslateName(player.Steam64));

            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                if (PlayerManager.OnlinePlayers[i].Steam64 != player.Steam64)
                {
                    player.Message("officer_announce_discharged", F.GetPlayerOriginalNames(player.Steam64).CharacterName, currentRank.TranslateName(PlayerManager.OnlinePlayers[i].Steam64));
                }
            }
        }
        public static bool IsOfficer(CSteamID playerID, out Officer officer)
        {
            officer = GetObject(o => o.steamID == playerID.m_SteamID);
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
        protected override string LoadDefaults() => "[]";
    }

    public class Officer
    {
        public ulong steamID;
        public ulong team;
        public int officerLevel;
        public EBranch branch;
        public Officer(ulong steamID, ulong team, int officerLevel, EBranch branch)
        {
            this.steamID = steamID;
            this.team = team;
            this.officerLevel = officerLevel;
            this.branch = branch;
        }
        public Officer()
        {
            this.steamID = 0;
            this.team = 0;
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
        public List<Rank> OfficerRanks;
        public char FullBlock;
        public char StarCharacter;
        public override void SetDefaults()
        {
            FriendlyKilledPoints = -1;
            MemberEnemyKilledPoints = 1;
            MemberFlagTickPoints = 1;
            MemberFlagCapturePoints = 30;
            MemberFlagNeutralizedPoints = 10;
            TransportPlayerPoints = 10;
            SpawnOnRallyPoints = 20;
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

            OfficerRanks = new List<Rank>
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
