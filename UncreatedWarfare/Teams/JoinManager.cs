using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Framework.UI;
using Uncreated.Players;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Singletons;
using UnityEngine;

namespace Uncreated.Warfare.Teams;

public class JoinManager : BaseSingletonComponent
{
    private static JoinManager Singleton;
    public static readonly JoinUI JoinUI = new JoinUI();
    private List<LobbyPlayer> LobbyPlayers;
    private float secondsLeft;
    private readonly float secondsTotal = 60;
    private const string SECONDS_LEFT_TIME_FORAMT = "mm\\:ss";
    public static bool Loaded => Singleton.IsLoaded2();
    public override void Load()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        //secondsTotal = 0;
        LobbyPlayers = new List<LobbyPlayer>(Provider.maxPlayers);
        secondsLeft = 0;
        Singleton = this;
        foreach (UCPlayer player in PlayerManager.OnlinePlayers)
        {
            LobbyPlayers.Add(LobbyPlayer.CreateNew(player, player.GetTeam()));
        }
        JoinUI.Team1Button.OnClicked += OnTeam1ButtonClicked;
        JoinUI.Team2Button.OnClicked += OnTeam2ButtonClicked;
        JoinUI.ConfirmButton.OnClicked += OnConfirmButtonClicked;
    }
    public override void Unload()
    {
        JoinUI.ConfirmButton.OnClicked += OnConfirmButtonClicked;
        JoinUI.Team2Button.OnClicked += OnTeam2ButtonClicked;
        JoinUI.Team1Button.OnClicked += OnTeam1ButtonClicked;
        foreach (LobbyPlayer pl in LobbyPlayers)
        {
            if (pl.IsInLobby && pl.Player.IsOnline)
            {
                if (pl.current is not null)
                    StopCoroutine(pl.current);
                JoinUI.ClearFromPlayer(pl.Player.Connection);
                pl.Player.Player.setAllPluginWidgetFlags(EPluginWidgetFlags.Default);
            }
        }
        secondsLeft = 0;
        Singleton = null!;
        LobbyPlayers.Clear();
        LobbyPlayers = null!;
    }
    public void UpdatePlayer(Player player)
    {
        AssertLoadedIntl();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong team = player.GetTeam();
        int ind = LobbyPlayers.FindIndex(x => x.Steam64 == player.channel.owner.playerID.steamID.m_SteamID);
        if (ind == -1)
        {
            UCPlayer? pl2 = UCPlayer.FromPlayer(player);
            if (pl2 != null)
                LobbyPlayers.Add(LobbyPlayer.CreateNew(pl2, team));
            return;
        }
        LobbyPlayer pl = LobbyPlayers[ind];
        if (pl != null)
        {
            if (!pl.Reset())
            {
                UCPlayer? pl2 = UCPlayer.FromPlayer(player);
                if (pl2 != null)
                    LobbyPlayers[ind] = LobbyPlayer.CreateNew(pl2, team);
                else
                    LobbyPlayers.RemoveAt(ind);
            }
            else
            {
                pl.Team = team;
            }
        }
        else
        {
            UCPlayer? pl2 = UCPlayer.FromPlayer(player);
            if (pl2 != null)
                LobbyPlayers.Add(LobbyPlayer.CreateNew(pl2, team));
            return;
        }
    }
    public bool IsInLobby(UCPlayer player)
    {
        if (!_isLoaded) return false;
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
        if (!_isLoaded) return;
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!isNewPlayer)
        {
            LobbyPlayers.RemoveAll(x => x.Steam64 == player.Steam64);
            LobbyPlayer lobbyPlayer = LobbyPlayer.CreateNew(player, player.GetTeam());
            lobbyPlayer.IsInLobby = false;
            LobbyPlayers.Add(lobbyPlayer);
            foreach (LobbyPlayer p in LobbyPlayers)
                UpdateUITeams(p, p.Team);
        }
        else
        {
            JoinLobby(player);
        }
    }

    public void OnPlayerDisconnected(UCPlayer player)
    {
        if (!_isLoaded) return;
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < LobbyPlayers.Count; i++)
        {
            if (LobbyPlayers[i].Steam64 == player.Steam64)
            {
                if (LobbyPlayers[i].IsInLobby)
                {
                    if (PlayerManager.HasSave(player.CSteamID.m_SteamID, out PlayerSave save))
                        save.ShouldRespawnOnJoin = true;
                    else PlayerManager.AddSave(new PlayerSave(player.Steam64) { ShouldRespawnOnJoin = true });
                }
                if (LobbyPlayers[i].current != null)
                {
                    StopCoroutine(LobbyPlayers[i].current);
                    LobbyPlayers[i].current = null;
                }
                LobbyPlayers.RemoveAt(i);
                foreach (LobbyPlayer p in LobbyPlayers)
                    UpdateUITeams(p, p.Team);
                break;
            }
        }
    }

    public void JoinLobby(UCPlayer player)
    {
        AssertLoadedIntl();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        LobbyPlayer lobbyPlayer = LobbyPlayers.Find(p => p.Player.Steam64 == player.Steam64);
        if (lobbyPlayer == null)
        {
            lobbyPlayer = LobbyPlayer.CreateNew(player);
            LobbyPlayers.Add(lobbyPlayer);
        }

        if (player.Player.life.isDead)
        {
            player.Player.life.ReceiveRespawnRequest(false);
        }

        player.Player.teleportToLocationUnsafe(TeamManager.LobbySpawn, TeamManager.LobbySpawnAngle);

        ulong oldgroup = player.GetTeam();

        player.Player.quests.leaveGroup(true);

        lobbyPlayer.IsInLobby = true;

        EventDispatcher.InvokeOnGroupChanged(player, oldgroup, 0);
        
        ShowUI(lobbyPlayer);

        foreach (LobbyPlayer p in LobbyPlayers)
            UpdateUITeams(p, p.Team);
    }

    public void ShowUI(LobbyPlayer player)
    {
        AssertLoadedIntl();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif

        player.Player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.None);
        player.Player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Modal);
        ITransportConnection c = player.Player.Connection;
        JoinUI.SendToPlayer(c);

        JoinUI.CloseButton.SetVisibility(c, false);

        JoinUI.Team1Name.SetText(c, TeamManager.TranslateShortName(1, player.Player, true));
        JoinUI.Team2Name.SetText(c, TeamManager.TranslateShortName(2, player.Player, true));

        JoinUI.Team1Image.SetImage(c, TeamManager.Team1Faction.FlagImageURL);
        JoinUI.Team2Image.SetImage(c, TeamManager.Team2Faction.FlagImageURL);

        int t1 = 0, t2 = 0;
        for (int i = 0; i < LobbyPlayers.Count; ++i)
        {
            LobbyPlayer pl = LobbyPlayers[i];
            if (pl.Team == 1) ++t1;
            else if (pl.Team == 2) ++t2;
        }
        JoinUI.Team1PlayerCount.SetText(c, t1.ToString(Data.Locale));
        JoinUI.Team2PlayerCount.SetText(c, t2.ToString(Data.Locale));
        ulong team = player.Player.GetTeam();

        if (team == 1)
        {
            JoinUI.Team1Select.SetText(c, "JOINED");
            JoinUI.Team1Highlight.SetVisibility(c, true);
            JoinUI.Team1Button.SetVisibility(c, false);
        }
        else if (team == 2)
        {
            JoinUI.Team2Select.SetText(c, "JOINED");
            JoinUI.Team2Highlight.SetVisibility(c, true);
            JoinUI.Team2Button.SetVisibility(c, false);
        }


        if (secondsLeft <= 0)
        {
            JoinUI.ConfirmButton.SetVisibility(c, true);
            JoinUI.GameStartingParent.SetVisibility(c, false);
        }
        else
        {
            JoinUI.ConfirmButton.SetVisibility(c, false);
            JoinUI.GameStartingParent.SetVisibility(c, true);
            JoinUI.GameStartingSeconds.SetText(c, TimeSpan.FromSeconds(secondsLeft).ToString(SECONDS_LEFT_TIME_FORAMT, Data.Locale));
            UpdateTime(c);
        }
        t1 = 0;
        t2 = 0;
        for (int i = 0; i < LobbyPlayers.Count; i++)
        {
            LobbyPlayer pl = LobbyPlayers[i];
            string name = pl.Player.CharacterName;
            if (pl.IsInLobby)
                name = name.Colorize("9F9F9F");
            if (pl.Team == 1)
            {
                if (t1 < JoinUI.Team1Players.Length)
                    JoinUI.Team1Players[t1++].SetText(c, name);
            }
            else if (pl.Team == 2)
            {
                if (t2 < JoinUI.Team2Players.Length)
                    JoinUI.Team2Players[t2++].SetText(c, name);
            }
        }
    }
    private void UpdateTime(ITransportConnection c)
    {
        if (secondsLeft <= 0)
        {
            JoinUI.ConfirmButton.SetVisibility(c, true);
            JoinUI.GameStartingParent.SetVisibility(c, false);
        }
        else
        {
            JoinUI.GameStartingCircle.SetText(c,
                new string(Gamemode.Config.UI.ProgressChars[CTFUI.FromMax(Mathf.RoundToInt(secondsLeft), Mathf.RoundToInt(secondsTotal))], 1));
        }
    }
    public void UpdateUITeams(LobbyPlayer player, ulong team)
    {
        AssertLoadedIntl();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!player.IsInLobby) return;

        ITransportConnection c = player.Player.Connection;

        int t1 = 0, t2 = 0;
        for (int i = 0; i < LobbyPlayers.Count; ++i)
        {
            LobbyPlayer pl = LobbyPlayers[i];
            if (pl.Team == 1) ++t1;
            else if (pl.Team == 2) ++t2;
        }
        JoinUI.Team1PlayerCount.SetText(c, t1.ToString(Data.Locale));
        JoinUI.Team2PlayerCount.SetText(c, t2.ToString(Data.Locale));

        if (!player.Player.OnDuty() && !player.IsDonatorT1 && IsTeamFull(player, 1))
        {
            JoinUI.Team1Select.SetText(c, "<color=#bf6363>FULL</color>");
            JoinUI.Team2Select.SetText(c, "CLICK TO JOIN");
            JoinUI.Team1Button.SetVisibility(c, false);
            JoinUI.Team2Button.SetVisibility(c, true);
        }
        else if (!player.Player.OnDuty() && !player.IsDonatorT2 && IsTeamFull(player, 2))
        {
            JoinUI.Team1Select.SetText(c, "CLICK TO JOIN");
            JoinUI.Team2Select.SetText(c, "<color=#bf6363>FULL</color>");
            JoinUI.Team1Button.SetVisibility(c, true);
            JoinUI.Team2Button.SetVisibility(c, false);
        }
        else
        {
            JoinUI.Team1Select.SetText(c, "CLICK TO JOIN");
            JoinUI.Team2Select.SetText(c, "CLICK TO JOIN");
            JoinUI.Team1Button.SetVisibility(c, true);
            JoinUI.Team2Button.SetVisibility(c, true);
        }
        t1 = 0;
        t2 = 0;
        for (int i = 0; i < LobbyPlayers.Count; i++)
        {
            LobbyPlayer pl = LobbyPlayers[i];
            string name = pl.Player.CharacterName;
            if (pl.IsInLobby)
                name = name.Colorize("9F9F9F");
            if (pl.Team == 1)
            {
                if (t1 < JoinUI.Team1Players.Length)
                    JoinUI.Team1Players[t1++].SetText(c, name);
            }
            else if (pl.Team == 2)
            {
                if (t2 < JoinUI.Team2Players.Length)
                    JoinUI.Team2Players[t2++].SetText(c, name);
            }
        }
        for (; t1 < JoinUI.Team1Players.Length; ++t1)
            JoinUI.Team1Players[t1++].SetText(c, string.Empty);
        for (; t2 < JoinUI.Team2Players.Length; ++t2)
            JoinUI.Team2Players[t2++].SetText(c, string.Empty);
    }
    private void OnConfirmButtonClicked(UnturnedButton button, Player player)
    {
        if (!_isLoaded) return;
        LobbyPlayer lobbyPlayer = LobbyPlayers.Find(p => p.Player.CSteamID == player.channel.owner.playerID.steamID);
        if (lobbyPlayer is not null && lobbyPlayer.Team != 0 && lobbyPlayer.current == null)
        {
            lobbyPlayer.current = StartCoroutine(ConfirmJoin(lobbyPlayer));
        }
    }
    private void OnTeam2ButtonClicked(UnturnedButton button, Player player)
    {
        if (!_isLoaded) return;
        LobbyPlayer lobbyPlayer = LobbyPlayers.Find(p => p.Player.CSteamID == player.channel.owner.playerID.steamID);
        if (lobbyPlayer is not null && lobbyPlayer.Team != 2)
        {
            lobbyPlayer.Team = 2;
            foreach (LobbyPlayer p in LobbyPlayers)
                UpdateUITeams(p, p.Team);
            JoinUI.Team2Select.SetText(lobbyPlayer.Player.Connection, "JOINED");
        }
    }
    private void OnTeam1ButtonClicked(UnturnedButton button, Player player)
    {
        if (!_isLoaded) return;
        LobbyPlayer lobbyPlayer = LobbyPlayers.Find(p => p.Player.CSteamID == player.channel.owner.playerID.steamID);
        if (lobbyPlayer is not null && lobbyPlayer.Team != 1)
        {
            lobbyPlayer.Team = 1;
            foreach (LobbyPlayer p in LobbyPlayers)
                UpdateUITeams(p, p.Team);
            JoinUI.Team1Select.SetText(lobbyPlayer.Player.Connection, "JOINED");
        }
    }
    private void JoinTeam(UCPlayer player, ulong newTeam)
    {
        AssertLoadedIntl();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        string teamName = TeamManager.TranslateName(newTeam, player.CSteamID);

        GroupInfo group = GroupManager.getGroupInfo(new CSteamID(TeamManager.GetGroupID(newTeam)));
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

        EventDispatcher.InvokeOnGroupChanged(player, oldgroup, newTeam);
        if (Data.Is(out TeamGamemode tg))
            tg.OnJoinTeam(player, newTeam);
        FPlayerName names = F.GetPlayerOriginalNames(player.Player);
        L.Log(Translation.Translate("join_player_joined_console", 0, out _,
            names.PlayerName, player.Steam64.ToString(), newTeam.ToString(Data.Locale), oldgroup.ToString(Data.Locale)),
            ConsoleColor.Cyan);

        player.Player.teleportToLocation(newTeam.GetBaseSpawnFromTeam(), newTeam.GetBaseAngle());

        if (KitManager.KitExists(TeamManager.GetUnarmedFromS64ID(player.Steam64), out Kit kit))
            KitManager.GiveKit(player, kit);

        player.SendChat("teams_join_success", TeamManager.TranslateName(newTeam, player.CSteamID, true));

        Chat.BroadcastToAllExcept(new ulong[1] { player.CSteamID.m_SteamID }, "teams_join_announce", names.CharacterName, teamName, TeamManager.GetTeamHexColor(newTeam));
        ActionLog.Add(EActionLogType.CHANGE_GROUP_WITH_UI, "GROUP: " + TeamManager.TranslateName(newTeam, 0).ToUpper(), player);

        if (player.Squad != null)
            Squads.SquadManager.LeaveSquad(player, player.Squad);
        PlayerManager.ApplyToOnline();

        CooldownManager.StartCooldown(player, ECooldownType.CHANGE_TEAMS, TeamManager.TeamSwitchCooldown);
        ToastMessage.QueueMessage(player, new ToastMessage("", Data.Gamemode.DisplayName, EToastMessageSeverity.BIG));
    }

    public void CloseUI(UCPlayer player)
    {
        if (!_isLoaded) return;
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (player is null || !player.IsOnline) return;
        LobbyPlayer lp = LobbyPlayers.Find(x => x.Steam64 == player.Steam64);
        if (lp is null) return;
        if (lp.Player is null)
            lp.Player = player;
        CloseUI(lp);
    }
    public void CloseUI(LobbyPlayer player)
    {
        AssertLoadedIntl();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        player.IsInLobby = false;
        player.Team = player.Player.GetTeam();
        player.Player.Player.setAllPluginWidgetFlags(EPluginWidgetFlags.Default);
        JoinUI.ClearFromPlayer(player.Player.Connection);

        foreach (LobbyPlayer p in LobbyPlayers)
            UpdateUITeams(p, p.Team);
    }

    public bool IsTeamFull(LobbyPlayer player, ulong team)
    {
        AssertLoadedIntl();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!TeamManager.Config.BalanceTeams)
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
                return (Team1Count + 1f) / (Team2Count - 1f) - 1f >= TeamManager.Config.AllowedDifferencePercent;
            }
            else if (player.Team == 1) // if player is already on the specified team
            {
                return (float)Team1Count / Team2Count - 1f >= TeamManager.Config.AllowedDifferencePercent;
            }
            else // if player has not joined a team yet
            {
                return (Team1Count + 1f) / Team2Count - 1f >= TeamManager.Config.AllowedDifferencePercent;
            }
        }
        else if (team == 2)
        {
            if (player.Team == 1) // if player is on the opposing team
            {
                return (Team2Count + 1f) / (Team1Count - 1f) - 1f >= TeamManager.Config.AllowedDifferencePercent;
            }
            else if (player.Team == 2) // if player is already on the specified team
            {
                return (float)(Team2Count) / Team1Count - 1f >= TeamManager.Config.AllowedDifferencePercent;
            }
            else // if player has not joi   ned a team yet
            {
                return (Team2Count + 1f) / Team1Count - 1f >= TeamManager.Config.AllowedDifferencePercent;
            }
        }
        return false;
    }

    public void OnNewGameStarting()
    {
        AssertLoadedIntl();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        //StartCoroutine(CountdownTick());
        LobbyPlayers.RemoveAll(x => !x.Reset());

        foreach (UCPlayer player in PlayerManager.OnlinePlayers)
            JoinLobby(player);
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
    private IEnumerator<WaitForSeconds> ConfirmJoin(LobbyPlayer player)
    {
        JoinUI.ConfirmText.SetText(player.Player.Connection, "<color=#999999>JOINING...</color>");
        yield return new WaitForSeconds(1);
        if (_isLoaded)
        {
            player.IsInLobby = false;
            JoinTeam(player.Player, player.Team);
            CloseUI(player);
            player.current = null;
        }
    }
    public class LobbyPlayer
    {
        public UCPlayer Player;
        public readonly ulong Steam64;
        public ulong Team;
        public bool IsInLobby;
        public bool IsDonatorT1;
        public bool IsDonatorT2;
        public Coroutine? current = null;

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
                Player = PlayerManager.OnlinePlayers.Find(x => x.Steam64 == Steam64);
            return Player != null;
        }
        public void CheckKits()
        {
            IsDonatorT1 = KitManager.GetKitsWhere(k => (k.IsPremium || k.IsLoadout) && KitManager.HasAccessFast(k, Player) && k.Team == 1).Count > 0;
            IsDonatorT2 = KitManager.GetKitsWhere(k => (k.IsPremium || k.IsLoadout) && KitManager.HasAccessFast(k, Player) && k.Team == 2).Count > 0;
        }
        public static LobbyPlayer CreateNew(UCPlayer player, ulong team = 0)
        {
            return new LobbyPlayer(player, team)
            {
                IsInLobby = true,
                IsDonatorT1 = KitManager.GetKitsWhere(k => (k.IsPremium || k.IsLoadout) && KitManager.HasAccessFast(k, player) && k.Team == 1).Count > 0,
                IsDonatorT2 = KitManager.GetKitsWhere(k => (k.IsPremium || k.IsLoadout) && KitManager.HasAccessFast(k, player) && k.Team == 2).Count > 0
            };
        }
    }
}
