using Rocket.Unturned;
using Rocket.Unturned.Player;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncreatedWarfare.Kits;
using UncreatedWarfare.Teams;

namespace UncreatedWarfare
{
    public class LogoutSaver : JSONSaver<LogoutSave>
    {
        public static List<LogoutSave> ActiveSaves;

        public LogoutSaver()
            : base(Data.KitsStorage + "playersaves.json")
        {
            ActiveSaves = new List<LogoutSave>();

            U.Events.OnPlayerConnected += OnPlayerConnected;
            U.Events.OnPlayerDisconnected += OnPlayerDisconnected;

        }
        protected override string LoadDefaults() => "[]";
        protected static void ReloadActiveSaves() => ActiveSaves = GetExistingObjects();
        private static void AddSave(CSteamID playerID, ulong team, Kit.EClass kitClass, EBranch branch, string kitName) => AddObjectToSave(new LogoutSave(playerID.m_SteamID, team, kitClass, branch, kitName));
        private static void RemoveSave(CSteamID playerID) => RemoveFromSaveWhere(ks => ks.Steam64 == playerID.m_SteamID);
        public static bool HasSave(CSteamID playerID, out LogoutSave save)
        {
            bool result = ObjectExists(ks => ks.Steam64 == playerID.m_SteamID, out var s);
            save = s;
            return result;
        }
        public static LogoutSave GetSave(CSteamID playerID) => GetObject(s => s.Steam64 == playerID.m_SteamID);
        public static void UpdateSave(Func<LogoutSave, bool> selector, Action<LogoutSave> operation)
        {
            ActiveSaves.Where(selector).ToList().ForEach(operation);
            OverwriteSavedList(ActiveSaves);
        }

        private void OnPlayerConnected(UnturnedPlayer rocketplayer)
        {
            if (!HasSave(rocketplayer.CSteamID, out var save))
            {
                AddSave(rocketplayer.CSteamID, rocketplayer.Player.quests.groupID.m_SteamID, Kit.EClass.NONE, EBranch.DEFAULT, "");
            }
            ActiveSaves.Add(save);
        }
        private void OnPlayerDisconnected(UnturnedPlayer player)
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
