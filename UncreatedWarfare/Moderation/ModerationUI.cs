using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SDG.NetTransport;
using System;
using System.Collections.Generic;
using System.Globalization;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Data;
using Uncreated.Framework.UI.Patterns;
using Uncreated.Framework.UI.Presets;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Moderation.Punishments.Presets;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Steam;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Moderation;

[UnturnedUI(BasePath = "Container/Backdrop/PageModeration")]
public partial class ModerationUI : UnturnedUI
{
    private readonly ITranslationValueFormatter _valueFormatter;
    private readonly IPlayerService _playerService;
    private readonly DatabaseInterface _moderationSql;
    private readonly ItemIconProvider _itemIconProvider;

    private readonly string _discordInviteCode;

    private readonly ISteamApiService _steamAPI;

    public const int ModerationHistoryLength = 30;
    public const string PositiveReputationColor = "00cc00";
    public const string NegativeReputationColor = "cc0000";
    public const string DateTimeFormat = "yyyy\\/MM\\/dd\\ hh\\:mm\\:ss\\ \\U\\T\\C\\-\\2\\4";
    public const string DateTimeFormatInput = "yyyy\\/MM\\/dd\\ hh\\:mm\\:ss";

    private readonly List<WarfarePlayer> _tempPlayerSearchBuffer = new List<WarfarePlayer>(Provider.maxPlayers);

    /* HEADERS */
    public LabeledButton[] Headers { get; } =
    {
        new LabeledButton("~/Container/ButtonModeration", "./ButtonModerationLabel"),
        new LabeledButton("~/Container/ButtonPlayers", "./ButtonPlayersLabel"),
        new LabeledButton("~/Container/ButtonTickets", "./ButtonTicketsLabel"),
        new LabeledButton("~/Container/ButtonLogs", "./ButtonLogsLabel")
    };

    public LabeledButton ButtonClose { get; } = new LabeledButton("~/Container/ButtonClose", "./ButtonCloseLabel");

    public UnturnedUIElement[] PageLogic { get; } =
    {
        new UnturnedUIElement("~/LogicPageModeration"),
        new UnturnedUIElement("~/LogicPagePlayers"),
        new UnturnedUIElement("~/LogicPageTickets"),
        new UnturnedUIElement("~/LogicPageLogs")
    };
    
    /* PLAYER LIST */
    public PlayerListEntry[] ModerationPlayerList { get; } = ElementPatterns.CreateArray<PlayerListEntry>("ModerationPlayerList/Viewport/Content/ModerationPlayer_{0}", 1, to: 30);
    public UnturnedTextBox ModerationPlayerSearch { get; } = new UnturnedTextBox("ModerationPlayersInputSearch")
    {
        UseData = true
    };
    public UnturnedEnumButton<PlayerSearchMode> ModerationPlayerSearchModeButton { get; }

    /* MODERATION HISTORY LIST */
    public ModerationHistoryEntry[] ModerationHistory { get; } = ElementPatterns.CreateArray<ModerationHistoryEntry>("ModerationList/Viewport/Content/ModerationEntry_{0}", 1, to: ModerationHistoryLength);
    public LabeledStateButton ModerationHistoryBackButton { get; } = new LabeledStateButton("ModerationListControls/ModerationListBackButton", "./ModerationListBackButtonLabel", "./ModerationListBackButtonState");
    public LabeledStateButton ModerationHistoryNextButton { get; } = new LabeledStateButton("ModerationListControls/ModerationListNextButton", "./ModerationListNextButtonLabel", "./ModerationListNextButtonState");
    public StateTextBox ModerationHistoryPage { get; } = new StateTextBox("ModerationListControls/ModerationListPageInput", "./ModerationListPageInputState");
    public UnturnedTextBox ModerationHistorySearch { get; } = new UnturnedTextBox("ModerationInputSearch")
    {
        UseData = true
    };
    public LabeledButton ModerationResetHistory { get; } = new LabeledButton("ModerationResetHistory", "./ModerationRefreshHistoryLabel");
    public UnturnedEnumButton<ModerationEntryType> ModerationHistoryTypeButton { get; }
        = new UnturnedEnumButton<ModerationEntryType>(
        [
            ModerationEntryType.None,
            ModerationEntryType.Commendation,
            ModerationEntryType.Note,
            ModerationEntryType.Warning,
            ModerationEntryType.Kick,
            ModerationEntryType.AssetBan,
            ModerationEntryType.Mute,
            ModerationEntryType.Ban,
            ModerationEntryType.BattlEyeKick,
            ModerationEntryType.Report,
            ModerationEntryType.Appeal,
            ModerationEntryType.Teamkill,
            ModerationEntryType.VehicleTeamkill,
        ], ModerationEntryType.None, "ModerationButtonToggleType", "./ModerationButtonToggleTypeLabel", null, "./ModerationButtonToggleTypeRightClickListener")
        {
            TextFormatter = (v, _) => v == ModerationEntryType.None ? "Type - Any" : ("Type - " + GetModerationTypeButtonText(v))
        };
    public UnturnedEnumButton<ModerationHistorySearchMode> ModerationHistorySearchTypeButton { get; }
    public UnturnedEnumButton<ModerationHistorySortMode> ModerationHistorySortModeButton { get; }

    /* MODERATION SELECTED ENTRY */
    public UnturnedUIElement ModerationInfoRoot { get; } = new UnturnedUIElement("ModerationInfo/Viewport/ModerationInfoContent");
    public UnturnedUIElement ModerationInfoActorsHeader { get; } = new UnturnedUIElement("ModerationInfo/Viewport/ModerationInfoContent/ModerationInfoActorsHeader");
    public UnturnedUIElement ModerationInfoEvidenceHeader { get; } = new UnturnedUIElement("ModerationInfo/Viewport/ModerationInfoContent/ModerationInfoEvidenceHeader");
    public UnturnedImage ModerationInfoProfilePicture { get; } = new UnturnedImage("ModerationInfo/Viewport/ModerationInfoContent/ModerationInfoSection1/ModerationInfoPfpMask/ModerationInfoPfp");
    public UnturnedLabel ModerationInfoType { get; } = new UnturnedLabel("ModerationInfo/Viewport/ModerationInfoContent/ModerationInfoSection1/ModerationInfoType");
    public UnturnedLabel ModerationInfoTimestamp { get; } = new UnturnedLabel("ModerationInfo/Viewport/ModerationInfoContent/ModerationInfoSection1/ModerationInfoTimestamp");
    public UnturnedLabel ModerationInfoReputation { get; } = new UnturnedLabel("ModerationInfo/Viewport/ModerationInfoContent/ModerationInfoSection1/ModerationInfoReputation");
    public UnturnedLabel ModerationInfoReason { get; } = new UnturnedLabel("ModerationInfo/Viewport/ModerationInfoContent/ModerationInfoSection1/ModerationInfoReasonBox/ModerationInfoReason");
    public UnturnedLabel ModerationInfoPlayerName { get; } = new UnturnedLabel("ModerationInfo/Viewport/ModerationInfoContent/ModerationInfoSection1/ModerationInfoPlayerName");
    public UnturnedLabel ModerationInfoPlayerId { get; } = new UnturnedLabel("ModerationInfo/Viewport/ModerationInfoContent/ModerationInfoSection1/ModerationInfoPlayerId");
    public UnturnedLabel[] ModerationInfoExtraInfo { get; } = ElementPatterns.CreateArray<UnturnedLabel>("ModerationInfo/Viewport/ModerationInfoContent/ModerationInfoExtraInfoBox/ModerationInfoExtra_{0}", 1, to: 12);
    public ModerationInfoActor[] ModerationInfoActors { get; } = ElementPatterns.CreateArray<ModerationInfoActor>("ModerationInfo/Viewport/ModerationInfoContent/ModerationActorGrid/ModerationActor_{0}", 1, to: 10);
    public ModerationInfoEvidence[] ModerationInfoEvidenceEntries { get; } = ElementPatterns.CreateArray<ModerationInfoEvidence>("ModerationInfo/Viewport/ModerationInfoContent/ModerationActorGrid/ModerationEvidence_{0}", 1, to: 10);
    public UnturnedUIElement LogicModerationInfoUpdateScrollVisual { get; } = new UnturnedUIElement("ModerationInfo/LogicModerationInfoUpdateScrollVisual");


    /* ACTION BUTTONS */
    public LabeledStateButton ModerationButtonNote { get; } = new LabeledStateButton("ActionsPane/ActionsButtonBox/ActionsButtonGrid/ButtonNote", "./ButtonNoteLabel", "./ButtonNoteState");
    public LabeledStateButton ModerationButtonCommend { get; } = new LabeledStateButton("ActionsPane/ActionsButtonBox/ActionsButtonGrid/ButtonCommend", "./ButtonCommendLabel", "./ButtonCommendState");
    public LabeledStateButton ModerationButtonAcceptedBugReport { get; } = new LabeledStateButton("ActionsPane/ActionsButtonBox/ActionsButtonGrid/ButtonAcceptedBugReport", "./ButtonAcceptedBugReportLabel", "./ButtonAcceptedBugReportState");
    public LabeledStateButton ModerationButtonAssetBan { get; } = new LabeledStateButton("ActionsPane/ActionsButtonBox/ActionsButtonGrid/ButtonAssetBan", "./ButtonAssetBanLabel", "./ButtonAssetBanState");
    public LabeledStateButton ModerationButtonWarn { get; } = new LabeledStateButton("ActionsPane/ActionsButtonBox/ActionsButtonGrid/ButtonWarn", "./ButtonWarnLabel", "./ButtonWarnState");
    public LabeledStateButton ModerationButtonKick { get; } = new LabeledStateButton("ActionsPane/ActionsButtonBox/ActionsButtonGrid/ButtonKick", "./ButtonKickLabel", "./ButtonKickState");
    public LabeledStateButton ModerationButtonMute { get; } = new LabeledStateButton("ActionsPane/ActionsButtonBox/ActionsButtonGrid/ButtonMute", "./ButtonMuteLabel", "./ButtonMuteState");
    public LabeledStateButton ModerationButtonBan { get; } = new LabeledStateButton("ActionsPane/ActionsButtonBox/ActionsButtonGrid/ButtonBan", "./ButtonBanLabel", "./ButtonBanState");

    public LabeledStateButton[] Presets { get; } = ElementPatterns.CreateArray(index =>
    {
        string ind = index.ToString(CultureInfo.InvariantCulture);
        return new LabeledStateButton(
            "ActionsPane/ActionsButtonBox/ActionsButtonGrid/ButtonPreset_" + ind,
            "./ButtonPreset_" + ind + "_Label",
            "./ButtonPreset_" + ind + "_State"
        );
    }, 1, to: 12);

    /* ACTION FORM */
    public UnturnedUIElement ActionButtonBox { get; } = new UnturnedUIElement("ActionsPane/ActionsButtonBox");
    public UnturnedUIElement ModerationFormRoot { get; } = new UnturnedUIElement("ActionsPane/ActionsScrollBox");
    public UnturnedLabel ModerationActionHeader { get; } = new UnturnedLabel("ModerationActionsHeader/ModerationActionsLabel");
    public UnturnedLabel ModerationActionTypeHeader { get; } = new UnturnedLabel("ActionsPane/ActionsScrollBox/Viewport/ActionsContent/ModerationsSelectedActionTopBar/ModerationSelectedActionBox/ModerationSelectedActionBoxLabel");
    public UnturnedLabel ModerationActionPlayerHeader { get; } = new UnturnedLabel("ActionsPane/ActionsScrollBox/Viewport/ActionsContent/ModerationsSelectedActionTopBar/ModerationSelectedPlayerBox/ModerationSelectedPlayerBoxLabel");
    public UnturnedLabel ModerationActionPresetHeader { get; } = new UnturnedLabel("ActionsPane/ActionsScrollBox/Viewport/ActionsContent/ModerationSelectedActionPresetBox/ModerationSelectedActionPresetBoxLabel");
    public UnturnedLabel ModerationActionOtherEditor { get; } = new UnturnedLabel("ActionsPane/ActionsScrollBox/Viewport/ActionsContent/ModerationSelectedActionWarningBox/ModerationSelectedActionWarningBoxLabel");
    public UnturnedUIElement ModerationActionPresetHeaderRoot { get; } = new UnturnedUIElement("ActionsPane/ActionsScrollBox/Viewport/ActionsContent/ModerationSelectedActionPresetBox");
    public UnturnedUIElement ModerationActionOtherEditorRoot { get; } = new UnturnedUIElement("ActionsPane/ActionsScrollBox/Viewport/ActionsContent/ModerationSelectedActionWarningBox");
    public UnturnedTextBox ModerationActionMessage { get; } = new UnturnedTextBox("ActionsPane/ActionsScrollBox/Viewport/ActionsContent/ModerationInputMessage")
    {
        UseData = true
    };
    public PlaceholderTextBox ModerationActionInputBox2 { get; } = new PlaceholderTextBox("ActionsPane/ActionsScrollBox/Viewport/ActionsContent/ModerationInputBox2", "./Viewport/ModerationInputBox2Placeholder")
    {
        UseData = true
    };
    public PlaceholderTextBox ModerationActionInputBox3 { get; } = new PlaceholderTextBox("ActionsPane/ActionsScrollBox/Viewport/ActionsContent/ModerationInputBox3", "./Viewport/ModerationInputBox3Placeholder")
    {
        UseData = true
    };
    public PlaceholderTextBox ModerationActionMiniInputBox1 { get; } = new PlaceholderTextBox("ActionsPane/ActionsScrollBox/Viewport/ActionsContent/ModerationInputHoriz/ModerationMiniInput1", "./Viewport/ModerationMiniInput1Placeholder")
    {
        UseData = true
    };
    public PlaceholderTextBox ModerationActionMiniInputBox2 { get; } = new PlaceholderTextBox("ActionsPane/ActionsScrollBox/Viewport/ActionsContent/ModerationInputHoriz/ModerationMiniInput2", "./Viewport/ModerationMiniInput2Placeholder")
    {
        UseData = true
    };
    public LabeledRightClickableButton ModerationActionToggleButton1 { get; } = new LabeledRightClickableButton("ActionsPane/ActionsScrollBox/Viewport/ActionsContent/ModerationInputHoriz/ModerationToggleButton1", "./ModerationToggleButton1Label", "./ModerationToggleButton1LabelRightClickListener");
    public LabeledRightClickableButton ModerationActionToggleButton2 { get; } = new LabeledRightClickableButton("ActionsPane/ActionsScrollBox/Viewport/ActionsContent/ModerationInputHoriz/ModerationToggleButton2", "./ModerationToggleButton2Label", "./ModerationToggleButton2RightClickListener");
    public ModerationSelectedActor[] ModerationActionActors { get; } = ElementPatterns.CreateArray<ModerationSelectedActor>("ActionsPane/ActionsScrollBox/Viewport/ActionsContent/ModerationSelectedActorGrid/ModerationSelectedActor_{0}", 1, to: 10);
    public ModerationSelectedEvidence[] ModerationActionEvidence { get; } = ElementPatterns.CreateArray<ModerationSelectedEvidence>("ActionsPane/ActionsScrollBox/Viewport/ActionsContent/ModerationInfoSelectedEvidenceBox/ModerationSelectedEvidence_{0}", 1, to: 10);
    public LabeledStateButton ModerationActionAddActorButton { get; } = new LabeledStateButton("ActionsPane/ActionsScrollBox/Viewport/ActionsContent/ModerationSelectedActorsHeader/ModerationSelectedActorsHeaderAdd", "./ModerationSelectedActorsHeaderAddLabel", "./ModerationSelectedActorsHeaderAddState");
    public LabeledStateButton ModerationActionAddEvidenceButton { get; } = new LabeledStateButton("ActionsPane/ActionsScrollBox/Viewport/ActionsContent/ModerationSelectedEvidenceHeader/ModerationSelectedEvidenceHeaderAdd", "./ModerationSelectedActorsHeaderAddLabel", "./ModerationSelectedActorsHeaderAddState");
    public UnturnedEnumButton<MuteType> MuteTypeTracker { get; }
    public UnturnedUIElement LogicModerationActionsUpdateScrollVisual { get; } = new UnturnedUIElement("ActionsPane/LogicModerationActionsUpdateScrollVisual");

    /* ACTION CONTROLS */
    public LabeledButton[] ModerationActionControls { get; } = ElementPatterns.CreateArray(index =>
    {
        string ind = index.ToString(CultureInfo.InvariantCulture);
        return new LabeledButton(
            "ActionsPane/ActionsControls/ModerationActionControl_" + ind,
            "./ModerationActionControlLabel_" + ind
        );
    }, 1, to: 4);

    public ModerationUI(IServiceProvider serviceProvider)
        : base(serviceProvider.GetRequiredService<ILoggerFactory>(), serviceProvider.GetRequiredService<AssetConfiguration>().GetAssetLink<EffectAsset>("UI:ModerationMenu"), debugLogging: false)
    {
        _valueFormatter = serviceProvider.GetRequiredService<ITranslationValueFormatter>();
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
        _steamAPI = serviceProvider.GetRequiredService<ISteamApiService>();
        _moderationSql = serviceProvider.GetRequiredService<DatabaseInterface>();
        _itemIconProvider = serviceProvider.GetRequiredService<ItemIconProvider>();

        IConfiguration systemConfig = serviceProvider.GetRequiredService<IConfiguration>();
        _discordInviteCode = systemConfig["discord_invite_code"];

        ModerationHistorySearchTypeButton = new UnturnedEnumButton<ModerationHistorySearchMode>(ModerationHistorySearchMode.Message, "ModerationButtonToggleSearchMode",
            "./ModerationButtonToggleSearchModeLabel", null, "./ModerationButtonToggleSearchModeRightClickListener")
        {
            TextFormatter = (v, player) => "Search - " + _valueFormatter.FormatEnum(v, _playerService.GetOnlinePlayerOrNull(player)?.Locale.LanguageInfo)
        };

        LateRegisterElement(ModerationHistorySearchTypeButton);

        ModerationHistorySortModeButton = new UnturnedEnumButton<ModerationHistorySortMode>(ModerationHistorySortMode.Latest, "ModerationButtonToggleSortType",
            "./ModerationButtonToggleSortTypeLabel", null, "./ModerationButtonToggleSortTypeRightClickListener")
        {
            TextFormatter = (v, player) => "Sort - " + _valueFormatter.FormatEnum(v, _playerService.GetOnlinePlayerOrNull(player)?.Locale.LanguageInfo)
        };

        LateRegisterElement(ModerationHistorySortModeButton);

        ModerationPlayerSearchModeButton = new UnturnedEnumButton<PlayerSearchMode>(PlayerSearchMode.Online, "ModerationButtonToggleOnline", "./ModerationButtonToggleOnlineLabel")
        {
            TextFormatter = (v, player) => "View - " + _valueFormatter.FormatEnum(v, _playerService.GetOnlinePlayerOrNull(player)?.Locale.LanguageInfo)
        };

        LateRegisterElement(ModerationPlayerSearchModeButton);

        MuteTypeTracker = new UnturnedEnumButton<MuteType>(MuteType.Both, ModerationActionToggleButton1)
        {
            Ignored = MuteType.None,
            TextFormatter = (type, player) => "Mute Type - " + _valueFormatter.FormatEnum(type, _playerService.GetOnlinePlayerOrNull(player)?.Locale.LanguageInfo)
        };

        LateRegisterElement(MuteTypeTracker);

        ButtonClose.OnClicked += OnButtonCloseClicked;
        ModerationPlayerSearchModeButton.OnValueUpdated += OnModerationPlayerSearchModeUpdated;
        ModerationPlayerSearch.OnTextUpdated += OnModerationPlayerSearchTextUpdated;
        for (int i = 0; i < ModerationPlayerList.Length; ++i)
        {
            ModerationPlayerList[i].ModerateButton.OnClicked += OnClickedModeratePlayer;
        }
        for (int i = 0; i < ModerationHistory.Length; ++i)
        {
            ModerationHistory[i].Root.OnClicked += OnClickedModerationEntry;
        }

        for (int i = 0; i < ModerationInfoEvidenceEntries.Length; ++i)
            ModerationInfoEvidenceEntries[i].PreviewImageButton.OnClicked += OnEvidenceClicked;

        ModerationButtonNote.OnClicked              += (_, pl) => OnClickedModerateButton(pl, ModerationEntryType.Note);
        ModerationButtonCommend.OnClicked           += (_, pl) => OnClickedModerateButton(pl, ModerationEntryType.Commendation);
        ModerationButtonAcceptedBugReport.OnClicked += (_, pl) => OnClickedModerateButton(pl, ModerationEntryType.BugReportAccepted);
        ModerationButtonAssetBan.OnClicked          += (_, pl) => OnClickedModerateButton(pl, ModerationEntryType.AssetBan);
        ModerationButtonWarn.OnClicked              += (_, pl) => OnClickedModerateButton(pl, ModerationEntryType.Warning);
        ModerationButtonKick.OnClicked              += (_, pl) => OnClickedModerateButton(pl, ModerationEntryType.Kick);
        ModerationButtonMute.OnClicked              += (_, pl) => OnClickedModerateButton(pl, ModerationEntryType.Mute);
        ModerationButtonBan.OnClicked               += (_, pl) => OnClickedModerateButton(pl, ModerationEntryType.Ban);

        for (int i = 0; i < Presets.Length; ++i)
        {
            Presets[i].OnClicked += OnClickedPreset;
        }

        ModerationHistoryNextButton.OnClicked += OnModerationHistoryNext;
        ModerationHistoryBackButton.OnClicked += OnModerationHistoryBack;
        ModerationHistoryPage.OnTextUpdated += OnModerationHistoryPageTyped;

        ModerationHistorySearch.OnTextUpdated += OnModerationHistorySearched;
        ModerationHistoryTypeButton.OnValueUpdated += OnModerationHistoryTypeUpdated;
        ModerationHistorySearchTypeButton.OnValueUpdated += OnModerationHistorySearchTypeUpdated;
        ModerationHistorySortModeButton.OnValueUpdated += OnModerationHistorySortModeUpdated;
        ModerationResetHistory.OnClicked += OnReset;

        ModerationActionAddActorButton.OnClicked += OnClickedAddActor;

        ModerationActionAddEvidenceButton.OnClicked += OnClickedAddEvidence;

        for (int i = 0; i < ModerationActionActors.Length; ++i)
        {
            ModerationSelectedActor ui = ModerationActionActors[i];
            ui.RemoveButton.OnClicked += OnClickedRemoveActor;
            ui.AsAdminToggleButton.OnClicked += OnClickedActorAdminToggle;
            ui.YouButton.OnClicked += OnClickedActorYouButton;
            ui.RoleInput.OnTextUpdated += OnTypedActorRole;
            ui.Steam64Input.OnTextUpdated += OnTypedActorSteam64;
        }
        for (int i = 0; i < ModerationActionEvidence.Length; ++i)
        {
            ModerationSelectedEvidence ui = ModerationActionEvidence[i];
            ui.RemoveButton.OnClicked += OnClickedRemoveEvidence;
            ui.LinkInput.OnTextUpdated += OnTypedEvidenceLink;
            ui.MessageInput.OnTextUpdated += OnTypedEvidenceMessage;
            ui.TimestampInput.OnTextUpdated += OnTypedEvidenceTimestamp;
            ui.Steam64Input.OnTextUpdated += OnTypedEvidenceSteam64;
            ui.NowButton.OnClicked += OnClickedEvidenceNowButton;
            ui.YouButton.OnClicked += OnClickedEvidenceYouButton;
        }

        ModerationActionMessage.OnTextUpdated += OnMessageUpdated;
        MuteTypeTracker.OnValueUpdated += OnMuteTypeUpdated;
        ModerationActionInputBox3.OnTextUpdated += OnVehicleListUpdated;
        ModerationActionInputBox2.OnTextUpdated += OnReputationUpdated;
        ModerationActionMiniInputBox1.OnTextUpdated += OnDurationUpdated;
        ModerationActionMiniInputBox2.OnTextUpdated += OnDurationUpdated;

        ElementPatterns.SubscribeAll(ModerationActionControls, OnActionControlClicked);
    }

    private void OnEvidenceClicked(UnturnedButton button, Player player)
    {
        int index = Array.FindIndex(ModerationInfoEvidenceEntries, x => x.PreviewImageButton == button);
        if (index == -1)
            return;

        ModerationData data = GetOrAddModerationData(player.channel.owner.playerID.steamID.m_SteamID);
        if (data.SelectedEntry == null || index >= data.SelectedEntry.Evidence.Length)
            return;

        Evidence evidence = data.SelectedEntry.Evidence[index];
        player.sendBrowserRequest(evidence.Message, evidence.URL);
    }

    private void OnReset(UnturnedButton button, Player player)
    {
        WarfarePlayer ucp = _playerService.GetOnlinePlayer(player);
        ModerationData data = GetOrAddModerationData(ucp);
        data.SelectedPlayer = 0ul;
        EndEditInActionMenu(ucp);

        data.HistoryPage = 0;
        data.HistoryView = null;

        ModerationHistoryTypeButton.SetDefault(player);
        ModerationHistorySearchTypeButton.SetDefault(player);
        ModerationHistorySortModeButton.SetDefault(player);
        ModerationHistorySearch.SetText(ucp, string.Empty);

        UnturnedTextBoxData? textBoxData = UnturnedUIDataSource.GetData<UnturnedTextBoxData>(ucp.Steam64, ModerationHistorySearch);
        if (textBoxData != null)
            textBoxData.Text = string.Empty;

        UpdateSelectedPlayer(ucp);

        _ = RefreshModerationHistory(ucp, ucp.DisconnectToken);
    }
    private static string FormatReputation(double rep, IFormatProvider formatter, bool endTag)
    {
        string str = Math.Abs(rep).ToString("0.#", formatter);
        if (rep is < 0.01 and > -0.01)
            return str;
        str = "<#" + (rep > 0 ? PositiveReputationColor : NegativeReputationColor) + ">" + (rep > 0 ? "+" : "-") + str;
        if (endTag)
            str += "</color>";
        return str;
    }
    public LabeledStateButton? GetModerationButton(ModerationEntryType type) => type switch
    {
        ModerationEntryType.Note => ModerationButtonNote,
        ModerationEntryType.Commendation => ModerationButtonCommend,
        ModerationEntryType.BugReportAccepted => ModerationButtonAcceptedBugReport,
        ModerationEntryType.AssetBan => ModerationButtonAssetBan,
        ModerationEntryType.Warning => ModerationButtonWarn,
        ModerationEntryType.Kick => ModerationButtonKick,
        ModerationEntryType.Mute => ModerationButtonMute,
        ModerationEntryType.Ban => ModerationButtonBan,
        _ => null
    };
    private void OnClickedModeratePlayer(UnturnedButton button, Player player)
    {
        int index = Array.FindIndex(ModerationPlayerList, x => x.ModerateButton == button);
        if (index == -1)
            return;

        WarfarePlayer ucp = _playerService.GetOnlinePlayer(player);

        ModerationData data = GetOrAddModerationData(ucp);
        if (data.PlayerList == null || index >= data.PlayerList.Length)
            return;

        if (data.SelectedPlayer != data.PlayerList[index])
        {
            data.SelectedPlayer = data.PlayerList[index];
            EndEditInActionMenu(ucp);
        }
        
        UpdateSelectedPlayer(ucp);

        _ = RefreshModerationHistory(ucp, ucp.DisconnectToken);
    }
    private void OnClickedModerationEntry(UnturnedButton button, Player player)
    {
        int index = Array.FindIndex(ModerationHistory, x => x.Root == button);
        if (index == -1)
            return;

        WarfarePlayer ucp = _playerService.GetOnlinePlayer(player);

        ModerationData data = GetOrAddModerationData(ucp);
        index += data.HistoryPage * ModerationHistory.Length;
        if (data.HistoryView == null || index >= data.HistoryView.Length)
        {
            Logger!.LogWarning("Invalid history index: {0} (p. {1} / {2}).", index, data.HistoryPage, data.PageCount);
            return;
        }
        
        SelectEntry(ucp, data.HistoryView[index]);
    }
    private void OnModerationPlayerSearchModeUpdated(UnturnedEnumButton<PlayerSearchMode> button, Player player, PlayerSearchMode value)
    {
        WarfarePlayer ucp = _playerService.GetOnlinePlayer(Player.player);
        SendModerationPlayerList(ucp);
    }
    private void OnModerationPlayerSearchTextUpdated(UnturnedTextBox button, Player player, string text)
    {
        WarfarePlayer ucp = _playerService.GetOnlinePlayer(player);
        SendModerationPlayerList(ucp);
    }
    private void OnButtonCloseClicked(UnturnedButton button, Player player)
    {
        WarfarePlayer ucp = _playerService.GetOnlinePlayer(player);
        Close(ucp);
    }
    private void TryQueueHistoryUpdate(Player player, int ms = 500)
    {
        WarfarePlayer ucp = _playerService.GetOnlinePlayer(player);

        UniTask.Create(async () =>
        {
            ModerationData mod = GetOrAddModerationData(ucp);
            int v = Interlocked.Increment(ref mod.HistorySearchUpdateVersion);

            if (ms > 0)
            {
                await UniTask.Delay(ms, cancellationToken: ucp.DisconnectToken);
                if (v != mod.HistorySearchUpdateVersion)
                    return;
            }

            await RefreshModerationHistory(ucp, ucp.DisconnectToken);
        });
    }
    private void OnModerationHistorySortModeUpdated(UnturnedEnumButton<ModerationHistorySortMode> button, Player player, ModerationHistorySortMode value)
    {
        TryQueueHistoryUpdate(player);
    }
    private void OnModerationHistorySearchTypeUpdated(UnturnedEnumButton<ModerationHistorySearchMode> button, Player player, ModerationHistorySearchMode value)
    {
        UnturnedTextBoxData data = ModerationHistorySearch.GetOrAddData(player);

        if (string.IsNullOrEmpty(data.Text))
            return;
        TryQueueHistoryUpdate(player);
    }
    private void OnModerationHistoryTypeUpdated(UnturnedEnumButton<ModerationEntryType> button, Player player, ModerationEntryType value)
    {
        TryQueueHistoryUpdate(player);
    }
    private void OnModerationHistorySearched(UnturnedTextBox textbox, Player player, string text)
    {
        TryQueueHistoryUpdate(player);
    }
    private void OnModerationHistoryNext(UnturnedButton button, Player player)
    {
        ModerationData data = GetOrAddModerationData(player.channel.owner.playerID.steamID.m_SteamID);
        WarfarePlayer? ucp = _playerService.GetOnlinePlayerOrNull(player);

        ++data.HistoryPage;

        ModerationHistoryNextButton.Disable(player);
        ModerationHistoryBackButton.Disable(player);
        ModerationHistoryPage.Disable(player);
        ModerationHistoryPage.SetText(player, (data.HistoryPage + 1).ToString((IFormatProvider?)ucp?.Locale.ParseFormat ?? CultureInfo.InvariantCulture));

        TryQueueHistoryUpdate(player, 0);
    }
    private void OnModerationHistoryPageTyped(UnturnedTextBox textBox, Player player, string text)
    {
        ModerationData data = GetOrAddModerationData(player.channel.owner.playerID.steamID.m_SteamID);
        WarfarePlayer? ucp = _playerService.GetOnlinePlayerOrNull(player);
        if (!int.TryParse(text, out int page))
        {
            ModerationHistoryPage.SetText(player, (data.HistoryPage + 1).ToString((IFormatProvider?)ucp?.Locale.ParseFormat ?? CultureInfo.InvariantCulture));
            return;
        }

        --page;
        if (page < 0)
            page = 0;

        data.HistoryPage = page;

        ModerationHistoryNextButton.Disable(player);
        ModerationHistoryBackButton.Disable(player);
        ModerationHistoryPage.Disable(player);
        ModerationHistoryPage.SetText(player, (page + 1).ToString((IFormatProvider?)ucp?.Locale.ParseFormat ?? CultureInfo.InvariantCulture));

        TryQueueHistoryUpdate(player, 0);
    }
    private void OnModerationHistoryBack(UnturnedButton button, Player player)
    {
        ModerationData data = GetOrAddModerationData(player.channel.owner.playerID.steamID.m_SteamID);
        WarfarePlayer? ucp = _playerService.GetOnlinePlayerOrNull(player);
        --data.HistoryPage;
        if (data.HistoryPage < 0)
            data.HistoryPage = 0;

        ModerationHistoryNextButton.Disable(player);
        ModerationHistoryBackButton.Disable(player);
        ModerationHistoryPage.Disable(player);
        ModerationHistoryPage.SetText(player, (data.HistoryPage + 1).ToString((IFormatProvider?)ucp?.Locale.ParseFormat ?? CultureInfo.InvariantCulture));

        TryQueueHistoryUpdate(player, 0);
    }
    public async UniTask Open(WarfarePlayer player, CancellationToken token = default)
    {
        using CombinedTokenSources tokens = token.CombineTokensIfNeeded(player.DisconnectToken);
        await UniTask.SwitchToMainThread(token);

        // todo if (TeamSelector.Instance != null)
        // todo     TeamSelector.Instance.Close(player);

        // player.ModalNeeded = true;
        player.UnturnedPlayer.enablePluginWidgetFlag(EPluginWidgetFlags.Modal | EPluginWidgetFlags.ForceBlur);
        player.UnturnedPlayer.disablePluginWidgetFlag(EPluginWidgetFlags.Default);

        // if (!player.HasModerationUI)
        // {
        //     SendToPlayer(player.Connection);
        //     player.HasModerationUI = true;
        // }

        ModerationData data = GetOrAddModerationData(player);
        data.HistoryCount = 0;
        data.PlayerCount = 0;
        data.InfoActorCount = 0;
        data.InfoEvidenceCount = 0;
        await SetPage(player, Page.Moderation, false, token);
    }
    public void Close(WarfarePlayer player)
    {
        GameThread.AssertCurrent();

        // player.ModalNeeded = false;
        player.UnturnedPlayer.disablePluginWidgetFlag(EPluginWidgetFlags.Modal | EPluginWidgetFlags.ForceBlur);
        player.UnturnedPlayer.enablePluginWidgetFlag(EPluginWidgetFlags.Default);
        ClearFromPlayer(player.Connection);
        // player.HasModerationUI = false;
    }
    public async UniTask SetPage(WarfarePlayer player, Page page, bool isAlreadyInView, CancellationToken token = default)
    {
        using CombinedTokenSources tokens = token.CombineTokensIfNeeded(player.DisconnectToken);
        if (page is not Page.Moderation and not Page.Players and not Page.Tickets and not Page.Logs)
            throw new ArgumentOutOfRangeException(nameof(page));
        await UniTask.SwitchToMainThread(token);
        if (!isAlreadyInView)
        {
            PageLogic[(int)page].Show(player);
        }
        await (page switch
        {
            Page.Moderation => PrepareModerationPage(player, token),
            Page.Players => PreparePlayersPage(player, token),
            Page.Tickets => PrepareTicketsPage(player, token),
            _ => PrepareLogsPage(player, token)
        });
    }
    private async UniTask PrepareModerationPage(WarfarePlayer player, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        UpdateSelectedPlayer(player);

        // todo await _steamAPI.TryDownloadAllPlayerSummaries(token: token);
        await UniTask.SwitchToMainThread(token);

        ModerationData data = GetOrAddModerationData(player);
        
        UnturnedTextBoxData? textBoxData = UnturnedUIDataSource.GetData<UnturnedTextBoxData>(player.Steam64, ModerationPlayerSearch);
        if (textBoxData != null)
            ModerationPlayerSearch.SetText(player, textBoxData.Text ?? string.Empty);

        ModerationHistoryTypeButton.Update(player.UnturnedPlayer, false);
        ModerationHistorySearchTypeButton.Update(player.UnturnedPlayer, false);
        ModerationHistorySortModeButton.Update(player.UnturnedPlayer, false);
        ModerationPlayerSearchModeButton.Update(player.UnturnedPlayer, false);

        SendModerationPlayerList(player);

        int i = 0;
        int ct = Math.Min(PunishmentPresets.Presets.Count, Presets.Length);
        for (; i < ct; ++i)
        {
            if (PunishmentPresets.TryGetPreset((PresetType)(i + 1), out _))
            {
                Presets[i].SetText(player, GetPresetButtonText((PresetType)(i + 1)));
            }
            else
            {
                Presets[i].Disable(player);
            }
        }

        for (; i < Presets.Length; ++i)
        {
            Presets[i].Button.Hide(player);
        }

        SelectEntry(player, data.SelectedEntry);

        LoadActionMenu(player, false);

        await RefreshModerationHistory(player, token);
    }
    private UniTask PreparePlayersPage(WarfarePlayer player, CancellationToken token = default)
    {
        return UniTask.CompletedTask;
    }
    private UniTask PrepareTicketsPage(WarfarePlayer player, CancellationToken token = default)
    {
        return UniTask.CompletedTask;
    }
    private UniTask PrepareLogsPage(WarfarePlayer player, CancellationToken token = default)
    {
        return UniTask.CompletedTask;
    }
    public ModerationData? GetModerationData(WarfarePlayer player)
    {
        GameThread.AssertCurrent();

        return UnturnedUIDataSource.GetData<ModerationData>(player.Steam64, Headers[(int)Page.Moderation].Button);
    }
    public ModerationData GetOrAddModerationData(WarfarePlayer player) => GetOrAddModerationData(player.Steam64.m_SteamID);
    public ModerationData GetOrAddModerationData(ulong steam64)
    {
        GameThread.AssertCurrent();

        ModerationData? data = UnturnedUIDataSource.GetData<ModerationData>(new CSteamID(steam64), Headers[(int)Page.Moderation].Button);
        if (data == null)
        {
            data = new ModerationData(new CSteamID(steam64), this);
            UnturnedUIDataSource.AddData(data);
        }

        return data;
    }
    private void UpdateSelectedPlayer(WarfarePlayer player)
    {
        ModerationData data = GetOrAddModerationData(player);
        if (new CSteamID(data.SelectedPlayer).GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
        {
            ModerationActionPlayerHeader.SetText(player, string.Empty);
            ModerationFormRoot.Hide(player);
            ActionButtonBox.Hide(player);
            ModerationActionHeader.SetText(player, "Actions");
            return;
        }

        
        if (_playerService.GetOnlinePlayerOrNull(data.SelectedPlayer) == null)
        {
            ModerationButtonKick.Disable(player);
            if (data.PendingType == ModerationEntryType.Kick)
            {
                data.PendingType = ModerationEntryType.None;
                data.PendingPreset = PresetType.None;
                LoadActionMenu(player, true);
            }
        }
        else
        {
            ModerationButtonKick.Enable(player);
        }

        UniTask.Create(async () =>
        {
            ValueTask<PlayerNames> nameTask = F.GetPlayerOriginalNamesAsync(data.SelectedPlayer, player.DisconnectToken);
            if (nameTask.IsCompleted)
            {
                ModerationActionPlayerHeader.SetText(player, nameTask.Result.PlayerName + " (" + data.SelectedPlayer.ToString(CultureInfo.InvariantCulture) + ")");
                ModerationActionHeader.SetText(player, "Actions - " + nameTask.Result.PlayerName);
            }
            else
            {
                PlayerNames names = await nameTask;
                ModerationActionPlayerHeader.SetText(player, names.PlayerName + " (" + data.SelectedPlayer.ToString(CultureInfo.InvariantCulture) + ")");
                ModerationActionHeader.SetText(player, "Actions - " + names.PlayerName);
            }

            ActionButtonBox.Show(player);

        });
    }
    public void SetHistoryPage(WarfarePlayer player, ModerationData data, int page)
    {
        ModerationEntry[] entries = data.HistoryView ?? Array.Empty<ModerationEntry>();
        data.HistoryPage = page;
        int pgCt = data.PageCount;
        int offset = page * ModerationHistoryLength;
        ModerationHistoryPage.SetText(player.Connection, (page + 1).ToString(player.Locale.ParseFormat));
        if (page > 0)
            ModerationHistoryBackButton.Enable(player.Connection);
        if (page < pgCt - 1)
            ModerationHistoryNextButton.Enable(player.Connection);
        if (pgCt > 1)
            ModerationHistoryPage.Enable(player.Connection);

        int c = 0;
        for (int i = 0; i < ModerationHistoryLength; ++i)
        {
            int index = i + offset;
            if (index >= entries.Length)
            {
                if (data.HistoryCount <= i)
                    break;
                ModerationHistory[i].Root.SetVisibility(player, false);
            }
            else
            {
                UpdateModerationEntry(player, i, entries[index]);
                c = i + 1;

                if (i >= data.HistoryCount)
                    ModerationHistory[i].Root.SetVisibility(player, true);
            }
        }

        data.HistoryCount = c;
    }
    public void SendModerationPlayerList(WarfarePlayer player)
    {
        GameThread.AssertCurrent();

        ITransportConnection connection = player.Connection;

        if (!ModerationPlayerSearchModeButton.TryGetSelection(player.UnturnedPlayer, out PlayerSearchMode searchMode))
            searchMode = PlayerSearchMode.Online;
        ModerationData data = GetOrAddModerationData(player);
        UnturnedTextBoxData? textBoxData = UnturnedUIDataSource.GetData<UnturnedTextBoxData>(player.Steam64, ModerationPlayerSearch);
        string searchText = textBoxData?.Text ?? string.Empty;
        data.PlayerList ??= new ulong[ModerationPlayerList.Length];
        if (searchText.Length < 1 || searchMode == PlayerSearchMode.Online)
        {
            IReadOnlyList<WarfarePlayer> buffer;
            bool clr;
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                // todo _playerService.Search(searchText, NameSearchPriority.PlayerName, _tempPlayerSearchBuffer);
                buffer = _tempPlayerSearchBuffer;
                clr = true;
            }
            else
            {
                buffer = _playerService.OnlinePlayers;
                clr = false;
            }

            try
            {
                int ct = Math.Min(ModerationPlayerList.Length, buffer.Count);
                int i = 0;
                for (; i < ct; ++i)
                {
                    WarfarePlayer listPlayer = buffer[i];
                    PlayerListEntry entry = ModerationPlayerList[i];
                    entry.SteamId.SetText(connection, listPlayer.Steam64.m_SteamID.ToString(CultureInfo.InvariantCulture));
                    entry.Name.SetText(connection, listPlayer.Names.PlayerName);
                    if (_moderationSql.TryGetAvatar(listPlayer.Steam64.m_SteamID, AvatarSize.Small, out string avatarUrl))
                        entry.ProfilePicture.SetImage(connection, avatarUrl);
                    else
                    {
                        entry.ProfilePicture.SetImage(connection, string.Empty);
                        UniTask.Create(async () =>
                        {
                            string? icon = null;// await listPlayer.GetProfilePictureURL(AvatarSize.Small, player.DisconnectToken);
                            await UniTask.SwitchToMainThread(player.DisconnectToken);
                            entry.ProfilePicture.SetImage(player, icon ?? string.Empty);
                        });
                    }

                    if (i >= data.InfoActorCount)
                        entry.Root.SetVisibility(player, true);

                    data.PlayerList[i] = listPlayer.Steam64.m_SteamID;
                }

                for (; i < data.PlayerCount; ++i)
                {
                    ModerationPlayerList[i].Root.SetVisibility(connection, false);
                    data.PlayerList[i] = 0;
                }

                data.PlayerCount = ct;
            }
            finally
            {
                if (clr)
                    ((IList<WarfarePlayer>)buffer).Clear();
            }
        }
        else
        {
            UniTask.Create(async () =>
            {
                CancellationToken token = player.DisconnectToken;
                ITransportConnection connection = player.Connection;

                token.ThrowIfCancellationRequested();

                int version = Interlocked.Increment(ref data.SearchVersion);
                List<PlayerNames> names = new List<PlayerNames>(); // todo await Data.AdminSql.SearchAllPlayers(searchText, UCPlayer.NameSearch.PlayerName, true, token);

                if (data.SearchVersion != version)
                    return;

                token.ThrowIfCancellationRequested();

                await UniTask.SwitchToMainThread(token);

                int ct = Math.Min(ModerationPlayerList.Length, names.Count);
                int i2 = 0;
                for (; i2 < ct; ++i2)
                {
                    PlayerNames name = names[i2];
                    PlayerListEntry entry = ModerationPlayerList[i2];
                    entry.SteamId.SetText(connection, name.Steam64.m_SteamID.ToString(CultureInfo.InvariantCulture));
                    entry.Name.SetText(connection, name.PlayerName);
                    if (_moderationSql.TryGetAvatar(name.Steam64.m_SteamID, AvatarSize.Small, out string avatarUrl))
                        entry.ProfilePicture.SetImage(connection, avatarUrl);
                    else
                        entry.ProfilePicture.SetImage(connection, string.Empty);

                    entry.Root.SetVisibility(player.Connection, true);
                    if (i2 >= data.InfoActorCount)
                        entry.Root.SetVisibility(player.Connection, true);

                    data.PlayerList[i2] = name.Steam64.m_SteamID;
                }

                for (; i2 < data.PlayerCount; ++i2)
                {
                    ModerationPlayerList[i2].Root.SetVisibility(connection, false);
                    data.PlayerList[i2] = 0;
                }

                data.PlayerCount = ct;

                
                // await _moderationSql.CacheAvatars(names.Select(x => x.Steam64.m_SteamID), token);
#if DEBUG
                GameThread.AssertCurrent();
#endif
                for (int i = 0; i < ct; ++i)
                {
                    PlayerNames name = names[i];
                    if (_moderationSql.TryGetAvatar(name.Steam64.m_SteamID, AvatarSize.Small, out string avatarUrl))
                        ModerationPlayerList[i].ProfilePicture.SetImage(connection, avatarUrl);
                }
            });
        }
    }
    public void SelectEntry(WarfarePlayer player, ModerationEntry? entry)
    {
        GameThread.AssertCurrent();

        ModerationData data = GetOrAddModerationData(player);
        if (entry != null && data.SelectedEntry == entry)
        {
            if (Time.realtimeSinceStartup - data.LastViewedTime is <= 1.5f and > 0f)
            {
                if (!EditEntry(player, entry))
                {
                    EndEditInActionMenu(player);
                }
                return;
            }

            data.LastViewedTime = Time.realtimeSinceStartup;
        }
        else
        {
            data.SelectedEntry = entry;
            data.LastViewedTime = Time.realtimeSinceStartup;
        }
        int v = Interlocked.Increment(ref data.InfoVersion);
        ITransportConnection c = player.Connection;

        if (entry == null)
        {
            ModerationInfoRoot.SetVisibility(c, false);
            return;
        }

        ModerationInfoType.SetText(c, entry.GetDisplayName());
        ModerationInfoTimestamp.SetText(c, (entry.ResolvedTimestamp ?? entry.StartedTimestamp).UtcDateTime.ToString(DateTimeFormat, CultureInfo.InvariantCulture));
        ModerationInfoReputation.SetText(c, FormatReputation(entry.Reputation, player.Locale.CultureInfo, false) + " Reputation");
        ModerationInfoReason.SetText(c, string.IsNullOrWhiteSpace(entry.Message) ? "No message." : entry.Message!);
        ModerationInfoPlayerId.SetText(c, entry.Player.ToString(CultureInfo.InvariantCulture));
        if (_moderationSql.TryGetAvatar(entry.Player, AvatarSize.Full, out string avatarUrl))
            ModerationInfoProfilePicture.SetImage(c, avatarUrl);
        else
        {
            ModerationInfoProfilePicture.SetImage(c, string.Empty);
            UniTask.Create(async () =>
            {
                string? icon = null; // todo await F.GetProfilePictureURL(entry.Player, AvatarSize.Full, player.DisconnectToken);

                await UniTask.SwitchToMainThread(player.DisconnectToken);

                if (data.InfoVersion == v)
                    ModerationInfoProfilePicture.SetImage(player.Connection, icon ?? string.Empty);
            });
        }

        ModerationInfoActorsHeader.SetVisibility(c, entry.Actors.Length > 0);
        ModerationInfoEvidenceHeader.SetVisibility(c, entry.Evidence.Length > 0);
        ModerationInfoRoot.SetVisibility(c, true);

        if (entry.Evidence.Length > 0)
        {
            int i = 0;
            int ct = Math.Min(entry.Evidence.Length, ModerationInfoEvidenceEntries.Length);
            for (; i < ct; ++i)
            {
                Evidence evidence = entry.Evidence[i];
                ModerationInfoEvidence evidenceUi = ModerationInfoEvidenceEntries[i];

                evidenceUi.Link.SetText(c, evidence.URL);
                evidenceUi.ActorId.SetText(c, evidence.Actor.Id.ToString(CultureInfo.InvariantCulture));
                evidenceUi.Timestamp.SetText(c, evidence.Timestamp.UtcDateTime.ToString(DateTimeFormat, player.Locale.CultureInfo));

                string name;
                if (evidence.URL.Length > 1)
                {
                    int lastSlash = evidence.URL.LastIndexOf('/');
                    if (lastSlash == evidence.URL.Length - 1)
                        lastSlash = evidence.URL.LastIndexOf('/', lastSlash - 1);

                    name = lastSlash < 0 ? evidence.URL : evidence.URL.Substring(lastSlash + 1);
                }
                else name = evidence.URL;

                if (evidence.Image)
                {
                    evidenceUi.PreviewImage.SetVisibility(c, true);
                    evidenceUi.PreviewMessage.SetVisibility(c, true);
                    evidenceUi.PreviewName.SetVisibility(c, true);
                    evidenceUi.PreviewImage.SetImage(c, evidence.URL);
                    evidenceUi.PreviewMessage.SetText(c, evidence.Message ?? "No message.");
                    evidenceUi.PreviewName.SetText(c, name);
                    evidenceUi.NoPreviewMessage.SetVisibility(c, false);
                    evidenceUi.NoPreviewName.SetVisibility(c, false);
                }
                else
                {
                    evidenceUi.NoPreviewMessage.SetVisibility(c, true);
                    evidenceUi.NoPreviewName.SetVisibility(c, true);
                    evidenceUi.NoPreviewMessage.SetText(c, evidence.Message ?? "No message.");
                    evidenceUi.NoPreviewName.SetText(c, name);
                    evidenceUi.PreviewImage.SetVisibility(c, false);
                    evidenceUi.PreviewMessage.SetVisibility(c, false);
                    evidenceUi.PreviewName.SetVisibility(c, false);
                }

                if (i >= data.InfoActorCount)
                    evidenceUi.Root.SetVisibility(c, true);
            }

            for (; i < data.InfoEvidenceCount; ++i)
                ModerationInfoEvidenceEntries[i].Root.SetVisibility(c, false);

            data.InfoEvidenceCount = ct;

            UniTask.Create(async () =>
            {
                if (data.InfoVersion != v)
                    return;
                for (int j = 0; j < ct; ++j)
                {
                    IModerationActor actor = entry.Evidence[j].Actor;
                    ValueTask<string> name = new ValueTask<string>(actor.ToString()); // todo actor.GetDisplayName(_moderationSql, player.DisconnectToken);
                    UnturnedLabel nameLbl = ModerationInfoEvidenceEntries[j].ActorName;
                    if (name.IsCompleted)
                    {
                        nameLbl.SetText(c, name.Result);
                    }
                    else
                    {
                        string nameText = await name.ConfigureAwait(false);

                        if (data.InfoVersion == v)
                            nameLbl.SetText(c, nameText);
                    }
                }
            });
        }
        else
        {
            for (int i = data.InfoEvidenceCount - 1; i >= 0; --i)
            {
                ModerationInfoEvidenceEntries[i].Root.SetVisibility(c, false);
            }
        }

        if (entry.Actors.Length > 0)
        {
            UniTask.Create(async () =>
            {
                if (data.InfoVersion != v)
                    return;
                int i = 0;
                int ct = Math.Min(ModerationInfoActors.Length, entry.Actors.Length);
                List<ulong> profilePictures = new List<ulong>(ct);
                for (int j = 0; j < ct; ++j)
                {
                    RelatedActor actor = entry.Actors[i];
                    if (new CSteamID(actor.Actor.Id).GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
                        profilePictures.Add(actor.Actor.Id);
                }

                //await _moderationSql.CacheAvatars(profilePictures, player.DisconnectToken);

                if (data.InfoVersion != v)
                    return;
                for (; i < ct; ++i)
                {
                    RelatedActor actor = entry.Actors[i];
                    ModerationInfoActor actorUi = ModerationInfoActors[i];
                    actorUi.Role.SetText(c, string.IsNullOrWhiteSpace(actor.Role) ? "No role" : actor.Role);
                    if (new CSteamID(actor.Actor.Id).GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
                        actorUi.Steam64.SetText(c, actor.Actor.Id.ToString(CultureInfo.InvariantCulture));
                    else actorUi.Steam64.SetText(c, "...");
                    if (i >= data.InfoActorCount)
                        actorUi.Root.SetVisibility(c, true);
                }

                for (; i < data.InfoActorCount; ++i)
                    ModerationInfoActors[i].Root.SetVisibility(c, false);

                data.InfoActorCount = ct;

                for (int j = 0; j < ct; ++j)
                {
                    RelatedActor actor = entry.Actors[j];
                    ModerationInfoActor actorUi = ModerationInfoActors[j];
                    ValueTask<string> unTask = actor.Actor.GetDisplayName(_moderationSql, player.DisconnectToken);
                    ValueTask<string?> imgTask;

                    if (new CSteamID(actor.Actor.Id).GetEAccountType() == EAccountType.k_EAccountTypeIndividual
                        && _moderationSql.TryGetAvatar(actor.Actor.Id, AvatarSize.Medium, out string avatar))
                    {
                        imgTask = new ValueTask<string?>(avatar);
                    }
                    else
                    {
                        imgTask = actor.Actor.GetProfilePictureURL(_moderationSql, AvatarSize.Medium, player.DisconnectToken);
                    }

                    bool imgDone = false;
                    if (unTask.IsCompleted)
                    {
                        actorUi.Name.SetText(c, unTask.Result ?? actor.Actor.ToString());
                    }
                    else
                    {
                        if (imgTask.IsCompleted)
                        {
                            actorUi.ProfilePicture.SetImage(c, imgTask.Result ?? string.Empty);
                            imgDone = true;
                        }
                        string name = await unTask ?? actor.Actor.ToString();
                        if (data.InfoVersion != v)
                            return;
                        actorUi.Name.SetText(c, name);
                    }
                    if (imgTask.IsCompleted)
                    {
                        if (!imgDone)
                            actorUi.ProfilePicture.SetImage(c, imgTask.Result ?? string.Empty);
                    }
                    else if (!imgDone)
                    {
                        string url = await imgTask ?? string.Empty;
                        if (data.InfoVersion != v)
                            return;
                        actorUi.ProfilePicture.SetImage(c, url);
                    }
                }

            });
        }
        else
        {
            for (int i = data.InfoActorCount - 1; i >= 0; --i)
            {
                ModerationInfoActors[i].Root.SetVisibility(c, false);
            }
        }

        UniTask.Create(async () =>
        {
            PlayerNames names = await F.GetPlayerOriginalNamesAsync(entry.Player, player.DisconnectToken).ConfigureAwait(false);
            if (data.InfoVersion == v)
                ModerationInfoPlayerName.SetText(c, names.ToString(false));
        });

        UniTask.Create(async () =>
        {
            List<string> extraInfo = new List<string>();
            await entry.AddExtraInfo(_moderationSql, extraInfo, player.Locale.CultureInfo, player.DisconnectToken);

            await UniTask.SwitchToMainThread(player.DisconnectToken);

            if (data.InfoVersion != v)
                return;

            int ct = Math.Min(extraInfo.Count, ModerationInfoExtraInfo.Length);
            int i = 0;
            for (; i < ct; ++i)
            {
                ModerationInfoExtraInfo[i].SetVisibility(c, true);
                ModerationInfoExtraInfo[i].SetText(c, extraInfo[i]);
            }

            for (; i < ModerationInfoExtraInfo.Length; ++i)
                ModerationInfoExtraInfo[i].SetVisibility(c, false);

            await UniTask.WaitForSeconds(0.125f, true, cancellationToken: player.DisconnectToken);

            if (data.InfoVersion == v)
                LogicModerationInfoUpdateScrollVisual.SetVisibility(c, true);
        });
    }
    private void UpdateModerationEntry(WarfarePlayer player, int index, ModerationEntry entry)
    {
        ITransportConnection connection = player.Connection;

        ModerationHistoryEntry ui = ModerationHistory[index];
        ModerationEntryType? type = ModerationReflection.GetType(entry.GetType());
        ui.Type.SetText(connection, type.HasValue ? type.Value.ToString() : entry.GetType().Name);
        string? msg = entry.GetDisplayMessage();
        ui.Message.SetText(connection, string.IsNullOrWhiteSpace(msg) ? "== No Message ==" : msg);
        ui.Reputation.SetText(connection, FormatReputation(entry.Reputation, player.Locale.CultureInfo, false));
        ui.Timestamp.SetText(connection, (entry.ResolvedTimestamp ?? entry.StartedTimestamp).UtcDateTime.ToString(DateTimeFormat));
        if (entry.TryGetDisplayActor(out RelatedActor actor))
        {
            if (!actor.Actor.Async)
            {
                ui.Admin.SetText(connection, actor.Actor.GetDisplayName(_moderationSql, CancellationToken.None).Result);
                ui.AdminProfilePicture.SetImage(connection, actor.Actor.GetProfilePictureURL(_moderationSql, AvatarSize.Medium, CancellationToken.None).Result ?? string.Empty, false);
            }
            else
            {
                bool av = _moderationSql.TryGetAvatar(actor.Actor, AvatarSize.Medium, out string avatarUrl);
                bool nm = _moderationSql.TryGetUsernames(actor.Actor, out PlayerNames names);
                if (av)
                    ui.AdminProfilePicture.SetImage(connection, avatarUrl);
                else
                    ui.AdminProfilePicture.SetImage(connection, Provider.configData.Browser.Icon);
                if (nm)
                    ui.Admin.SetText(connection, names.PlayerName);
                else
                    ui.Admin.SetText(connection, "...");
                if (!av || !nm)
                {
                    UniTask.Create(async () =>
                    {
                        ValueTask<string?> pfpTask = av ? new ValueTask<string?>(avatarUrl) : actor.Actor.GetProfilePictureURL(_moderationSql, AvatarSize.Medium, player.DisconnectToken);
                        ValueTask<string> displayNameTask = nm ? new ValueTask<string>(names.PlayerName) : actor.Actor.GetDisplayName(_moderationSql, player.DisconnectToken);
                        string displayName = await displayNameTask.ConfigureAwait(false);
                        string? icon = await pfpTask.ConfigureAwait(false);
                        await UniTask.SwitchToMainThread(player.DisconnectToken);
                        if (!nm)
                            ui.Admin.SetText(player.Connection, displayName ?? actor.Actor.Id.ToString());
                        if (!av)
                            ui.AdminProfilePicture.SetImage(player.Connection, icon ?? string.Empty);
                    });
                }
            }
        }
        else
        {
            ui.Admin.SetText(connection, "No Admin");
            ui.AdminProfilePicture.SetImage(connection, Provider.configData.Browser.Icon);
        }

        if (entry is IDurationModerationEntry duration)
        {
            ui.Duration.SetText(connection, duration.IsPermanent ? "∞" : FormattingUtility.ToTimeString(duration.Duration, 2));
            ui.Icon.SetText(connection, string.Empty);
        }
        else
        {
            ui.Duration.SetText(connection, string.Empty);
            Guid? icon = entry.GetIcon();
            ui.Icon.SetText(connection, icon.HasValue ? _itemIconProvider.GetIcon(icon.Value, tmpro: true) : string.Empty);
        }
    }
    private async UniTask RefreshModerationHistory(WarfarePlayer player, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        ModerationHistoryNextButton.Disable(player.Connection);
        ModerationHistoryBackButton.Disable(player.Connection);
        ModerationHistoryPage.Disable(player.Connection);

        ModerationData data = GetOrAddModerationData(player);
        UnturnedTextBoxData? textBoxData = UnturnedUIDataSource.GetData<UnturnedTextBoxData>(player.Steam64, ModerationHistorySearch);
        ModerationHistoryTypeButton.TryGetSelection(player.UnturnedPlayer, out ModerationEntryType filter);
        ModerationHistorySearchTypeButton.TryGetSelection(player.UnturnedPlayer, out ModerationHistorySearchMode searchMode);
        ModerationHistorySortModeButton.TryGetSelection(player.UnturnedPlayer, out ModerationHistorySortMode sortMode);
        Type type = (filter == ModerationEntryType.None ? null : ModerationReflection.GetType(filter)) ?? typeof(ModerationEntry);
        DateTimeOffset? start = null, end = null;
        string? orderBy = null;
        string? text = textBoxData?.Text;

        bool noType = !(searchMode == ModerationHistorySearchMode.Type && text is { Length: > 0 } || filter != ModerationEntryType.None);

        string condition = !noType ? string.Empty : (
            $"(`main`.`{DatabaseInterface.ColumnEntriesType}` != '{ModerationEntryType.Teamkill}' AND " +
            $"`main`.`{DatabaseInterface.ColumnEntriesType}` != '{ModerationEntryType.VehicleTeamkill}')");

        object[]? conditionArgs = null;

        if (text is { Length: > 0 })
        {
            if (searchMode is ModerationHistorySearchMode.Before or ModerationHistorySearchMode.After
                && DateTimeOffset.TryParse(text, player.Locale.CultureInfo, DateTimeStyles.AssumeUniversal, out DateTimeOffset offset))
            {
                if (searchMode == ModerationHistorySearchMode.Before)
                    end = offset;
                else
                    start = offset;
            }
            else if (searchMode == ModerationHistorySearchMode.Message)
            {
                if (noType)
                    condition += " AND ";
                condition += $"`main`.`{DatabaseInterface.ColumnEntriesMessage}` LIKE {{0}}";
                conditionArgs = [ "%" + text + "%" ];
            }
            else if (searchMode == ModerationHistorySearchMode.Admin)
            {
                if (noType)
                    condition += " AND ";
                if (ulong.TryParse(text, NumberStyles.Number, player.Locale.CultureInfo, out ulong steam64) && new CSteamID(steam64).GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
                {
                    condition += $"EXISTS (SELECT COUNT(*) FROM `{DatabaseInterface.TableActors}` AS `a` " +
                                 $"WHERE `a`.`{DatabaseInterface.ColumnExternalPrimaryKey}` = `main`.`{DatabaseInterface.ColumnEntriesPrimaryKey}` " +
                                 $"AND `a`.`{DatabaseInterface.ColumnActorsId}`={{0}} " +
                                 $"AND `a`.`{DatabaseInterface.ColumnActorsAsAdmin}` != 0) " +
                                $"> 0";
                    conditionArgs = [ steam64 ];
                }
                else
                {
                    condition += $"(SELECT COUNT(*) FROM `{DatabaseInterface.TableActors}` AS `a` " +
                                 $"WHERE `a`.`{DatabaseInterface.ColumnExternalPrimaryKey}` = `main`.`{DatabaseInterface.ColumnEntriesPrimaryKey}` " +
                                 $"AND " +
                                 $"EXISTS (SELECT COUNT(*) FROM `{DatabaseInterface.TableUsernames}` AS `u` " +
                                  $"WHERE `a`.`{DatabaseInterface.ColumnActorsId}`=`u`.`{DatabaseInterface.ColumnUsernamesSteam64}` " +
                                 $"AND " +
                                  $"(`u`.`{DatabaseInterface.ColumnUsernamesPlayerName}` LIKE {{0}} OR `u`.`{DatabaseInterface.ColumnUsernamesCharacterName}` LIKE {{0}} OR `u`.`{DatabaseInterface.ColumnUsernamesNickName}` LIKE {{0}}))" +
                                 $" > 0)" +
                                $" > 0";
                    conditionArgs = [ "%" + text + "%" ];
                }
            }
            else if (!noType)
            {
                if (noType)
                    condition += " AND";
                if (filter != ModerationEntryType.None)
                {
                    condition += $"`main`.`{DatabaseInterface.ColumnEntriesType}` = {{0}}";
                    conditionArgs = [ filter.ToString() ];
                }
                else if (Enum.TryParse(text, true, out ModerationEntryType entryType))
                {
                    condition += $"`main`.`{DatabaseInterface.ColumnEntriesType}` = {{0}}";
                    conditionArgs = [ entryType.ToString() ];
                }
                else
                {
                    condition += $"`main`.`{DatabaseInterface.ColumnEntriesType}` LIKE {{0}}";
                    conditionArgs = [ "%" + text + "%" ];
                }
            }
        }

        if (sortMode is ModerationHistorySortMode.Latest or ModerationHistorySortMode.Oldest)
        {
            orderBy = $"`main`.`{DatabaseInterface.ColumnEntriesStartTimestamp}`";
            if (sortMode == ModerationHistorySortMode.Latest)
                orderBy += " DESC";
        }
        else if (sortMode is ModerationHistorySortMode.HighestReputation or ModerationHistorySortMode.LowestReputation)
        {
            orderBy = $"`main`.`{DatabaseInterface.ColumnEntriesReputation}`";
            if (sortMode == ModerationHistorySortMode.HighestReputation)
                orderBy += " DESC";
        }
        else if (sortMode == ModerationHistorySortMode.Type)
        {
            orderBy = $"`main`.`{DatabaseInterface.ColumnEntriesType}`";
        }

        ModerationEntry[] entries;

        bool validPlayer = new CSteamID(data.SelectedPlayer).GetEAccountType() == EAccountType.k_EAccountTypeIndividual;
        bool showRecentActors = text is not { Length: > 0 } && !validPlayer;
        if (showRecentActors || validPlayer)
        {
            entries = (ModerationEntry[])await _moderationSql.ReadAll(type, showRecentActors ? player.Steam64 : new CSteamID(data.SelectedPlayer),
                showRecentActors ? ActorRelationType.IsActor : ActorRelationType.IsTarget, false, true, start, end, condition, orderBy, conditionArgs, token);
        }
        else
        {
            entries = (ModerationEntry[])await _moderationSql.ReadAll(type, false, true, start, end, condition, orderBy, conditionArgs, token);
        }

        await UniTask.SwitchToMainThread(token);

        data.HistoryView = entries;
        int pgCt = data.PageCount;
        int newPage = data.HistoryPage >= pgCt ? (pgCt - 1) : data.HistoryPage;
        int historyOffset = newPage * ModerationHistoryLength;
        List<IModerationActor> usernamesAndPicturesToCache = new List<IModerationActor>();

        for (int i = 0; i < ModerationHistoryLength; ++i)
        {
            int index = i + historyOffset;
            if (index >= entries.Length || index < 0)
                break;
            if (entries[index].TryGetDisplayActor(out RelatedActor actor) && actor.Actor.Async)
                usernamesAndPicturesToCache.Add(actor.Actor);
        }

        UniTask.Create(async () =>
        {
            ulong[] steam64Ids = await _moderationSql.GetActorSteam64IDs(usernamesAndPicturesToCache, player.DisconnectToken);

            Task usernames = _moderationSql.CacheUsernames(steam64Ids, player.DisconnectToken);

            // todo await _moderationSql.CacheAvatars(steam64Ids, player.DisconnectToken);
            await usernames;

            await UniTask.SwitchToMainThread(player.DisconnectToken);
            SetHistoryPage(player, data, newPage);
        });
    }
    public enum Page
    {
        Moderation,
        Players,
        Tickets,
        Logs
    }
    public class PlayerListEntry
    {
        [Pattern(Root = true)]
        public UnturnedUIElement Root { get; set; }

        [Pattern("ModerationPlayerName_{0}")]
        public UnturnedLabel Name { get; set; }

        [Pattern("ModerationPlayerModerateButton_{0}")]
        public UnturnedButton ModerateButton { get; set; }

        [Pattern("ModerationPlayerModerateButtonLabel_{0}", AdditionalPath = "ModerationPlayerModerateButton_{0}")]
        public UnturnedLabel ModerateButtonLabel { get; set; }

        [Pattern("ModerationPlayerSteamID_{0}")]
        public UnturnedLabel SteamId { get; set; }

        [Pattern("ModerationPlayerPfp_{0}", AdditionalPath = "ModerationPlayerPfpMask_{0}")]
        public UnturnedImage ProfilePicture { get; set; }
    }
    public class ModerationHistoryEntry
    {
        [Pattern(Root = true)]
        public UnturnedButton Root { get; set; }

        [Pattern("ModerationEntryType_{0}")]
        public UnturnedLabel Type { get; set; }

        [Pattern("ModerationEntryReputation_{0}")]
        public UnturnedLabel Reputation { get; set; }

        [Pattern("ModerationEntryDuration_{0}")]
        public UnturnedLabel Duration { get; set; }

        [Pattern("ModerationEntryIcon_{0}")]
        public UnturnedLabel Icon { get; set; }

        [Pattern("ModerationEntryMessage_{0}")]
        public UnturnedLabel Message { get; set; }

        [Pattern("ModerationEntryAdminPfp_{0}", AdditionalPath = "ModerationEntryAdminPfpMask_{0}")]
        public UnturnedImage AdminProfilePicture { get; set; }

        [Pattern("ModerationEntryAdmin_{0}")]
        public UnturnedLabel Admin { get; set; }

        [Pattern("ModerationEntryTimestamp_{0}")]
        public UnturnedLabel Timestamp { get; set; }
    }
    public class ModerationInfoActor
    {
        [Pattern(Root = true)]
        public UnturnedUIElement Root { get; set; }

        [Pattern("ModerationPfpActor_{0}", AdditionalPath = "ModerationPfpActorMask_{0}")]
        public UnturnedImage ProfilePicture { get; set; }

        [Pattern("ModerationNameActor_{0}")]
        public UnturnedLabel Name { get; set; }

        [Pattern("ModerationSteam64Actor_{0}")]
        public UnturnedLabel Steam64 { get; set; }

        [Pattern("ModerationRoleActor_{0}")]
        public UnturnedLabel Role { get; set; }
    }
    public class ModerationInfoEvidence
    {
        [Pattern(Root = true)]
        public UnturnedUIElement Root { get; set; }

        [Pattern("ModerationPreviewEvidence_{0}", AdditionalPath = "ModerationPreviewEvidenceMask_{0}")]
        public UnturnedImage PreviewImage { get; set; }

        [Pattern("ModerationOpenEvidence_{0}")]
        public UnturnedButton PreviewImageButton { get; set; }

        [Pattern("ModerationPreviewNameEvidence_{0}")]
        public UnturnedLabel PreviewName { get; set; }

        [Pattern("ModerationNoPreviewNameEvidence_{0}")]
        public UnturnedLabel NoPreviewName { get; set; }

        [Pattern("ModerationPreviewMessageEvidence_{0}")]
        public UnturnedLabel PreviewMessage { get; set; }

        [Pattern("ModerationNoPreviewMessageEvidence_{0}")]
        public UnturnedLabel NoPreviewMessage { get; set; }

        [Pattern("ModerationActorEvidence_{0}")]
        public UnturnedLabel ActorName { get; set; }

        [Pattern("ModerationActor64Evidence_{0}")]
        public UnturnedLabel ActorId { get; set; }

        [Pattern("ModerationLink_{0}")]
        public UnturnedLabel Link { get; set; }

        [Pattern("ModerationTimestampEvidence_{0}")]
        public UnturnedLabel Timestamp { get; set; }

        [Pattern("ModerationOpenEvidence_{0}")]
        public UnturnedButton OpenButton { get; set; }
    }
    public class ModerationSelectedActor
    {
        [Pattern(Root = true)]
        public UnturnedUIElement Root { get; set; }

        [Pattern("ModerationPfpSelectedActor_{0}", AdditionalPath = "ModerationPfpSelectedActorMask_{0}")]
        public UnturnedImage ProfilePicture { get; set; }

        [Pattern("ModerationNameSelectedActor_{0}")]
        public UnturnedLabel Name { get; set; }

        [Pattern("ModerationRoleSelectedActor_{0}")]
        public UnturnedTextBox RoleInput { get; set; }

        [Pattern("ModerationSteam64SelectedActor_{0}")]
        public UnturnedTextBox Steam64Input { get; set; }

        [Pattern("ModerationYouSelectedActor_{0}")]
        public UnturnedButton YouButton { get; set; }

        [Pattern("ModerationAsAdminCheckSelectedActor_{0}")]
        public UnturnedButton AsAdminToggleButton { get; set; }

        [Pattern("ModerationAsAdminCheckToggleStateSelectedActor_{0}", AdditionalPath = "ModerationAsAdminCheckSelectedActor_{0}")]
        public UnturnedUIElement AsAdminToggleState { get; set; }

        [Pattern("ModerationRemoveSelectedActor_{0}")]
        public UnturnedButton RemoveButton { get; set; }
    }
    public class ModerationSelectedEvidence
    {
        [Pattern(Root = true)]
        public UnturnedUIElement Root { get; set; }

        [Pattern("ModerationSelectedEvidencePreview_{0}", AdditionalPath = "ModerationSelectedEvidencePreviewMask_{0}")]
        public UnturnedImage PreviewImage { get; set; }

        [Pattern("ModerationSelectedEvidencePreviewMask_{0}")]
        public UnturnedUIElement PreviewRoot { get; set; }

        [Pattern("ModerationSelectedEvidencePreviewName_{0}")]
        public UnturnedLabel PreviewName { get; set; }

        [Pattern("ModerationSelectedEvidenceNoPreviewName_{0}")]
        public UnturnedLabel NoPreviewName { get; set; }

        [Pattern("ModerationSelectedEvidenceActor_{0}")]
        public UnturnedLabel ActorName { get; set; }

        [Pattern("ModerationSelectedEvidenceTimestamp_{0}")]
        public UnturnedTextBox TimestampInput { get; set; }

        [Pattern("ModerationSelectedEvidenceMessage_{0}")]
        public UnturnedTextBox MessageInput { get; set; }

        [Pattern("ModerationSelectedEvidenceLink_{0}")]
        public UnturnedTextBox LinkInput { get; set; }

        [Pattern("ModerationSelectedEvidenceSteam64_{0}")]
        public UnturnedTextBox Steam64Input { get; set; }

        [Pattern("ModerationSelectedEvidenceButtonNow_{0}")]
        public UnturnedButton NowButton { get; set; }

        [Pattern("ModerationSelectedEvidenceButtonYou_{0}")]
        public UnturnedButton YouButton { get; set; }

        [Pattern("ModerationSelectedEvidenceButtonRemove_{0}")]
        public UnturnedButton RemoveButton { get; set; }
    }
    public class ModerationData : IUnturnedUIData
    {
        internal int ActionModeVersion;
        internal int SearchVersion;
        internal int InfoVersion;
        internal int ActionVersion;
        internal int HistorySearchUpdateVersion;
        internal int EvidenceVersion;
        public CSteamID Player { get; }
        public ModerationUI Owner { get; }
        UnturnedUI IUnturnedUIData.Owner => Owner;
        UnturnedUIElement IUnturnedUIData.Element => Owner.Headers[(int)Page.Moderation].Button;
        public ModerationEntry[]? HistoryView { get; set; }
        public ulong[]? PlayerList { get; set; }
        public int PageCount => HistoryView is not { Length: > 0 } ? 1 : Mathf.CeilToInt(HistoryView.Length / (float)ModerationHistoryLength);
        public int HistoryPage { get; set; }
        public ulong SelectedPlayer { get; set; }
        public int HistoryCount { get; set; }
        public int PlayerCount { get; set; }
        public int InfoActorCount { get; set; }
        public int InfoEvidenceCount { get; set; }
        public ModerationEntry? SelectedEntry { get; set; }
        public float LastViewedTime { get; set; }

        /* START ACTION MENU */
        
        public ModerationEntry? PrimaryEditingEntry { get; set; }
        public ModerationEntry? SecondaryEditingEntry { get; set; }
        public ModerationEntryType PendingType { get; set; } = ModerationEntryType.None;
        public PresetType PendingPreset { get; set; } = PresetType.None;
        public PunishmentPreset? PendingPresetValue { get; set; }
        public List<RelatedActor> Actors { get; set; } = new List<RelatedActor>();
        public List<Evidence> Evidence { get; set; } = new List<Evidence>();

        /* END ACTION MENU */
        public ModerationData(CSteamID player, ModerationUI owner)
        {
            Owner = owner;
            Player = player;
        }
    }

    public enum PlayerSearchMode
    {
        Any,
        Online
    }
    public enum ModerationHistorySearchMode
    {
        Admin,
        Message,
        Type,
        Before,
        After
    }
    public enum ModerationHistorySortMode
    {
        Latest,
        Oldest,
        [Translatable("Rep (H)")]
        HighestReputation,
        [Translatable("Rep (L)")]
        LowestReputation,
        Type
    }
}
