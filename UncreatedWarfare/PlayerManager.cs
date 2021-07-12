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
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare
{
    public class PlayerManager : JSONSaver<PlayerSave>
    {
        public static List<UCPlayer> OnlinePlayers;
        public static List<UCPlayer> Team1Players;
        public static List<UCPlayer> Team2Players;

        public PlayerManager() : base(Data.KitsStorage + "playersaves.json")
        {
            OnlinePlayers = new List<UCPlayer>();
            Team1Players = new List<UCPlayer>();
            Team2Players = new List<UCPlayer>();
        }
        protected override string LoadDefaults() => "[]";
        public static bool HasSave(ulong playerID, out PlayerSave save) => ObjectExists(ks => ks.Steam64 == playerID, out save, true);
        public static PlayerSave GetSave(ulong playerID) => GetObject(ks => ks.Steam64 == playerID, true);
        public static new void Save()
        {
            for (int i = 0; i < OnlinePlayers.Count; i++)
            {
                UpdateObjectsWhere(p => p.Steam64 == OnlinePlayers[i].Steam64, p =>
                {
                    p.Team = OnlinePlayers[i].GetTeam();
                    p.KitClass = OnlinePlayers[i].KitClass;
                    p.Branch = OnlinePlayers[i].Branch;
                    p.KitName = OnlinePlayers[i].KitName;
                    p.SquadName = OnlinePlayers[i].Squad != null ? OnlinePlayers[i].Squad.Name : "";
                });
            }
        }
        public static void InvokePlayerConnected(UnturnedPlayer player) => OnPlayerConnected(player);
        public static void InvokePlayerDisconnected(UnturnedPlayer player) => OnPlayerDisconnected(player);
        private static void OnPlayerConnected(UnturnedPlayer rocketplayer)
        {
            PlayerSave save;

            if (!HasSave(rocketplayer.CSteamID.m_SteamID, out var existingSave))
            {
                save = new PlayerSave(rocketplayer.CSteamID.m_SteamID);
                AddObjectToSave(save);
            }
            else
            {
                save = existingSave;
            }

            var player = new UCPlayer(
                    rocketplayer.CSteamID,
                    save.KitClass,
                    save.Branch,
                    save.KitName,
                    rocketplayer.Player,
                    rocketplayer.Player.channel.owner.playerID.characterName,
                    rocketplayer.Player.channel.owner.playerID.nickName
                );

            OnlinePlayers.Add(player);
            if (player.IsTeam1())
                Team1Players.Add(player);
            else if (player.IsTeam2())
                Team2Players.Add(player);

            SquadManager.InvokePlayerJoined(player, save.SquadName);
            FOBManager.UpdateUI(player);
        }
        private static void OnPlayerDisconnected(UnturnedPlayer rocketplayer)
        {
            UCPlayer player = UCPlayer.FromUnturnedPlayer(rocketplayer);
            player.IsOnline = false;

            OnlinePlayers.RemoveAll(s => s == default || s.Steam64 == rocketplayer.CSteamID.m_SteamID);

            if (TeamManager.IsTeam1(rocketplayer))
                Team1Players.RemoveAll(s => s == default || s.Steam64 == rocketplayer.CSteamID.m_SteamID);
            else if (TeamManager.IsTeam2(rocketplayer))
                Team2Players.RemoveAll(s => s == default || s.Steam64 == rocketplayer.CSteamID.m_SteamID);

            SquadManager.InvokePlayerLeft(player);
        }
        public static string GetKitName(ulong playerID) => ObjectExists(p => p.Steam64 == playerID, out var data)? data.KitName : "";

        public static void VerifyTeam(Player nelsonplayer)
        {
            if (nelsonplayer == default) return;

            UCPlayer player = OnlinePlayers.Find(p => p.Steam64 == nelsonplayer.channel.owner.playerID.steamID.m_SteamID);
            if (player == default)
            {
                F.LogError("Failed to get UCPlayer instance of " + nelsonplayer.name);
                return;
            }

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
