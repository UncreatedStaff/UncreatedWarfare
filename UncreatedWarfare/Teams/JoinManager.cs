using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Kits;
using UnityEngine;

namespace Uncreated.Warfare.Teams
{
    public class JoinManager : MonoBehaviour, IDisposable
    {
        private List<LobbyPlayer> LobbyPlayers;
        private List<LobbyPlayer> Team1Players;
        private List<LobbyPlayer> Team2Players;
        private TimeSpan countdown;

        public void Initialize()
        {
            LobbyPlayers = new List<LobbyPlayer>();
            Team1Players = new List<LobbyPlayer>();
            Team2Players = new List<LobbyPlayer>();
            countdown = TimeSpan.FromTicks(0);

            EffectManager.onEffectButtonClicked += OnButtonClicked;
        }
        public void OnPlayerConnected(UCPlayer player, bool fromLobby)
        {
            if (fromLobby)
            {
                LobbyPlayer lobbyPlayer = new LobbyPlayer(player, 0, true);
                LobbyPlayers.Add(lobbyPlayer);
                ShowUI(lobbyPlayer, false);
            }
            else
            {
                var lobbyPlayer = new LobbyPlayer(player, player.GetTeam(), false);

                LobbyPlayers.Add(lobbyPlayer);
                if (lobbyPlayer.Team == 1)
                    Team1Players.Add(lobbyPlayer);
                else if (lobbyPlayer.Team == 2)
                    Team2Players.Add(lobbyPlayer);

                foreach (var p in LobbyPlayers)
                    UpdateUITeams(p, p.Team);
            }
        }

        public void OnPlayerDisconnected(UCPlayer player)
        {
            LobbyPlayers.RemoveAll(p => p.Player.Steam64 == player.Steam64);
            Team1Players.RemoveAll(p => p.Player.Steam64 == player.Steam64);
            Team2Players.RemoveAll(p => p.Player.Steam64 == player.Steam64);
        }
        public void ShowUI(UCPlayer player, bool showX)
        {
            var lobbyPlayer = LobbyPlayers.Find(p => p.Player == player);

            if (lobbyPlayer != null)
                ShowUI(player, showX);
        }

        public void ShowUI(LobbyPlayer player, bool showX)
        {
            EffectManager.sendUIEffect(29000, 2900, player.Player.connection, true);

            EffectManager.sendUIEffectText(29000, player.Player.connection, true, "Team1Name", TeamManager.Team1Name.ToUpper().Colorize(TeamManager.Team1ColorHex));
            EffectManager.sendUIEffectText(29000, player.Player.connection, true, "Team2Name", TeamManager.Team2Name.ToUpper().Colorize(TeamManager.Team2ColorHex));

            EffectManager.sendUIEffectText(29000, player.Player.connection, true, "Team1PlayerCount", Team1Players.Count.ToString());
            EffectManager.sendUIEffectText(29000, player.Player.connection, true, "Team2PlayerCount", Team2Players.Count.ToString());

            EffectManager.sendUIEffectText(29000, player.Player.connection, true, "Team1Select", "CLICK TO JOIN");
            EffectManager.sendUIEffectText(29000, player.Player.connection, true, "Team2Select", "CLICK TO JOIN");

            if (player.Player.GetTeam() == 1)
                EffectManager.sendEffectClicked("Team1Button");
            else if (player.Player.GetTeam() == 2)
                EffectManager.sendEffectClicked("Team2Button");

            if (!showX)
                EffectManager.sendUIEffectVisibility(29000, player.Player.connection, true, "X", false);

            if (countdown.Ticks <= 0)
            {
                EffectManager.sendUIEffectVisibility(29000, player.Player.connection, true, "Confirm", true);
            }
            else
            {
                EffectManager.sendUIEffectVisibility(29000, player.Player.connection, true, "GameStarting", true);
                EffectManager.sendUIEffectText(29000, player.Player.connection, true, "GameStartingSeconds", countdown.Minutes.ToString("D2") + ":" + countdown.Seconds.ToString("D2"));
            }

            for (int i = 0; i < Team1Players.Count; i++)
            {
                string name = Team1Players[i].Player.CharacterName;
                if (Team1Players[i].IsInLobby)
                    name.Colorize("9F9F9F");
                EffectManager.sendUIEffectText(29000, player.Player.connection, true, "T1P" + (i + 1), name);
            }

            for (int i = 0; i < Team2Players.Count; i++)
            {
                string name = Team2Players[i].Player.CharacterName;
                if (Team2Players[i].IsInLobby)
                    name.Colorize("9F9F9F");
                EffectManager.sendUIEffectText(29000, player.Player.connection, true, "T2P" + (i + 1), name);
            }
        }

        public void UpdateUITeams(LobbyPlayer player, ulong team)
        {
            EffectManager.sendUIEffectText(29000, player.Player.connection, true, "Team1PlayerCount", Team1Players.Count.ToString());
            EffectManager.sendUIEffectText(29000, player.Player.connection, true, "Team2PlayerCount", Team2Players.Count.ToString());

            if (IsTeamFull(1) && team != 1)
            {
                EffectManager.sendUIEffectText(29000, player.Player.connection, true, "Team1Select", "<color=#9c6b6b>FULL</color>");
                EffectManager.sendUIEffectText(29000, player.Player.connection, true, "Team2Select", "<color=#9c6b6b>CLICK TO JOIN</color>");
                EffectManager.sendUIEffectVisibility(29000, player.Player.connection, true, "Team1Button", false);
                EffectManager.sendUIEffectVisibility(29000, player.Player.connection, true, "Team2Button", true);
            }
            else if (IsTeamFull(2) && team != 2)
            {
                EffectManager.sendUIEffectText(29000, player.Player.connection, true, "Team2Select", "<color=#9c6b6b>FULL</color>");
                EffectManager.sendUIEffectText(29000, player.Player.connection, true, "Team1Select", "<color=#9c6b6b>CLICK TO JOIN</color>");
                EffectManager.sendUIEffectVisibility(29000, player.Player.connection, true, "Team2Button", false);
                EffectManager.sendUIEffectVisibility(29000, player.Player.connection, true, "Team1Button", true);
            }

            for (int i = 0; i < Team1Players.Count; i++)
            {
                string name = Team1Players[i].Player.CharacterName;
                if (Team1Players[i].IsInLobby)
                    name.Colorize("9F9F9F");
                EffectManager.sendUIEffectText(29000, player.Player.connection, true, "T1P" + (i + 1), name);
            }

            for (int i = 0; i < Team2Players.Count; i++)
            {
                string name = Team2Players[i].Player.CharacterName;
                if (Team2Players[i].IsInLobby)
                    name.Colorize("9F9F9F");
                EffectManager.sendUIEffectText(29000, player.Player.connection, true, "T2P" + (i + 1), name);
            }
        }

        public void UpdateUICountDown(LobbyPlayer player)
        {
            if (countdown.Ticks > 0)
            {
                EffectManager.sendUIEffectText(29000, player.Player.connection, true, "GameStartingSeconds", countdown.Minutes.ToString("D2") + ":" + countdown.Seconds.ToString("D2"));
            }
            else
            {
                EffectManager.sendUIEffectVisibility(29000, player.Player.connection, true, "GameStarting", false);
                EffectManager.sendUIEffectVisibility(29000, player.Player.connection, true, "Confirm", true);

            }
        }

        public void OnButtonClicked(Player nelsonplayer, string buttonName)
        {
            var lobbyPlayer = LobbyPlayers.Find(p => p.Player.CSteamID == nelsonplayer.channel.owner.playerID.steamID);

            if (buttonName == "Team1Button")
            {
                if (lobbyPlayer.Team != 1)
                {
                    lobbyPlayer.Team = 1;
                    Team1Players.Add(lobbyPlayer);
                    Team2Players.Remove(lobbyPlayer);
                    foreach (var p in LobbyPlayers)
                        UpdateUITeams(p, p.Team);

                    EffectManager.sendUIEffectText(29000, lobbyPlayer.Player.connection, true, "Team1Select", "<color=#9c6b6b>JOINED</color>");
                }
            }
            else if (buttonName == "Team2Button")
            {
                if (lobbyPlayer.Team != 2)
                {
                    lobbyPlayer.Team = 2;
                    Team2Players.Add(lobbyPlayer);
                    Team1Players.Remove(lobbyPlayer);
                    foreach (var p in LobbyPlayers)
                        UpdateUITeams(p, p.Team);

                    EffectManager.sendUIEffectText(29000, lobbyPlayer.Player.connection, true, "Team2Select", "<color=#9c6b6b>JOINED</color>");
                }
            }

            if (buttonName == "Confirm")
            {
                if (lobbyPlayer.Team != 0)
                {
                    StartCoroutine(ConfirmJoin(lobbyPlayer));
                }
            }
            else if (buttonName == "X")
            {
                CloseUI(lobbyPlayer);
            }
        }

        private void JoinTeam(UCPlayer player, ulong newTeam)
        {
            string teamName = TeamManager.TranslateName(newTeam, player.CSteamID);

            GroupInfo group = GroupManager.getGroupInfo(new CSteamID(newTeam));
            if (group == null)
            {
                player.SendChat("join_e_groupnoexist", TeamManager.TranslateName(newTeam, player.CSteamID, true));
                return;
            }
            Kits.UCInventoryManager.ClearInventory(player);
            if (!group.hasSpaceForMoreMembersInGroup)
            {
                player.SendChat("join_e_teamfull", teamName);
                return;
            }
            ulong oldgroup = player.GetTeam();
            player.Player.quests.ServerAssignToGroup(group.groupID, EPlayerGroupRank.MEMBER, true);
            GroupManager.save();

            EventFunctions.OnGroupChangedInvoke(player.Player.channel.owner, oldgroup, newTeam);

            Players.FPlayerName names = F.GetPlayerOriginalNames(player.Player);
            F.Log(F.Translate("join_player_joined_console", 0, out _,
                names.PlayerName, player.Steam64.ToString(), newTeam.ToString(Data.Locale), oldgroup.ToString(Data.Locale)),
                ConsoleColor.Cyan);

            player.Player.teleportToLocation(newTeam.GetBaseSpawnFromTeam(), newTeam.GetBaseAngle());

            if (KitManager.KitExists(TeamManager.GetUnarmedFromS64ID(player.Steam64), out var kit))
                KitManager.GiveKit(player, kit);

            player.SendChat("join_s", TeamManager.TranslateName(newTeam, player.CSteamID, true));

            new List<CSteamID>(1) { player.CSteamID }.BroadcastToAllExcept("join_announce", names.CharacterName, teamName);

            if (player.Squad != null)
                Squads.SquadManager.LeaveSquad(player, player.Squad);
            PlayerManager.ApplyToOnline();
        }

        public void CloseUI(LobbyPlayer player)
        {
            EffectManager.askEffectClearByID(29000, player.Player.connection);
        }

        public bool IsTeamFull(ulong team)
        {
            if (UCWarfare.Config.TeamSettings.BalanceTeams)
            {
                int Team1Count = Team1Players.Count;
                int Team2Count = Team2Players.Count;
                if (Team1Count == Team2Count) return true;
                if (team == 1)
                {
                    if (Team2Count > Team1Count) return true;
                    if ((float)(Team1Count - Team2Count) / (Team1Count + Team2Count) >= UCWarfare.Config.TeamSettings.AllowedDifferencePercent) return false;
                }
                else if (team == 2)
                {
                    if (Team1Count > Team2Count) return true;
                    if ((float)(Team2Count - Team1Count) / (Team1Count + Team2Count) >= UCWarfare.Config.TeamSettings.AllowedDifferencePercent) return false;
                }
            }
            return true;
        }

        public void StartNewGameCountdown(LobbyPlayer player)
        {
            StartCoroutine(CountdownTick());
        }

        IEnumerator<WaitForSeconds> CountdownTick()
        {
            for (int seconds = 30; seconds >= 0; seconds--)
            {
                countdown = TimeSpan.FromSeconds(seconds);

                foreach (var p in LobbyPlayers)
                    UpdateUICountDown(p);

                yield return new WaitForSeconds(1);
            }
        }

        IEnumerator<WaitForSeconds> ConfirmJoin(LobbyPlayer player)
        {
            EffectManager.sendUIEffectText(29000, player.Player.connection, true, "Confirm", "<color=#9c6b6b>JOINING...</color>");
            yield return new WaitForSeconds(2);
            JoinTeam(player.Player, player.Team);
            LobbyPlayers.Remove(player);
            player.IsInLobby = false;
            CloseUI(player);
        }

        public void Dispose()
        {
            EffectManager.onEffectButtonClicked -= OnButtonClicked;
        }

        public class LobbyPlayer
        {
            public readonly UCPlayer Player;
            public ulong Team;
            public bool IsInLobby;

            public LobbyPlayer(UCPlayer player, ulong team, bool isInLobby)
            {
                Player = player;
                Team = team;
                IsInLobby = isInLobby;
            }
        }
    }
}
