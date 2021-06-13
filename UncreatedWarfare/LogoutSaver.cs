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
        public LogoutSaver() : base(Data.KitsStorage + "playersaves.json") { }
        protected override string LoadDefaults() => "[]";
        private static LogoutSave AddSave(LogoutSave newSave) => 
            AddObjectToSave(newSave);
        private static void RemoveSave(CSteamID playerID) => RemoveSave(playerID.m_SteamID);
        private static void RemoveSave(ulong playerID) => RemoveWhere(ks => ks.Steam64 == playerID);
        public static bool HasSave(CSteamID playerID, out LogoutSave save) => HasSave(playerID.m_SteamID, out save);
        public static bool HasSave(ulong playerID, out LogoutSave save) => ObjectExists(ks => ks.Steam64 == playerID, out save);
        public static LogoutSave GetSave(CSteamID playerID) => GetObject(s => s.Steam64 == playerID.m_SteamID);
        public static void UpdateSave(Func<LogoutSave, bool> selector, Action<LogoutSave> operation) => UpdateObjectsWhere(selector, operation);
        public static void InvokePlayerConnected(UnturnedPlayer player) => OnPlayerConnected(player);
        public static void InvokePlayerDisconnected(UnturnedPlayer player) => OnPlayerDisconnected(player);
        private static void OnPlayerConnected(UnturnedPlayer rocketplayer)
        {
            if (!HasSave(rocketplayer.CSteamID, out var currentSave))
                AddObjectToSave(new LogoutSave(rocketplayer.CSteamID.m_SteamID, rocketplayer.GetTeam(), Kit.EClass.NONE, EBranch.DEFAULT, ""));
            else
                AddObjectToSave(currentSave, false);
        }
        private static void OnPlayerDisconnected(UnturnedPlayer player)
        {
            RemoveWhere(s => s.Steam64 == player.CSteamID.m_SteamID, false);
        }
        public static string GetKit(ulong player) => 
            HasSave(player, out LogoutSave save) ? save.KitName ?? TeamManager.GetUnarmedFromS64ID(player) : 
            TeamManager.GetUnarmedFromS64ID(player);
        public static ulong GetTeam(ulong player) => HasSave(player, out LogoutSave save) ? save.Team : 0;
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
