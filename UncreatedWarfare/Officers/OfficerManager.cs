using Rocket.Unturned.Player;
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

namespace Uncreated.Warfare.Officers
{
    public class OfficerManager :JSONSaver<Officer>
    {
        public static Config<OfficerConfigData> config;

        public OfficerManager()
            :base(Data.OfficerStorage + "officers.json")
        {
            config = new Config<OfficerConfigData>(Data.OfficerStorage + "config.json");
            Reload();
        }

        public static void OnEnemyKilled(UCWarfare.KillEventArgs parameters)
        {
            AddOfficerPoints(parameters.killer.channel.owner.playerID.steamID, parameters.killer.GetTeam(), config.data.MemberEnemyKilledPoints);
        }
        public static void OnFriendlyKilled(UCWarfare.KillEventArgs parameters)
        {
            AddOfficerPoints(parameters.killer.channel.owner.playerID.steamID, parameters.killer.GetTeam(), config.data.FriendlyKilledPoints);
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
                        AddOfficerPoints(player.CSteamID, player.GetTeam(), PointsToGive);
                    }
                }
            }
        }
        public static void OnFlagNeutralized(Flag flag, ulong capturedTeam, ulong lostTeam)
        {

        }

        public static uint GetOfficerPoints(CSteamID playerID, ulong team) => Data.DatabaseManager.GetOfficerPointsSync(playerID.m_SteamID, (byte)team);
        public static void AddOfficerPoints(CSteamID playerID, ulong team, int amount) => Data.DatabaseManager.AddOfficerPoints(playerID.m_SteamID, (byte)team, amount);

        public void ChangeOfficerRank(UCPlayer player, EOfficerLevel newLevel, EBranch branch)
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
                AddObjectToSave(new Officer(player.CSteamID.m_SteamID, newLevel, branch));

                player.Message("officer_promoted", newLevel.ToString(), branch.ToString());
            }
        }

        public void DischargeOfficer(UnturnedPlayer player)
        {
            RemoveWhere(o => o.steamID == player.CSteamID.m_SteamID);

            player.Message("officer_discharged");
        }

        public bool IsOfficer(CSteamID playerID, out Officer officer)
        {
            officer = GetObject(o => o.steamID == playerID.m_SteamID);
            return officer != null;
        }

        protected override string LoadDefaults() => "[]";
    }

    public class Officer
    {
        public ulong steamID;
        public EOfficerLevel officerLevel;
        public EBranch branch;

        public Officer(ulong steamID, EOfficerLevel officerLevel, EBranch branch)
        {
            this.steamID = steamID;
            this.officerLevel = officerLevel;
            this.branch = branch;
        }
    }

    public enum EOfficerLevel
    {
        CAPTAIN = 1,
        MAJOR = 2,
        LIEUTENANT = 3,
        COLONEL = 4,
        GENERAL = 5
    }

    public class OfficerConfigData : ConfigData
    {
        public int FriendlyKilledPoints;
        public int MemberEnemyKilledPoints;
        public int MemberFlagCapturePoints;
        public int MemberFlagNeutralized;

        public override void SetDefaults()
        {
            FriendlyKilledPoints = -1;
            MemberEnemyKilledPoints = 1;
            MemberFlagCapturePoints = 30;
            MemberFlagNeutralized = 10;
        }

        public OfficerConfigData() { }
    }
}
