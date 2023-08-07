using SDG.NetTransport;
using SDG.Unturned;
using System;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Presets;
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
    public readonly UnturnedUIElement[] LogicTeamToggle = UnturnedUIPatterns.CreateArray<UnturnedUIElement>("anim_logic_team_{0}", 1, length: 2);
    public readonly UnturnedUIElement[] LogicTeamSelectedToggle = UnturnedUIPatterns.CreateArray<UnturnedUIElement>("anim_logic_team_{0}_selected", 1, length: 2);

    public readonly UnturnedButton ButtonConfirm = new UnturnedButton("btn_confirm");
    public readonly UnturnedLabel LabelConfirm = new UnturnedLabel("lbl_confirm");
    public readonly UnturnedButton ButtonOptionsBack = new UnturnedButton("btn_options_close");
    public readonly UnturnedLabel LabelOptionsBack = new UnturnedLabel("lbl_options_close");
    public readonly UnturnedLabel TeamsTitle = new UnturnedLabel("lbl_title");

    /* TEAM SELECTORS */
    public readonly UnturnedButton[] TeamButtons = UnturnedUIPatterns.CreateArray<UnturnedButton>("btn_{0}", 1, length: 2);
    public readonly UnturnedLabel[] TeamNames = UnturnedUIPatterns.CreateArray<UnturnedLabel>("lbl_name_{0}", 1, length: 2);
    public readonly UnturnedImage[] TeamFlags = UnturnedUIPatterns.CreateArray<UnturnedImage>("img_flag_{0}", 1, length: 2);
    public readonly UnturnedLabel[] TeamCounts = UnturnedUIPatterns.CreateArray<UnturnedLabel>("lbl_ct_{0}", 1, length: 2);
    public readonly UnturnedLabel[] TeamStatus = UnturnedUIPatterns.CreateArray<UnturnedLabel>("lbl_status_{0}", 1, length: 2);

    public readonly UnturnedLabel[] TeamPlayersTeam1 = UnturnedUIPatterns.CreateArray<UnturnedLabel>("pl_1_{0}", 1, length: PlayerListCount);
    public readonly UnturnedLabel[] TeamPlayersTeam2 = UnturnedUIPatterns.CreateArray<UnturnedLabel>("pl_2_{0}", 1, length: PlayerListCount);

    public readonly UnturnedLabel[][] TeamPlayers;

    /* OPTIONS */
    public readonly UnturnedUIElement OptionsIMGUICheckToggle = new UnturnedUIElement("chk_imgui_btn");
    public readonly UnturnedButton OptionsIMGUICheckButton = new UnturnedButton("chk_imgui_background");

    public readonly UnturnedUIElement OptionsTrackQuestsCheckToggle = new UnturnedUIElement("chk_track_quests_btn");
    public readonly UnturnedButton OptionsTrackQuestsCheckButton = new UnturnedButton("chk_track_quests");

    /* INTERNATIONALIZATION */
    public readonly LanguageBox[] Languages = UnturnedUIPatterns.CreateArray<LanguageBox>("box_l10n_{1}{0}", 1, to: 6);
    public readonly CultureBox[] Cultures = UnturnedUIPatterns.CreateArray<CultureBox>("box_i14n_{1}{0}", 1, to: 6);

    public readonly ChangeableTextBox LanguageSearchBox = new ChangeableTextBox("txt_search_l10n", "txt_search_l10n_text", "txt_search_l10n_placeholder");
    public readonly ChangeableTextBox CultureSearchBox = new ChangeableTextBox("txt_search_i14n", "txt_search_i14n_text", "txt_search_i14n_placeholder");

    public readonly UnturnedLabel NoLanguagesLabel = new UnturnedLabel("lbl_no_l10n_found");
    public readonly UnturnedLabel NoCulturesLabel = new UnturnedLabel("lbl_no_i14n_found");

    public readonly UnturnedLabel NewOptionsLabel = new UnturnedLabel("lbl_new_options");

    public event Action<UCPlayer, ulong>? OnTeamButtonClicked;
    public event Action<UCPlayer>? OnConfirmClicked;
    public event Action<UCPlayer>? OnOptionsBackClicked;
    public event Action<UCPlayer, string>? OnLanguageSearch;
    public event Action<UCPlayer, string>? OnCultureSearch;
    public event Action<UCPlayer, int>? OnLanguageApply;
    public event Action<UCPlayer, int>? OnCultureApply;
    public TeamSelectorUI() : base(Gamemode.Config.UITeamSelector, true, false)
    {
        TeamPlayers = new UnturnedLabel[][]
        {
            TeamPlayersTeam1,
            TeamPlayersTeam2
        };
        OptionsIMGUICheckButton.OnClicked += OnIMGUIToggle;
        OptionsTrackQuestsCheckButton.OnClicked += OnTrackQuestsToggle;
        TeamButtons[0].OnClicked += OnTeamClicked;
        TeamButtons[1].OnClicked += OnTeamClicked;

        ButtonConfirm.OnClicked += OnConfirm;
        ButtonOptionsBack.OnClicked += OnOptionsBack;

        foreach (LanguageBox box in Languages)
            box.ApplyButton.OnClicked += OnApplyLanguageButtonPressed;

        foreach (CultureBox box in Cultures)
            box.ApplyButton.OnClicked += OnApplyCultureButtonPressed;

        LanguageSearchBox.OnTextUpdated += OnLanguageTextUpdated;
        CultureSearchBox.OnTextUpdated += OnCultureTextUpdated;
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
            OptionsIMGUICheckToggle.SetVisibility(player.channel.owner.transportConnection, save.IMGUI);
            PlayerSave.WriteToSaveFile(save);
        }
    }
    private void OnTrackQuestsToggle(UnturnedButton button, Player player)
    {
        if (PlayerSave.TryReadSaveFile(player.channel.owner.playerID.steamID.m_SteamID, out PlayerSave save))
        {
            save.TrackQuests = !save.TrackQuests;
            OptionsIMGUICheckToggle.SetVisibility(player.channel.owner.transportConnection, save.TrackQuests);
            PlayerSave.WriteToSaveFile(save);
        }
    }
    private void OnApplyLanguageButtonPressed(UnturnedButton button, Player player)
    {
        if (UCPlayer.FromPlayer(player) is not { TeamSelectorData.IsSelecting: true } ucPlayer) return;
        int index = Array.FindIndex(Languages, x => x.ApplyButton == button);
        if (index == -1) return;

        OnLanguageApply?.Invoke(ucPlayer, index);
    }
    private void OnApplyCultureButtonPressed(UnturnedButton button, Player player)
    {
        if (UCPlayer.FromPlayer(player) is not { TeamSelectorData.IsSelecting: true } ucPlayer) return;
        int index = Array.FindIndex(Cultures, x => x.ApplyButton == button);
        if (index == -1) return;

        OnCultureApply?.Invoke(ucPlayer, index);
    }
    private void OnLanguageTextUpdated(UnturnedTextBox textBox, Player player, string text)
    {
        if (UCPlayer.FromPlayer(player) is not { TeamSelectorData.IsSelecting: true } ucPlayer) return;

        OnLanguageSearch?.Invoke(ucPlayer, text);
    }
    private void OnCultureTextUpdated(UnturnedTextBox textBox, Player player, string text)
    {
        if (UCPlayer.FromPlayer(player) is not { TeamSelectorData.IsSelecting: true } ucPlayer) return;

        OnCultureSearch?.Invoke(ucPlayer, text);
    }
    private void OnTeamClicked(UnturnedButton button, Player player)
    {
        if (OnTeamButtonClicked == null) return;
        ulong team = (ulong)(1 + Array.IndexOf(TeamButtons, button));
        if (team is not 1 and not 2 || UCPlayer.FromPlayer(player) is not { } ucplayer)
            return;
        OnTeamButtonClicked?.Invoke(ucplayer, team);
    }
    private void OnConfirm(UnturnedButton button, Player player)
    {
        if (OnConfirmClicked == null || UCPlayer.FromPlayer(player) is not { } ucplayer)
            return;
        OnConfirmClicked?.Invoke(ucplayer);
    }
    public class LanguageBox
    {
        [UIPattern("", Mode = FormatMode.Format)]
        public UnturnedUIElement Root { get; set; }

        [UIPattern("name_", Mode = FormatMode.Format)]
        public UnturnedLabel Name { get; set; }

        [UIPattern("code_", Mode = FormatMode.Format)]
        public UnturnedLabel Details { get; set; }

        [UIPattern("contributors_", Mode = FormatMode.Format)]
        public UnturnedLabel ContributorsLabel { get; set; }

        [UIPattern("contributors_value_", Mode = FormatMode.Format)]
        public UnturnedLabel Contributors { get; set; }

        [UIPattern("state_select_", Mode = FormatMode.Format)]
        public UnturnedUIElement ApplyState { get; set; }

        [UIPattern("lbl_select_", Mode = FormatMode.Format)]
        public UnturnedLabel ApplyLabel { get; set; }

        [UIPattern("btn_select_", Mode = FormatMode.Format)]
        public UnturnedButton ApplyButton { get; set; }
    }
    public class CultureBox
    {
        [UIPattern("", Mode = FormatMode.Format)]
        public UnturnedUIElement Root { get; set; }

        [UIPattern("name_", Mode = FormatMode.Format)]
        public UnturnedLabel Name { get; set; }

        [UIPattern("code_", Mode = FormatMode.Format)]
        public UnturnedLabel Details { get; set; }

        [UIPattern("btn_state_select_", Mode = FormatMode.Format)]
        public UnturnedUIElement ApplyState { get; set; }

        [UIPattern("btn_lbl_select_", Mode = FormatMode.Format)]
        public UnturnedLabel ApplyLabel { get; set; }

        [UIPattern("btn_select_", Mode = FormatMode.Format)]
        public UnturnedButton ApplyButton { get; set; }
    }
}
