using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.XP;
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

        public static async Task OnPlayerJoined(UCPlayer player)
        {
            int points = await GetOfficerPoints(player.Player, player.GetTeam());

            SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
            await rtn;
            if (IsOfficer(player.CSteamID, out var officer) && player.GetTeam() == officer.team)
            {
                player.OfficerRank = GetOfficerRank(officer.officerLevel);
            }
            UpdateUI(player.Player, points);
        }
        public static async Task OnPlayerLeft(UCPlayer player)
        {
            await Task.Yield(); // just to remove the warning, feel free to remove, its basically an empty line.
        }
        public static async Task OnGroupChanged(SteamPlayer player, ulong oldGroup, ulong newGroup)
        {
            int op = await GetOfficerPoints(player.player, newGroup);
            SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
            UpdateUI(player.player, op);
            await rtn;
        }

        public static async Task<int> GetOfficerPoints(Player player, ulong team) => await Data.DatabaseManager.GetOfficerPoints(player.channel.owner.playerID.steamID.m_SteamID, team);
        public static async Task<int> GetOfficerPoints(ulong playerID, ulong team) => await Data.DatabaseManager.GetOfficerPoints(playerID, team);
        public static async Task AddOfficerPoints(Player player, ulong team, int amount)
        {
            int newBalance = await Data.DatabaseManager.AddOfficerPoints(player.channel.owner.playerID.steamID.m_SteamID, team, (int)(Math.Round(amount * config.data.PointsMultiplier)));
            SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
            UpdateUI(player, newBalance);
            await rtn;
        }
        public static Rank GetOfficerRank(int officerRankLevel)
        {
            return config.data.OfficerRanks.Where(r => r.level == officerRankLevel).FirstOrDefault();
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
                    player.Message("officer_promoted", newLevel.ToString(Data.Locale), branch.ToString());
                }
                else
                {
                    player.Message("officer_demoted", newLevel.ToString(Data.Locale));
                }
            }
            else
            {
                AddObjectToSave(new Officer(player.CSteamID.m_SteamID, player.GetTeam(), newLevel, branch));

                player.Message("officer_promoted", newLevel.ToString(Data.Locale), branch.ToString());
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

            int stars = GetStars(balance);

            EffectManager.sendUIEffect(config.data.StarsUI, (short)config.data.StarsUI, player.channel.owner.transportConnection, true);
            EffectManager.sendUIEffectText((short)config.data.StarsUI, player.channel.owner.transportConnection, true, "Icon",
                stars == 0 ? "<color=#737373>¼</color>" : "<color=#ffd683>¼</color>"
            );
            EffectManager.sendUIEffectText((short)config.data.StarsUI, player.channel.owner.transportConnection, true, "Count",
                stars < 2 ? "" : stars.ToString()
            );
            EffectManager.sendUIEffectText((short)config.data.StarsUI, player.channel.owner.transportConnection, true, "Info",
                stars == 0 ? "<color=#737373>no stars</color>" : (stars.ToString() + " star" + (stars == 1 ? "" : "s"))
            );
            EffectManager.sendUIEffectText((short)config.data.StarsUI, player.channel.owner.transportConnection, true, "Points",
                currentPoints + "/" + requiredPoints
            );
            EffectManager.sendUIEffectText((short)config.data.StarsUI, player.channel.owner.transportConnection, true, "Progress",
                GetProgress(currentPoints, requiredPoints)
            );
        }
        private static string GetProgress(int currentPoints, int totalPoints, uint barLength = 40)
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

            int stars = unchecked((int)Math.Floor(1 + ((0.5 * d) - a + Math.Sqrt(Math.Pow(a - 0.5 * d, 2) + (2 * d * totalPoints))) / d));

            return unchecked((int)(stars / 2.0 * ((2 * a) + ((stars - 1) * d)) - (stars - 1) / 2.0 * ((2 * a) + ((stars - 2) * d))));
        }
        public static int GetCurrentLevelPoints(int totalPoints)
        {
            int a = config.data.FirstStarPoints;
            int d = config.data.PointsIncreasePerStar;

            int stars = unchecked((int)Math.Floor(1 + ((0.5 * d) - a + Math.Sqrt(Math.Pow(a - 0.5 * d, 2) + (2 * d * totalPoints))) / d));

            return unchecked((int)(GetRequiredLevelPoints(totalPoints) - ((stars / 2.0 * ((2 * a) + ((stars - 1) * d))) - totalPoints)));
        }
        public static int GetStars(int totalPoints)
        {
            int a = config.data.FirstStarPoints;
            int d = config.data.PointsIncreasePerStar;

            return unchecked((int)Math.Floor(((0.5 * d) - a + Math.Sqrt(Math.Pow(a - 0.5 * d, 2) + (2 * d * totalPoints))) / d));
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
        public int FirstStarPoints;
        public int PointsIncreasePerStar;
        public float PointsMultiplier;
        public ushort StarsUI;
        public List<Rank> OfficerRanks;

        public override void SetDefaults()
        {
            FriendlyKilledPoints = -1;
            MemberEnemyKilledPoints = 1;
            MemberFlagTickPoints = 1;
            MemberFlagCapturePoints = 30;
            MemberFlagNeutralizedPoints = 10;
            TransportPlayerPoints = 1;
            SpawnOnRallyPoints = 1;
            BuiltFOBPoints = 70;
            BuiltAmmoCratePoints = 10;
            BuiltAmmoCratePoints = 40;
            BuiltEmplacementPoints = 5;
            BuiltBarricadePoints = 1;
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
