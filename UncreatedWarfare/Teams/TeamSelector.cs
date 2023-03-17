using System;
using SDG.NetTransport;
using SDG.Unturned;
using System.Collections;
using System.Linq;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Singletons;
using UnityEngine;

namespace Uncreated.Warfare.Teams;

public class TeamSelector : BaseSingletonComponent
{
    public static TeamSelector Instance;
    public static readonly TeamSelectorUI JoinUI = new TeamSelectorUI();
    public static event PlayerDelegate? OnPlayerSelecting;
    public static event PlayerDelegate? OnPlayerSelected;
    public static bool ShuffleTeamsNextGame = false;
    private const string SelectedHex = "afffc9";
    private const string SelfHex = "9bf3f3";
    public override void Load()
    {
        Instance = this;
        JoinUI.OnConfirmClicked += OnConfirmed;
        JoinUI.OnTeamButtonClicked += OnTeamClicked;
        EventDispatcher.GroupChanged += OnGroupChanged;
        EventDispatcher.PlayerLeft += OnPlayerDisconnect;
        Gamemode.OnStateUpdated += OnGamemodeStateUpdated;
        JoinUI.OnOptionsBackClicked += OnOptionsBackClicked;
    }
    public override void Unload()
    {
        JoinUI.OnOptionsBackClicked -= OnOptionsBackClicked;
        Gamemode.OnStateUpdated -= OnGamemodeStateUpdated;
        EventDispatcher.PlayerLeft -= OnPlayerDisconnect;
        EventDispatcher.GroupChanged -= OnGroupChanged;
        JoinUI.OnTeamButtonClicked -= OnTeamClicked;
        JoinUI.OnConfirmClicked -= OnConfirmed;
        Instance = null!;
    }
    public void ForceUpdate() => UpdateList();
    public bool IsSelecting(UCPlayer player) => player.TeamSelectorData is not null && player.TeamSelectorData.IsSelecting;
    public void ResetState(UCPlayer player)
    {
        if (player.TeamSelectorData is not null)
        {
            if (player.TeamSelectorData.JoiningCoroutine != null)
            {
                StopCoroutine(player.TeamSelectorData.JoiningCoroutine);
                player.TeamSelectorData.JoiningCoroutine = null;
            }
            if (player.TeamSelectorData.IsSelecting)
            {
                player.TeamSelectorData.IsSelecting = false;
                JoinUI.ClearFromPlayer(player.Connection);
            }
        }

        JoinSelectionMenu(player);
    }
    private static void OnGamemodeStateUpdated()
    {
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
        {
            UCPlayer player = PlayerManager.OnlinePlayers[i];
            if (player.TeamSelectorData is { IsSelecting: true })
            {
                if (Data.Gamemode.State is State.Staging or State.Active)
                {
                    JoinUI.LogicConfirmToggle.SetVisibility(player.Connection, player.TeamSelectorData.SelectedTeam is 1 or 2);
                }
                else
                {
                    JoinUI.LogicConfirmToggle.SetVisibility(player.Connection, true);
                }
            }
        }
    }
    private static void OnGroupChanged(GroupChanged e) => UpdateList();
    private void OnPlayerDisconnect(PlayerEvent e)
    {
        if (e.Player.TeamSelectorData?.JoiningCoroutine != null)
        {
            StopCoroutine(e.Player.TeamSelectorData.JoiningCoroutine);
            e.Player.TeamSelectorData.JoiningCoroutine = null;
        }

        UpdateList();
    }
    private static void OnOptionsBackClicked(UCPlayer player)
    {
        if (player.TeamSelectorData is { IsOptionsOnly: true })
        {
            CloseOptions(player);
        }
    }
    private static void CloseOptions(UCPlayer player)
    {
        JoinUI.ClearFromPlayer(player.Connection);
        player.ModalNeeded = false;
        player.HasUIHidden = false;
        UCWarfare.I.UpdateLangs(player, true);
        if (player.TeamSelectorData != null)
        {
            player.TeamSelectorData.IsOptionsOnly = false;
            player.TeamSelectorData.IsSelecting = false;
        }
        player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.Modal | EPluginWidgetFlags.ForceBlur);
        player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Default);
    }
    private static void OnTeamClicked(UCPlayer? player, ulong team)
    {
        if (player?.TeamSelectorData is null || !player.IsOnline || !player.TeamSelectorData.IsSelecting || player.TeamSelectorData.SelectedTeam == team) return;

        ITransportConnection c = player.Connection;

        GetTeamCounts(out int t1, out int t2);
        if (CheckTeam(team, player.TeamSelectorData.SelectedTeam, t1, t2))
        {
            if (player.TeamSelectorData.SelectedTeam != 0 && player.TeamSelectorData.SelectedTeam != team)
            {
                ulong other = player.TeamSelectorData.SelectedTeam;
                if (other == 1)
                {
                    ++t2;
                    --t1;
                }
                else
                {
                    ++t1;
                    --t2;
                }
                bool otherTeamHasRoom = CheckTeam(other, team, t1, t2);
                JoinUI.LogicTeamSelectedToggle[other - 1].SetVisibility(c, false);
                JoinUI.TeamStatus[other - 1].SetText(c, (otherTeamHasRoom ? T.TeamsUIClickToJoin : T.TeamsUIFull).Translate(player));
                JoinUI.SetTeamEnabled(c, other, otherTeamHasRoom);
            }
            player.TeamSelectorData.SelectedTeam = team;
            UpdateList();
            JoinUI.SetTeamEnabled(c, team, true);
            JoinUI.LogicTeamSelectedToggle[team - 1].SetVisibility(c, true);
            JoinUI.TeamStatus[team - 1].SetText(c, T.TeamsUIClickToJoin.Translate(player));
            JoinUI.LabelConfirm.SetText(c, T.TeamsUIConfirm.Translate(player));
            JoinUI.LogicConfirmToggle.SetVisibility(c, true);
        }
    }
    private void OnConfirmed(UCPlayer player)
    {
        if (player.TeamSelectorData is { IsOptionsOnly: true })
        {
            if (player.TeamSelectorData.JoiningCoroutine != null)
                StopCoroutine(player.TeamSelectorData.JoiningCoroutine);

            CloseOptions(player);
            return;
        }
        if (player.TeamSelectorData is not { IsSelecting: true, SelectedTeam: 1 or 2, JoiningCoroutine: null } ||
            Data.Gamemode == null ||
            Data.Gamemode.State is not State.Staging and not State.Active ||
            player.Player.life.isDead)
            return;
        
        JoinUI.LogicConfirmToggle.SetVisibility(player.Connection, false);
        player.TeamSelectorData.JoiningCoroutine = StartCoroutine(JoinCoroutine(player, player.TeamSelectorData.SelectedTeam));
    }
    private IEnumerator JoinCoroutine(UCPlayer player, ulong targetTeam)
    {
        ITransportConnection c = player.Connection;
        JoinUI.LabelConfirm.SetText(c, T.TeamsUIJoining.Translate(player));
        yield return new WaitForSeconds(1f);
        if (player.TeamSelectorData is not null)
            player.TeamSelectorData.JoiningCoroutine = null;

        if (!player.IsOnline) yield break;
        if (player.Player.life.isDead)
        {
            JoinUI.LogicConfirmToggle.SetVisibility(player.Connection, true);
            yield break;
        }

        JoinTeam(player, targetTeam);
    }

    public enum JoinTeamBehavior
    {
        NoTeam,
        Shuffle,
        KeepTeam
    }
    public void JoinSelectionMenu(UCPlayer player, JoinTeamBehavior joinBehavior = JoinTeamBehavior.NoTeam)
    {
        bool options = false;
        if (player.TeamSelectorData is null)
        {
            player.TeamSelectorData = new TeamSelectorData(true);
        }
        else
        {
            if (player.TeamSelectorData.IsSelecting)
                return;
            player.TeamSelectorData.IsSelecting = true;
            player.TeamSelectorData.SelectedTeam = 0;
            options = player.TeamSelectorData.IsOptionsOnly;
            player.TeamSelectorData.IsOptionsOnly = false;
            if (player.TeamSelectorData.JoiningCoroutine != null)
            {
                StopCoroutine(player.TeamSelectorData.JoiningCoroutine);
                player.TeamSelectorData.JoiningCoroutine = null;
            }
        }

        ulong oldTeam = player.GetTeam();
        if (joinBehavior == JoinTeamBehavior.Shuffle)
        {
            GetTeamCounts(out int t1, out int t2);
            ulong otherTeam = TeamManager.Other(oldTeam);
            if (oldTeam is 1 or 2 && CheckTeam(otherTeam, oldTeam, t1, t2))
                oldTeam = otherTeam;
        }
        else if (joinBehavior != JoinTeamBehavior.KeepTeam)
        {
            oldTeam = 0ul;
        }

        player.TeamSelectorData.SelectedTeam = oldTeam;
        player.Player.quests.leaveGroup(true);

        if (!TeamManager.LobbyZone.IsInside(player.Player.transform.position) || player.IsInVehicle)
        {
            player.Player.movement.forceRemoveFromVehicle();
            player.Player.teleportToLocationUnsafe(TeamManager.LobbySpawn, TeamManager.LobbySpawnAngle);
        }
        player.ModalNeeded = true;

        if (!options)
        {
            player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Modal | EPluginWidgetFlags.ForceBlur);
            player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.Default);
        }

        ClearAllUI(player);

        SendSelectionMenu(player, options, oldTeam);
        
        OnPlayerSelecting?.Invoke(player);
    }
    private static void ClearAllUI(UCPlayer player)
    {
        player.HasUIHidden = true;
        Data.HideAllUI(player);
    }
    private void JoinTeam(UCPlayer player, ulong team)
    {
        if (team is not 1 and not 2) return;

        if (player.TeamSelectorData is not null)
            player.TeamSelectorData.IsSelecting = false;
        if (TeamManager.JoinTeam(player, team, announce: true, teleport: true))
        {
            JoinUI.ClearFromPlayer(player.Connection);
            player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.Modal | EPluginWidgetFlags.ForceBlur);
            player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Default);
            player.ModalNeeded = false;
            player.HasUIHidden = false;
            TeamManager.TeleportToMain(player);
            CooldownManager.StartCooldown(player, CooldownType.ChangeTeams, TeamManager.TeamSwitchCooldown);
            ToastMessage.QueueMessage(player, new ToastMessage(string.Empty, Data.Gamemode.DisplayName, ToastMessageSeverity.Big));

            ActionLog.Add(ActionLogType.ChangeGroupWithUI, "GROUP: " + TeamManager.TranslateName(team, 0).ToUpper(), player.Steam64);

            if (Data.Gamemode is IJoinedTeamListener tl)
                tl.OnJoinTeam(player, team);

            OnPlayerSelected?.Invoke(player);
        }
        else
        {
            L.LogError("Failed to assign group " + team.ToString(Data.LocalLocale) + " to " + player.CharacterName + ".", method: "TEAM SELECTOR");
            ResetState(player);
        }
    }
    public static void OpenOptionsMenu(UCPlayer player)
    {
        if (player.TeamSelectorData is not { IsSelecting: true })
        {
            player.TeamSelectorData ??= new TeamSelectorData(false);
            player.ModalNeeded = true;
            player.TeamSelectorData.IsOptionsOnly = true;
            player.HasUIHidden = true;
            ClearAllUI(player);
            JoinUI.SendToPlayer(player.Connection);
            JoinUI.LogicTeamSettings.SetVisibility(player.Connection, false);
            JoinUI.LabelOptionsBack.SetText(player.Connection, T.TeamsUIConfirm.Translate(player));
            player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Modal | EPluginWidgetFlags.ForceBlur);
            player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.Default);
        }
        JoinUI.LogicOpenOptionsMenu.SetVisibility(player.Connection, true);
    }
    private static void SendSelectionMenu(UCPlayer player, bool optionsAlreadyOpen, ulong team)
    {
        ITransportConnection c = player.Connection;
        if (!optionsAlreadyOpen)
            JoinUI.SendToPlayer(c);
        else
            JoinUI.LogicOpenTeamMenu.SetVisibility(c, true);
        JoinUI.TeamsTitle.SetText(c, T.TeamsUIHeader.Translate(player));
        JoinUI.TeamNames[0].SetText(c, TeamManager.Team1Faction.GetShortName(player.Language));
        JoinUI.TeamNames[1].SetText(c, TeamManager.Team2Faction.GetShortName(player.Language));
        string status = T.TeamsUIClickToJoin.Translate(player);
        JoinUI.TeamStatus[0].SetText(c, status);
        JoinUI.TeamStatus[1].SetText(c, status);

        JoinUI.LabelConfirm.SetText(c, T.TeamsUIConfirm.Translate(player));
        JoinUI.LabelOptionsBack.SetText(player.Connection, T.TeamsUIBack.Translate(player));

        if (Data.Gamemode == null || Data.Gamemode.State is not State.Staging and not State.Active)
            JoinUI.LogicConfirmToggle.SetVisibility(c, false);

        if (!string.IsNullOrEmpty(TeamManager.Team1Faction.FlagImageURL))
            JoinUI.TeamFlags[0].SetImage(c, TeamManager.Team1Faction.FlagImageURL);

        if (!string.IsNullOrEmpty(TeamManager.Team2Faction.FlagImageURL))
            JoinUI.TeamFlags[1].SetImage(c, TeamManager.Team2Faction.FlagImageURL);

        int t1Ct = 0, t2Ct = 0;
        foreach (UCPlayer pl in PlayerManager.OnlinePlayers.OrderBy(pl => pl.TeamSelectorData is not null && pl.TeamSelectorData.IsSelecting))
        {
            bool sel = pl.TeamSelectorData is not null && pl.TeamSelectorData.IsSelecting;
            ulong team2 = sel ? pl.TeamSelectorData!.SelectedTeam : pl.GetTeam();
            if (team2 is not 1 and not 2) continue;
            string text = player.Steam64 == pl.Steam64 ? pl.CharacterName.Colorize(SelfHex) : (sel ? pl.CharacterName.Colorize(SelectedHex) : pl.CharacterName);
            if (team2 is 1)
                JoinUI.TeamPlayers[0][t1Ct++].SetText(c, text);
            else
                JoinUI.TeamPlayers[1][t2Ct++].SetText(c, text);
        }
        if (t1Ct > 0)
            JoinUI.TeamPlayers[0][t1Ct - 1].SetVisibility(c, true);
        if (t2Ct > 0)
            JoinUI.TeamPlayers[1][t2Ct - 1].SetVisibility(c, true);
        if (TeamSelectorUI.PlayerListCount > t1Ct)
            JoinUI.TeamPlayers[0][t1Ct].SetVisibility(c, false);
        if (TeamSelectorUI.PlayerListCount > t2Ct)
            JoinUI.TeamPlayers[1][t2Ct].SetVisibility(c, false);

        SetButtonState(player, 1, team == 1 || CheckTeam(1, team, t1Ct, t2Ct));
        SetButtonState(player, 2, team == 2 || CheckTeam(2, team, t1Ct, t2Ct));

        JoinUI.TeamCounts[0].SetText(c, t1Ct.ToString(Data.LocalLocale));
        JoinUI.TeamCounts[1].SetText(c, t2Ct.ToString(Data.LocalLocale));

        JoinUI.OptionsIMGUICheckToggle.SetVisibility(c, player.Save.IMGUI);
        JoinUI.LogicTeamSettings.SetVisibility(player.Connection, true);
    }
    private static void SetButtonState(UCPlayer player, ulong team, bool hasSpace)
    {
        if (team is not 1ul and not 2ul) return;
        ITransportConnection c = player.Connection;
        JoinUI.SetTeamEnabled(c, team, hasSpace);
        JoinUI.TeamStatus[team - 1].SetText(c, (hasSpace ? T.TeamsUIClickToJoin : T.TeamsUIFull).Translate(player));
    }
    
    private static void UpdateList()
    {
        int t1Ct = 0, t2Ct = 0;
        foreach (UCPlayer pl in PlayerManager.OnlinePlayers.OrderBy(pl => pl.TeamSelectorData is not null && pl.TeamSelectorData.IsSelecting))
        {
            bool sel = pl.TeamSelectorData is not null && pl.TeamSelectorData.IsSelecting;
            ulong team = sel ? pl.TeamSelectorData!.SelectedTeam : pl.GetTeam();
            if (team is not 1 and not 2)
                continue;
            if (team == 1 && t1Ct >= TeamSelectorUI.PlayerListCount || team == 2 && t2Ct >= TeamSelectorUI.PlayerListCount)
            {
                if (team == 1)
                    ++t1Ct;
                else
                    ++t2Ct;
                continue;
            }
            string text = sel ? pl.CharacterName.ColorizeTMPro(SelectedHex) : pl.CharacterName;
            UnturnedLabel lbl = team is 1 ? JoinUI.TeamPlayers[0][t1Ct++] : JoinUI.TeamPlayers[1][t2Ct++];
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
            {
                UCPlayer pl2 = PlayerManager.OnlinePlayers[i];
                if (pl2.TeamSelectorData is { IsSelecting: true })
                    lbl.SetText(pl2.Connection, pl.Steam64 == pl2.Steam64 ? pl.CharacterName.Colorize(SelfHex) : text);
            }
        }

        // will cascade with animations
        if (t1Ct > 0 || t2Ct > 0)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
            {
                UCPlayer pl2 = PlayerManager.OnlinePlayers[i];
                if (pl2.TeamSelectorData is { IsSelecting: true })
                {
                    if (t1Ct > 0)
                        JoinUI.TeamPlayers[0][t1Ct - 1].SetVisibility(pl2.Connection, true);
                    if (t2Ct > 0)
                        JoinUI.TeamPlayers[1][t2Ct - 1].SetVisibility(pl2.Connection, true);
                }
            }
        }
        if (TeamSelectorUI.PlayerListCount > t1Ct)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
            {
                UCPlayer pl2 = PlayerManager.OnlinePlayers[i];
                if (pl2.TeamSelectorData is { IsSelecting: true })
                    JoinUI.TeamPlayers[0][t1Ct].SetVisibility(pl2.Connection, false);
            }
        }
        if (TeamSelectorUI.PlayerListCount > t2Ct)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
            {
                UCPlayer pl2 = PlayerManager.OnlinePlayers[i];
                if (pl2.TeamSelectorData is { IsSelecting: true })
                    JoinUI.TeamPlayers[1][t2Ct].SetVisibility(pl2.Connection, false);
            }
        }

        bool b1 = CheckTeam(1, 0, t1Ct, t2Ct),
             b2 = CheckTeam(2, 0, t1Ct, t2Ct),
             b3 = CheckTeam(1, 2, t1Ct, t2Ct),
             b4 = CheckTeam(2, 1, t1Ct, t2Ct);
        
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            if (pl.TeamSelectorData is not null && pl.TeamSelectorData.IsSelecting)
            {
                ITransportConnection c = pl.Connection;
                JoinUI.TeamCounts[0].SetText(c, t1Ct.ToString(pl.Culture));
                JoinUI.TeamCounts[1].SetText(c, t2Ct.ToString(pl.Culture));
                if (pl.TeamSelectorData.SelectedTeam is 1)
                {
                    JoinUI.LogicTeamSelectedToggle[0].SetVisibility(c, true);
                    JoinUI.LogicTeamSelectedToggle[1].SetVisibility(c, false);
                    JoinUI.LogicTeamToggle[0].SetVisibility(c, true);
                    JoinUI.LogicTeamToggle[1].SetVisibility(c, b4);
                }
                else if (pl.TeamSelectorData.SelectedTeam is 2)
                {
                    JoinUI.LogicTeamSelectedToggle[0].SetVisibility(c, false);
                    JoinUI.LogicTeamSelectedToggle[1].SetVisibility(c, true);
                    JoinUI.LogicTeamToggle[0].SetVisibility(c, b3);
                    JoinUI.LogicTeamToggle[1].SetVisibility(c, true);
                }
                else
                {
                    JoinUI.LogicTeamSelectedToggle[0].SetVisibility(c, false);
                    JoinUI.LogicTeamSelectedToggle[1].SetVisibility(c, false);
                    JoinUI.LogicTeamToggle[0].SetVisibility(c, b1);
                    JoinUI.LogicTeamToggle[1].SetVisibility(c, b2);
                }
            }
        }
    }
    private static void GetTeamCounts(out int t1, out int t2)
    {
        t1 = 0;
        t2 = 0;
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            if (pl.TeamSelectorData is not { IsSelecting: true, SelectedTeam: 1ul or 2ul })
            {
                ulong team2 = pl.Player.quests.groupID.m_SteamID;
                if (team2 == TeamManager.Team1ID)
                    ++t1;
                else if (team2 == TeamManager.Team2ID)
                    ++t2;
            }
            else if (pl.TeamSelectorData.SelectedTeam == 1ul)
                ++t1;
            else if (pl.TeamSelectorData.SelectedTeam == 2ul)
                ++t2;
        }
    }
    private static bool CheckTeam(ulong team, ulong toBeLeft, int t1, int t2)
    {
        if (toBeLeft is 1)
        {
            --t1;
        }
        else if (toBeLeft is 2)
        {
            --t2;
        }

        return TeamManager.CanJoinTeam(team, t1, t2);
    }
}

public class TeamSelectorData
{
    public bool IsSelecting;
    public bool IsOptionsOnly;
    public ulong SelectedTeam;
    public Coroutine? JoiningCoroutine;
    public TeamSelectorData(bool isInLobby)
    {
        IsSelecting = isInLobby;
    }
}

public delegate void PlayerDelegate(UCPlayer player);