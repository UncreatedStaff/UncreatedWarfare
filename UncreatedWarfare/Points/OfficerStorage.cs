using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Players;

namespace Uncreated.Warfare.Point
{
    public class OfficerStorage : JSONSaver<OfficerData>
    {
        public OfficerStorage()
            : base(Data.PointsStorage + "officers.json")
        {
            Reload();
        }
        protected override string LoadDefaults() => "[]";
        public static bool IsOfficer(ulong playerID, ulong team, out OfficerData officer)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            officer = GetObject(o => o.Steam64 == playerID && o.Team == team, true);
            return officer != null;
        }
        public static void ChangeOfficerRank(ulong playerID, int newOfficerTier, ulong newTeam)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            bool isNewOfficer = false;

            if (ObjectExists(o => o.Steam64 == playerID && o.Team == newTeam, out var officer))
            {
                if (officer.OfficerTier == newOfficerTier)
                    return;

                UpdateObjectsWhere(o => o.Steam64 == playerID, o => { o.Team = newTeam; o.OfficerTier = newOfficerTier; });                
            }
            else
            {
                AddObjectToSave(new OfficerData(playerID, newTeam, newOfficerTier));
                isNewOfficer = true;
            }

            UCPlayer player = UCPlayer.FromID(playerID);
            if (player != null)
            {
                player.RedownloadRanks();

                if (isNewOfficer || newOfficerTier >= officer.OfficerTier)
                {
                    player.Message("officer_promoted", player.CurrentRank.Name, Translation.Translate("team_" + newTeam, player));

                    FPlayerName names = F.GetPlayerOriginalNames(player);
                    for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
                    {
                        if (PlayerManager.OnlinePlayers[i].Steam64 != player.Steam64)
                        {
                            PlayerManager.OnlinePlayers[i].Message("officer_announce_promoted", names.CharacterName, player.CurrentRank.Name, Translation.Translate("team_" + newTeam, PlayerManager.OnlinePlayers[i]));
                        }
                    }
                }
                else
                {
                    player.Message("officer_demoted", player.CurrentRank.Name, Translation.Translate("team_" + newTeam, player));

                    FPlayerName names = F.GetPlayerOriginalNames(player);
                    for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
                    {
                        if (PlayerManager.OnlinePlayers[i].Steam64 != player.Steam64)
                        {
                            PlayerManager.OnlinePlayers[i].Message("officer_announce_demoted", names.CharacterName, player.CurrentRank.Name, Translation.Translate("team_" + newTeam, PlayerManager.OnlinePlayers[i]));
                        }
                    }
                }
                Points.UpdateXPUI(player);
            }
        }
        public static void DischargeOfficer(ulong playerID)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            RemoveWhere(o => o.Steam64 == playerID);

            UCPlayer player = UCPlayer.FromID(playerID);
            if (player != null)
            {
                player.Message("officer_discharged", player.CurrentRank.Name);
                FPlayerName names = F.GetPlayerOriginalNames(player);
                for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
                {
                    if (PlayerManager.OnlinePlayers[i].Steam64 != player.Steam64)
                    {
                        PlayerManager.OnlinePlayers[i].Message("officer_announce_discharged", names.CharacterName, player.CurrentRank.Name);
                    }
                }
                Points.UpdateXPUI(player);
            }
        }
    }
    public class OfficerData
    {
        public ulong Steam64;
        public ulong Team;
        public int OfficerTier;

        public OfficerData(ulong steam64, ulong team, int officerLevel)
        {
            Steam64 = steam64;
            Team = team;
            OfficerTier = officerLevel;
        }
        public OfficerData() { }
    }
}
