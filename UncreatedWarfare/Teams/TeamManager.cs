﻿using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncreatedWarfare.Flags;

namespace UncreatedWarfare.Teams
{
    public class TeamManager
    {
        public List<Team> Teams;
        List<TeamData> Data;
        public Team T1 { get => Teams.Count > 0 ? Teams[0] : null; }
        public Team T2 { get => Teams.Count > 1 ? Teams[1] : null; }
        public Zone LobbyZone;
        public Zone T1Zone;
        public Zone T2Zone;
        public Zone T1AMCZone;
        public Zone T2AMCZone;
        public TeamManager()
        {
            Data = JSONMethods.ReadTeams();
            Teams = new List<Team>();
            foreach(TeamData data in Data)
            {
                Team team = new Team(data);
                Teams.Add(team);
            }
        }
        public void AddTeam(TeamData data) => JSONMethods.AddTeam(data);
        public bool RenameTeam(ulong teamID, string newName, out string oldName) => JSONMethods.RenameTeam(teamID, newName, out oldName);
        public bool DeleteTeam(ulong teamID, out TeamData deletedTeam) => JSONMethods.DeleteTeam(teamID, out deletedTeam);
        public bool AddPlayerToTeam(ulong teamID, ulong playerID) => JSONMethods.AddPlayerToTeam(teamID, playerID);
        public bool RemovePlayerFromTeam(ulong teamID, ulong playerID) => JSONMethods.RemovePlayerFromTeam(teamID, playerID);
        public void PlayerJoinProcess(SteamPlayer player)
        {
            Team team = Teams.FirstOrDefault(t => t.OfflinePlayers.Contains(player.playerID.steamID.m_SteamID));
            if(team != null)
            {
                team.PlayerGoOnline(player);
                CommandWindow.LogWarning(player.playerID.playerName + " joined team " + team.LocalizedName);
            }
        }
        public void PlayerLeaveProcess(SteamPlayer player)
        {
            Team team = Teams.FirstOrDefault(t => t.OnlinePlayers.Contains(player));
            if (team != null)
            {
                team.PlayerGoOffline(player);
                CommandWindow.LogWarning(player.playerID.playerName + " left team " + team.LocalizedName);
            }
        }
        public bool CanAddToTeam(ulong Team)
        {
            if (UCWarfare.Config.TeamSettings.BalanceTeams)
            {
                if (Team == 1)
                {
                    if (T1.OnlinePlayers.Count <= T2.OnlinePlayers.Count) return true;
                    List<SteamPlayer> OnlinePlayers = Provider.clients;
                    int MaxDifference = (int)Math.Round(UCWarfare.Config.TeamSettings.AllowedDifferencePercent / 100f * OnlinePlayers.Count);
                    if (MaxDifference < 1) MaxDifference = 1;
                    if (T1.OnlinePlayers.Count - MaxDifference > T2.OnlinePlayers.Count) return false;
                    else return true;
                }
                else if (Team == 2)
                {
                    if (T2.OnlinePlayers.Count <= T1.OnlinePlayers.Count) return true;
                    List<SteamPlayer> OnlinePlayers = Provider.clients;
                    int MaxDifference = (int)Math.Round(UCWarfare.Config.TeamSettings.AllowedDifferencePercent / 100f * OnlinePlayers.Count);
                    if (MaxDifference < 1) MaxDifference = 1;
                    if (T2.OnlinePlayers.Count - MaxDifference > T1.OnlinePlayers.Count) return false;
                    else return true;
                }
                else return true;
            }
            else return true;
        }
        public bool CanAddToTeam1() => CanAddToTeam(1);
        public bool CanAddToTeam2() => CanAddToTeam(2);
    }
}
