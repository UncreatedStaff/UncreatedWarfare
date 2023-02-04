using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Gamemodes;

namespace Uncreated.Warfare.Teams;
public class TeamSelectorUI : UnturnedUI
{
    public const int PlayerListCount = 30;

    /* LOGIC */
    public readonly UnturnedUIElement LogicTeamSettings = new UnturnedUIElement("anim_logic_team_stuff");
    public readonly UnturnedUIElement LogicConfirmToggle = new UnturnedUIElement("anim_logic_btn_confirm");
    public readonly UnturnedUIElement LogicOpenTeamMenu = new UnturnedUIElement("anim_logic_page_teams");
    public readonly UnturnedUIElement LogicOpenOptionsMenu = new UnturnedUIElement("anim_logic_page_options");
    public readonly UnturnedUIElement[] LogicTeamToggle = UnturnedUIElement.GetPattern("anim_logic_team_{0}", 2, 1);
    public readonly UnturnedUIElement[] LogicTeamSelectedToggle = UnturnedUIElement.GetPattern("anim_logic_team_{0}_selected", 2, 1);

    public readonly UnturnedButton ButtonConfirm = new UnturnedButton("btn_confirm");
    public readonly UnturnedLabel LabelConfirm = new UnturnedLabel("lbl_confirm");
    public readonly UnturnedButton ButtonOptionsBack = new UnturnedButton("btn_options_close");
    public readonly UnturnedLabel LabelOptionsBack = new UnturnedLabel("lbl_options_close");
    public readonly UnturnedLabel TeamsTitle = new UnturnedLabel("lbl_title");

    /* TEAM SELECTORS */
    public readonly UnturnedButton[] TeamButtons = UnturnedButton.GetPattern("btn_{0}", 2, 1);
    public readonly UnturnedLabel[] TeamNames = UnturnedLabel.GetPattern("lbl_name_{0}", 2, 1);
    public readonly UnturnedImage[] TeamFlags = UnturnedImage.GetPattern("img_flag_{0}", 2, 1);
    public readonly UnturnedLabel[] TeamCounts = UnturnedLabel.GetPattern("lbl_ct_{0}", 2, 1);
    public readonly UnturnedLabel[] TeamStatus = UnturnedLabel.GetPattern("lbl_status_{0}", 2, 1);

    public readonly UnturnedLabel[] TeamPlayersTeam1 = UnturnedLabel.GetPattern("pl_1_{0}", PlayerListCount, 1);
    public readonly UnturnedLabel[] TeamPlayersTeam2 = UnturnedLabel.GetPattern("pl_2_{0}", PlayerListCount, 1);

    public readonly UnturnedLabel[][] TeamPlayers;

    /* OPTIONS */
    public readonly UnturnedUIElement OptionsIMGUICheckToggle = new UnturnedUIElement("chk_imgui_btn");
    public readonly UnturnedButton OptionsIMGUICheckButton = new UnturnedButton("chk_imgui_background");

    public event Action<UCPlayer, ulong>? OnTeamButtonClicked;
    public event Action<UCPlayer>? OnConfirmClicked;
    public event Action<UCPlayer>? OnOptionsBackClicked;
    public TeamSelectorUI() : base(29000, Gamemode.Config.UITeamSelector, true, false)
    {
        TeamPlayers = new UnturnedLabel[][]
        {
            TeamPlayersTeam1,
            TeamPlayersTeam2
        };
        OptionsIMGUICheckButton.OnClicked += OnIMGUIToggle;
        TeamButtons[0].OnClicked += OnTeamClicked;
        TeamButtons[1].OnClicked += OnTeamClicked;
        ButtonConfirm.OnClicked += OnConfirm;
        ButtonOptionsBack.OnClicked += OnOptionsBack;
    }

    private void OnOptionsBack(UnturnedButton button, Player player)
    {
        if (OnOptionsBackClicked == null || UCPlayer.FromPlayer(player) is not { } ucplayer)
            return;
        OnOptionsBackClicked?.Invoke(ucplayer);
    }

    public void SetTeamEnabled(ITransportConnection c, ulong team, bool value)
    {
        if (team is 1 or 2) LogicTeamToggle[team - 1].SetVisibility(c, value);
    }
    private void OnIMGUIToggle(UnturnedButton button, Player player)
    {
        if (PlayerSave.TryReadSaveFile(player.channel.owner.playerID.steamID.m_SteamID, out PlayerSave save))
        {
            save.IMGUI = !save.IMGUI;
            L.LogDebug($"IMGUI: {save.IMGUI}");
            OptionsIMGUICheckToggle.SetVisibility(player.channel.owner.transportConnection, save.IMGUI);
            PlayerSave.WriteToSaveFile(save);
        }
    }
    private void OnTeamClicked(UnturnedButton button, Player player)
    {
        if (OnTeamButtonClicked == null) return;
        ulong team = (ulong)(1 + Array.IndexOf(TeamButtons, button));
        if (team is not 1 and not 2 || UCPlayer.FromPlayer(player) is not { } ucplayer)
            return;
        L.LogDebug($"Team clicked: {team}");
        OnTeamButtonClicked?.Invoke(ucplayer, team);
    }
    private void OnConfirm(UnturnedButton button, Player player)
    {
        if (OnConfirmClicked == null || UCPlayer.FromPlayer(player) is not { } ucplayer)
            return;
        L.LogDebug($"Confirm clicked");
        OnConfirmClicked?.Invoke(ucplayer);
    }
}
