using Newtonsoft.Json;
using Rocket.API;
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
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare
{
    public class PlayerManager : JSONSaver<UCPlayer>
    {
        public static List<UCPlayer> OnlinePlayers;
        public static List<UCPlayer> Team1Players;
        public static List<UCPlayer> Team2Players;

        public PlayerManager() : base(Data.KitsStorage + "playersaves.json")
        {
            OnlinePlayers = new List<UCPlayer>();
            Team1Players = new List<UCPlayer>();
            Team2Players = new List<UCPlayer>();
            foreach (SteamPlayer steamplayer in Provider.clients)
                OnlinePlayers.Add(GetSave(steamplayer.playerID.steamID));
        }
        protected override string LoadDefaults() => "[]";
        public static void SaveData() => OverwriteSavedList(OnlinePlayers);
        private static void RemoveSave(CSteamID playerID) => RemoveSave(playerID.m_SteamID);
        private static void RemoveSave(ulong playerID) => RemoveWhere(ks => ks.Steam64 == playerID);
        public static bool HasSave(CSteamID playerID, out UCPlayer save) => HasSave(playerID.m_SteamID, out save);
        public static bool HasSave(ulong playerID, out UCPlayer save) => ObjectExists(ks => ks.Steam64 == playerID, out save);
        public static UCPlayer GetSave(CSteamID playerID) => GetObject(s => s.Steam64 == playerID.m_SteamID);
        public static UCPlayer GetSave(ulong playerID) => GetObject(s => s.Steam64 == playerID);
        public static UCPlayer GetPlayer(CSteamID playerID) => GetPlayer(playerID.m_SteamID);
        public static UCPlayer GetPlayer(ulong playerID) => OnlinePlayers.Find(p => p.Steam64 == playerID);
        public static bool PlayerExists(CSteamID playerID, out UCPlayer data)
        {
            data = GetPlayer(playerID);
            return data != null;
        }
        public static bool PlayerExists(ulong playerID, out UCPlayer data)
        {
            data = GetPlayer(playerID);
            return data != null;
        }
        public static void UpdatePlayer(Func<UCPlayer, bool> selector, Action<UCPlayer> operation)
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
                var newSave = new UCPlayer(rocketplayer.CSteamID, rocketplayer.GetTeam(), Kit.EClass.NONE, EBranch.DEFAULT, "", rocketplayer.Player, rocketplayer.CharacterName, rocketplayer.DisplayName);
                AddObjectToSave(newSave);
                OnlinePlayers.Add(newSave);
                if (TeamManager.IsTeam1(rocketplayer))
                    Team1Players.Add(currentSave);
                else if (TeamManager.IsTeam2(rocketplayer))
                    Team2Players.Add(currentSave);
            }
            else
            {
                currentSave.Player = rocketplayer.Player;
                currentSave.CSteamID = rocketplayer.CSteamID;
                currentSave.CharacterName = rocketplayer.CharacterName;
                currentSave.NickName = rocketplayer.DisplayName;

                OnlinePlayers.Add(currentSave);
                if (TeamManager.IsTeam1(rocketplayer))
                    Team1Players.Add(currentSave);
                else if (TeamManager.IsTeam2(rocketplayer))
                    Team2Players.Add(currentSave);
            }
        }
        private static void OnPlayerDisconnected(UnturnedPlayer rocketplayer)
        {
            OnlinePlayers.RemoveAll(s => s == default || s.Steam64 == rocketplayer.CSteamID.m_SteamID);

            if (TeamManager.IsTeam1(rocketplayer))
                Team1Players.RemoveAll(s => s == default || s.Steam64 == rocketplayer.CSteamID.m_SteamID);
            else if (TeamManager.IsTeam2(rocketplayer))
                Team2Players.RemoveAll(s => s == default || s.Steam64 == rocketplayer.CSteamID.m_SteamID);
        }
        public static string GetKitName(ulong playerID) => PlayerExists(playerID, out var data)? data.KitName : "";

        public static void VerifyTeam(Player nelsonplayer)
        {
            if (nelsonplayer == default) return;

            UCPlayer player = OnlinePlayers.Find(p => p.Steam64 == nelsonplayer.channel.owner.playerID.steamID.m_SteamID);
            if (player == default)
            {
                F.LogError("Failed to get UCPlayer instance of " + nelsonplayer.name);
                return;
            }
            player.Team = nelsonplayer.GetTeam();

            if (TeamManager.IsTeam1(nelsonplayer))
            {
                Team2Players.RemoveAll(p => p == default || p.Steam64 == nelsonplayer.channel.owner.playerID.steamID.m_SteamID);
                if (!Team1Players.Exists(p => p == default || p.Steam64 == nelsonplayer.channel.owner.playerID.steamID.m_SteamID))
                {
                    Team1Players.Add(player);
                }
            }
            else if (TeamManager.IsTeam2(nelsonplayer))
            {
                Team1Players.RemoveAll(p => p == default || p.Steam64 == nelsonplayer.channel.owner.playerID.steamID.m_SteamID);
                if (!Team2Players.Exists(p => p == default || p.Steam64 == nelsonplayer.channel.owner.playerID.steamID.m_SteamID))
                {
                    Team2Players.Add(player);
                }
            }

            if (UCWarfare.Config.Debug)
            {
                F.Log("Team 1 Count: " + Team1Players.Count);
                F.Log("Team 2 Count: " + Team2Players.Count);
            }
        }
    }
}
