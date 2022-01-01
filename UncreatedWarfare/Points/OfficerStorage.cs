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
        public static bool IsOfficer(ulong playerID, out OfficerData officer)
        {
            L.Log("playerID: " + playerID);
            officer = GetObject(o => o.Steam64 == playerID, true);
            return officer != null;
        }
        public static void ChangeOfficerRank(ulong playerID, int newOfficerLevel, EBranch newBranch)
        {
            bool isNewOfficer = false;

            if (ObjectExists(o => o.Steam64 == playerID, out var officer))
            {
                if (officer.OfficerTier == newOfficerLevel && officer.Branch == newBranch)
                    return;

                UpdateObjectsWhere(o => o.Steam64 == playerID, o => { o.Branch = newBranch; o.OfficerTier = newOfficerLevel; });                
            }
            else
            {
                AddObjectToSave(new OfficerData(playerID, newBranch, newOfficerLevel));
                isNewOfficer = true;
            }

            UCPlayer player = UCPlayer.FromID(playerID);
            if (player != null)
            {
                player.RedownloadRanks();

                if (isNewOfficer || newBranch != officer.Branch || newOfficerLevel >= officer.OfficerTier)
                {
                    player.Message("officer_promoted", player.CurrentRank.Name, Translation.TranslateBranch(newBranch, player));

                    FPlayerName names = F.GetPlayerOriginalNames(player);
                    for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
                    {
                        if (PlayerManager.OnlinePlayers[i].Steam64 != player.Steam64)
                        {
                            PlayerManager.OnlinePlayers[i].Message("officer_announce_promoted", names.CharacterName, player.CurrentRank.Name, Translation.TranslateBranch(newBranch, PlayerManager.OnlinePlayers[i]));
                        }
                    }
                }
                else
                {
                    player.Message("officer_demoted", player.CurrentRank.Name);

                    FPlayerName names = F.GetPlayerOriginalNames(player);
                    for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
                    {
                        if (PlayerManager.OnlinePlayers[i].Steam64 != player.Steam64)
                        {
                            PlayerManager.OnlinePlayers[i].Message("officer_announce_demoted", names.CharacterName, player.CurrentRank.Name);
                        }
                    }
                }
                Points.UpdateXPUI(player);
            }
        }
        public static void DischargeOfficer(ulong playerID)
        {
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
        public EBranch Branch;
        public int OfficerTier;

        public OfficerData(ulong steam64, EBranch branch, int officerLevel)
        {
            Steam64 = steam64;
            Branch = branch;
            OfficerTier = officerLevel;
        }
        public OfficerData() { }
    }
}
