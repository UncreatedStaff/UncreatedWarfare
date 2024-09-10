using SDG.NetTransport;
using SDG.Unturned;
using System;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Patterns;
using Uncreated.Framework.UI.Presets;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Gamemodes;

namespace Uncreated.Warfare.Teams;

[UnturnedUI(BasePath = "Canvas/menu_base")]
public class TeamSelectorUI : UnturnedUI
{
    public const int PlayerListCount = 30;

    /* LOGIC */
    public readonly UnturnedUIElement LogicTeamSettings = new UnturnedUIElement("~/anim_logic_team_stuff");
    public readonly UnturnedUIElement LogicConfirmToggle = new UnturnedUIElement("~/anim_logic_btn_confirm");
    public readonly UnturnedUIElement LogicOpenTeamMenu = new UnturnedUIElement("~/anim_logic_page_teams");
    public readonly UnturnedUIElement LogicOpenOptionsMenu = new UnturnedUIElement("~/anim_logic_page_options");
    public readonly UnturnedUIElement[] LogicTeamToggle = ElementPatterns.CreateArray<UnturnedUIElement>("~/anim_logic_team_{0}", 1, length: 2);
    public readonly UnturnedUIElement[] LogicTeamSelectedToggle = ElementPatterns.CreateArray<UnturnedUIElement>("~/anim_logic_team_{0}_selected", 1, length: 2);

    public readonly LabeledButton ButtonConfirm = new LabeledButton("menu_teams/btn_confirm", "./lbl_confirm");
    public readonly LabeledButton ButtonOptionsBack = new LabeledButton("menu_options/btn_options_close", "./lbl_options_close");
    public readonly UnturnedLabel TeamsTitle = new UnturnedLabel("menu_teams/lbl_title");

    /* TEAM SELECTORS */
    public readonly TeamButton[] Teams = ElementPatterns.CreateArray<TeamButton>("menu_teams/btn_{0}", 1, length: 2);

    public readonly UnturnedLabel[] TeamPlayersTeam1 = ElementPatterns.CreateArray<UnturnedLabel>("menu_teams/players_1/pl_1_{0}", 1, length: PlayerListCount);
    public readonly UnturnedLabel[] TeamPlayersTeam2 = ElementPatterns.CreateArray<UnturnedLabel>("menu_teams/players_2/pl_2_{0}", 1, length: PlayerListCount);

    public readonly UnturnedLabel[][] TeamPlayers;

    /* OPTIONS */
    public readonly UnturnedToggle OptionsIMGUICheckToggle = new UnturnedToggle(false, "menu_options/chk_imgui_background", "./chk_imgui_btn");

    public readonly UnturnedToggle OptionsTrackQuestsCheckToggle = new UnturnedToggle(true, "menu_options/chk_track_quests", "./chk_track_quests_btn");

    /* INTERNATIONALIZATION */
    public readonly LanguageBox[] Languages = ElementPatterns.CreateArray<LanguageBox>("menu_options/lang_settings/Viewport/Content/box_l10n_{0}", 1, to: 6);
    public readonly CultureBox[] Cultures = ElementPatterns.CreateArray<CultureBox>("menu_options/lang_settings/Viewport/Content/box_i14n_{0}", 1, to: 25);

    public readonly PlaceholderTextBox LanguageSearchBox = new PlaceholderTextBox("menu_options/lang_settings/Viewport/Content/search_l10n/txt_search_l10n", "./Viewport/txt_search_l10n_placeholder");
    public readonly PlaceholderTextBox CultureSearchBox = new PlaceholderTextBox("menu_options/lang_settings/Viewport/Content/search_i14n/txt_search_i14n", "./Viewport/txt_search_i14n_placeholder");

    public readonly UnturnedLabel NoLanguagesLabel = new UnturnedLabel("menu_options/lang_settings/Viewport/Content/box_l10n_1/lbl_no_l10n_found");
    public readonly UnturnedLabel NoCulturesLabel = new UnturnedLabel("menu_options/lang_settings/Viewport/Content/box_i14n_1/lbl_no_i14n_found");

    public readonly UnturnedLabel NewOptionsLabel = new UnturnedLabel("menu_teams/btn_options/lbl_new_options");

    public readonly LabeledUnturnedToggle UseCultureForCommandInput = new LabeledUnturnedToggle(true,
        "menu_options/lang_settings/Viewport/Content/search_i14n/tgl_use_for_cmd_input",
        "./state_tgl_use_for_cmd_input", "../lbl_use_for_cmd_input", null);

    public event Action<UCPlayer, ulong>? OnTeamButtonClicked;
    public event Action<UCPlayer>? OnConfirmClicked;
    public event Action<UCPlayer>? OnOptionsBackClicked;
    public event Action<UCPlayer, string>? OnLanguageSearch;
    public event Action<UCPlayer, string>? OnCultureSearch;
    public event Action<UCPlayer, int>? OnLanguageApply;
    public event Action<UCPlayer, int>? OnCultureApply;
    public TeamSelectorUI() : base(Gamemode.Config.UITeamSelector.AsAssetContainer(), true, false)
    {
        TeamPlayers =
        [
            TeamPlayersTeam1,
            TeamPlayersTeam2
        ];

        OptionsIMGUICheckToggle.OnToggleUpdated += OnIMGUIToggle;
        OptionsTrackQuestsCheckToggle.OnToggleUpdated += OnTrackQuestsToggle;

        ElementPatterns.SubscribeAll(Teams, btn => btn.Root, OnTeamClicked);

        ButtonConfirm.OnClicked += OnConfirm;
        ButtonOptionsBack.OnClicked += OnOptionsBack;

        ElementPatterns.SubscribeAll(Languages, box => box.ApplyButton, OnApplyLanguageButtonPressed);
        ElementPatterns.SubscribeAll(Cultures, box => box.ApplyButton, OnApplyCultureButtonPressed);

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
    private static void OnIMGUIToggle(UnturnedToggle toggle, Player player, bool value)
    {
        if (!PlayerSave.TryReadSaveFile(player.channel.owner.playerID.steamID.m_SteamID, out PlayerSave save))
            return;

        save.IMGUI = value;
        PlayerSave.WriteToSaveFile(save);
    }
    private static void OnTrackQuestsToggle(UnturnedToggle toggle, Player player, bool value)
    {
        if (!PlayerSave.TryReadSaveFile(player.channel.owner.playerID.steamID.m_SteamID, out PlayerSave save))
            return;

        save.TrackQuests = value;
        PlayerSave.WriteToSaveFile(save);
    }
    private void OnApplyLanguageButtonPressed(UnturnedButton button, Player player)
    {
        UCPlayer? ucPlayer = UCPlayer.FromPlayer(player);
        if (ucPlayer is not { TeamSelectorData.IsSelecting: true } and not { TeamSelectorData.IsOptionsOnly: true }) return;
        int index = Array.FindIndex(Languages, x => x.ApplyButton == button);
        if (index == -1) return;

        OnLanguageApply?.Invoke(ucPlayer, index);
    }
    private void OnApplyCultureButtonPressed(UnturnedButton button, Player player)
    {
        UCPlayer? ucPlayer = UCPlayer.FromPlayer(player);
        if (ucPlayer is not { TeamSelectorData.IsSelecting: true } and not { TeamSelectorData.IsOptionsOnly: true }) return;
        int index = Array.FindIndex(Cultures, x => x.ApplyButton == button);
        if (index == -1) return;

        OnCultureApply?.Invoke(ucPlayer, index);
    }
    private void OnLanguageTextUpdated(UnturnedTextBox textBox, Player player, string text)
    {
        UCPlayer? ucPlayer = UCPlayer.FromPlayer(player);
        if (ucPlayer is not { TeamSelectorData.IsSelecting: true } and not { TeamSelectorData.IsOptionsOnly: true }) return;

        OnLanguageSearch?.Invoke(ucPlayer, text);
    }
    private void OnCultureTextUpdated(UnturnedTextBox textBox, Player player, string text)
    {
        UCPlayer? ucPlayer = UCPlayer.FromPlayer(player);
        if (ucPlayer is not { TeamSelectorData.IsSelecting: true } and not { TeamSelectorData.IsOptionsOnly: true }) return;

        OnCultureSearch?.Invoke(ucPlayer, text);
    }
    private void OnTeamClicked(UnturnedButton button, Player player)
    {
        if (OnTeamButtonClicked == null) return;
        ulong team = (ulong)(1 + Array.FindIndex(Teams, team => team.Root == button));
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
    public class TeamButton
    {
        [Pattern(Root = true)]
        public UnturnedButton Root { get; set; }

        [Pattern("lbl_name_{0}")]
        public UnturnedLabel Name { get; set; }

        [Pattern("img_flag_{0}")]
        public UnturnedImage Flag { get; set; }

        [Pattern("lbl_ct_{0}")]
        public UnturnedLabel Count { get; set; }

        [Pattern("lbl_status_{0}")]
        public UnturnedLabel Status { get; set; }
    }

    public class LanguageBox
    {
        [Pattern(Root = true)]
        public UnturnedUIElement Root { get; set; }

        [Pattern("box_l10n_name_{0}")]
        public UnturnedLabel Name { get; set; }

        [Pattern("box_l10n_code_{0}")]
        public UnturnedLabel Details { get; set; }

        [Pattern("box_l10n_contributors_{0}")]
        public UnturnedLabel ContributorsLabel { get; set; }

        [Pattern("box_l10n_contributors_value_{0}")]
        public UnturnedLabel Contributors { get; set; }

        [Pattern("box_l10n_btn_state_select_{0}", AdditionalPath = "box_l10n_btn_select_{0}")]
        public UnturnedUIElement ApplyState { get; set; }

        [Pattern("box_l10n_btn_label_select_{0}", AdditionalPath = "box_l10n_btn_select_{0}")]
        public UnturnedLabel ApplyLabel { get; set; }

        [Pattern("box_l10n_btn_select_{0}")]
        public UnturnedButton ApplyButton { get; set; }
    }
    public class CultureBox
    {
        [Pattern(Root = true)]
        public UnturnedUIElement Root { get; set; }

        [Pattern("box_i14n_name_{0}")]
        public UnturnedLabel Name { get; set; }

        [Pattern("box_i14n_code_{0}")]
        public UnturnedLabel Details { get; set; }

        [Pattern("box_i14n_btn_state_select_{0}", AdditionalPath = "box_i14n_btn_select_{0}")]
        public UnturnedUIElement ApplyState { get; set; }

        [Pattern("box_i14n_btn_label_select_{0}", AdditionalPath = "box_i14n_btn_select_{0}")]
        public UnturnedLabel ApplyLabel { get; set; }

        [Pattern("box_i14n_btn_select_{0}")]
        public UnturnedButton ApplyButton { get; set; }
    }
}
