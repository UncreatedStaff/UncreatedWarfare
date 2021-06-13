using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;
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
    public class PlayerManager : JSONSaver<LogoutSave>
    {
        public static List<LogoutSave> OnlinePlayers;

        public PlayerManager() : base(Data.KitsStorage + "playersaves.json")
        {
            OnlinePlayers = new List<LogoutSave>();
            foreach (var steamplayer in Provider.clients)
                OnlinePlayers.Add(GetSave(steamplayer.playerID.steamID));
        }
        protected override string LoadDefaults() => "[]";
        private static LogoutSave AddSave(LogoutSave newSave) => 
            AddObjectToSave(newSave);
        private static void RemoveSave(CSteamID playerID) => RemoveSave(playerID.m_SteamID);
        private static void RemoveSave(ulong playerID) => RemoveWhere(ks => ks.Steam64 == playerID);
        public static bool HasSave(CSteamID playerID, out LogoutSave save) => HasSave(playerID.m_SteamID, out save);
        public static bool HasSave(ulong playerID, out LogoutSave save) => ObjectExists(ks => ks.Steam64 == playerID, out save);
        public static LogoutSave GetSave(CSteamID playerID) => GetObject(s => s.Steam64 == playerID.m_SteamID);
        public static LogoutSave GetPlayerData(CSteamID playerID) => GetPlayerData(playerID.m_SteamID);
        public static LogoutSave GetPlayerData(ulong playerID) => OnlinePlayers.Find(p => p.Steam64 == playerID);
        public static bool HasPlayerData(CSteamID playerID, out LogoutSave data)
        {
            data = GetPlayerData(playerID);
            return data != null;
        }
        public static bool HasPlayerData(ulong playerID, out LogoutSave data)
        {
            data = GetPlayerData(playerID);
            return data != null;
        }
        public static void UpdateData(Func<LogoutSave, bool> selector, Action<LogoutSave> operation)
        {
            OnlinePlayers.Where(selector).ToList().ForEach(operation);
            OverwriteSavedList(OnlinePlayers);
        }
        public static void InvokePlayerConnected(UnturnedPlayer player) => OnPlayerConnected(player);
        public static void InvokePlayerDisconnected(UnturnedPlayer player) => OnPlayerDisconnected(player);
        private static void OnPlayerConnected(UnturnedPlayer rocketplayer)
        {
            if (!HasSave(rocketplayer.CSteamID, out var currentSave))
            {
                var newSave = new LogoutSave(rocketplayer.CSteamID.m_SteamID, rocketplayer.GetTeam(), Kit.EClass.NONE, EBranch.DEFAULT, "");
                AddObjectToSave(newSave);
                OnlinePlayers.Add(newSave);
            }
            else
                OnlinePlayers.Add(currentSave);
        }
        private static void OnPlayerDisconnected(UnturnedPlayer player)
        {
            OnlinePlayers.RemoveAll(s => s.Steam64 == player.CSteamID.m_SteamID);
        }
        public static string GetKitName(ulong playerID) => HasPlayerData(playerID, out var data)? data.KitName : "";
        public static ulong GetTeam(ulong player) => HasPlayerData(player, out LogoutSave save) ? save.Team : 0;
    }
    public class LogoutSave
    {
        public ulong Steam64;
        [JsonSettable]
        public ulong Team;
        [JsonSettable]
        public Kit.EClass KitClass;
        [JsonSettable]
        public EBranch Branch;
        [JsonSettable]
        public string KitName;
        public LogoutSave(ulong steam64, ulong team, Kit.EClass kitClass, EBranch branch, string kitName)
        {
            this.Steam64 = steam64;
            this.Team = team;
            this.KitClass = kitClass;
            this.Branch = branch;
            this.KitName = kitName;
        }

        public string Icon
        {
            get
            {
                switch (KitClass)
                {
                    case Kit.EClass.NONE:
                        return "";
                    case Kit.EClass.UNARMED:
                        return "±";
                    case Kit.EClass.SQUADLEADER:
                        return "¦";
                    case Kit.EClass.RIFLEMAN:
                        return "¡";
                    case Kit.EClass.MEDIC:
                        return "¢";
                    case Kit.EClass.BREACHER:
                        return "¤";
                    case Kit.EClass.AUTOMATIC_RIFLEMAN:
                        return "¥";
                    case Kit.EClass.GRENADIER:
                        return "¬";
                    case Kit.EClass.MACHINE_GUNNER:
                        return "«";
                    case Kit.EClass.LAT:
                        return "®";
                    case Kit.EClass.HAT:
                        return "¯";
                    case Kit.EClass.MARKSMAN:
                        return "¨";
                    case Kit.EClass.SNIPER:
                        return "£";
                    case Kit.EClass.AP_RIFLEMAN:
                        return "©";
                    case Kit.EClass.COMBAT_ENGINEER:
                        return "ª";
                    case Kit.EClass.CREWMAN:
                        return "§";
                    case Kit.EClass.PILOT:
                        return "°";
                    default:
                        return "";
                }
            }
        }
    }
}
