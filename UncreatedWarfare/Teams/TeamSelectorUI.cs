using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SDG.NetTransport;
using SDG.Unturned;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Gamemodes;

namespace Uncreated.Warfare.Teams;
public class TeamSelectorUI : UnturnedUI
{
    /* LOGIC */
    public readonly UnturnedUIElement LogicTeamSettings = new UnturnedUIElement("anim_logic_team_stuff");
    public readonly UnturnedUIElement LogicConfirmToggle = new UnturnedUIElement("anim_logic_btn_confirm");
    public readonly UnturnedUIElement[] LogicTeamToggle = UnturnedUIElement.GetPattern("anim_logic_team_{0}", 2, 1);
    public readonly UnturnedUIElement[] LogicTeamClearPlayers = UnturnedUIElement.GetPattern("anim_logic_clear_players_{0}", 2, 1);

    public readonly UnturnedButton ButtonConfirm = new UnturnedButton("btn_confirm");

    /* TEAM SELECTORS */
    public readonly UnturnedButton[] TeamButtons = UnturnedButton.GetPattern("btn_{0}", 2, 1);
    public readonly UnturnedLabel[] TeamNames = UnturnedLabel.GetPattern("lbl_name_{0}", 2, 1);
    public readonly UnturnedImage[] TeamFlags = UnturnedImage.GetPattern("img_flag_{0}", 2, 1);
    public readonly UnturnedLabel[] TeamCounts = UnturnedLabel.GetPattern("lbl_ct_{0}", 2, 1);
    public readonly UnturnedLabel[] TeamStatus = UnturnedLabel.GetPattern("lbl_status_{0}", 2, 1);

    public readonly UnturnedLabel[][] TeamPlayers =
    {
        UnturnedLabel.GetPattern("pl_1_{0}", 30, 1),
        UnturnedLabel.GetPattern("pl_2_{0}", 30, 1)
    };

    /* OPTIONS */
    public readonly UnturnedUIElement OptionsIMGUICheckToggle = new UnturnedUIElement("chk_imgui_btn");
    public readonly UnturnedButton OptionsIMGUICheckButton = new UnturnedButton("chk_imgui_background");

    public TeamSelectorUI() : base(29000, Gamemode.Config.UITeamSelector, true, false)
    {
        OptionsIMGUICheckButton.OnClicked += OnIMGUIToggle;
        TeamButtons[0].OnClicked += OnTeamClicked;
        TeamButtons[1].OnClicked += OnTeamClicked;
        ButtonConfirm.OnClicked += OnConfirm;
    }
    private void SetTeamEnabled(ITransportConnection c, ulong team, bool value)
    {
        if (team is 1 or 2) LogicTeamToggle[team - 1].SetVisibility(c, value);
    }
    private void ClearPlayers(ITransportConnection c, ulong team)
    {
        if (team is 1 or 2) LogicTeamClearPlayers[team - 1].SetVisibility(c, true);
    }
    private void OnIMGUIToggle(UnturnedButton button, Player player)
    {
        if (PlayerSave.TryReadSaveFile(player.channel.owner.playerID.steamID.m_SteamID, out PlayerSave save))
        {
            save.IMGUI = !save.IMGUI;
            OptionsIMGUICheckToggle.SetVisibility(player.channel.owner.transportConnection, save.IMGUI);
            PlayerSave.WriteToSaveFile(save);
        }
    }
    private void OnTeamClicked(UnturnedButton button, Player player)
    {
        ulong team = (ulong)(1 + Array.IndexOf(TeamButtons, button));
        if (team is not 1 and not 2)
            return;

    }
    private void OnConfirm(UnturnedButton button, Player player)
    {
        throw new NotImplementedException();
    }
}
