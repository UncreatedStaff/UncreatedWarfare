using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Players;
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

        private void Start()
        {
            LobbyPlayers = new List<LobbyPlayer>();
            Team1Players = new List<LobbyPlayer>();
            Team2Players = new List<LobbyPlayer>();
            countdown = TimeSpan.FromTicks(0);

            EffectManager.onEffectButtonClicked += OnButtonClicked;
        }
        public bool IsInLobby(UCPlayer player)
        {
            foreach (LobbyPlayer lobbyPlayer in LobbyPlayers)
            {
                if (lobbyPlayer.IsInLobby && lobbyPlayer.Player == player)
                {
                    return true;
                }
            }
            return false;
        }
        public void OnPlayerConnected(UCPlayer player, bool isNewPlayer, bool isNewGame)
        {
            bool isDonatorT1 = KitManager.GetKitsWhere(k => (k.IsPremium || k.IsLoadout) && k.AllowedUsers.Contains(player.Steam64) && k.Team == 1).Count() > 0;
            bool isDonatorT2 = KitManager.GetKitsWhere(k => (k.IsPremium || k.IsLoadout) && k.AllowedUsers.Contains(player.Steam64) && k.Team == 2).Count() > 0;

            if (isNewPlayer)
            {
                player.Player.teleportToLocationUnsafe(TeamManager.LobbySpawn, TeamManager.LobbySpawnAngle);
                LobbyPlayer lobbyPlayer = new LobbyPlayer(player, 0);
                lobbyPlayer.IsInLobby = true;
                lobbyPlayer.IsDonatorT1 = isDonatorT1;
                lobbyPlayer.IsDonatorT2 = isDonatorT2;
                LobbyPlayers.Add(lobbyPlayer);
                ShowUI(lobbyPlayer, false);
            }
            else if (isNewGame)
            {
                LobbyPlayer lobbyPlayer = new LobbyPlayer(player, player.GetTeam());
                lobbyPlayer.IsDonatorT1 = isDonatorT1;
                lobbyPlayer.IsDonatorT2 = isDonatorT2;

                if (!(player.GetTeam() == 1 || player.GetTeam() == 2) || IsTeamFull(lobbyPlayer, lobbyPlayer.Team))
                {
                    player.Player.teleportToLocationUnsafe(TeamManager.LobbySpawn, TeamManager.LobbySpawnAngle);
                    lobbyPlayer.Team = 0;
                    lobbyPlayer.IsInLobby = true;
                    LobbyPlayers.Add(lobbyPlayer);
                    ShowUI(lobbyPlayer, false);
                }
                else
                {
                    player.Player.teleportToLocationUnsafe(player.Player.GetBaseSpawn(out ulong team), F.GetBaseAngle(team));

                    LobbyPlayers.Add(lobbyPlayer);
                    if (lobbyPlayer.Team == 1)
                        Team1Players.Add(lobbyPlayer);
                    else if (lobbyPlayer.Team == 2)
                        Team2Players.Add(lobbyPlayer);
                    ToastMessage.QueueMessage(player, "", Data.Gamemode.DisplayName, ToastMessageSeverity.BIG);
                }
            }
            else if (player.GetTeam() == 0)
            {
                LobbyPlayer lobbyPlayer = new LobbyPlayer(player, 0);
                lobbyPlayer.IsInLobby = true;
                lobbyPlayer.IsDonatorT1 = isDonatorT1;
                lobbyPlayer.IsDonatorT2 = isDonatorT2;
                player.Player.teleportToLocationUnsafe(TeamManager.LobbySpawn, TeamManager.LobbySpawnAngle);
                LobbyPlayers.Add(lobbyPlayer);
                ShowUI(lobbyPlayer, false);
            }

            foreach (LobbyPlayer p in LobbyPlayers)
                UpdateUITeams(p, p.Team);
        }

        public void OnPlayerDisconnected(UCPlayer player)
        {
            LobbyPlayers.RemoveAll(p => p.Player.Steam64 == player.Steam64);
            Team1Players.RemoveAll(p => p.Player.Steam64 == player.Steam64);
            Team2Players.RemoveAll(p => p.Player.Steam64 == player.Steam64);

            foreach (LobbyPlayer p in LobbyPlayers)
                UpdateUITeams(p, p.Team);
        }
        public void JoinLobby(UCPlayer player, bool showX)
        {
            LobbyPlayer lobbyPlayer = LobbyPlayers.Find(p => p.Player == player);
            lobbyPlayer.IsInLobby = true;
            ShowUI(lobbyPlayer, showX);

            foreach (LobbyPlayer p in LobbyPlayers)
                UpdateUITeams(p, p.Team);
        }

        public void ShowUI(LobbyPlayer player, bool showX)
        {
            player.Player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.None);
            player.Player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Modal);

            EffectManager.sendUIEffect(36036, 29000, player.Player.connection, true);

            EffectManager.sendUIEffectText(29000, player.Player.connection, true, "Team1Name", TeamManager.Team1Name.ToUpper().Colorize(TeamManager.Team1ColorHex));
            EffectManager.sendUIEffectText(29000, player.Player.connection, true, "Team2Name", TeamManager.Team2Name.ToUpper().Colorize(TeamManager.Team2ColorHex));

            EffectManager.sendUIEffectText(29000, player.Player.connection, true, "Team1PlayerCount", Team1Players.Count.ToString());
            EffectManager.sendUIEffectText(29000, player.Player.connection, true, "Team2PlayerCount", Team2Players.Count.ToString());

            if (showX && player.Player.GetTeam() == 1)
            {
                EffectManager.sendUIEffectText(29000, player.Player.connection, true, "Team1Select", "<color=A4A4A4>JOINED</color>");
                EffectManager.sendUIEffectVisibility(29000, player.Player.connection, true, "Team1Highlight", true);
                EffectManager.sendUIEffectVisibility(29000, player.Player.connection, true, "Team1Button", false);
            }
            else if (showX && player.Player.GetTeam() == 2)
            {
                EffectManager.sendUIEffectText(29000, player.Player.connection, true, "Team2Select", "<color=A4A4A4>JOINED</color>");
                EffectManager.sendUIEffectVisibility(29000, player.Player.connection, true, "Team2Highlight", true);
                EffectManager.sendUIEffectVisibility(29000, player.Player.connection, true, "Team2Button", false);
            }

            if (!showX)
                EffectManager.sendUIEffectVisibility(29000, player.Player.connection, true, "X", false);

            if (countdown.Ticks == 0)
            {
                EffectManager.sendUIEffectVisibility(29000, player.Player.connection, true, "Confirm", true);
                EffectManager.sendUIEffectVisibility(29000, player.Player.connection, true, "GameStarting", false);
            }
            else
            {
                EffectManager.sendUIEffectVisibility(29000, player.Player.connection, true, "Confirm", false);
                EffectManager.sendUIEffectVisibility(29000, player.Player.connection, true, "GameStarting", true);
                EffectManager.sendUIEffectText(29000, player.Player.connection, true, "GameStartingSeconds", countdown.Minutes.ToString("D2") + ":" + countdown.Seconds.ToString("D2"));
            }

            for (int i = 0; i < Team1Players.Count; i++)
            {
                string name = Team1Players[i].Player.CharacterName;
                if (Team1Players[i].IsInLobby)
                    name = name.Colorize("9F9F9F");
                EffectManager.sendUIEffectText(29000, player.Player.connection, true, "T1P" + (i + 1), name);
            }

            for (int i = 0; i < Team2Players.Count; i++)
            {
                string name = Team2Players[i].Player.CharacterName;
                if (Team2Players[i].IsInLobby)
                    name = name.Colorize("9F9F9F");
                EffectManager.sendUIEffectText(29000, player.Player.connection, true, "T2P" + (i + 1), name);
            }
        }

        public void UpdateUITeams(LobbyPlayer player, ulong team)
        {
            if (!player.IsInLobby) return;

            //F.Log($"UI teams updated: T1: {Team1Players.Count} - T2: {Team2Players.Count}");

            EffectManager.sendUIEffectText(29000, player.Player.connection, true, "Team1PlayerCount", Team1Players.Count.ToString());
            EffectManager.sendUIEffectText(29000, player.Player.connection, true, "Team2PlayerCount", Team2Players.Count.ToString());

            if (!player.Player.OnDuty() && !player.IsDonatorT1 && IsTeamFull(player, 1))
            {
                EffectManager.sendUIEffectText(29000, player.Player.connection, true, "Team1Select", "<color=#bf6363>FULL</color>");
                EffectManager.sendUIEffectText(29000, player.Player.connection, true, "Team2Select", "CLICK TO JOIN");
                EffectManager.sendUIEffectVisibility(29000, player.Player.connection, true, "Team1Button", false);
                EffectManager.sendUIEffectVisibility(29000, player.Player.connection, true, "Team2Button", true);
            }
            else if (!player.Player.OnDuty() && !player.IsDonatorT2 && IsTeamFull(player, 2))
            {
                EffectManager.sendUIEffectText(29000, player.Player.connection, true, "Team2Select", "<color=#bf6363>FULL</color>");
                EffectManager.sendUIEffectText(29000, player.Player.connection, true, "Team1Select", "CLICK TO JOIN");
                EffectManager.sendUIEffectVisibility(29000, player.Player.connection, true, "Team2Button", false);
                EffectManager.sendUIEffectVisibility(29000, player.Player.connection, true, "Team1Button", true);
            }
            else
            {
                EffectManager.sendUIEffectText(29000, player.Player.connection, true, "Team1Select", "CLICK TO JOIN");
                EffectManager.sendUIEffectText(29000, player.Player.connection, true, "Team2Select", "CLICK TO JOIN");
                EffectManager.sendUIEffectVisibility(29000, player.Player.connection, true, "Team1Button", true);
                EffectManager.sendUIEffectVisibility(29000, player.Player.connection, true, "Team2Button", true);
            }

            for (int i = 0; i < 32; i++)
            {
                EffectManager.sendUIEffectText(29000, player.Player.connection, true, "T1P" + (i + 1), "");
                EffectManager.sendUIEffectText(29000, player.Player.connection, true, "T2P" + (i + 1), "");
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
            if (!player.IsInLobby) return;

            if (countdown.Seconds > 0)
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
                    if (!Team1Players.Contains(lobbyPlayer))
                        Team1Players.Add(lobbyPlayer);
                    Team2Players.Remove(lobbyPlayer);
                    foreach (LobbyPlayer p in LobbyPlayers)
                        UpdateUITeams(p, p.Team);

                    EffectManager.sendUIEffectText(29000, lobbyPlayer.Player.connection, true, "Team1Select", "JOINED");
                }
            }
            else if (buttonName == "Team2Button")
            {
                if (lobbyPlayer.Team != 2)
                {
                    lobbyPlayer.Team = 2;
                    if (!Team2Players.Contains(lobbyPlayer))
                        Team2Players.Add(lobbyPlayer);
                    Team1Players.Remove(lobbyPlayer);
                    foreach (var p in LobbyPlayers)
                        UpdateUITeams(p, p.Team);

                    EffectManager.sendUIEffectText(29000, lobbyPlayer.Player.connection, true, "Team2Select", "JOINED");
                }
            }
            else if (buttonName == "Confirm")
            {
                if (lobbyPlayer.Team != 0)
                {
                    if (lobbyPlayer.Team != lobbyPlayer.Player.GetTeam())
                    {
                        StartCoroutine(ConfirmJoin(lobbyPlayer));
                    }
                    else
                    {
                        CloseUI(lobbyPlayer);
                    }
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
            UCInventoryManager.ClearInventory(player);
            if (!group.hasSpaceForMoreMembersInGroup)
            {
                player.SendChat("join_e_teamfull", teamName);
                return;
            }
            ulong oldgroup = player.GetTeam();
            player.Player.quests.ServerAssignToGroup(group.groupID, EPlayerGroupRank.MEMBER, true);
            GroupManager.save();

            EventFunctions.OnGroupChangedInvoke(player.Player.channel.owner, oldgroup, newTeam);

            FPlayerName names = F.GetPlayerOriginalNames(player.Player);
            F.Log(F.Translate("join_player_joined_console", 0, out _,
                names.PlayerName, player.Steam64.ToString(), newTeam.ToString(Data.Locale), oldgroup.ToString(Data.Locale)),
                ConsoleColor.Cyan);

            player.Player.teleportToLocation(newTeam.GetBaseSpawnFromTeam(), newTeam.GetBaseAngle());

            if (KitManager.KitExists(TeamManager.GetUnarmedFromS64ID(player.Steam64), out Kit kit))
                KitManager.GiveKit(player, kit);

            player.SendChat("teams_join_success", TeamManager.TranslateName(newTeam, player.CSteamID, true));

            new List<CSteamID>(1) { player.CSteamID }.BroadcastToAllExcept("teams_join_announce", names.CharacterName, teamName);

            if (player.Squad != null)
                Squads.SquadManager.LeaveSquad(player, player.Squad);
            PlayerManager.ApplyToOnline();

            CooldownManager.StartCooldown(player, ECooldownType.CHANGE_TEAMS, TeamManager.TeamSwitchCooldown);
            ToastMessage.QueueMessage(player, "", Data.Gamemode.DisplayName, ToastMessageSeverity.BIG);
        }

        public void CloseUI(LobbyPlayer player)
        {
            player.IsInLobby = false;
            player.Team = player.Player.GetTeam();
            player.Player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.None);
            player.Player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.Modal);
            EffectManager.askEffectClearByID(36036, player.Player.connection);

            foreach (LobbyPlayer p in LobbyPlayers)
                UpdateUITeams(p, p.Team);
        }

        public bool IsTeamFull(LobbyPlayer player, ulong team)
        {
            if (!UCWarfare.Config.TeamSettings.BalanceTeams)
                return false;
            if (player.Team == team)
                return false;

            int Team1Count = Team1Players.Count;
            int Team2Count = Team2Players.Count;

            if (Team1Count == 0 || Team2Count == 0)
                return false;

            if (team == 1)
            {
                if (player.Team == 2) // if player is on the opposing team
                {
                    return (float)(Team1Count + 1) / (Team2Count - 1) - 1 >= UCWarfare.Config.TeamSettings.AllowedDifferencePercent;
                }
                else if (player.Team == 1) // if player is already on the specified team
                {
                    return (float)(Team1Count) / Team2Count - 1 >= UCWarfare.Config.TeamSettings.AllowedDifferencePercent;
                }
                else // if player has not joined a team yet
                {
                    return (float)(Team1Count + 1) / Team2Count - 1 >= UCWarfare.Config.TeamSettings.AllowedDifferencePercent;
                }
            }
            else if (team == 2)
            {
                if (player.Team == 1) // if player is on the opposing team
                {
                    return (float)(Team2Count + 1) / (Team1Count - 1) - 1 >= UCWarfare.Config.TeamSettings.AllowedDifferencePercent;
                }
                else if (player.Team == 2) // if player is already on the specified team
                {
                    return (float)(Team2Count) / Team1Count - 1 >= UCWarfare.Config.TeamSettings.AllowedDifferencePercent;
                }
                else // if player has not joined a team yet
                {
                    return (float)(Team2Count + 1) / Team1Count - 1 >= UCWarfare.Config.TeamSettings.AllowedDifferencePercent;
                }
            }
            return false;
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
            EffectManager.sendUIEffectText(29000, player.Player.connection, true, "ConfirmText", "<color=#999999>JOINING...</color>");
            yield return new WaitForSeconds(1);
            JoinTeam(player.Player, player.Team);
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
            public bool IsDonatorT1;
            public bool IsDonatorT2;

            public LobbyPlayer(UCPlayer player, ulong team)
            {
                Player = player;
                Team = team;
                IsInLobby = true;
                IsDonatorT1 = false;
                IsDonatorT2 = false;
            }
        }
    }
}
