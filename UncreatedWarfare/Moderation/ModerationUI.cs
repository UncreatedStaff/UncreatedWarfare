using Cysharp.Threading.Tasks;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Data;
using Uncreated.Framework.UI.Presets;
using Uncreated.Players;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Moderation.Punishments.Presets;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Moderation;
internal partial class ModerationUI : UnturnedUI
{
    public const int ModerationHistoryLength = 30;
    public const string PositiveReputationColor = "00cc00";
    public const string NegativeReputationColor = "cc0000";
    public const string DateTimeFormat = "yyyy\\/MM\\/dd\\ hh\\:mm\\:ss\\ \\U\\T\\C\\-\\2\\4";
    public const string DateTimeFormatInput = "yyyy\\/MM\\/dd\\ hh\\:mm\\:ss";

    private readonly List<UCPlayer> _tempPlayerSearchBuffer = new List<UCPlayer>(Provider.maxPlayers);
    public static ModerationUI Instance { get; } = new ModerationUI();

    /* HEADERS */
    public LabeledButton[] Headers { get; } =
    {
        new LabeledButton("ButtonModeration"),
        new LabeledButton("ButtonPlayers"),
        new LabeledButton("ButtonTickets"),
        new LabeledButton("ButtonLogs")
    };

    public LabeledButton ButtonClose { get; } = new LabeledButton("ButtonClose");

    public UnturnedUIElement[] PageLogic { get; } =
    {
        new UnturnedUIElement("LogicPageModeration"),
        new UnturnedUIElement("LogicPagePlayers"),
        new UnturnedUIElement("LogicPageTickets"),
        new UnturnedUIElement("LogicPageLogs")
    };
    
    /* PLAYER LIST */
    public PlayerListEntry[] ModerationPlayerList { get; } = UnturnedUIPatterns.CreateArray<PlayerListEntry>("ModerationPlayer{1}_{0}", 1, to: 30);
    public UnturnedTextBox PlayerSearch { get; } = new UnturnedTextBox("ModerationPlayersInputSearch");
    public UnturnedTextBox ModerationPlayerSearch { get; } = new UnturnedTextBox("ModerationPlayersInputSearch")
    {
        UseData = true
    };
    public UnturnedEnumButton<PlayerSearchMode> ModerationPlayerSearchModeButton { get; }
        = new UnturnedEnumButton<PlayerSearchMode>(PlayerSearchMode.Online, "ModerationButtonToggleOnline", "ModerationButtonToggleOnlineLabel")
        {
            TextFormatter = (v, player) => "View - " + Localization.TranslateEnum(v, UCPlayer.FromPlayer(player)?.Locale.LanguageInfo)
        };

    /* MODERATION HISTORY LIST */
    public ModerationHistoryEntry[] ModerationHistory { get; } = UnturnedUIPatterns.CreateArray<ModerationHistoryEntry>("ModerationEntry{1}_{0}", 1, to: ModerationHistoryLength);
    public LabeledStateButton ModerationHistoryBackButton { get; } = new LabeledStateButton("ModerationListBackButton");
    public LabeledStateButton ModerationHistoryNextButton { get; } = new LabeledStateButton("ModerationListNextButton");
    public StateTextBox ModerationHistoryPage { get; } = new StateTextBox("ModerationListPageInput");
    public UnturnedTextBox ModerationHistorySearch { get; } = new UnturnedTextBox("ModerationInputSearch")
    {
        UseData = true
    };
    public LabeledButton ModerationResetHistory { get; } = new LabeledButton("ModerationResetHistory");
    public UnturnedEnumButton<ModerationEntryType> ModerationHistoryTypeButton { get; }
        = new UnturnedEnumButton<ModerationEntryType>(new ModerationEntryType[]
        {
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
        }, ModerationEntryType.None, "ModerationButtonToggleType", "ModerationButtonToggleTypeLabel", null, "ModerationButtonToggleTypeRightClickListener")
        {
            TextFormatter = (v, _) => v == ModerationEntryType.None ? "Type - Any" : ("Type - " + GetModerationTypeButtonText(v))
        };
    public UnturnedEnumButton<ModerationHistorySearchMode> ModerationHistorySearchTypeButton { get; }
        = new UnturnedEnumButton<ModerationHistorySearchMode>(ModerationHistorySearchMode.Message, "ModerationButtonToggleSearchMode", "ModerationButtonToggleSearchModeLabel", null, "ModerationButtonToggleSearchModeRightClickListener")
        {
            TextFormatter = (v, player) => "Search - " + Localization.TranslateEnum(v, UCPlayer.FromPlayer(player)?.Locale.LanguageInfo)
        };
    public UnturnedEnumButton<ModerationHistorySortMode> ModerationHistorySortModeButton { get; }
        = new UnturnedEnumButton<ModerationHistorySortMode>(ModerationHistorySortMode.Latest, "ModerationButtonToggleSortType", "ModerationButtonToggleSortTypeLabel", null, "ModerationButtonToggleSortTypeRightClickListener")
        {
            TextFormatter = (v, player) => "Sort - " + Localization.TranslateEnum(v, UCPlayer.FromPlayer(player)?.Locale.LanguageInfo)
        };

    /* MODERATION SELECTED ENTRY */
    public UnturnedUIElement ModerationInfoRoot { get; } = new UnturnedUIElement("ModerationInfoContent");
    public UnturnedUIElement ModerationInfoActorsHeader { get; } = new UnturnedUIElement("ModerationInfoActorsHeader");
    public UnturnedUIElement ModerationInfoEvidenceHeader { get; } = new UnturnedUIElement("ModerationInfoEvidenceHeader");
    public UnturnedImage ModerationInfoProfilePicture { get; } = new UnturnedImage("ModerationInfoPfp");
    public UnturnedLabel ModerationInfoType { get; } = new UnturnedLabel("ModerationInfoType");
    public UnturnedLabel ModerationInfoTimestamp { get; } = new UnturnedLabel("ModerationInfoTimestamp");
    public UnturnedLabel ModerationInfoReputation { get; } = new UnturnedLabel("ModerationInfoReputation");
    public UnturnedLabel ModerationInfoReason { get; } = new UnturnedLabel("ModerationInfoReason");
    public UnturnedLabel ModerationInfoPlayerName { get; } = new UnturnedLabel("ModerationInfoPlayerName");
    public UnturnedLabel ModerationInfoPlayerId { get; } = new UnturnedLabel("ModerationInfoPlayerId");
    public UnturnedLabel[] ModerationInfoExtraInfo { get; } = UnturnedUIPatterns.CreateArray<UnturnedLabel>("ModerationInfoExtra_{0}", 1, to: 12);
    public ModerationInfoActor[] ModerationInfoActors { get; } = UnturnedUIPatterns.CreateArray<ModerationInfoActor>("Moderation{1}Actor_{0}", 1, to: 10);
    public ModerationInfoEvidence[] ModerationInfoEvidenceEntries { get; } = UnturnedUIPatterns.CreateArray<ModerationInfoEvidence>("Moderation{1}Evidence_{0}", 1, to: 10);
    public UnturnedUIElement LogicModerationInfoUpdateScrollVisual { get; } = new UnturnedUIElement("LogicModerationInfoUpdateScrollVisual");


    /* ACTION BUTTONS */
    public LabeledStateButton ModerationButtonNote { get; } = new LabeledStateButton("ButtonNote");
    public LabeledStateButton ModerationButtonCommend { get; } = new LabeledStateButton("ButtonCommend");
    public LabeledStateButton ModerationButtonAcceptedBugReport { get; } = new LabeledStateButton("ButtonAcceptedBugReport");
    public LabeledStateButton ModerationButtonAssetBan { get; } = new LabeledStateButton("ButtonAssetBan");
    public LabeledStateButton ModerationButtonWarn { get; } = new LabeledStateButton("ButtonWarn");
    public LabeledStateButton ModerationButtonKick { get; } = new LabeledStateButton("ButtonKick");
    public LabeledStateButton ModerationButtonMute { get; } = new LabeledStateButton("ButtonMute");
    public LabeledStateButton ModerationButtonBan { get; } = new LabeledStateButton("ButtonBan");

    public LabeledStateButton[] Presets { get; } = UnturnedUIPatterns.CreateArray(index =>
        new LabeledStateButton("ButtonPreset_" + index.ToString(CultureInfo.InvariantCulture),
            "ButtonPreset_" + index.ToString(CultureInfo.InvariantCulture) + "_Label",
            "ButtonPreset_" + index.ToString(CultureInfo.InvariantCulture) + "_State"), 1, to: 12);

    /* ACTION FORM */
    public UnturnedUIElement ActionButtonBox { get; } = new UnturnedUIElement("ActionsButtonBox");
    public UnturnedUIElement ModerationFormRoot { get; } = new UnturnedUIElement("ActionsScrollBox");
    public UnturnedLabel ModerationActionHeader { get; } = new UnturnedLabel("ModerationActionsLabel");
    public UnturnedLabel ModerationActionTypeHeader { get; } = new UnturnedLabel("ModerationSelectedActionBoxLabel");
    public UnturnedLabel ModerationActionPlayerHeader { get; } = new UnturnedLabel("ModerationSelectedPlayerBoxLabel");
    public UnturnedLabel ModerationActionPresetHeader { get; } = new UnturnedLabel("ModerationSelectedActionPresetBoxLabel");
    public UnturnedLabel ModerationActionOtherEditor { get; } = new UnturnedLabel("ModerationSelectedActionWarningBoxLabel");
    public UnturnedUIElement ModerationActionPresetHeaderRoot { get; } = new UnturnedUIElement("ModerationSelectedActionPresetBox");
    public UnturnedUIElement ModerationActionOtherEditorRoot { get; } = new UnturnedUIElement("ModerationSelectedActionWarningBox");
    public UnturnedTextBox ModerationActionMessage { get; } = new UnturnedTextBox("ModerationInputMessage")
    {
        UseData = true
    };
    public PlaceholderTextBox ModerationActionInputBox2 { get; } = new PlaceholderTextBox("ModerationInputBox2")
    {
        UseData = true
    };
    public PlaceholderTextBox ModerationActionInputBox3 { get; } = new PlaceholderTextBox("ModerationInputBox3")
    {
        UseData = true
    };
    public PlaceholderTextBox ModerationActionMiniInputBox1 { get; } = new PlaceholderTextBox("ModerationMiniInput1")
    {
        UseData = true
    };
    public PlaceholderTextBox ModerationActionMiniInputBox2 { get; } = new PlaceholderTextBox("ModerationMiniInput2")
    {
        UseData = true
    };
    public LabeledRightClickableButton ModerationActionToggleButton1 { get; } = new LabeledRightClickableButton("ModerationToggleButton1");
    public LabeledRightClickableButton ModerationActionToggleButton2 { get; } = new LabeledRightClickableButton("ModerationToggleButton2");
    public ModerationSelectedActor[] ModerationActionActors { get; } = UnturnedUIPatterns.CreateArray<ModerationSelectedActor>("Moderation{1}SelectedActor_{0}", 1, to: 10);
    public ModerationSelectedEvidence[] ModerationActionEvidence { get; } = UnturnedUIPatterns.CreateArray<ModerationSelectedEvidence>("ModerationSelectedEvidence{1}_{0}", 1, to: 10);
    public LabeledStateButton ModerationActionAddActorButton { get; } = new LabeledStateButton("ModerationSelectedActorsHeaderAdd");
    public LabeledStateButton ModerationActionAddEvidenceButton { get; } = new LabeledStateButton("ModerationSelectedEvidenceHeaderAdd");
    public UnturnedEnumButtonTracker<MuteType> MuteTypeTracker { get; }
    public UnturnedUIElement LogicModerationActionsUpdateScrollVisual { get; } = new UnturnedUIElement("LogicModerationActionsUpdateScrollVisual");

    /* ACTION CONTROLS */
    public ActionControl[] ModerationActionControls { get; } = UnturnedUIPatterns.CreateArray<ActionControl>("ModerationActionControl{1}_{0}", 1, to: 4);

    public ModerationUI() : base(Gamemode.Config.UIModerationMenu, debugLogging: false)
    {
        MuteTypeTracker = new UnturnedEnumButtonTracker<MuteType>(MuteType.Both, ModerationActionToggleButton1)
        {
            Ignored = MuteType.None,
            TextFormatter = (type, player) => "Mute Type - " + Localization.TranslateEnum(type, Localization.GetLanguageCached(player.channel.owner.playerID.steamID.m_SteamID))
        };

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

        for (int i = 0; i < ModerationActionControls.Length; ++i)
            ModerationActionControls[i].Root.OnClicked += OnActionControlClicked;
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
        UCPlayer? ucp = UCPlayer.FromPlayer(player);
        if (ucp == null)
            return;

        ModerationData data = GetOrAddModerationData(ucp);
        data.SelectedPlayer = 0ul;
        EndEditInActionMenu(ucp);

        data.HistoryPage = 0;
        data.HistoryView = null;

        ModerationHistoryTypeButton.SetDefault(player);
        ModerationHistorySearchTypeButton.SetDefault(player);
        ModerationHistorySortModeButton.SetDefault(player);
        ModerationHistorySearch.SetText(ucp, string.Empty);

        UnturnedTextBoxData? textBoxData = UnturnedUIDataSource.GetData<UnturnedTextBoxData>(ucp.CSteamID, ModerationHistorySearch);
        if (textBoxData != null)
            textBoxData.Text = string.Empty;

        UpdateSelectedPlayer(ucp);

        UCWarfare.RunTask(RefreshModerationHistory, ucp, ucp.DisconnectToken, ctx: $"Reset history for {ucp.Steam64}");
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

        UCPlayer? ucp = UCPlayer.FromPlayer(player);

        if (ucp == null)
            return;

        ModerationData data = GetOrAddModerationData(ucp);
        if (data.PlayerList == null || index >= data.PlayerList.Length)
            return;

        if (data.SelectedPlayer != data.PlayerList[index])
        {
            data.SelectedPlayer = data.PlayerList[index];
            EndEditInActionMenu(ucp);
        }
        
        UpdateSelectedPlayer(ucp);

        UCWarfare.RunTask(RefreshModerationHistory, ucp, ucp.DisconnectToken, $"Update moderation history of {data.SelectedPlayer} for {ucp.Steam64}.");
    }
    private void OnClickedModerationEntry(UnturnedButton button, Player player)
    {
        int index = Array.FindIndex(ModerationHistory, x => x.Root == button);
        if (index == -1)
            return;

        UCPlayer? ucp = UCPlayer.FromPlayer(player);

        if (ucp == null)
            return;

        ModerationData data = GetOrAddModerationData(ucp);
        index += data.HistoryPage * ModerationHistory.Length;
        if (data.HistoryView == null || index >= data.HistoryView.Length)
        {
            L.LogWarning($"Invalid history index: {index} (p. {data.HistoryPage} / {data.PageCount}).");
            return;
        }
        
        SelectEntry(ucp, data.HistoryView[index]);
    }
    private void OnModerationPlayerSearchModeUpdated(UnturnedEnumButtonTracker<PlayerSearchMode> button, Player player, PlayerSearchMode value)
    {
        UCPlayer? ucPlayer = UCPlayer.FromPlayer(player);
        if (ucPlayer != null)
            SendModerationPlayerList(ucPlayer);
    }
    private void OnModerationPlayerSearchTextUpdated(UnturnedTextBox button, Player player, string text)
    {
        UCPlayer? ucPlayer = UCPlayer.FromPlayer(player);
        if (ucPlayer != null)
            SendModerationPlayerList(ucPlayer);
    }
    private void OnButtonCloseClicked(UnturnedButton button, Player player)
    {
        UCPlayer? ucPlayer = UCPlayer.FromPlayer(player);
        if (ucPlayer != null)
            Close(ucPlayer);
    }
    private void TryQueueHistoryUpdate(Player player, int ms = 500)
    {
        UCPlayer? ucPlayer = UCPlayer.FromPlayer(player);
        if (ucPlayer == null)
            return;

        UniTask.Create(async () =>
        {
            ModerationData mod = GetOrAddModerationData(ucPlayer);
            int v = Interlocked.Increment(ref mod.HistorySearchUpdateVersion);

            if (ms > 0)
            {
                await UniTask.Delay(ms, cancellationToken: ucPlayer.DisconnectToken);
                if (v != mod.HistorySearchUpdateVersion)
                    return;
            }

            UCWarfare.RunTask(RefreshModerationHistory, ucPlayer, ucPlayer.DisconnectToken);
        });
    }
    private void OnModerationHistorySortModeUpdated(UnturnedEnumButtonTracker<ModerationHistorySortMode> button, Player player, ModerationHistorySortMode value)
    {
        TryQueueHistoryUpdate(player);
    }
    private void OnModerationHistorySearchTypeUpdated(UnturnedEnumButtonTracker<ModerationHistorySearchMode> button, Player player, ModerationHistorySearchMode value)
    {
        UnturnedTextBoxData data = ModerationHistorySearch.GetOrAddData(player);

        if (string.IsNullOrEmpty(data.Text))
            return;
        TryQueueHistoryUpdate(player);
    }
    private void OnModerationHistoryTypeUpdated(UnturnedEnumButtonTracker<ModerationEntryType> button, Player player, ModerationEntryType value)
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
        UCPlayer? ucPlayer = UCPlayer.FromPlayer(player);
        ++data.HistoryPage;

        ModerationHistoryNextButton.Disable(player);
        ModerationHistoryBackButton.Disable(player);
        ModerationHistoryPage.Disable(player);
        ModerationHistoryPage.SetText(player, (data.HistoryPage + 1).ToString((IFormatProvider?)ucPlayer?.Locale.ParseFormat ?? CultureInfo.InvariantCulture));

        TryQueueHistoryUpdate(player, 0);
    }
    private void OnModerationHistoryPageTyped(UnturnedTextBox textBox, Player player, string text)
    {
        ModerationData data = GetOrAddModerationData(player.channel.owner.playerID.steamID.m_SteamID);
        UCPlayer? ucPlayer = UCPlayer.FromPlayer(player);
        if (!int.TryParse(text, out int page))
        {
            ModerationHistoryPage.SetText(player, (data.HistoryPage + 1).ToString((IFormatProvider?)ucPlayer?.Locale.ParseFormat ?? CultureInfo.InvariantCulture));
            return;
        }

        --page;
        if (page < 0)
            page = 0;

        data.HistoryPage = page;

        ModerationHistoryNextButton.Disable(player);
        ModerationHistoryBackButton.Disable(player);
        ModerationHistoryPage.Disable(player);
        ModerationHistoryPage.SetText(player, (page + 1).ToString((IFormatProvider?)ucPlayer?.Locale.ParseFormat ?? CultureInfo.InvariantCulture));

        TryQueueHistoryUpdate(player, 0);
    }
    private void OnModerationHistoryBack(UnturnedButton button, Player player)
    {
        ModerationData data = GetOrAddModerationData(player.channel.owner.playerID.steamID.m_SteamID);
        UCPlayer? ucPlayer = UCPlayer.FromPlayer(player);
        --data.HistoryPage;
        if (data.HistoryPage < 0)
            data.HistoryPage = 0;

        ModerationHistoryNextButton.Disable(player);
        ModerationHistoryBackButton.Disable(player);
        ModerationHistoryPage.Disable(player);
        ModerationHistoryPage.SetText(player, (data.HistoryPage + 1).ToString((IFormatProvider?)ucPlayer?.Locale.ParseFormat ?? CultureInfo.InvariantCulture));

        TryQueueHistoryUpdate(player, 0);
    }
    public async Task Open(UCPlayer player, CancellationToken token = default)
    {
        token.CombineIfNeeded(player.DisconnectToken);
        await UCWarfare.ToUpdate(token);

        if (TeamSelector.Instance != null)
            TeamSelector.Instance.Close(player);

        player.ModalNeeded = true;
        player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Modal | EPluginWidgetFlags.ForceBlur);
        player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.Default);

        if (!player.HasModerationUI)
        {
            SendToPlayer(player.Connection);
            player.HasModerationUI = true;
        }

        ModerationData data = GetOrAddModerationData(player);
        data.HistoryCount = 0;
        data.PlayerCount = 0;
        data.InfoActorCount = 0;
        data.InfoEvidenceCount = 0;
        await SetPage(player, Page.Moderation, false, token).ConfigureAwait(false);
    }
    public void Close(UCPlayer player)
    {
        ThreadUtil.assertIsGameThread();

        player.ModalNeeded = false;
        player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.Modal | EPluginWidgetFlags.ForceBlur);
        player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Default);
        ClearFromPlayer(player.Connection);
        player.HasModerationUI = false;
    }
    public async Task SetPage(UCPlayer player, Page page, bool isAlreadyInView, CancellationToken token = default)
    {
        token.CombineIfNeeded(player.DisconnectToken);
        if (page is not Page.Moderation and not Page.Players and not Page.Tickets and not Page.Logs)
            throw new ArgumentOutOfRangeException(nameof(page));
        await UCWarfare.ToUpdate(token);
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
        }).ConfigureAwait(false);
    }
    private async Task PrepareModerationPage(UCPlayer player, CancellationToken token = default)
    {
        await UCWarfare.ToUpdate(token);

        UpdateSelectedPlayer(player);

        await PlayerManager.TryDownloadAllPlayerSummaries(token: token);
        await UCWarfare.ToUpdate(token);

        ModerationData data = GetOrAddModerationData(player);
        
        UnturnedTextBoxData? textBoxData = UnturnedUIDataSource.GetData<UnturnedTextBoxData>(player.CSteamID, ModerationPlayerSearch);
        if (textBoxData != null)
            ModerationPlayerSearch.SetText(player, textBoxData.Text ?? string.Empty);

        ModerationHistoryTypeButton.Update(player.Player, false);
        ModerationHistorySearchTypeButton.Update(player.Player, false);
        ModerationHistorySortModeButton.Update(player.Player, false);
        ModerationPlayerSearchModeButton.Update(player.Player, false);

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

        await RefreshModerationHistory(player, token).ConfigureAwait(false);
    }
    private Task PreparePlayersPage(UCPlayer player, CancellationToken token = default)
    {
        return Task.CompletedTask;
    }
    private Task PrepareTicketsPage(UCPlayer player, CancellationToken token = default)
    {
        return Task.CompletedTask;
    }
    private Task PrepareLogsPage(UCPlayer player, CancellationToken token = default)
    {
        return Task.CompletedTask;
    }
    public ModerationData? GetModerationData(UCPlayer player)
    {
        ThreadUtil.assertIsGameThread();

        return UnturnedUIDataSource.GetData<ModerationData>(player.CSteamID, Headers[(int)Page.Moderation].Button);
    }
    public ModerationData GetOrAddModerationData(UCPlayer player) => GetOrAddModerationData(player.Steam64);
    public ModerationData GetOrAddModerationData(ulong steam64)
    {
        ThreadUtil.assertIsGameThread();

        ModerationData? data = UnturnedUIDataSource.GetData<ModerationData>(new CSteamID(steam64), Headers[(int)Page.Moderation].Button);
        if (data == null)
        {
            data = new ModerationData(new CSteamID(steam64), this);
            UnturnedUIDataSource.AddData(data);
        }

        return data;
    }
    private void UpdateSelectedPlayer(UCPlayer player)
    {
        ModerationData data = GetOrAddModerationData(player);
        if (!Util.IsValidSteam64Id(data.SelectedPlayer))
        {
            ModerationActionPlayerHeader.SetText(player, string.Empty);
            ModerationFormRoot.Hide(player);
            ActionButtonBox.Hide(player);
            ModerationActionHeader.SetText(player, "Actions");
            return;
        }

        if (UCPlayer.FromID(data.SelectedPlayer) is null)
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

        UCWarfare.RunTask(async token =>
        {
            ValueTask<PlayerNames> nameTask = F.GetPlayerOriginalNamesAsync(data.SelectedPlayer, token);
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

        }, player.DisconnectToken, ctx: "Update player name.");
    }
    public void SetHistoryPage(UCPlayer player, ModerationData data, int page)
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
    public void SendModerationPlayerList(UCPlayer player)
    {
        ThreadUtil.assertIsGameThread();

        ITransportConnection connection = player.Connection;

        if (!ModerationPlayerSearchModeButton.TryGetSelection(player.Player, out PlayerSearchMode searchMode))
            searchMode = PlayerSearchMode.Online;
        ModerationData data = GetOrAddModerationData(player);
        UnturnedTextBoxData? textBoxData = UnturnedUIDataSource.GetData<UnturnedTextBoxData>(player.CSteamID, ModerationPlayerSearch);
        string searchText = textBoxData?.Text ?? string.Empty;
        data.PlayerList ??= new ulong[ModerationPlayerList.Length];
        if (searchText.Length < 1 || searchMode == PlayerSearchMode.Online)
        {
            List<UCPlayer> buffer;
            bool clr;
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                UCPlayer.Search(searchText, UCPlayer.NameSearch.PlayerName, _tempPlayerSearchBuffer);
                buffer = _tempPlayerSearchBuffer;
                clr = true;
            }
            else
            {
                buffer = PlayerManager.OnlinePlayers;
                clr = false;
            }

            try
            {
                int ct = Math.Min(ModerationPlayerList.Length, buffer.Count);
                int i = 0;
                for (; i < ct; ++i)
                {
                    UCPlayer listPlayer = buffer[i];
                    PlayerListEntry entry = ModerationPlayerList[i];
                    entry.SteamId.SetText(connection, listPlayer.Steam64.ToString(CultureInfo.InvariantCulture));
                    entry.Name.SetText(connection, listPlayer.Name.PlayerName);
                    if (Data.ModerationSql.TryGetAvatar(listPlayer.Steam64, AvatarSize.Small, out string avatarUrl))
                        entry.ProfilePicture.SetImage(connection, avatarUrl);
                    else
                    {
                        entry.ProfilePicture.SetImage(connection, string.Empty);
                        UniTask.Create(async () =>
                        {
                            string? icon = await listPlayer.GetProfilePictureURL(AvatarSize.Small, player.DisconnectToken);
                            await UniTask.SwitchToMainThread(player.DisconnectToken);
                            entry.ProfilePicture.SetImage(player, icon ?? string.Empty);
                        });
                    }

                    if (i >= data.InfoActorCount)
                        entry.Root.SetVisibility(player, true);

                    data.PlayerList[i] = listPlayer.Steam64;
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
                    buffer.Clear();
            }
        }
        else
        {
            UCWarfare.RunTask(async token =>
            {
                ITransportConnection connection = player.Connection;

                token.ThrowIfCancellationRequested();

                int version = Interlocked.Increment(ref data.SearchVersion);
                List<PlayerNames> names = await Data.AdminSql.SearchAllPlayers(searchText, UCPlayer.NameSearch.PlayerName, true, token);

                if (data.SearchVersion != version)
                    return;
                token.ThrowIfCancellationRequested();

                await UCWarfare.ToUpdate(token);

                int ct = Math.Min(ModerationPlayerList.Length, names.Count);
                int i2 = 0;
                for (; i2 < ct; ++i2)
                {
                    PlayerNames name = names[i2];
                    PlayerListEntry entry = ModerationPlayerList[i2];
                    entry.SteamId.SetText(connection, name.Steam64.ToString(CultureInfo.InvariantCulture));
                    entry.Name.SetText(connection, name.PlayerName);
                    if (Data.ModerationSql.TryGetAvatar(name.Steam64, AvatarSize.Small, out string avatarUrl))
                        entry.ProfilePicture.SetImage(connection, avatarUrl);
                    else
                        entry.ProfilePicture.SetImage(connection, string.Empty);

                    entry.Root.SetVisibility(player.Connection, true);
                    if (i2 >= data.InfoActorCount)
                        entry.Root.SetVisibility(player.Connection, true);

                    data.PlayerList[i2] = name.Steam64;
                }

                for (; i2 < data.PlayerCount; ++i2)
                {
                    ModerationPlayerList[i2].Root.SetVisibility(connection, false);
                    data.PlayerList[i2] = 0;
                }

                data.PlayerCount = ct;

                ulong[] ids = new ulong[ct];
                for (int i = 0; i < ct; ++i)
                    ids[i] = names[i].Steam64;

                await Data.ModerationSql.CacheAvatars(ids, token);
#if DEBUG
                ThreadUtil.assertIsGameThread();
#endif
                for (int i = 0; i < ct; ++i)
                {
                    PlayerNames name = names[i];
                    if (Data.ModerationSql.TryGetAvatar(name.Steam64, AvatarSize.Small, out string avatarUrl))
                        ModerationPlayerList[i].ProfilePicture.SetImage(connection, avatarUrl);
                }
            }, player.DisconnectToken, ctx: "Update moderation player list for " + player.Steam64.ToString(CultureInfo.InvariantCulture) + ".");
        }
    }
    public void SelectEntry(UCPlayer player, ModerationEntry? entry)
    {
        ThreadUtil.assertIsGameThread();

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
        if (Data.ModerationSql.TryGetAvatar(entry.Player, AvatarSize.Full, out string avatarUrl))
            ModerationInfoProfilePicture.SetImage(c, avatarUrl);
        else
        {
            ModerationInfoProfilePicture.SetImage(c, string.Empty);
            UniTask.Create(async () =>
            {
                string? icon = await F.GetProfilePictureURL(entry.Player, AvatarSize.Full, player.DisconnectToken);

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

            UCWarfare.RunTask(async token =>
            {
                if (data.InfoVersion != v)
                    return;
                for (int j = 0; j < ct; ++j)
                {
                    IModerationActor actor = entry.Evidence[j].Actor;
                    ValueTask<string> name = actor.GetDisplayName(Data.ModerationSql, token);
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
            }, player.DisconnectToken);
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
            UCWarfare.RunTask(async token =>
            {
                if (data.InfoVersion != v)
                    return;
                int i = 0;
                int ct = Math.Min(ModerationInfoActors.Length, entry.Actors.Length);
                List<ulong> profilePictures = new List<ulong>(ct);
                for (int j = 0; j < ct; ++j)
                {
                    RelatedActor actor = entry.Actors[i];
                    if (Util.IsValidSteam64Id(actor.Actor.Id))
                        profilePictures.Add(actor.Actor.Id);
                }

                await Data.ModerationSql.CacheAvatars(profilePictures, token);

                if (data.InfoVersion != v)
                    return;
                for (; i < ct; ++i)
                {
                    RelatedActor actor = entry.Actors[i];
                    ModerationInfoActor actorUi = ModerationInfoActors[i];
                    actorUi.Role.SetText(c, string.IsNullOrWhiteSpace(actor.Role) ? "No role" : actor.Role);
                    if (Util.IsValidSteam64Id(actor.Actor.Id))
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
                    ValueTask<string> unTask = actor.Actor.GetDisplayName(Data.ModerationSql, token);
                    ValueTask<string?> imgTask;

                    if (Util.IsValidSteam64Id(actor.Actor.Id) && Data.ModerationSql.TryGetAvatar(actor.Actor.Id, AvatarSize.Medium, out string avatar))
                    {
                        imgTask = new ValueTask<string?>(avatar);
                    }
                    else
                    {
                        imgTask = actor.Actor.GetProfilePictureURL(Data.ModerationSql, AvatarSize.Medium, token);
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

            }, player.DisconnectToken, ctx: $"Update actor info for moderation entry {entry.Id}.");
        }
        else
        {
            for (int i = data.InfoActorCount - 1; i >= 0; --i)
            {
                ModerationInfoActors[i].Root.SetVisibility(c, false);
            }
        }

        UCWarfare.RunTask(async token =>
        {
            PlayerNames names = await F.GetPlayerOriginalNamesAsync(entry.Player, token).ConfigureAwait(false);
            if (data.InfoVersion == v)
                ModerationInfoPlayerName.SetText(c, names.ToString(false));
        }, player.DisconnectToken, ctx: $"Update username for entry owner: {entry.Player}");

        UCWarfare.RunTask(async token =>
        {
            List<string> extraInfo = new List<string>();
            await entry.AddExtraInfo(Data.ModerationSql, extraInfo, player.Locale.CultureInfo, token);

            await UCWarfare.ToUpdate(token);

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

            await UniTask.WaitForSeconds(0.125f, true, cancellationToken: token);

            if (data.InfoVersion == v)
                LogicModerationInfoUpdateScrollVisual.SetVisibility(c, true);
        }, player.DisconnectToken, ctx: $"Update extra info for moderation entry {entry.Id}.");
    }
    private void UpdateModerationEntry(UCPlayer player, int index, ModerationEntry entry)
    {
        ITransportConnection connection = player.Connection;

        ModerationHistoryEntry ui = ModerationHistory[index];
        ModerationEntryType? type = ModerationReflection.GetType(entry.GetType());
        ui.Type.SetText(connection, type.HasValue ? type.Value.ToString() : entry.GetType().Name);
        string? msg = entry.GetDisplayMessage();
        ui.Message.SetText(connection, string.IsNullOrWhiteSpace(msg) ? "== No Message ==" : msg!);
        ui.Reputation.SetText(connection, FormatReputation(entry.Reputation, player.Locale.CultureInfo, false));
        ui.Timestamp.SetText(connection, (entry.ResolvedTimestamp ?? entry.StartedTimestamp).UtcDateTime.ToString(DateTimeFormat));
        if (entry.TryGetDisplayActor(out RelatedActor actor))
        {
            if (!actor.Actor.Async)
            {
                ui.Admin.SetText(connection, actor.Actor.GetDisplayName(Data.ModerationSql, CancellationToken.None).Result);
                ui.AdminProfilePicture.SetImage(connection, actor.Actor.GetProfilePictureURL(Data.ModerationSql, AvatarSize.Medium, CancellationToken.None).Result ?? string.Empty, false);
            }
            else
            {
                bool av = Data.ModerationSql.TryGetAvatar(actor.Actor, AvatarSize.Medium, out string avatarUrl);
                bool nm = Data.ModerationSql.TryGetUsernames(actor.Actor, out PlayerNames names);
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
                        ValueTask<string?> pfpTask = av ? new ValueTask<string?>(avatarUrl) : actor.Actor.GetProfilePictureURL(Data.ModerationSql, AvatarSize.Medium, player.DisconnectToken);
                        ValueTask<string> displayNameTask = nm ? new ValueTask<string>(names.PlayerName) : actor.Actor.GetDisplayName(Data.ModerationSql, player.DisconnectToken);
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
            ui.Duration.SetText(connection, duration.IsPermanent ? "∞" : Util.ToTimeString((int)Math.Round(duration.Duration.TotalSeconds), 2));
            ui.Icon.SetText(connection, string.Empty);
        }
        else
        {
            ui.Duration.SetText(connection, string.Empty);
            Guid? icon = entry.GetIcon();
            ui.Icon.SetText(connection, icon.HasValue ? ItemIconProvider.GetIcon(icon.Value, tmpro: true) : string.Empty);
        }
    }
    private async Task RefreshModerationHistory(UCPlayer player, CancellationToken token = default)
    {
        await UCWarfare.ToUpdate(token);

        ModerationHistoryNextButton.Disable(player.Connection);
        ModerationHistoryBackButton.Disable(player.Connection);
        ModerationHistoryPage.Disable(player.Connection);

        ModerationData data = GetOrAddModerationData(player);
        UnturnedTextBoxData? textBoxData = UnturnedUIDataSource.GetData<UnturnedTextBoxData>(player.CSteamID, ModerationHistorySearch);
        ModerationHistoryTypeButton.TryGetSelection(player.Player, out ModerationEntryType filter);
        ModerationHistorySearchTypeButton.TryGetSelection(player.Player, out ModerationHistorySearchMode searchMode);
        ModerationHistorySortModeButton.TryGetSelection(player.Player, out ModerationHistorySortMode sortMode);
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
                conditionArgs = new object[] { "%" + text + "%" };
            }
            else if (searchMode == ModerationHistorySearchMode.Admin)
            {
                if (noType)
                    condition += " AND ";
                if (ulong.TryParse(text, NumberStyles.Number, player.Locale.CultureInfo, out ulong steam64) && Util.IsValidSteam64Id(steam64))
                {
                    condition += $"EXISTS (SELECT COUNT(*) FROM `{DatabaseInterface.TableActors}` AS `a` " +
                                 $"WHERE `a`.`{DatabaseInterface.ColumnExternalPrimaryKey}` = `main`.`{DatabaseInterface.ColumnEntriesPrimaryKey}` " +
                                 $"AND `a`.`{DatabaseInterface.ColumnActorsId}`={{0}} " +
                                 $"AND `a`.`{DatabaseInterface.ColumnActorsAsAdmin}` != 0) " +
                                $"> 0";
                    conditionArgs = new object[] { steam64 };
                }
                else
                {
                    condition += $"(SELECT COUNT(*) FROM `{DatabaseInterface.TableActors}` AS `a` " +
                                 $"WHERE `a`.`{DatabaseInterface.ColumnExternalPrimaryKey}` = `main`.`{DatabaseInterface.ColumnEntriesPrimaryKey}` " +
                                 $"AND " +
                                 $"EXISTS (SELECT COUNT(*) FROM `{WarfareSQL.TableUsernames}` AS `u` " +
                                  $"WHERE `a`.`{DatabaseInterface.ColumnActorsId}`=`u`.`{WarfareSQL.ColumnUsernamesSteam64}` " +
                                 $"AND " +
                                  $"(`u`.`{WarfareSQL.ColumnUsernamesPlayerName}` LIKE {{0}} OR `u`.`{WarfareSQL.ColumnUsernamesCharacterName}` LIKE {{0}} OR `u`.`{WarfareSQL.ColumnUsernamesNickName}` LIKE {{0}}))" +
                                 $" > 0)" +
                                $" > 0";
                    conditionArgs = new object[] { "%" + text + "%" };
                }
            }
            else if (!noType)
            {
                if (noType)
                    condition += " AND";
                if (filter != ModerationEntryType.None)
                {
                    condition += $"`main`.`{DatabaseInterface.ColumnEntriesType}` = {{0}}";
                    conditionArgs = new object[] { filter.ToString() };
                }
                else if (Enum.TryParse(text, true, out ModerationEntryType entryType))
                {
                    condition += $"`main`.`{DatabaseInterface.ColumnEntriesType}` = {{0}}";
                    conditionArgs = new object[] { entryType.ToString() };
                }
                else
                {
                    condition += $"`main`.`{DatabaseInterface.ColumnEntriesType}` LIKE {{0}}";
                    conditionArgs = new object[] { "%" + text + "%" };
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

        L.LogDebug($"Conditions: \"{condition}\", OrderBy: \"{orderBy}\"");

        bool showRecentActors = text is not { Length: > 0 } && !Util.IsValidSteam64Id(data.SelectedPlayer);
        if (showRecentActors || Util.IsValidSteam64Id(data.SelectedPlayer))
        {
            entries = (ModerationEntry[])await Data.ModerationSql.ReadAll(type, showRecentActors ? player.Steam64 : data.SelectedPlayer,
                showRecentActors ? ActorRelationType.IsActor : ActorRelationType.IsTarget, false, true, start, end, condition, orderBy, conditionArgs, token);
        }
        else
        {
            entries = (ModerationEntry[])await Data.ModerationSql.ReadAll(type, false, true, start, end, condition, orderBy, conditionArgs, token);
        }

        await UCWarfare.ToUpdate(token);

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

        UCWarfare.RunTask(async token =>
        {
            ulong[] steam64Ids = await Data.ModerationSql.GetActorSteam64IDs(usernamesAndPicturesToCache, token);

            Task usernames = Data.ModerationSql.CacheUsernames(steam64Ids, token);

            await Data.ModerationSql.CacheAvatars(steam64Ids, token);
            await usernames;

            await UCWarfare.ToUpdate(token);
            SetHistoryPage(player, data, newPage);

        }, player.DisconnectToken);
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
        [UIPattern("", Mode = FormatMode.Format)]
        public UnturnedUIElement Root { get; set; }

        [UIPattern("Name", Mode = FormatMode.Format)]
        public UnturnedLabel Name { get; set; }

        [UIPattern("ModerateButton", Mode = FormatMode.Format)]
        public UnturnedButton ModerateButton { get; set; }

        [UIPattern("ModerateButtonLabel", Mode = FormatMode.Format)]
        public UnturnedLabel ModerateButtonLabel { get; set; }

        [UIPattern("SteamID", Mode = FormatMode.Format)]
        public UnturnedLabel SteamId { get; set; }

        [UIPattern("Pfp", Mode = FormatMode.Format)]
        public UnturnedImage ProfilePicture { get; set; }
    }
    public class ModerationHistoryEntry
    {
        [UIPattern("", Mode = FormatMode.Format)]
        public UnturnedButton Root { get; set; }

        [UIPattern("Type", Mode = FormatMode.Format)]
        public UnturnedLabel Type { get; set; }

        [UIPattern("Reputation", Mode = FormatMode.Format)]
        public UnturnedLabel Reputation { get; set; }

        [UIPattern("Duration", Mode = FormatMode.Format)]
        public UnturnedLabel Duration { get; set; }

        [UIPattern("Icon", Mode = FormatMode.Format)]
        public UnturnedLabel Icon { get; set; }

        [UIPattern("Message", Mode = FormatMode.Format)]
        public UnturnedLabel Message { get; set; }

        [UIPattern("AdminPfp", Mode = FormatMode.Format)]
        public UnturnedImage AdminProfilePicture { get; set; }

        [UIPattern("Admin", Mode = FormatMode.Format)]
        public UnturnedLabel Admin { get; set; }

        [UIPattern("Timestamp", Mode = FormatMode.Format)]
        public UnturnedLabel Timestamp { get; set; }
    }
    public class ModerationInfoActor
    {
        [UIPattern("", Mode = FormatMode.Format)]
        public UnturnedUIElement Root { get; set; }

        [UIPattern("Pfp", Mode = FormatMode.Format)]
        public UnturnedImage ProfilePicture { get; set; }

        [UIPattern("Name", Mode = FormatMode.Format)]
        public UnturnedLabel Name { get; set; }

        [UIPattern("Steam64", Mode = FormatMode.Format)]
        public UnturnedLabel Steam64 { get; set; }

        [UIPattern("Role", Mode = FormatMode.Format)]
        public UnturnedLabel Role { get; set; }
    }
    public class ModerationInfoEvidence
    {
        [UIPattern("", Mode = FormatMode.Format)]
        public UnturnedUIElement Root { get; set; }

        [UIPattern("Preview", Mode = FormatMode.Format)]
        public UnturnedImage PreviewImage { get; set; }

        [UIPattern("Open", Mode = FormatMode.Format)]
        public UnturnedButton PreviewImageButton { get; set; }

        [UIPattern("PreviewName", Mode = FormatMode.Format)]
        public UnturnedLabel PreviewName { get; set; }

        [UIPattern("NoPreviewName", Mode = FormatMode.Format)]
        public UnturnedLabel NoPreviewName { get; set; }

        [UIPattern("PreviewMessage", Mode = FormatMode.Format)]
        public UnturnedLabel PreviewMessage { get; set; }

        [UIPattern("NoPreviewMessage", Mode = FormatMode.Format)]
        public UnturnedLabel NoPreviewMessage { get; set; }

        [UIPattern("Actor", Mode = FormatMode.Format)]
        public UnturnedLabel ActorName { get; set; }

        [UIPattern("Actor64", Mode = FormatMode.Format)]
        public UnturnedLabel ActorId { get; set; }

        [UIPattern("Link", Mode = FormatMode.Format)]
        public UnturnedLabel Link { get; set; }

        [UIPattern("Timestamp", Mode = FormatMode.Format)]
        public UnturnedLabel Timestamp { get; set; }

        [UIPattern("Open", Mode = FormatMode.Format)]
        public UnturnedButton OpenButton { get; set; }
    }
    public class ModerationSelectedActor
    {
        [UIPattern("", Mode = FormatMode.Format)]
        public UnturnedUIElement Root { get; set; }

        [UIPattern("Pfp", Mode = FormatMode.Format)]
        public UnturnedImage ProfilePicture { get; set; }

        [UIPattern("Name", Mode = FormatMode.Format)]
        public UnturnedLabel Name { get; set; }

        [UIPattern("Role", Mode = FormatMode.Format)]
        public UnturnedTextBox RoleInput { get; set; }

        [UIPattern("Steam64", Mode = FormatMode.Format)]
        public UnturnedTextBox Steam64Input { get; set; }

        [UIPattern("You", Mode = FormatMode.Format)]
        public UnturnedButton YouButton { get; set; }

        [UIPattern("AsAdminCheck", Mode = FormatMode.Format)]
        public UnturnedButton AsAdminToggleButton { get; set; }

        [UIPattern("AsAdminCheckToggleState", Mode = FormatMode.Format)]
        public UnturnedUIElement AsAdminToggleState { get; set; }

        [UIPattern("Remove", Mode = FormatMode.Format)]
        public UnturnedButton RemoveButton { get; set; }
    }
    public class ModerationSelectedEvidence
    {
        [UIPattern("", Mode = FormatMode.Format)]
        public UnturnedUIElement Root { get; set; }

        [UIPattern("Preview", Mode = FormatMode.Format)]
        public UnturnedImage PreviewImage { get; set; }

        [UIPattern("PreviewMask", Mode = FormatMode.Format)]
        public UnturnedUIElement PreviewRoot { get; set; }

        [UIPattern("PreviewName", Mode = FormatMode.Format)]
        public UnturnedLabel PreviewName { get; set; }

        [UIPattern("NoPreviewName", Mode = FormatMode.Format)]
        public UnturnedLabel NoPreviewName { get; set; }

        [UIPattern("Actor", Mode = FormatMode.Format)]
        public UnturnedLabel ActorName { get; set; }

        [UIPattern("Timestamp", Mode = FormatMode.Format)]
        public UnturnedTextBox TimestampInput { get; set; }

        [UIPattern("Message", Mode = FormatMode.Format)]
        public UnturnedTextBox MessageInput { get; set; }

        [UIPattern("Link", Mode = FormatMode.Format)]
        public UnturnedTextBox LinkInput { get; set; }

        [UIPattern("Steam64", Mode = FormatMode.Format)]
        public UnturnedTextBox Steam64Input { get; set; }

        [UIPattern("ButtonNow", Mode = FormatMode.Format)]
        public UnturnedButton NowButton { get; set; }

        [UIPattern("ButtonYou", Mode = FormatMode.Format)]
        public UnturnedButton YouButton { get; set; }

        [UIPattern("ButtonRemove", Mode = FormatMode.Format)]
        public UnturnedButton RemoveButton { get; set; }
    }
    public class ActionControl
    {
        [UIPattern("", Mode = FormatMode.Format)]
        public UnturnedButton Root { get; set; }

        [UIPattern("Label", Mode = FormatMode.Format)]
        public UnturnedLabel Text { get; set; }
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
