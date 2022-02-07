using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Players;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Kits;
using UnityEngine;

namespace Uncreated.Warfare.Teams
{
    public class JoinManager : MonoBehaviour, IDisposable
    {
        private static ushort JOIN_UI_ID;
        internal const short joinUiKey = 29000;
        internal static void CacheIDs()
        {
            if (Assets.find(Gamemode.Config.UI.JoinUIGUID) is EffectAsset ea)
                JOIN_UI_ID = ea.id;
        }
        private List<LobbyPlayer> LobbyPlayers;
        private TimeSpan countdown;
        public void Initialize()
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            LobbyPlayers = new List<LobbyPlayer>();
            countdown = TimeSpan.FromTicks(0);

            if (PlayerManager.OnlinePlayers != null)
            {
                foreach (UCPlayer player in PlayerManager.OnlinePlayers)
                {
                    LobbyPlayers.Add(LobbyPlayer.CreateNew(player, player.GetTeam()));
                }
            }
            EffectManager.onEffectButtonClicked += OnButtonClicked;
        }
        public void UpdatePlayer(Player player)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            ulong team = player.GetTeam();
            LobbyPlayer pl = LobbyPlayers.FirstOrDefault(x => x.Steam64 == player.channel.owner.playerID.steamID.m_SteamID);
            if (pl != null)
            {
                if (!pl.Reset())
                {
                    pl = LobbyPlayer.CreateNew(UCPlayer.FromPlayer(player), team);
                }
                else
                {
                    pl.Team = team;
                }
            }
            else
            {
                pl = LobbyPlayer.CreateNew(UCPlayer.FromPlayer(player), team);
            }
        }
        public bool IsInLobby(UCPlayer player)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            foreach (LobbyPlayer lobbyPlayer in LobbyPlayers)
            {
                if (lobbyPlayer.IsInLobby && lobbyPlayer.Player == player)
                {
                    return true;
                }
            }
            return false;
        }
        public void OnPlayerConnected(UCPlayer player, bool isNewPlayer)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (!isNewPlayer)
            {
                LobbyPlayer lobbyPlayer = LobbyPlayer.CreateNew(player, player.GetTeam());
                lobbyPlayer.IsInLobby = false;
                LobbyPlayers.Add(lobbyPlayer);
                foreach (LobbyPlayer p in LobbyPlayers)
                    UpdateUITeams(p, p.Team);
            }
            else
            {
                JoinLobby(player, false);
            }
        }

        public void OnPlayerDisconnected(UCPlayer player)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            bool x = false;
            for (int i = 0; i < LobbyPlayers.Count; i++)
            {
                if (LobbyPlayers[i].Steam64 == player.Steam64)
                {
                    if (LobbyPlayers[i].IsInLobby)
                    {
                        if (PlayerManager.HasSave(player.CSteamID.m_SteamID, out PlayerSave save))
                            save.ShouldRespawnOnJoin = true;
                        else
                            save.ShouldRespawnOnJoin = false;
                    }


                    if (LobbyPlayers[i].current != null)
                    {
                        StopCoroutine(LobbyPlayers[i].current);
                        LobbyPlayers[i].current = null;
                    }
                    LobbyPlayers.RemoveAt(i);
                    x = true;
                    break;
                }
            }
            if (x)
                foreach (LobbyPlayer p in LobbyPlayers)
                    UpdateUITeams(p, p.Team);
        }

        public void JoinLobby(UCPlayer player, bool showX)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            LobbyPlayer lobbyPlayer = LobbyPlayers.Find(p => p.Player.Steam64 == player.Steam64);
            if (lobbyPlayer == null)
            {
                lobbyPlayer = LobbyPlayer.CreateNew(player);
                LobbyPlayers.Add(lobbyPlayer);
            }
            //else if (lobbyPlayer.IsInLobby)
            //{
            //    EffectManager.askEffectClearByID(JOIN_UI_ID, player.connection);
            //}

            showX = false;

            if (player.Player.life.isDead)
            {
                player.Player.life.ReceiveRespawnRequest(false);
            }

            player.Player.teleportToLocationUnsafe(TeamManager.LobbySpawn, TeamManager.LobbySpawnAngle);

            ulong oldgroup = player.GetTeam();

            player.Player.quests.leaveGroup(true);

            lobbyPlayer.IsInLobby = true;

            EventFunctions.OnGroupChangedInvoke(player.Player.channel.owner, oldgroup, 0);
            
            ShowUI(lobbyPlayer, showX);

            foreach (LobbyPlayer p in LobbyPlayers)
                UpdateUITeams(p, p.Team);
        }

        public void ShowUI(LobbyPlayer player, bool showX)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            showX = false;

            player.Player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.None);
            player.Player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Modal);

            EffectManager.sendUIEffect(JOIN_UI_ID, joinUiKey, player.Player.connection, true);

            EffectManager.sendUIEffectText(joinUiKey, player.Player.connection, true, "Team1Name", Translation.Translate("team_1_short", player.Player));
            EffectManager.sendUIEffectText(joinUiKey, player.Player.connection, true, "Team2Name", Translation.Translate("team_2_short", player.Player));

            EffectManager.sendUIEffectText(joinUiKey, player.Player.connection, true, "Team1PlayerCount", LobbyPlayers.Count(x => x.Team == 1).ToString(Data.Locale));
            EffectManager.sendUIEffectText(joinUiKey, player.Player.connection, true, "Team2PlayerCount", LobbyPlayers.Count(x => x.Team == 2).ToString(Data.Locale));

            if (showX && player.Player.GetTeam() == 1)
            {
                EffectManager.sendUIEffectText(joinUiKey, player.Player.connection, true, "Team1Select", "<color=A4A4A4>JOINED</color>");
                EffectManager.sendUIEffectVisibility(joinUiKey, player.Player.connection, true, "Team1Highlight", true);
                EffectManager.sendUIEffectVisibility(joinUiKey, player.Player.connection, true, "Team1Button", false);
            }
            else if (showX && player.Player.GetTeam() == 2)
            {
                EffectManager.sendUIEffectText(joinUiKey, player.Player.connection, true, "Team2Select", "<color=A4A4A4>JOINED</color>");
                EffectManager.sendUIEffectVisibility(joinUiKey, player.Player.connection, true, "Team2Highlight", true);
                EffectManager.sendUIEffectVisibility(joinUiKey, player.Player.connection, true, "Team2Button", false);
            }

            if (!showX)
                EffectManager.sendUIEffectVisibility(joinUiKey, player.Player.connection, true, "X", false);

            if (countdown.Ticks == 0)
            {
                EffectManager.sendUIEffectVisibility(joinUiKey, player.Player.connection, true, "Confirm", true);
                EffectManager.sendUIEffectVisibility(joinUiKey, player.Player.connection, true, "GameStarting", false);
            }
            else
            {
                EffectManager.sendUIEffectVisibility(joinUiKey, player.Player.connection, true, "Confirm", false);
                EffectManager.sendUIEffectVisibility(joinUiKey, player.Player.connection, true, "GameStarting", true);
                EffectManager.sendUIEffectText(joinUiKey, player.Player.connection, true, "GameStartingSeconds", countdown.Minutes.ToString("D2") + ":" + countdown.Seconds.ToString("D2"));
            }
            int t1 = 0;
            int t2 = 0;
            for (int i = 0; i < LobbyPlayers.Count; i++)
            {
                string name = LobbyPlayers[i].Player.CharacterName;
                if (LobbyPlayers[i].IsInLobby)
                    name = name.Colorize("9F9F9F");
                if (LobbyPlayers[i].Team == 1)
                {
                    EffectManager.sendUIEffectText(joinUiKey, player.Player.connection, true, "T1P" + (t1 + 1), name);
                    t1++;
                }
                else if (LobbyPlayers[i].Team == 2)
                {
                    EffectManager.sendUIEffectText(joinUiKey, player.Player.connection, true, "T2P" + (t2 + 1), name);
                    t2++;
                }
            }
        }

        public void UpdateUITeams(LobbyPlayer player, ulong team)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (!player.IsInLobby) return;

            //L.Log($"UI teams updated: T1: {Team1Players.Count} - T2: {Team2Players.Count}");

            EffectManager.sendUIEffectText(joinUiKey, player.Player.connection, true, "Team1PlayerCount", LobbyPlayers.Count(x => x.Team == 1).ToString(Data.Locale));
            EffectManager.sendUIEffectText(joinUiKey, player.Player.connection, true, "Team2PlayerCount", LobbyPlayers.Count(x => x.Team == 2).ToString(Data.Locale));

            if (!player.Player.OnDuty() && !player.IsDonatorT1 && IsTeamFull(player, 1))
            {
                EffectManager.sendUIEffectText(joinUiKey, player.Player.connection, true, "Team1Select", "<color=#bf6363>FULL</color>");
                EffectManager.sendUIEffectText(joinUiKey, player.Player.connection, true, "Team2Select", "CLICK TO JOIN");
                EffectManager.sendUIEffectVisibility(joinUiKey, player.Player.connection, true, "Team1Button", false);
                EffectManager.sendUIEffectVisibility(joinUiKey, player.Player.connection, true, "Team2Button", true);
            }
            else if (!player.Player.OnDuty() && !player.IsDonatorT2 && IsTeamFull(player, 2))
            {
                EffectManager.sendUIEffectText(joinUiKey, player.Player.connection, true, "Team2Select", "<color=#bf6363>FULL</color>");
                EffectManager.sendUIEffectText(joinUiKey, player.Player.connection, true, "Team1Select", "CLICK TO JOIN");
                EffectManager.sendUIEffectVisibility(joinUiKey, player.Player.connection, true, "Team2Button", false);
                EffectManager.sendUIEffectVisibility(joinUiKey, player.Player.connection, true, "Team1Button", true);
            }
            else
            {
                EffectManager.sendUIEffectText(joinUiKey, player.Player.connection, true, "Team1Select", "CLICK TO JOIN");
                EffectManager.sendUIEffectText(joinUiKey, player.Player.connection, true, "Team2Select", "CLICK TO JOIN");
                EffectManager.sendUIEffectVisibility(joinUiKey, player.Player.connection, true, "Team1Button", true);
                EffectManager.sendUIEffectVisibility(joinUiKey, player.Player.connection, true, "Team2Button", true);
            }

            for (int i = 0; i < 32; i++)
            {
                EffectManager.sendUIEffectText(joinUiKey, player.Player.connection, true, "T1P" + (i + 1), "");
                EffectManager.sendUIEffectText(joinUiKey, player.Player.connection, true, "T2P" + (i + 1), "");
            }
            int t1 = 0;
            int t2 = 0;
            for (int i = 0; i < LobbyPlayers.Count; i++)
            {
                string name = LobbyPlayers[i].Player.CharacterName;
                if (LobbyPlayers[i].IsInLobby)
                    name = name.Colorize("9F9F9F");
                if (LobbyPlayers[i].Team == 1)
                {
                    EffectManager.sendUIEffectText(joinUiKey, player.Player.connection, true, "T1P" + (t1 + 1), name);
                    t1++;
                }
                else if (LobbyPlayers[i].Team == 2)
                {
                    EffectManager.sendUIEffectText(joinUiKey, player.Player.connection, true, "T2P" + (t2 + 1), name);
                    t2++;
                }
            }
        }

        public void UpdateUICountDown(LobbyPlayer player)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (!player.IsInLobby) return;

            if (countdown.Seconds > 0)
            {
                EffectManager.sendUIEffectText(joinUiKey, player.Player.connection, true, "GameStartingSeconds", countdown.Minutes.ToString("D2") + ":" + countdown.Seconds.ToString("D2"));
            }
            else
            {
                EffectManager.sendUIEffectVisibility(joinUiKey, player.Player.connection, true, "GameStarting", false);
                EffectManager.sendUIEffectVisibility(joinUiKey, player.Player.connection, true, "Confirm", true);
            }
        }

        public void OnButtonClicked(Player nelsonplayer, string buttonName)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            LobbyPlayer lobbyPlayer = LobbyPlayers.Find(p => p.Player.CSteamID == nelsonplayer.channel.owner.playerID.steamID);

            if (buttonName == "Team1Button")
            {
                if (lobbyPlayer.Team != 1)
                {
                    lobbyPlayer.Team = 1;
                    foreach (LobbyPlayer p in LobbyPlayers)
                        UpdateUITeams(p, p.Team);

                    EffectManager.sendUIEffectText(joinUiKey, lobbyPlayer.Player.connection, true, "Team1Select", "JOINED");
                }
            }
            else if (buttonName == "Team2Button")
            {
                if (lobbyPlayer.Team != 2)
                {
                    lobbyPlayer.Team = 2;
                    foreach (LobbyPlayer p in LobbyPlayers)
                        UpdateUITeams(p, p.Team);

                    EffectManager.sendUIEffectText(joinUiKey, lobbyPlayer.Player.connection, true, "Team2Select", "JOINED");
                }
            }
            else if (buttonName == "Confirm")
            {
                if (lobbyPlayer.Team != 0 && lobbyPlayer.current == null)
                {
                    lobbyPlayer.current = StartCoroutine(ConfirmJoin(lobbyPlayer));
                }
            }
        }

        private void JoinTeam(UCPlayer player, ulong newTeam)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
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
            L.Log(Translation.Translate("join_player_joined_console", 0, out _,
                names.PlayerName, player.Steam64.ToString(), newTeam.ToString(Data.Locale), oldgroup.ToString(Data.Locale)),
                ConsoleColor.Cyan);

            player.Player.teleportToLocation(newTeam.GetBaseSpawnFromTeam(), newTeam.GetBaseAngle());

            if (KitManager.KitExists(TeamManager.GetUnarmedFromS64ID(player.Steam64), out Kit kit))
                KitManager.GiveKit(player, kit);

            player.SendChat("teams_join_success", TeamManager.TranslateName(newTeam, player.CSteamID, true));

            Chat.BroadcastToAllExcept(new ulong[1] { player.CSteamID.m_SteamID }, "teams_join_announce", names.CharacterName, teamName);

            if (player.Squad != null)
                Squads.SquadManager.LeaveSquad(player, player.Squad);
            PlayerManager.ApplyToOnline();

            CooldownManager.StartCooldown(player, ECooldownType.CHANGE_TEAMS, TeamManager.TeamSwitchCooldown);
            ToastMessage.QueueMessage(player, new ToastMessage("", Data.Gamemode.DisplayName, EToastMessageSeverity.BIG));
        }

        public void CloseUI(UCPlayer player)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (player == null) return;
            LobbyPlayer lp = LobbyPlayers.Find(x => x.Steam64 == player.Steam64);
            if (lp == null || lp.Player == null) return;
            CloseUI(lp);
        }
        public void CloseUI(LobbyPlayer player)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            player.IsInLobby = false;
            player.Team = player.Player.GetTeam();
            player.Player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.None);
            player.Player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.Modal);
            player.Player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Default);
            EffectManager.askEffectClearByID(JOIN_UI_ID, player.Player.connection);

            foreach (LobbyPlayer p in LobbyPlayers)
                UpdateUITeams(p, p.Team);
        }

        public bool IsTeamFull(LobbyPlayer player, ulong team)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (!UCWarfare.Config.TeamSettings.BalanceTeams)
                return false;
            if (player.Team == team)
                return false;

            int Team1Count = LobbyPlayers.Count(x => x.Team == 1);
            int Team2Count = LobbyPlayers.Count(x => x.Team == 2);

            if (Team1Count == 0 || Team2Count == 0)
                return false;

            if (team == 1)
            {
                if (player.Team == 2) // if player is on the opposing team
                {
                    return (Team1Count + 1f) / (Team2Count - 1f) - 1f >= UCWarfare.Config.TeamSettings.AllowedDifferencePercent;
                }
                else if (player.Team == 1) // if player is already on the specified team
                {
                    return (float)Team1Count / Team2Count - 1f >= UCWarfare.Config.TeamSettings.AllowedDifferencePercent;
                }
                else // if player has not joined a team yet
                {
                    return (Team1Count + 1f) / Team2Count - 1f >= UCWarfare.Config.TeamSettings.AllowedDifferencePercent;
                }
            }
            else if (team == 2)
            {
                if (player.Team == 1) // if player is on the opposing team
                {
                    return (Team2Count + 1f) / (Team1Count - 1f) - 1f >= UCWarfare.Config.TeamSettings.AllowedDifferencePercent;
                }
                else if (player.Team == 2) // if player is already on the specified team
                {
                    return (float)(Team2Count) / Team1Count - 1f >= UCWarfare.Config.TeamSettings.AllowedDifferencePercent;
                }
                else // if player has not joi   ned a team yet
                {
                    return (Team2Count + 1f) / Team1Count - 1f >= UCWarfare.Config.TeamSettings.AllowedDifferencePercent;
                }
            }
            return false;
        }

        public void OnNewGameStarting()
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            //StartCoroutine(CountdownTick());
            LobbyPlayers.RemoveAll(x => !x.Reset());

            foreach (UCPlayer player in PlayerManager.OnlinePlayers)
                JoinLobby(player, false);
        }
#if false
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
#endif
        IEnumerator<WaitForSeconds> ConfirmJoin(LobbyPlayer player)
        {
            EffectManager.sendUIEffectText(joinUiKey, player.Player.connection, true, "ConfirmText", "<color=#999999>JOINING...</color>");
            yield return new WaitForSeconds(1);
            player.IsInLobby = false;
            JoinTeam(player.Player, player.Team);
            CloseUI(player);
        }

        public void Dispose()
        {
            EffectManager.onEffectButtonClicked -= OnButtonClicked;
        }

        public class LobbyPlayer
        {
            public UCPlayer Player;
            public readonly ulong Steam64;
            public ulong Team;
            public bool IsInLobby;
            public bool IsDonatorT1;
            public bool IsDonatorT2;
            public Coroutine current = null;

            public LobbyPlayer(UCPlayer player, ulong team)
            {
                Player = player;
                Team = team;
                IsInLobby = true;
                IsDonatorT1 = false;
                IsDonatorT2 = false;
                Steam64 = player.Steam64;
            }
            public bool Reset()
            {
                Team = 0;
                current = null;
                if (Player == null || PlayerTool.getSteamPlayer(Player.Steam64) == null)
                    Player = PlayerManager.OnlinePlayers.Find(x => x.Steam64 == Steam64) ?? null;
                return Player != null;
            }
            public void CheckKits()
            {
                IsDonatorT1 = KitManager.GetKitsWhere(k => (k.IsPremium || k.IsLoadout) && k.AllowedUsers.Contains(Player.Steam64) && k.Team == 1).Count() > 0;
                IsDonatorT2 = KitManager.GetKitsWhere(k => (k.IsPremium || k.IsLoadout) && k.AllowedUsers.Contains(Player.Steam64) && k.Team == 2).Count() > 0;
            }
            public static LobbyPlayer CreateNew(UCPlayer player, ulong team = 0)
            {
                return new LobbyPlayer(player, team)
                {
                    IsInLobby = true,
                    IsDonatorT1 = KitManager.GetKitsWhere(k => (k.IsPremium || k.IsLoadout) && k.AllowedUsers.Contains(player.Steam64) && k.Team == 1).Count() > 0,
                    IsDonatorT2 = KitManager.GetKitsWhere(k => (k.IsPremium || k.IsLoadout) && k.AllowedUsers.Contains(player.Steam64) && k.Team == 2).Count() > 0
                };
            }
        }
    }
}
