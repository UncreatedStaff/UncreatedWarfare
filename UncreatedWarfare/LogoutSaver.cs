using Rocket.Unturned;
using Rocket.Unturned.Player;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare
{
    public class LogoutSaver : JSONSaver<LogoutSave>
    {
        public static List<LogoutSave> ActiveSaves;

        public LogoutSaver()
            : base(Data.KitsStorage + "playersaves.json")
        {
            ActiveSaves = new List<LogoutSave>();
        }
        protected override string LoadDefaults() => "[]";
        protected static void ReloadActiveSaves() => ActiveSaves = GetExistingObjects();
        private static LogoutSave AddSave(CSteamID playerID, ulong team, Kit.EClass kitClass, EBranch branch, string kitName) => AddObjectToSave(new LogoutSave(playerID.m_SteamID, team, kitClass, branch, kitName));
        private static void RemoveSave(CSteamID playerID) => RemoveFromSaveWhere(ks => ks.Steam64 == playerID.m_SteamID);
        public static bool HasSave(CSteamID playerID, out LogoutSave save) => ObjectExists(ks => ks.Steam64 == playerID.m_SteamID, out save);
        public static bool HasSave(ulong playerID, out LogoutSave save) => ObjectExists(ks => ks.Steam64 == playerID, out save);
        public static LogoutSave GetSave(CSteamID playerID) => GetObject(s => s.Steam64 == playerID.m_SteamID);
        public static void UpdateSave(Func<LogoutSave, bool> selector, Action<LogoutSave> operation)
        {
            ActiveSaves.Where(selector).ToList().ForEach(operation);
            OverwriteSavedList(ActiveSaves);
        }
        public static void InvokePlayerConnected(UnturnedPlayer player) => OnPlayerConnected(player);
        public static void InvokePlayerDisconnected(UnturnedPlayer player) => OnPlayerDisconnected(player);
        private static void OnPlayerConnected(UnturnedPlayer rocketplayer)
        {
            if (!HasSave(rocketplayer.CSteamID, out LogoutSave save))
            {
                ActiveSaves.Add(AddSave(rocketplayer.CSteamID, rocketplayer.Player.quests.groupID.m_SteamID, Kit.EClass.NONE, EBranch.DEFAULT, TeamManager.DefaultKit));
            } else
            {
                ActiveSaves.Add(save);
            }
        }
        private static void OnPlayerDisconnected(UnturnedPlayer player)
        {
            ActiveSaves.RemoveAll(s => s.Steam64 == player.CSteamID.m_SteamID);
        }
    }

    public class LogoutSave
    {
        public ulong Steam64;
        public ulong Team;
        public Kit.EClass KitClass;
        public EBranch Branch;
        public string KitName;

        public LogoutSave(ulong steam64, ulong team, Kit.EClass kitClass, EBranch branch, string kitName)
        {
            Steam64 = steam64;
            this.Team = team;
            this.KitClass = kitClass;
            this.Branch = branch;
            KitName = kitName;
        }
    }
}
