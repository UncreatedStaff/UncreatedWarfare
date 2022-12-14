using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System.Collections;
using System.Linq;
using Uncreated.Framework.UI;
using Uncreated.Players;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Singletons;
using UnityEngine;

namespace Uncreated.Warfare.Teams;

public class TeamSelector : BaseSingletonComponent, IPlayerPostInitListener
{
    public static TeamSelector Instance;
    public static readonly JoinUI JoinUI = new JoinUI();
    public static event PlayerDelegate OnPlayerSelecting;
    public static event PlayerDelegate OnPlayerSelected;
    private const string SelectedHex = "afffc9";
    private const string SelfHex = "9bf3f3";
    public override void Load()
    {
        Instance = this;
        JoinUI.Team1Button.OnClicked += OnTeam1Clicked;
        JoinUI.Team2Button.OnClicked += OnTeam2Clicked;
        JoinUI.ConfirmButton.OnClicked += OnConfirmClicked;
        EventDispatcher.PlayerLeaving += OnPlayerDisconnect;
    }
    public override void Unload()
    {
        EventDispatcher.PlayerLeaving -= OnPlayerDisconnect;
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
    public void OnPostPlayerInit(UCPlayer player)
    {
        player.TeamSelectorData ??= new TeamSelectorData(false);

        JoinSelectionMenu(player);
    }
    private void OnTeam1Clicked(UnturnedButton button, Player player)
    {
        UCPlayer? ucplayer = UCPlayer.FromPlayer(player);
        OnTeamClicked(ucplayer, 1);
    }
    private void OnTeam2Clicked(UnturnedButton button, Player player)
    {
        UCPlayer? ucplayer = UCPlayer.FromPlayer(player);
        OnTeamClicked(ucplayer, 2);
    }
    private void OnTeamClicked(UCPlayer? player, ulong team)
    {
        if (player is null || player.TeamSelectorData is null || !player.TeamSelectorData.IsSelecting) return;

        ITransportConnection c = player.Connection;

        if (CheckTeam(team, player.TeamSelectorData.SelectedTeam))
        {
            if (player.TeamSelectorData.SelectedTeam != 0 && player.TeamSelectorData.SelectedTeam != team)
            {
                (team == 2 ? JoinUI.Team1Highlight : JoinUI.Team2Highlight).SetVisibility(c, false);
                bool otherTeamHasRoom = CheckTeam(team == 1 ? 2ul : 1ul, team);
                (team == 2 ? JoinUI.Team1Select : JoinUI.Team2Select).SetText(c, (otherTeamHasRoom ? T.TeamsUIClickToJoin : T.TeamsUIFull).Translate(player));
            }
            player.TeamSelectorData.SelectedTeam = team;
            UpdateList();
            (team == 1 ? JoinUI.Team1Highlight : JoinUI.Team2Highlight).SetVisibility(c, true);
            (team == 1 ? JoinUI.Team1Select : JoinUI.Team2Select).SetText(c, T.TeamsUIClickToJoin.Translate(player));
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
        JoinUI.ConfirmText.SetText(c, T.TeamsUIJoining.Translate(player));
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
            player.TeamSelectorData = new TeamSelectorData(true);
        }
        else
        {
            if (player.TeamSelectorData.IsSelecting)
                return;
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
        Data.SendEffectClearAll.Invoke(ENetReliability.Reliable, player.Connection);

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

        GroupInfo? groupInfo = GroupManager.getGroupInfo(new CSteamID(TeamManager.GetGroupID(team)));
        if (groupInfo is not null && player.Player.quests.ServerAssignToGroup(groupInfo.groupID, EPlayerGroupRank.MEMBER, true))
        {
            JoinUI.ClearFromPlayer(player.Connection);

            GroupManager.save();

            EventDispatcher.InvokeOnGroupChanged(player, 0, groupInfo.groupID.m_SteamID);

            player.HasUIHidden = false;

            UpdateList();

            ActionLogger.Add(EActionLogType.CHANGE_GROUP_WITH_UI, "GROUP: " + TeamManager.TranslateName(team, 0).ToUpper(), player.Steam64);

            player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.Modal | EPluginWidgetFlags.ForceBlur);

            player.Player.teleportToLocationUnsafe(
                team is 1 ? TeamManager.Team1Main.Center3D : TeamManager.Team2Main.Center3D,
                team is 1 ? TeamManager.Team1SpawnAngle : TeamManager.Team2SpawnAngle);

            UpdateList();

            ulong id = player.Steam64;
            Chat.Broadcast(LanguageSet.Where(x => x.GetTeam() == team && x.Steam64 != id), T.TeamJoinAnnounce, TeamManager.GetFactionSafe(team)!, player);

            CooldownManager.StartCooldown(player, ECooldownType.CHANGE_TEAMS, TeamManager.TeamSwitchCooldown);
            ToastMessage.QueueMessage(player, new ToastMessage(string.Empty, Data.Gamemode.DisplayName, EToastMessageSeverity.BIG));

            if (Data.Gamemode is IJoinedTeamListener tl)
                tl.OnJoinTeam(player, team);

            OnPlayerSelected?.Invoke(player);
        }
        else
        {
            L.LogError("Failed to assign group " + team.ToString(Data.LocalLocale) + " to " + player.CharacterName + ".", method: "TEAM SELECTOR");
        }
    }

    private void SendSelectionMenu(UCPlayer player)
    {
        ITransportConnection c = player.Connection;
        JoinUI.SendToPlayer(c);

        JoinUI.Heading.SetText(c, T.TeamsUIHeader.Translate(player));
        JoinUI.Team1Name.SetText(c, TeamManager.Team1Name.Colorize(TeamManager.Team1ColorHex));
        JoinUI.Team2Name.SetText(c, TeamManager.Team2Name.Colorize(TeamManager.Team2ColorHex));

        OnDonorsChanged(player);

        JoinUI.ConfirmText.SetText(c, T.TeamsUIConfirm.Translate(player));

        JoinUI.Team1Image.SetImage(c, TeamManager.Team1Faction.FlagImageURL);
        JoinUI.Team2Image.SetImage(c, TeamManager.Team2Faction.FlagImageURL);

        int t1Ct = 0, t2Ct = 0;
        foreach (UCPlayer pl in PlayerManager.OnlinePlayers.OrderBy(pl => pl.TeamSelectorData is not null && pl.TeamSelectorData.IsSelecting))
        {
            bool sel = pl.TeamSelectorData is not null && pl.TeamSelectorData.IsSelecting;
            ulong team = sel ? pl.TeamSelectorData!.SelectedTeam : pl.GetTeam();
            if (team is not 1 and not 2) continue;
            string text = player.Steam64 == pl.Steam64 ? pl.CharacterName.Colorize(SelfHex) : (sel ? pl.CharacterName.Colorize(SelectedHex) : pl.CharacterName);
            if (team is 1)
                JoinUI.Team1Players[t1Ct++].SetText(c, text);
            else
                JoinUI.Team2Players[t2Ct++].SetText(c, text);
        }

        SetButtonState(player, 1, CheckTeam(1, 0, t1Ct, t2Ct));
        SetButtonState(player, 2, CheckTeam(2, 0, t1Ct, t2Ct));

        JoinUI.Team1PlayerCount.SetText(c, t1Ct.ToString(Data.LocalLocale));
        JoinUI.Team2PlayerCount.SetText(c, t2Ct.ToString(Data.LocalLocale));
    }
    private void SetButtonState(UCPlayer player, ulong team, bool hasSpace)
    {
        ITransportConnection c = player.Connection;
        if (team == 1)
        {
            JoinUI.Team1Button.SetVisibility(c, hasSpace);
            JoinUI.Team1Select.SetText(c, (hasSpace ? T.TeamsUIClickToJoin : T.TeamsUIFull).Translate(player));
        }
        else if (team == 2)
        {
            JoinUI.Team2Button.SetVisibility(c, hasSpace);
            JoinUI.Team2Select.SetText(c, (hasSpace ? T.TeamsUIClickToJoin : T.TeamsUIFull).Translate(player));
        }
    }

    private int _t1Amt = -1;
    private int _t2Amt = -1;
    private void UpdateList()
    {
        int t1Ct = 0, t2Ct = 0;
        foreach (UCPlayer pl in PlayerManager.OnlinePlayers.OrderBy(pl => pl.TeamSelectorData is not null && pl.TeamSelectorData.IsSelecting))
        {
            bool sel = pl.TeamSelectorData is not null && pl.TeamSelectorData.IsSelecting;
            ulong team = sel ? pl.TeamSelectorData!.SelectedTeam : pl.GetTeam();
            if (team is not 1 and not 2) continue;
            string text = sel ? pl.CharacterName.Colorize(SelectedHex) : pl.CharacterName;
            UnturnedLabel lbl = team is 1 ? JoinUI.Team1Players[t1Ct++] : JoinUI.Team2Players[t2Ct++];
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
            {
                UCPlayer pl2 = PlayerManager.OnlinePlayers[i];
                if (pl2.TeamSelectorData is not null && pl2.TeamSelectorData.IsSelecting)
                    lbl.SetText(pl2.Connection, pl.Steam64 == pl2.Steam64 ? pl.CharacterName.Colorize(SelfHex) : text);
            }
        }

        for (int j = 0; j < PlayerManager.OnlinePlayers.Count; ++j)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[j];
            if (pl.TeamSelectorData is null || !pl.TeamSelectorData.IsSelecting)
                continue;
            ITransportConnection c = pl.Connection;
            for (int i = t1Ct; i < _t1Amt; ++i)
                JoinUI.Team1Players[i].SetText(c, string.Empty);
            for (int i = t2Ct; i < _t2Amt; ++i)
                JoinUI.Team2Players[i].SetText(c, string.Empty);
        }

        if (_t1Amt < t1Ct)
            _t1Amt = t1Ct;

        if (_t2Amt < t2Ct)
            _t2Amt = t2Ct;

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
                JoinUI.Team1PlayerCount.SetText(c, t1Ct.ToString(Data.LocalLocale));
                JoinUI.Team2PlayerCount.SetText(c, t2Ct.ToString(Data.LocalLocale));
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
            OnPostPlayerInit(player);
    }
    private void OnDonorsChanged(UCPlayer player)
    {
        GetTeamCounts(out int t1Ct, out int t2Ct);
        if (player.TeamSelectorData!.SelectedTeam is 1)
        {
            SetButtonState(player, 2, CheckTeam(2, 1, t1Ct, t2Ct));
        }
        else if (player.TeamSelectorData.SelectedTeam is 2)
        {
            SetButtonState(player, 1, CheckTeam(1, 2, t1Ct, t2Ct));
        }
        else
        {
            SetButtonState(player, 1, CheckTeam(1, 0, t1Ct, t2Ct));
            SetButtonState(player, 2, CheckTeam(2, 0, t1Ct, t2Ct));
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
        t1 = 0;
        t2 = 0;
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            if (pl.TeamSelectorData is null || !pl.TeamSelectorData.IsSelecting)
            {
                ulong team2 = pl.GetTeam();
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

        if (team == 2)
            return t2 - maxDiff <= t1;
        if (team == 1)
            return t1 - maxDiff <= t2;

        return false;
    }
}

public class TeamSelectorData
{
    public bool IsSelecting;
    public ulong SelectedTeam;
    public Coroutine? JoiningCoroutine;
    public TeamSelectorData(bool isInLobby)
    {
        IsSelecting = isInLobby;
    }
}

public delegate void PlayerDelegate(UCPlayer player);