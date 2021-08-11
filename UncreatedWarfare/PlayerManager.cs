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

            UCPlayer player = new UCPlayer(
                    rocketplayer.CSteamID,
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
        public static List<UCPlayer> GetNearbyPlayers(float range, Vector3 point) => OnlinePlayers.Where(p => !p.Player.life.isDead && (p.Position - point).sqrMagnitude < Math.Pow(range, 2)).ToList();
        public static bool IsPlayerNearby(ulong playerID, float range, Vector3 point) => OnlinePlayers.Find(p => p.Steam64 == playerID && !p.Player.life.isDead && (p.Position - point).sqrMagnitude < Math.Pow(range, 2)) != null;

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
        }

        internal static void PickGroupAfterJoin(UCPlayer ucplayer)
        {
            ulong oldGroup = ucplayer.Player.quests.groupID.m_SteamID;
            if (HasSave(ucplayer.Steam64, out PlayerSave save))
            {
                if (TeamManager.CanJoinTeam(save.Team) && ucplayer.Player.quests.groupID.m_SteamID != save.Team)
                {
                    ucplayer.Player.quests.ServerAssignToGroup(new CSteamID(TeamManager.GetGroupID(save.Team)), EPlayerGroupRank.MEMBER, true);
                } else
                {
                    ulong other = TeamManager.Other(save.Team);
                    if (TeamManager.CanJoinTeam(other) && ucplayer.Player.quests.groupID.m_SteamID != other)
                    {
                        ucplayer.Player.quests.ServerAssignToGroup(new CSteamID(TeamManager.GetGroupID(other)), EPlayerGroupRank.MEMBER, true);
                    }
                }
            }
            if (oldGroup != ucplayer.Player.quests.groupID.m_SteamID)
            {
                ulong team = ucplayer.Player.quests.groupID.m_SteamID.GetTeam();
                if (team != oldGroup.GetTeam())
                {
                    ucplayer.Player.teleportToLocation(F.GetBaseSpawn(ucplayer.Player), F.GetBaseAngle(team));
                }
            }
            GroupManager.save();
        }
    }
}
