﻿using Cysharp.Threading.Tasks;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Remoting.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Data;
using Uncreated.Framework.UI.Presets;
using Uncreated.Players;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Moderation.Punishments;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Moderation;
internal class ModerationUI : UnturnedUI
{
    public const int ModerationHistoryLength = 30;
    public const string PositiveReputationColor = "cc0000";
    public const string NegativeReputationColor = "00cc00";
    public const string DateTimeFormat = "yyyy\\/MM\\/dd\\ hh\\:mm\\:ss\\ \\U\\T\\C\\-\\2\\4";
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
    public ChangeableTextBox ModerationPlayerSearch { get; } = new ChangeableTextBox("ModerationPlayersInputSearch");
    public UnturnedEnumButton<PlayerSearchMode> ModerationPlayerSearchModeButton { get; }
        = new UnturnedEnumButton<PlayerSearchMode>(PlayerSearchMode.Online, "ModerationButtonToggleOnline", "ModerationButtonToggleOnlineLabel")
        {
            TextFormatter = (v, player) => "View - " + Localization.TranslateEnum(v, UCPlayer.FromPlayer(player)?.Locale.LanguageInfo)
        };

    /* MODERATION HISTORY LIST */
    public ModerationHistoryEntry[] ModerationHistory { get; } = UnturnedUIPatterns.CreateArray<ModerationHistoryEntry>("ModerationEntry{1}_{0}", 1, to: ModerationHistoryLength);
    public LabeledStateButton ModerationHistoryBackButton { get; } = new LabeledStateButton("ModerationListBackButton");
    public LabeledStateButton ModerationHistoryNextButton { get; } = new LabeledStateButton("ModerationListNextButton");
    public ChangeableStateTextBox ModerationHistoryPage { get; } = new ChangeableStateTextBox("ModerationListPageInput");
    public ChangeableTextBox ModerationHistorySearch { get; } = new ChangeableTextBox("ModerationInputSearch");
    public LabeledButton ModerationResetHistory { get; } = new LabeledButton("ModerationResetHistory");
    public UnturnedEnumButton<ModerationEntryType> ModerationHistroyTypeButton { get; }
        = new UnturnedEnumButton<ModerationEntryType>(ModerationEntryType.None, "ModerationButtonToggleType", "ModerationButtonToggleTypeLabel")
        {
            TextFormatter = (v, player) => v == ModerationEntryType.None ? "Type - Any" : ("Type - " + Localization.TranslateEnum(v, UCPlayer.FromPlayer(player)?.Locale.LanguageInfo))
        };
    public UnturnedEnumButton<ModerationHistorySearchMode> ModerationHistroySearchTypeButton { get; }
        = new UnturnedEnumButton<ModerationHistorySearchMode>(ModerationHistorySearchMode.Message, "ModerationButtonToggleSearchMode", "ModerationButtonToggleSearchModeLabel")
        {
            TextFormatter = (v, player) => "Search - " + Localization.TranslateEnum(v, UCPlayer.FromPlayer(player)?.Locale.LanguageInfo)
        };
    public UnturnedEnumButton<ModerationHistorySortMode> ModerationHistroySortTypeButton { get; }
        = new UnturnedEnumButton<ModerationHistorySortMode>(ModerationHistorySortMode.Latest, "ModerationButtonToggleSortType", "ModerationButtonToggleSortTypeLabel")
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
    public UnturnedUIElement ModerationFormRoot { get; } = new UnturnedUIElement("ActionsScrollBox");
    public UnturnedLabel ModerationActionHeader { get; } = new UnturnedLabel("ModerationSelectedActionBoxLabel");
    public ChangeableTextBox ModerationActionMessage { get; } = new ChangeableTextBox("ModerationInputMessage");
    public ChangeableTextBox ModerationActionInputBox2 { get; } = new ChangeableTextBox("ModerationInputBox2");
    public ChangeableTextBox ModerationActionInputBox3 { get; } = new ChangeableTextBox("ModerationInputBox3");
    public ChangeableTextBox ModerationActionMiniInputBox1 { get; } = new ChangeableTextBox("ModerationMiniInput1");
    public ChangeableTextBox ModerationActionMiniInputBox2 { get; } = new ChangeableTextBox("ModerationMiniInput2");
    public LabeledButton ModerationActionToggleButton1 { get; } = new LabeledButton("ModerationToggleButton1");
    public LabeledButton ModerationActionToggleButton2 { get; } = new LabeledButton("ModerationToggleButton2");
    public ModerationSelectedActor[] ModerationActionActors { get; } = UnturnedUIPatterns.CreateArray<ModerationSelectedActor>("Moderation{1}SelectedActor_{0}", 1, to: 10);
    public ModerationSelectedEvidence[] ModerationActionEvidence { get; } = UnturnedUIPatterns.CreateArray<ModerationSelectedEvidence>("ModerationSelectedEvidence{1}_{0}", 1, to: 10);
    public LabeledStateButton ModerationActionAddActorButton { get; } = new LabeledStateButton("ModerationSelectedActorsHeaderAdd");
    public LabeledStateButton ModerationActionRemoveActorButton { get; } = new LabeledStateButton("ModerationSelectedActorsHeaderRemove");
    public LabeledStateButton ModerationActionAddEvidenceButton { get; } = new LabeledStateButton("ModerationSelectedEvidenceHeaderAdd");
    public LabeledStateButton ModerationActionRemoveEvidenceButton { get; } = new LabeledStateButton("ModerationSelectedEvidenceHeaderRemove");

    /* ACTION CONTROLS */
    public ActionControl[] ModerationActionControls { get; } = UnturnedUIPatterns.CreateArray<ActionControl>("ModerationActionControl{1}_{0}", 1, to: 4);

    public ModerationUI() : base(Gamemode.Config.UIModerationMenu, debugLogging: true)
    {
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

        data.SelectedPlayer = data.PlayerList[index];

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
        if (data.HistoryView == null || index >= data.HistoryView.Length)
            return;
        
        SelectEntry(ucp, data.HistoryView[index]);
    }

    private void OnModerationPlayerSearchModeUpdated(UnturnedEnumButton<PlayerSearchMode> button, Player player, PlayerSearchMode value)
    {
        UCPlayer? ucPlayer = UCPlayer.FromPlayer(player);
        if (ucPlayer != null)
            SendModerationPlayerList(ucPlayer);
    }
    private void OnModerationPlayerSearchTextUpdated(UnturnedTextBox button, Player player, string text)
    {
        UnturnedTextBoxData? data = UnturnedUIDataSource.GetData<UnturnedTextBoxData>(player.channel.owner.playerID.steamID, ModerationPlayerSearch.TextBox);
        if (data == null)
        {
            data = new UnturnedTextBoxData(player.channel.owner.playerID.steamID, ModerationPlayerSearch.TextBox);
            UnturnedUIDataSource.AddData(data);
        }

        data.Text = text;
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
            PageLogic[(int)page].SetVisibility(player.Connection, true);
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

        await PlayerManager.TryDownloadAllPlayerSummaries(token: token);
        await UCWarfare.ToUpdate(token);
        
        UnturnedTextBoxData? textBoxData = UnturnedUIDataSource.GetData<UnturnedTextBoxData>(player.CSteamID, ModerationPlayerSearch.TextBox);
        if (textBoxData != null)
            ModerationPlayerSearch.SetText(player.Connection, textBoxData.Text ?? string.Empty);

        ModerationHistroyTypeButton.Update(player.Player, false);
        ModerationHistroySearchTypeButton.Update(player.Player, false);
        ModerationHistroySortTypeButton.Update(player.Player, false);
        ModerationPlayerSearchModeButton.Update(player.Player, false);

        SendModerationPlayerList(player);
        
        await RefreshModerationHistory(player, token).ConfigureAwait(false);
        await UCWarfare.ToUpdate(token);
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
    public ModerationData? GetModerationData(UCPlayer player) => UnturnedUIDataSource.GetData<ModerationData>(player.CSteamID, Headers[(int)Page.Moderation].Button);
    public ModerationData GetOrAddModerationData(UCPlayer player) => GetOrAddModerationData(player.Steam64);
    public ModerationData GetOrAddModerationData(ulong steam64)
    {
        ModerationData? data = UnturnedUIDataSource.GetData<ModerationData>(new CSteamID(steam64), Headers[(int)Page.Moderation].Button);
        if (data == null)
        {
            data = new ModerationData(new CSteamID(steam64), this);
            UnturnedUIDataSource.AddData(data);
        }

        return data;
    }
    public void SetHistoryPage(UCPlayer player, ModerationData data, int page)
    {
        ModerationEntry[] entries = data.HistoryView ?? Array.Empty<ModerationEntry>();
        data.HistoryPage = page;
        int pgCt = data.PageCount;
        int offset = page * ModerationHistoryLength;
        ModerationHistoryPage.SetText(player.Connection, page.ToString(player.Locale.CultureInfo));
        if (page > 0)
            ModerationHistoryBackButton.Enable(player.Connection);
        else
            ModerationHistoryBackButton.Disable(player.Connection);
        if (page < pgCt - 1)
            ModerationHistoryNextButton.Enable(player.Connection);
        else
            ModerationHistoryNextButton.Disable(player.Connection);
        for (int i = 0; i < ModerationHistoryLength; ++i)
        {
            int index = i + offset;
            if (index >= entries.Length)
                ModerationHistory[i].Root.SetVisibility(player.Connection, false);
            else
                UpdateModerationEntry(player, index, entries[index]);
        }
    }

    private readonly List<UCPlayer> _tempPlayerSearchBuffer = new List<UCPlayer>(Provider.maxPlayers);
    public void SendModerationPlayerList(UCPlayer player)
    {
        ThreadUtil.assertIsGameThread();

        ITransportConnection connection = player.Connection;

        if (!ModerationPlayerSearchModeButton.TryGetSelection(player.Player, out PlayerSearchMode searchMode))
            searchMode = PlayerSearchMode.Online;
        ModerationData data = GetOrAddModerationData(player);
        UnturnedTextBoxData? textBoxData = UnturnedUIDataSource.GetData<UnturnedTextBoxData>(player.CSteamID, ModerationPlayerSearch.TextBox);
        string searchText = textBoxData?.Text ?? string.Empty;
        data.PlayerList ??= new ulong[ModerationPlayerList.Length];
        if (searchText.Length < 3 || searchMode == PlayerSearchMode.Online)
        {
            List<UCPlayer> buffer;
            bool clr = false;
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                UCPlayer.Search(searchText, UCPlayer.NameSearch.PlayerName, _tempPlayerSearchBuffer);
                buffer = _tempPlayerSearchBuffer;
                clr = true;
            }
            else
            {
                buffer = PlayerManager.OnlinePlayers;
            }

            try
            {
                int ct = Math.Min(ModerationPlayerList.Length, buffer.Count);
                int i = 0;
                for (; i < ct; ++i)
                {
                    UCPlayer listPlayer = buffer[i];
                    UpdateModerationPlayerListEntry(player, i, listPlayer, true);

                    data.PlayerList[i] = listPlayer.Steam64;
                }
                
                for (; i < ModerationPlayerList.Length; ++i)
                {
                    ModerationPlayerList[i].Root.SetVisibility(connection, false);
                    data.PlayerList[i] = 0;
                }
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
                    UpdateModerationPlayerListEntry(player, i2, name, false);

                    data.PlayerList[i2] = name.Steam64;
                }

                for (; i2 < ModerationPlayerList.Length; ++i2)
                {
                    ModerationPlayerList[i2].Root.SetVisibility(connection, false);
                    data.PlayerList[i2] = 0;
                }
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
    private void UpdateModerationPlayerListEntry(UCPlayer player, int index, UCPlayer listPlayer, bool downloadAvatar)
    {
        PlayerListEntry entry = ModerationPlayerList[index];
        ITransportConnection connection = player.Connection;
        entry.SteamId.SetText(connection, listPlayer.Steam64.ToString(CultureInfo.InvariantCulture));
        entry.Name.SetText(connection, listPlayer.Name.PlayerName);
        if (Data.ModerationSql.TryGetAvatar(listPlayer.Steam64, AvatarSize.Small, out string avatarUrl))
            entry.ProfilePicture.SetImage(connection, avatarUrl);
        else
        {
            entry.ProfilePicture.SetImage(connection, string.Empty);
            if (downloadAvatar)
            {
                UniTask.Create(async () =>
                {
                    string? icon = await listPlayer.GetProfilePictureURL(AvatarSize.Small, player.DisconnectToken);
                    await UniTask.SwitchToMainThread(player.DisconnectToken);
                    entry.ProfilePicture.SetImage(player.Connection, icon ?? string.Empty);
                });
            }
        }

        entry.Root.SetVisibility(player.Connection, true);
    }
    
    public void SelectEntry(UCPlayer player, ModerationEntry? entry)
    {
        ThreadUtil.assertIsGameThread();

        ModerationData data = GetOrAddModerationData(player);
        data.SelectedEntry = entry;
        ITransportConnection c = player.Connection;

        if (entry == null)
        {
            ModerationInfoRoot.SetVisibility(c, false);
            return;
        }

        ModerationInfoType.SetText(c, entry.GetDisplayName());
        ModerationInfoTimestamp.SetText(c, (entry.ResolvedTimestamp ?? entry.StartedTimestamp).UtcDateTime.ToString(DateTimeFormat, CultureInfo.InvariantCulture));
        ModerationInfoReputation.SetText(c, FormatReputation(entry.Reputation, player.Locale.CultureInfo, false));
        ModerationInfoReason.SetText(c, string.IsNullOrWhiteSpace(entry.Message) ? "No message." : entry.Message!);
        if (Data.ModerationSql.TryGetAvatar(entry.Player, AvatarSize.Full, out string avatarUrl))
            ModerationInfoProfilePicture.SetImage(c, avatarUrl);
        else
        {
            ModerationInfoProfilePicture.SetImage(c, string.Empty);
            UniTask.Create(async () =>
            {
                string? icon = await F.GetProfilePictureURL(entry.Player, AvatarSize.Full, player.DisconnectToken);

                await UniTask.SwitchToMainThread(player.DisconnectToken);
                ModerationInfoProfilePicture.SetImage(player.Connection, icon ?? string.Empty);
            });
        }

        ModerationInfoActorsHeader.SetVisibility(c, entry.Actors.Length > 0);
        ModerationInfoEvidenceHeader.SetVisibility(c, entry.Evidence.Length > 0);

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

                evidenceUi.Root.SetVisibility(c, true);
            }

            for (; i < ModerationInfoEvidenceEntries.Length; ++i)
                ModerationInfoEvidenceEntries[i].Root.SetVisibility(c, false);

            UCWarfare.RunTask(async token =>
            {
                for (int j = 0; j < ct; ++j)
                {
                    IModerationActor actor = entry.Evidence[i].Actor;
                    ValueTask<string> name = actor.GetDisplayName(Data.ModerationSql, token);
                    UnturnedLabel nameLbl = ModerationInfoEvidenceEntries[i].ActorName;
                    if (name.IsCompleted)
                    {
                        nameLbl.SetText(c, name.Result);
                    }
                    else
                    {
                        string nameText = await name.ConfigureAwait(false);
                        nameLbl.SetText(c, nameText);
                    }
                }
            }, player.DisconnectToken);
        }

        if (entry.Actors.Length > 0)
        {
            UCWarfare.RunTask(async token =>
            {
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

                for (; i < ct; ++i)
                {
                    RelatedActor actor = entry.Actors[i];
                    ModerationInfoActor actorUi = ModerationInfoActors[i];
                    actorUi.Role.SetText(c, string.IsNullOrWhiteSpace(actor.Role) ? "No role" : actor.Role);
                    if (Util.IsValidSteam64Id(actor.Actor.Id))
                        actorUi.Steam64.SetText(c, actor.Actor.Id.ToString(CultureInfo.InvariantCulture));
                    else actorUi.Steam64.SetText(c, "...");
                    actorUi.Root.SetVisibility(c, true);
                }

                for (; i < ModerationInfoActors.Length; ++i)
                    ModerationInfoActors[i].Root.SetVisibility(c, false);

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
                        actorUi.ProfilePicture.SetImage(c, url);
                    }
                }

            }, player.DisconnectToken, ctx: $"Update actor info for moderation entry {entry.Id}.");
        }
        UCWarfare.RunTask(async token =>
        {
            List<string> extraInfo = new List<string>();
            await entry.AddExtraInfo(Data.ModerationSql, extraInfo, player.Locale.CultureInfo, token);

            await UCWarfare.ToUpdate(token);

            int ct = Math.Min(extraInfo.Count, ModerationInfoExtraInfo.Length);
            int i = 0;
            for (; i < ct; ++i)
            {
                ModerationInfoExtraInfo[i].SetVisibility(c, true);
                ModerationInfoExtraInfo[i].SetText(c, extraInfo[i]);
            }

            for (; i < ModerationInfoExtraInfo.Length; ++i)
                ModerationInfoExtraInfo[i].SetVisibility(c, false);

            LogicModerationInfoUpdateScrollVisual.SetVisibility(c, true);

        }, player.DisconnectToken, ctx: $"Update extra info for moderation entry {entry.Id}.");
    }
    private void UpdateModerationPlayerListEntry(UCPlayer player, int index, PlayerNames listPlayerNames, bool downloadAvatar)
    {
        PlayerListEntry entry = ModerationPlayerList[index];
        ITransportConnection connection = player.Connection;
        entry.SteamId.SetText(connection, listPlayerNames.Steam64.ToString(CultureInfo.InvariantCulture));
        entry.Name.SetText(connection, listPlayerNames.PlayerName);
        if (Data.ModerationSql.TryGetAvatar(listPlayerNames.Steam64, AvatarSize.Small, out string avatarUrl))
            entry.ProfilePicture.SetImage(connection, avatarUrl);
        else
        {
            entry.ProfilePicture.SetImage(connection, string.Empty);
            if (downloadAvatar)
            {
                UniTask.Create(async () =>
                {
                    string? icon = await F.GetProfilePictureURL(listPlayerNames.Steam64, AvatarSize.Small, player.DisconnectToken);
                    await UniTask.SwitchToMainThread(player.DisconnectToken);
                    entry.ProfilePicture.SetImage(player.Connection, icon ?? string.Empty);
                });
            }
        }

        entry.Root.SetVisibility(player.Connection, true);
    }
    private void UpdateModerationEntry(UCPlayer player, int index, ModerationEntry entry)
    {
        ITransportConnection connection = player.Connection;

        ModerationHistoryEntry ui = ModerationHistory[index];
        ModerationEntryType? type = ModerationReflection.GetType(entry.GetType());
        ui.Type.SetText(connection, type.HasValue ? type.Value.ToString() : entry.GetType().Name);
        ui.Message.SetText(connection, string.IsNullOrWhiteSpace(entry.Message) ? "== No Message ==" : entry.Message!);
        ui.Reputation.SetText(connection, FormatReputation(entry.Reputation, player.Locale.CultureInfo, false));
        ui.Timestamp.SetText(connection, (entry.ResolvedTimestamp ?? entry.StartedTimestamp).UtcDateTime.ToString(DateTimeFormat));
        if (entry.TryGetPrimaryAdmin(out RelatedActor actor))
        {
            if (!actor.Actor.Async)
            {
                ui.Admin.SetText(connection, actor.Actor.GetDisplayName(Data.ModerationSql, CancellationToken.None).Result);
                ui.AdminProfilePicture.SetImage(connection, actor.Actor.GetProfilePictureURL(Data.ModerationSql, AvatarSize.Medium, CancellationToken.None).Result ?? string.Empty, false);
            }
            else
            {
                ui.Admin.SetText(connection, string.Empty);
                ui.AdminProfilePicture.SetImage(connection, string.Empty);
                UniTask.Create(async () =>
                {
                    ValueTask<string?> pfpTask = actor.Actor.GetProfilePictureURL(Data.ModerationSql, AvatarSize.Medium, player.DisconnectToken);
                    ValueTask<string> displayNameTask = actor.Actor.GetDisplayName(Data.ModerationSql, player.DisconnectToken);
                    string displayName = await displayNameTask.ConfigureAwait(false);
                    string? icon = await pfpTask.ConfigureAwait(false);
                    await UniTask.SwitchToMainThread(player.DisconnectToken);
                    ui.Admin.SetText(player.Connection, displayName ?? actor.Actor.Id.ToString());
                    ui.AdminProfilePicture.SetImage(player.Connection, icon ?? string.Empty);
                });
            }
        }
        else
        {
            ui.Admin.SetText(connection, string.Empty);
            ui.AdminProfilePicture.SetImage(connection, string.Empty);
        }

        if (entry is IDurationModerationEntry duration)
            ui.Duration.SetText(connection, duration.IsPermanent ? "∞" : Util.ToTimeString((int)Math.Round(duration.Duration.TotalSeconds), 2));
        else
            ui.Duration.SetText(connection, string.Empty);

        ui.Root.SetVisibility(connection, true);
    }
    private async Task RefreshModerationHistory(UCPlayer player, CancellationToken token = default)
    {
        ThreadUtil.assertIsGameThread();

        ModerationData data = GetOrAddModerationData(player);
        UnturnedTextBoxData? textBoxData = UnturnedUIDataSource.GetData<UnturnedTextBoxData>(player.CSteamID, ModerationHistorySearch.TextBox);
        ModerationHistroyTypeButton.TryGetSelection(player.Player, out ModerationEntryType filter);
        ModerationHistroySearchTypeButton.TryGetSelection(player.Player, out ModerationHistorySearchMode searchMode);
        ModerationHistroySortTypeButton.TryGetSelection(player.Player, out ModerationHistorySortMode sortMode);
        Type type = (filter == ModerationEntryType.None ? null : ModerationReflection.GetType(filter)) ?? typeof(ModerationEntry);
        DateTimeOffset? start = null, end = null;
        string? orderBy = null, condition = null;
        object[]? conditionArgs = null;
        string? text = textBoxData?.Text;
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
                condition = $"`main`.`{DatabaseInterface.ColumnEntriesMessage}` LIKE {{0}}";
                conditionArgs = new object[] { "%" + text + "%" };
            }
            else if (searchMode == ModerationHistorySearchMode.Admin)
            {
                if (ulong.TryParse(text, NumberStyles.Number, player.Locale.CultureInfo, out ulong steam64) && Util.IsValidSteam64Id(steam64))
                {
                    condition = $"(SELECT COUNT(*) FROM `{DatabaseInterface.TableActors}` AS `a` " +
                                 $"WHERE `a`.`{DatabaseInterface.ColumnExternalPrimaryKey}` = `main`.`{DatabaseInterface.ColumnEntriesPrimaryKey}` " +
                                 $"AND `a`.`{DatabaseInterface.ColumnActorsId}`={{0}} " +
                                 $"AND `a`.`{DatabaseInterface.ColumnActorsAsAdmin}` != 0) " +
                                $"> 0";
                    conditionArgs = new object[] { steam64 };
                }
                else
                {
                    condition = $"(SELECT COUNT(*) FROM `{DatabaseInterface.TableActors}` AS `a` " +
                                 $"WHERE `a`.`{DatabaseInterface.ColumnExternalPrimaryKey}` = `main`.`{DatabaseInterface.ColumnEntriesPrimaryKey}` " +
                                 $"AND " +
                                 $"(SELECT COUNT(*) FROM `{WarfareSQL.TableUsernames}` AS `u` " +
                                  $"WHERE `a`.`{DatabaseInterface.ColumnActorsId}`=`u`.`{WarfareSQL.ColumnUsernamesSteam64}` " +
                                 $"AND " +
                                  $"(`u`.`{WarfareSQL.ColumnUsernamesPlayerName}` LIKE @0 OR `u`.`{WarfareSQL.ColumnUsernamesCharacterName}` LIKE @0 OR `u`.`{WarfareSQL.ColumnUsernamesNickName}` LIKE @0))" +
                                 $" > 0)" +
                                $" > 0";
                    conditionArgs = new object[] { "%" + text + "%" };
                }
            }
            else if (searchMode == ModerationHistorySearchMode.Type)
            {
                if (Enum.TryParse(text, true, out ModerationEntryType entryType))
                {
                    condition = $"`main`.`{DatabaseInterface.ColumnEntriesType}` = @0";
                    conditionArgs = new object[] { entryType.ToString() };
                }
                else
                {
                    condition = $"`main`.`{DatabaseInterface.ColumnEntriesType}` LIKE @0";
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


        ModerationEntry[] entries = (ModerationEntry[])await Data.ModerationSql.ReadAll(type, data.SelectedPlayer == 0ul ? player.Steam64 : data.SelectedPlayer,
            data.SelectedPlayer == 0ul ? ActorRelationType.IsActor : ActorRelationType.IsTarget, false, start, end, orderBy, condition, conditionArgs, token);

        await UCWarfare.ToUpdate(token);

        data.HistoryView = entries;
        int pgCt = data.PageCount;
        SetHistoryPage(player, data, data.HistoryPage >= pgCt ? pgCt : data.HistoryPage);
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

        [UIPattern("RoleText", Mode = FormatMode.Format)]
        public UnturnedLabel RoleText { get; set; }

        [UIPattern("Steam64", Mode = FormatMode.Format)]
        public UnturnedTextBox Steam64Input { get; set; }

        [UIPattern("Steam64Text", Mode = FormatMode.Format)]
        public UnturnedLabel Steam64Text { get; set; }

        [UIPattern("You", Mode = FormatMode.Format)]
        public UnturnedButton YouButton { get; set; }

        [UIPattern("AsAdminCheck", Mode = FormatMode.Format)]
        public UnturnedButton AsAdminToggleButton { get; set; }

        [UIPattern("AsAdminCheckToggleState", Mode = FormatMode.Format)]
        public UnturnedButton AsAdminToggleState { get; set; }
    }
    public class ModerationSelectedEvidence
    {
        [UIPattern("", Mode = FormatMode.Format)]
        public UnturnedUIElement Root { get; set; }

        [UIPattern("Preview", Mode = FormatMode.Format)]
        public UnturnedImage PreviewImage { get; set; }

        [UIPattern("PreviewName", Mode = FormatMode.Format)]
        public UnturnedLabel PreviewName { get; set; }

        [UIPattern("NoPreviewName", Mode = FormatMode.Format)]
        public UnturnedLabel NoPreviewName { get; set; }

        [UIPattern("Actor", Mode = FormatMode.Format)]
        public UnturnedLabel ActorName { get; set; }

        [UIPattern("Timestamp", Mode = FormatMode.Format)]
        public UnturnedTextBox TimestampInput { get; set; }

        [UIPattern("TimestampText", Mode = FormatMode.Format)]
        public UnturnedTextBox TimestampText { get; set; }

        [UIPattern("Message", Mode = FormatMode.Format)]
        public UnturnedTextBox MessageInput { get; set; }

        [UIPattern("MessageText", Mode = FormatMode.Format)]
        public UnturnedTextBox MessageText { get; set; }

        [UIPattern("Link", Mode = FormatMode.Format)]
        public UnturnedTextBox LinkInput { get; set; }

        [UIPattern("LinkText", Mode = FormatMode.Format)]
        public UnturnedTextBox LinkText { get; set; }

        [UIPattern("Steam64", Mode = FormatMode.Format)]
        public UnturnedTextBox Steam64Input { get; set; }

        [UIPattern("Steam64Text", Mode = FormatMode.Format)]
        public UnturnedTextBox Steam64Text { get; set; }

        [UIPattern("Now", Mode = FormatMode.Format)]
        public UnturnedButton NowButton { get; set; }

        [UIPattern("You", Mode = FormatMode.Format)]
        public UnturnedButton YouButton { get; set; }
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
        internal int SearchVersion;
        public CSteamID Player { get; }
        public ModerationUI Owner { get; }
        UnturnedUI IUnturnedUIData.Owner => Owner;
        UnturnedUIElement IUnturnedUIData.Element => Owner.Headers[(int)Page.Moderation].Button;
        public ModerationEntry[]? HistoryView { get; set; }
        public ulong[]? PlayerList { get; set; }
        public int PageCount => HistoryView is not { Length: > 0 } ? 1 : Mathf.CeilToInt(HistoryView.Length / (float)ModerationHistoryLength);
        public int HistoryPage { get; set; }
        public ulong SelectedPlayer { get; set; }
        public ModerationEntry? SelectedEntry { get; set; }
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
        [Translatable("Reputation (Highest)")]
        HighestReputation,
        [Translatable("Reputation (Lowest)")]
        LowestReputation,
        Type
    }
}
