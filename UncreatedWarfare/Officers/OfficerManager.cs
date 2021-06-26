using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Flags;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.XP;
using Flag = Uncreated.Warfare.Flags.Flag;

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
            int points = GetOfficerPoints(player.Player, player.GetTeam());

            if (points > 0)
                UpdateUI(player.Player, points);

            if (IsOfficer(player.CSteamID, out var officer) && player.GetTeam() == officer.team)
            {
                player.OfficerRank =  config.data.OfficerRanks.Where(r => r.level == officer.officerLevel).FirstOrDefault();
            }
        }
        public static void OnPlayerLeft(UCPlayer player)
        {
            
        }
        public static void OnGroupChanged(SteamPlayer player, ulong oldGroup, ulong newGroup)
        {
            UpdateUI(player.player, GetOfficerPoints(player.player, newGroup));
        }
        public static void OnEnemyKilled(UCWarfare.KillEventArgs parameters)
        {
            AddOfficerPoints(parameters.killer, parameters.killer.GetTeam(), config.data.MemberEnemyKilledPoints);
        }
        public static void OnFriendlyKilled(UCWarfare.KillEventArgs parameters)
        {
            AddOfficerPoints(parameters.killer, parameters.killer.GetTeam(), config.data.FriendlyKilledPoints);
        }
        public static void OnFlagCaptured(Flag flag, ulong capturedTeam, ulong lostTeam)
        {
            foreach (var nelsonplayer in flag.PlayersOnFlag)
            {
                var player = UCPlayer.FromPlayer(nelsonplayer);

                if (player.Squad?.Members.Count > 1)
                {
                    int PointsToGive = 0;

                    foreach (var member in player.Squad.Members)
                    {
                        if ((member.Position - player.Squad.Leader.Position).sqrMagnitude < Math.Pow(100, 2))
                        {
                            PointsToGive += config.data.MemberFlagCapturePoints;
                        }
                    }
                    if (PointsToGive > 0)
                    {
                        AddOfficerPoints(player.Player, capturedTeam, PointsToGive);
                    }
                }
            }
        }
        public static void OnFlagNeutralized(Flag flag, ulong capturedTeam, ulong lostTeam)
        {

        }

        public static int GetOfficerPoints(Player player, ulong team) => Data.SyncDB.GetOfficerPoints(player.channel.owner.playerID.steamID.m_SteamID, team);
        public static void AddOfficerPoints(Player player, ulong team, int amount)
        {
            int newBalance = Data.SyncDB.AddOfficerPoints(player.channel.owner.playerID.steamID.m_SteamID, team, (int)(amount * config.data.PointsMultiplier));
            UpdateUI(player, newBalance);
        }

        public static void ChangeOfficerRank(UCPlayer player, int newLevel, EBranch branch)
        {
            if (ObjectExists(o => o.steamID == player.Steam64, out var officer))
            {
                if (newLevel == officer.officerLevel && branch == officer.branch)
                    return;

                UpdateObjectsWhere(o => o.steamID == player.CSteamID.m_SteamID, o => o.officerLevel = newLevel);

                if (branch != officer.branch || newLevel >= officer.officerLevel)
                {
                    player.Message("officer_promoted", newLevel.ToString(), branch.ToString());
                }
                else
                {
                    player.Message("officer_demoted", newLevel.ToString());
                }
            }
            else
            {
                AddObjectToSave(new Officer(player.CSteamID.m_SteamID, player.GetTeam(), newLevel, branch));

                player.Message("officer_promoted", newLevel.ToString(), branch.ToString());
            }
        }

        public static void DischargeOfficer(UCPlayer player)
        {
            RemoveWhere(o => o.steamID == player.CSteamID.m_SteamID);

            player.Message("officer_discharged");
        }

        public static bool IsOfficer(CSteamID playerID, out Officer officer)
        {
            officer = GetObject(o => o.steamID == playerID.m_SteamID);
            return officer != null;
        }

        public static void UpdateUI(Player player, int balance)
        {
            int currentPoints = GetCurrentLevelPoints(balance);
            int requiredPoints = GetRequiredLevelPoints(balance);

            EffectManager.sendUIEffect(config.data.StarsUI, (short)config.data.StarsUI, player.channel.owner.transportConnection, true,
                GetStars(balance).ToString(),
                currentPoints + "/" + requiredPoints,
                GetProgress(currentPoints, requiredPoints)
            );
        }
        private static string GetProgress(int currentPoints, int totalPoints, int barLength = 40)
        {
            float ratio = currentPoints / (float)totalPoints;

            int progress = (int)Math.Round(ratio * barLength);

            string bars = "";
            for (int i = 0; i < progress; i++)
            {
                bars += "█";
            }
            return bars;
        }
        public static int GetRequiredLevelPoints(int totalPoints)
        {
            int a = config.data.FirstStarPoints;
            int d = config.data.PointsIncreasePerStar;

            int stars = GetStars(totalPoints);

            return (int)(stars / 2.0 * ((2 * a) + ((stars - 1) * d)) - (stars - 1) / 2.0 * ((2 * a) + ((stars - 2) * d)));
        }
        public static int GetCurrentLevelPoints(int totalPoints)
        {
            int a = config.data.FirstStarPoints;
            int d = config.data.PointsIncreasePerStar;

            int stars = GetStars(totalPoints);

            return (int)(GetRequiredLevelPoints(totalPoints) - ((stars / 2.0 * ((2 * a) + ((stars - 1) * d))) - totalPoints));
        }
        public static int GetStars(int totalPoints)
        {
            int a = config.data.FirstStarPoints;
            int d = config.data.PointsIncreasePerStar;

            return (int)Math.Floor(1 + ((0.5 * d) - a + Math.Sqrt(Math.Pow(a - 0.5 * d, 2) + (2 * d * totalPoints))) / d);
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
    }

    public class OfficerConfigData : ConfigData
    {
        public int FriendlyKilledPoints;
        public int MemberEnemyKilledPoints;
        public int MemberFlagCapturePoints;
        public int MemberFlagNeutralized;
        public int FirstStarPoints;
        public int PointsIncreasePerStar;
        public float PointsMultiplier;
        public ushort StarsUI;
        public List<Rank> OfficerRanks;

        public override void SetDefaults()
        {
            FriendlyKilledPoints = -1;
            MemberEnemyKilledPoints = 1;
            MemberFlagCapturePoints = 30;
            MemberFlagNeutralized = 10;

            FirstStarPoints = 1000;
            PointsIncreasePerStar = 500;
            PointsMultiplier = 1;

            StarsUI = 32364;

            OfficerRanks = new List<Rank>();
            OfficerRanks.Add(new Rank(1, "Captain", "Cpt.", 30000));
            OfficerRanks.Add(new Rank(2, "Major", "Maj.", 40000));
            OfficerRanks.Add(new Rank(3, "Lieutenant", "Lt.", 50000));
            OfficerRanks.Add(new Rank(4, "Colonel", "Col.", 60000));
            OfficerRanks.Add(new Rank(5, "General", "Gen.", 100000));
        }

        public OfficerConfigData() { }
    }
}
