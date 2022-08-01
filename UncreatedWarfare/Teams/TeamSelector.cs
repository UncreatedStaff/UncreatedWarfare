using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Uncreated.Framework.UI;
using Uncreated.Players;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Singletons;
using UnityEngine;

namespace Uncreated.Warfare.Teams;

public class TeamSelector : BaseSingletonComponent, IPlayerAsyncInitListener
{
    public static TeamSelector Instance;
    public static readonly JoinUI JoinUI = new JoinUI();
    public static event PlayerDelegate OnPlayerSelecting;
    public static event PlayerDelegate OnPlayerSelected;
    private const string SELECTED_HEX = "afffc9";
    private const string SELF_HEX = "9bf3f3";
    public override void Load()
    {
        Instance = this;
        JoinUI.Team1Button.OnClicked += OnTeam1Clicked;
        JoinUI.Team2Button.OnClicked += OnTeam2Clicked;
        JoinUI.ConfirmButton.OnClicked += OnConfirmClicked;
        EventDispatcher.OnPlayerLeaving += OnPlayerDisconnect;
    }
    public override void Unload()
    {
        EventDispatcher.OnPlayerLeaving -= OnPlayerDisconnect;
        JoinUI.ConfirmButton.OnClicked -= OnConfirmClicked;
        JoinUI.Team2Button.OnClicked -= OnTeam2Clicked;
        JoinUI.Team1Button.OnClicked -= OnTeam1Clicked;
        Instance = null!;
    }
    private void OnPlayerDisconnect(PlayerEvent e)
    {
        if (e.Player.TeamSelectorData is not null)
        {
            if (e.Player.TeamSelectorData.JoiningCoroutine != null)
            {
                StopCoroutine(e.Player.TeamSelectorData.JoiningCoroutine);
                e.Player.TeamSelectorData.JoiningCoroutine = null;
            }

            UpdateList();
        }
    }
    public void OnAsyncInitComplete(UCPlayer player)
    {
        bool t1Donor = false, t2Donor = false;
        if (player.IsOtherDonator)
        {
            t1Donor = true;
            t2Donor = true;
        }
        else if (player.AccessibleKits is not null)
            CheckAccess(player.AccessibleKits, ref t1Donor, ref t2Donor);
        if (player.TeamSelectorData is null)
        {
            player.TeamSelectorData = new TeamSelectorData(false, t1Donor, t2Donor);
        }
        else
        {
            if (player.TeamSelectorData.IsTeam1Donator != t1Donor || player.TeamSelectorData.IsTeam2Donator != t2Donor)
            {
                player.TeamSelectorData.IsTeam1Donator = t1Donor;
                player.TeamSelectorData.IsTeam2Donator = t2Donor;
                if (player.TeamSelectorData.IsSelecting)
                    OnDonorsChanged(player);
            }
        }

        JoinSelectionMenu(player);
    }
    private void OnTeam1Clicked(UnturnedButton button, Player player)
    {
        UCPlayer? ucplayer = UCPlayer.FromPlayer(player);
        if (ucplayer is null || ucplayer.TeamSelectorData is null || !ucplayer.TeamSelectorData.IsSelecting) return;

        ITransportConnection c = ucplayer.Connection;

        if (CheckTeam(1, ucplayer.TeamSelectorData.SelectedTeam))
        {
            if (ucplayer.TeamSelectorData.SelectedTeam == 2)
            {
                JoinUI.Team2Highlight.SetVisibility(c, false);
                JoinUI.Team2Select.SetText(c, Localization.Translate(ucplayer.TeamSelectorData!.IsTeam2Donator ? "team_ui_click_to_join_donor" : "team_ui_click_to_join", player));
            }
            ucplayer.TeamSelectorData.SelectedTeam = 1;
            UpdateList();
            JoinUI.Team1Highlight.SetVisibility(c, true);
            JoinUI.Team1Select.SetText(c, Localization.Translate(ucplayer.TeamSelectorData.IsTeam1Donator ? "team_ui_joined_donor" : "team_ui_joined", ucplayer));
        }
    }
    private void OnTeam2Clicked(UnturnedButton button, Player player)
    {
        UCPlayer? ucplayer = UCPlayer.FromPlayer(player);
        if (ucplayer is null || ucplayer.TeamSelectorData is null || !ucplayer.TeamSelectorData.IsSelecting) return;

        ITransportConnection c = ucplayer.Connection;

        if (CheckTeam(2, ucplayer.TeamSelectorData.SelectedTeam))
        {
            if (ucplayer.TeamSelectorData.SelectedTeam == 1)
            {
                JoinUI.Team1Highlight.SetVisibility(c, false);
                JoinUI.Team1Select.SetText(c, Localization.Translate(ucplayer.TeamSelectorData!.IsTeam1Donator ? "team_ui_click_to_join_donor" : "team_ui_click_to_join", player));
            }
            ucplayer.TeamSelectorData.SelectedTeam = 2;
            UpdateList();
            JoinUI.Team2Highlight.SetVisibility(c, true);
            JoinUI.Team2Select.SetText(c, Localization.Translate(ucplayer.TeamSelectorData.IsTeam2Donator ? "team_ui_joined_donor" : "team_ui_joined", ucplayer));
        }
    }
    private void OnConfirmClicked(UnturnedButton button, Player player)
    {
        UCPlayer? ucplayer = UCPlayer.FromPlayer(player);
        if (ucplayer is null || ucplayer.TeamSelectorData is null || !ucplayer.TeamSelectorData.IsSelecting ||
            ucplayer.TeamSelectorData.SelectedTeam is not 1 and not 2 ||
            ucplayer.TeamSelectorData.JoiningCoroutine != null)
            return;
        
        ucplayer.TeamSelectorData.JoiningCoroutine = StartCoroutine(JoinCoroutine(ucplayer, ucplayer.TeamSelectorData.SelectedTeam));
    }
    private IEnumerator JoinCoroutine(UCPlayer player, ulong targetTeam)
    {
        ITransportConnection c = player.Connection;
        JoinUI.ConfirmText.SetText(c, Localization.Translate("team_ui_joining", player));
        yield return new WaitForSeconds(1f);

        if (!player.IsOnline) yield break;

        if (player.TeamSelectorData is not null)
            player.TeamSelectorData.JoiningCoroutine = null;

        JoinTeam(player, targetTeam);
    }

    public void ForceUpdate() => UpdateList();
    public bool IsSelecting(UCPlayer player) => player.TeamSelectorData is not null && player.TeamSelectorData.IsSelecting;
    public void JoinSelectionMenu(UCPlayer player)
    {
        if (player.TeamSelectorData is null)
        {
            bool t1Donor = false, t2Donor = false;
            if (player.IsOtherDonator)
            {
                t1Donor = true;
                t2Donor = true;
            }
            else
            if (player.HasDownloadedKits && player.AccessibleKits is not null)
                CheckAccess(player.AccessibleKits, ref t1Donor, ref t2Donor);
            player.TeamSelectorData = new TeamSelectorData(true, t1Donor, t2Donor);
        }
        else
        {
            player.TeamSelectorData.IsSelecting = true;
            player.TeamSelectorData.SelectedTeam = 0;
        }

        ulong grpOld = player.Player.quests.groupID.m_SteamID;
        player.Player.quests.leaveGroup(true);
        EventDispatcher.InvokeOnGroupChanged(player, grpOld, 0);

        if (!TeamManager.LobbyZone.IsInside(player.Player.transform.position))
            player.Player.teleportToLocationUnsafe(TeamManager.LobbySpawn, TeamManager.LobbySpawnAngle);

        player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Modal | EPluginWidgetFlags.ForceBlur);

        player.HasUIHidden = true;
        EffectManager.askEffectClearAll();

        SendSelectionMenu(player);

        OnPlayerSelecting?.Invoke(player);
    }
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
    private void JoinTeam(UCPlayer player, ulong team)
    {
        if (team is not 1 and not 2) return;

        if (player.TeamSelectorData is not null)
            player.TeamSelectorData.IsSelecting = false;

        JoinUI.ClearFromPlayer(player.Connection);

        GroupInfo groupInfo = GroupManager.getGroupInfo(new CSteamID(TeamManager.GetGroupID(team)));
        if (groupInfo is not null && player.Player.quests.ServerAssignToGroup(groupInfo.groupID, EPlayerGroupRank.MEMBER, true))
        {
            GroupManager.save();

            EventDispatcher.InvokeOnGroupChanged(player, 0, groupInfo.groupID.m_SteamID);

            player.HasUIHidden = false;

            UpdateList();

            ActionLogger.Add(EActionLogType.CHANGE_GROUP_WITH_UI, "GROUP: " + TeamManager.TranslateName(team, 0).ToUpper(), player.Steam64);

            player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.Modal | EPluginWidgetFlags.ForceBlur);

            player.Player.teleportToLocationUnsafe(
                team is 1 ? TeamManager.Team1Main.Center3D : TeamManager.Team2Main.Center3D, 
                team is 1 ? TeamManager.Team1SpawnAngle    : TeamManager.Team2SpawnAngle);

            UpdateList();

            string clr = TeamManager.GetTeamHexColor(team);
            foreach (LanguageSet set in Localization.EnumerateLanguageSetsExclude(player.Steam64))
                Chat.Broadcast(set, "teams_join_announce", player.CharacterName, TeamManager.TranslateName(team, set.Language), clr);

            CooldownManager.StartCooldown(player, ECooldownType.CHANGE_TEAMS, TeamManager.TeamSwitchCooldown);
            ToastMessage.QueueMessage(player, new ToastMessage(string.Empty, Data.Gamemode.DisplayName, EToastMessageSeverity.BIG));

            if (Data.Gamemode is IJoinedTeamListener tl)
                tl.OnJoinTeam(player, team);

            OnPlayerSelected?.Invoke(player);
        }
        else
        {
            L.LogError("Failed to assign group " + team.ToString(Data.Locale) + " to " + player.CharacterName + ".", method: "TEAM SELECTOR");
            SendSelectionMenu(player);
        }
    }

    private void SendSelectionMenu(UCPlayer player)
    {
        ITransportConnection c = player.Connection;
        JoinUI.SendToPlayer(c);

        JoinUI.Heading.SetText(c, Localization.Translate("team_ui_header", player));
        JoinUI.Team1Name.SetText(c, F.Colorize(TeamManager.Team1Name, TeamManager.Team1ColorHex));
        JoinUI.Team2Name.SetText(c, F.Colorize(TeamManager.Team2Name, TeamManager.Team2ColorHex));

        OnDonorsChanged(player);

        JoinUI.ConfirmText.SetText(c, Localization.Translate("team_ui_confirm", player));

        JoinUI.Team1Image.SetImage(c, TeamManager.Team1Faction.FlagImageURL);
        JoinUI.Team2Image.SetImage(c, TeamManager.Team2Faction.FlagImageURL);

        int t1ct = 0, t2ct = 0;
        foreach (UCPlayer pl in PlayerManager.OnlinePlayers.OrderBy(pl => pl.TeamSelectorData is not null && pl.TeamSelectorData.IsSelecting))
        {
            bool sel = pl.TeamSelectorData is not null && pl.TeamSelectorData.IsSelecting;
            ulong team = sel ? pl.TeamSelectorData!.SelectedTeam : pl.GetTeam();
            if (team is not 1 and not 2) continue;
            string text = player.Steam64 == pl.Steam64 ? F.Colorize(pl.CharacterName, SELF_HEX) : (sel ? F.Colorize(pl.CharacterName, SELECTED_HEX) : pl.CharacterName);
            if (team is 1)
                JoinUI.Team1Players[t1ct++].SetText(c, text);
            else
                JoinUI.Team2Players[t2ct++].SetText(c, text);
        }

        SetButtonState(player, 1, CheckTeam(1, 0, t1ct, t2ct));
        SetButtonState(player, 2, CheckTeam(2, 0, t1ct, t2ct));

        JoinUI.Team1PlayerCount.SetText(c, t1ct.ToString(Data.Locale));
        JoinUI.Team2PlayerCount.SetText(c, t2ct.ToString(Data.Locale));
    }
    private void SetButtonState(UCPlayer player, ulong team, bool hasSpace)
    {
        ITransportConnection c = player.Connection;
        if (team is 1)
        {
            if (player.TeamSelectorData!.IsTeam1Donator)
                hasSpace = true;
            JoinUI.Team1Button.SetVisibility(c, hasSpace);
            JoinUI.Team1Select.SetText(c,
                Localization.Translate(hasSpace
                    ? (player.TeamSelectorData!.IsTeam1Donator
                        ? "team_ui_click_to_join_donor"
                        : "team_ui_click_to_join") : "team_ui_full", player));
        }
        else if (team is 2)
        {
            if (player.TeamSelectorData!.IsTeam2Donator)
                hasSpace = true;
            JoinUI.Team2Button.SetVisibility(c, hasSpace);
            JoinUI.Team2Select.SetText(c,
                 Localization.Translate(hasSpace
                     ? (player.TeamSelectorData!.IsTeam2Donator
                        ? "team_ui_click_to_join_donor"
                        : "team_ui_click_to_join") : "team_ui_full", player));
        }
    }

    private int _t1Amt = -1;
    private int _t2Amt = -1;
    private void UpdateList()
    {
        int t1ct = 0, t2ct = 0;
        foreach (UCPlayer pl in PlayerManager.OnlinePlayers.OrderBy(pl => pl.TeamSelectorData is not null && pl.TeamSelectorData.IsSelecting))
        {
            bool sel = pl.TeamSelectorData is not null && pl.TeamSelectorData.IsSelecting;
            ulong team = sel ? pl.TeamSelectorData!.SelectedTeam : pl.GetTeam();
            if (team is not 1 and not 2) continue;
            string text = sel ? F.Colorize(pl.CharacterName, SELECTED_HEX) : pl.CharacterName;
            UnturnedLabel lbl = team is 1 ? JoinUI.Team1Players[t1ct++] : JoinUI.Team2Players[t2ct++];
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
            {
                UCPlayer pl2 = PlayerManager.OnlinePlayers[i];
                if (pl2.TeamSelectorData is not null && pl2.TeamSelectorData.IsSelecting)
                    lbl.SetText(pl2.Connection, pl.Steam64 == pl2.Steam64 ? F.Colorize(pl.CharacterName, SELF_HEX) : text);
            }
        }

        for (int j = 0; j < PlayerManager.OnlinePlayers.Count; ++j)
        {
            ITransportConnection c = PlayerManager.OnlinePlayers[j].Connection;
            for (int i = t1ct; i < _t1Amt; ++i)
                JoinUI.Team1Players[i].SetText(c, string.Empty);
            for (int i = t2ct; i < _t2Amt; ++i)
                JoinUI.Team2Players[i].SetText(c, string.Empty);
        }

        if (_t1Amt < t1ct)
            _t1Amt = t1ct;

        if (_t2Amt < t2ct)
            _t2Amt = t2ct;

        bool b1 = CheckTeam(1, 0, t1ct, t2ct),
             b2 = CheckTeam(2, 0, t1ct, t2ct),
             b3 = CheckTeam(1, 2, t1ct, t2ct),
             b4 = CheckTeam(2, 1, t1ct, t2ct);
        
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            if (pl.TeamSelectorData is not null && pl.TeamSelectorData.IsSelecting)
            {
                ITransportConnection c = pl.Connection;
                JoinUI.Team1PlayerCount.SetText(c, t1ct.ToString(Data.Locale));
                JoinUI.Team2PlayerCount.SetText(c, t2ct.ToString(Data.Locale));
                if (pl.TeamSelectorData.SelectedTeam is 1)
                {
                    JoinUI.Team1Button.SetVisibility(c, false);
                    JoinUI.Team2Button.SetVisibility(c, b4);
                }
                else if (pl.TeamSelectorData.SelectedTeam is 2)
                {
                    JoinUI.Team1Button.SetVisibility(c, b3);
                    JoinUI.Team2Button.SetVisibility(c, false);
                }
                else
                {
                    JoinUI.Team1Button.SetVisibility(c, b1);
                    JoinUI.Team2Button.SetVisibility(c, b2);
                }
            }
        }
    }
    public void OnKitsUpdated(UCPlayer player)
    {
        if (player.TeamSelectorData is not null && player.TeamSelectorData.IsSelecting)
            OnAsyncInitComplete(player);
    }
    private void OnDonorsChanged(UCPlayer player)
    {
        ITransportConnection c = player.Connection;
        JoinUI.Team1Select.SetText(c, Localization.Translate(player.TeamSelectorData!.IsTeam1Donator ? "team_ui_click_to_join_donor" : "team_ui_click_to_join", player));
        JoinUI.Team2Select.SetText(c, Localization.Translate(player.TeamSelectorData!.IsTeam2Donator ? "team_ui_click_to_join_donor" : "team_ui_click_to_join", player));

        GetTeamCounts(out int t1ct, out int t2ct);
        if (player.TeamSelectorData!.SelectedTeam is 1)
        {
            SetButtonState(player, 2, CheckTeam(2, 1, t1ct, t2ct));
        }
        else if (player.TeamSelectorData.SelectedTeam is 2)
        {
            SetButtonState(player, 1, CheckTeam(1, 2, t1ct, t2ct));
        }
        else
        {
            SetButtonState(player, 1, CheckTeam(1, 0, t1ct, t2ct));
            SetButtonState(player, 2, CheckTeam(2, 0, t1ct, t2ct));
        }
    }
    private static void CheckAccess(List<Kit> kits, ref bool t1Donor, ref bool t2Donor)
    {
        for (int i = 0; i < kits.Count; ++i)
        {
            Kit kit = kits[i];
            if (kit.IsPremium || kit.IsLoadout)
            {
                if (kit.Team == 0)
                {
                    t1Donor = true;
                    t2Donor = true;
                    break;
                }
                if (kit.Team == 1)
                {
                    t1Donor = true;
                    if (t2Donor) break;
                }
                if (kit.Team == 2)
                {
                    t2Donor = true;
                    if (t1Donor) break;
                }
            }
        }
    }
    private bool CheckTeam(ulong team, ulong toBeLeft)
    {
        if (team is not 1 and not 2) return false;
        GetTeamCounts(out int t1, out int t2);
        return CheckTeam(team, toBeLeft, t1, t2);
    }
    private void GetTeamCounts(out int t1, out int t2)
    {
        ulong team2;
        t1 = 0;
        t2 = 0;
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            if (pl.TeamSelectorData is null || !pl.TeamSelectorData.IsSelecting)
            {
                team2 = pl.GetTeam();
                if (team2 is 1)
                    ++t1;
                else if (team2 is 2)
                    ++t2;
            }
            else
            {
                if (pl.TeamSelectorData.SelectedTeam is 1)
                    ++t1;
                else if (pl.TeamSelectorData.SelectedTeam is 2)
                    ++t2;
            }
        }
    }
    private bool CheckTeam(ulong team, ulong toBeLeft, int t1, int t2)
    {
        if (toBeLeft is 1)
        {
            --t1;
            ++t2;
        }
        else if (toBeLeft is 2)
        {
            ++t1;
            --t2;
        }
        int maxDiff = Mathf.Max(2, Mathf.CeilToInt(Provider.clients.Count * 0.10f));
        if (t1 == t2)
            return true;
        if (team == 1 && t1 <= t2)
            return true;
        if (team == 2 && t2 <= t1)
            return true;
        

        if (team == 1 && t2 - maxDiff <= t1)
            return true;
        if (team == 2 && t1 - maxDiff <= t2)
            return true;

        return false;
    }
}

public class TeamSelectorData
{
    public bool IsSelecting;
    public bool IsTeam1Donator;
    public bool IsTeam2Donator;
    public ulong SelectedTeam;
    public Coroutine? JoiningCoroutine;
    public TeamSelectorData(bool isInLobby, bool t1Donor, bool t2Donor)
    {
        IsSelecting = isInLobby;
        IsTeam1Donator = t1Donor;
        IsTeam2Donator = t2Donor;
    }
}

public delegate void PlayerDelegate(UCPlayer player);