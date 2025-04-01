using SDG.Framework.Utilities;
using SDG.NetTransport;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Presets;
using Uncreated.Warfare.Database.Manual;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Moderation.Punishments;
using Uncreated.Warfare.Moderation.Punishments.Presets;
using Uncreated.Warfare.Moderation.Records;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;
using UnityEngine.Networking;

namespace Uncreated.Warfare.Moderation;
partial class ModerationUI
{
    private void EndEditInActionMenu(WarfarePlayer player)
    {
        ModerationData data = GetOrAddModerationData(player);

        data.PrimaryEditingEntry = null;
        data.SecondaryEditingEntry = null;
        if (data.PendingPreset != PresetType.None)
        {
            if ((int)data.PendingPreset - 1 < Presets.Length)
                Presets[(int)data.PendingPreset - 1].Enable(player.Connection);
            data.PendingPreset = PresetType.None;
        }

        if (data.PendingType != ModerationEntryType.None)
        {
            LabeledStateButton? btn = GetModerationButton(data.PendingType);
            btn?.Enable(player.Connection);

            data.PendingType = ModerationEntryType.None;
        }

        ModerationFormRoot.SetVisibility(player.Connection, false);
        for (int i = 0; i < ModerationActionControls.Length; ++i)
            ModerationActionControls[i].Hide(player);

        data.Actors.Clear();
        data.Evidence.Clear();
    }
    public bool EditEntry(WarfarePlayer player, ModerationEntry entry)
    {
        if (entry.IsLegacy && entry is not Punishment)
            return false;

        ModerationData data = GetOrAddModerationData(player);

        if (entry is not Punishment punishment || punishment.PresetType == PresetType.None)
        {
            data.PendingType = ModerationReflection.GetType(entry.GetType()) ?? ModerationEntryType.None;
            data.PendingPreset = PresetType.None;
            data.PrimaryEditingEntry = entry;
            data.SecondaryEditingEntry = null;
            data.SelectedPlayer = entry.Player;
            UpdateSelectedPlayer(player);
            LoadActionMenu(player, true);
        }
        else
        {
            UniTask.Create(async () =>
            {
                Punishment[] punishments = await _moderationSql.GetEntriesOfLevel<Punishment>(new CSteamID(punishment.Player),
                    punishment.PresetLevel, punishment.PresetType, token: player.DisconnectToken);

                await UniTask.SwitchToMainThread(player.DisconnectToken);

                // prevent spamming
                data.LastViewedTime = Time.realtimeSinceStartup + 5f;

                if (punishments.Length > 0)
                    punishment = punishments[0];

                data.PendingType = ModerationEntryType.None;
                data.PendingPreset = punishment.PresetType;
                data.PrimaryEditingEntry = punishment;
                data.SecondaryEditingEntry = punishments.Length > 1 ? punishments[1] : null;
                data.SelectedPlayer = entry.Player;
                UpdateSelectedPlayer(player);
                LoadActionMenu(player, true);
            });
        }

        return true;
    }
    private void LoadActionMenu(WarfarePlayer player, bool editingExisting)
    {
        ModerationData data = GetOrAddModerationData(player);

        ITransportConnection c = player.Connection;
        if (new CSteamID(data.SelectedPlayer).GetEAccountType() != EAccountType.k_EAccountTypeIndividual
            || !editingExisting && data is { PendingPreset: PresetType.None, PendingType: ModerationEntryType.None })
        {
            ModerationFormRoot.SetVisibility(c, false);
            return;
        }

        if (editingExisting)
        {
            if (data.PrimaryEditingEntry == null)
            {
                GetLogger().LogWarning("Missing moderation entry.");
                return;
            }
            if (data.SecondaryEditingEntry == null)
            {
                data.PendingType = ModerationReflection.GetType(data.PrimaryEditingEntry.GetType()) ?? ModerationEntryType.None;
                data.PendingPreset = PresetType.None;
            }
            else
            {
                data.PendingType = ModerationEntryType.None;
                data.PendingPreset = data.PrimaryEditingEntry is Punishment punishment ? punishment.PresetType : PresetType.None;
            }

            data.Actors.Clear();
            data.Evidence.Clear();

            data.Actors.AddRange(data.PrimaryEditingEntry.Actors);
            data.Evidence.AddRange(data.PrimaryEditingEntry.Evidence);

            if (data is { PendingType: ModerationEntryType.None, PendingPreset: PresetType.None })
            {
                GetLogger().LogWarning("Invalid moderation types.");
                return;
            }
            SendActorsAndEvidence(player);
        }
        else
        {
            for (int i = 1; i < ModerationActionActors.Length; ++i)
                ModerationActionActors[i].Root.SetVisibility(c, false);

            for (int i = 1; i < ModerationActionEvidence.Length; ++i)
                ModerationActionEvidence[i].Root.SetVisibility(c, false);
            
            RelatedActor actor = new RelatedActor(RelatedActor.RolePrimaryAdmin, true, Actors.GetActor(player.Steam64));
            if (data.Actors.Count >= 1)
            {
                data.Actors.RemoveRange(1, data.Actors.Count - 1);
                data.Actors[0] = actor;
            }
            else if (data.Actors.Count == 0)
                data.Actors.Add(actor);

            Evidence evidence = new Evidence(string.Empty, null, null, false, actor.Actor, DateTimeOffset.Now);
            if (data.Evidence.Count >= 1)
            {
                data.Evidence.RemoveRange(1, data.Evidence.Count - 1);
                data.Evidence[0] = evidence;
            }
            else if (data.Evidence.Count == 0)
                data.Evidence.Add(evidence);

            ModerationActionMessage.SetText(player.Connection, data.PrimaryEditingEntry?.Message ?? string.Empty);

            Interlocked.Increment(ref data.ActionVersion);

            ModerationSelectedActor mainActor = ModerationActionActors[0];
            mainActor.Root.SetVisibility(c, true);
            mainActor.Name.SetText(c, player.Names.GetDisplayNameOrPlayerName());
            mainActor.YouButton.SetVisibility(c, false);
            mainActor.Steam64Input.SetText(c, player.Steam64.m_SteamID.ToString(CultureInfo.InvariantCulture));
            mainActor.RoleInput.SetText(c, RelatedActor.RolePrimaryAdmin);
            mainActor.AsAdminToggleState.SetVisibility(c, false);
            mainActor.AsAdminToggleButton.SetVisibility(c, false);
            mainActor.RemoveButton.SetVisibility(c, false);

            ModerationActionAddActorButton.Enable(c);

            ModerationSelectedEvidence mainEvidence = ModerationActionEvidence[0];
            mainEvidence.Root.SetVisibility(c, true);
            mainEvidence.PreviewRoot.SetVisibility(c, false);
            mainEvidence.PreviewName.SetVisibility(c, false);
            mainEvidence.NoPreviewName.SetText(c, string.Empty);
            mainEvidence.ActorName.SetText(c, player.Names.GetDisplayNameOrPlayerName());
            mainEvidence.TimestampInput.SetText(c, evidence.Timestamp.UtcDateTime.ToString(DateTimeFormatInput));
            mainEvidence.MessageInput.SetText(c, string.Empty);
            mainEvidence.LinkInput.SetText(c, string.Empty);
            mainEvidence.Steam64Input.SetText(c, player.Steam64.m_SteamID.ToString(CultureInfo.InvariantCulture));
            mainEvidence.YouButton.SetVisibility(c, false);
            mainEvidence.RemoveButton.SetVisibility(c, false);

            ModerationActionAddEvidenceButton.Enable(c);

            if (player.SteamSummary != null)
            {
                mainActor.ProfilePicture.SetImage(c, player.SteamSummary.AvatarUrlMedium ?? string.Empty);
            }
            else
            {
                UniTask.Create(async () =>
                {
                    string? url = await _moderationSql.GetAvatarAsync(player.Steam64, AvatarSize.Medium, token: player.DisconnectToken);
                    mainActor.ProfilePicture.SetImage(c, url ?? string.Empty);
                });
            }
        }

        if (data.PendingType != ModerationEntryType.None)
        {
            ModerationActionPresetHeaderRoot.SetVisibility(c, false);
            ModerationActionTypeHeader.SetText(c, _valueFormatter.FormatEnum(data.PendingType, player.Locale.LanguageInfo));
            ModerationActionMessage.SetText(player.Connection, string.Empty);
            CreateInstances(data, player);
            UpdateArgumentTypes(player, editingExisting);
            Interlocked.Increment(ref data.ActionModeVersion);
        }
        else
        {
            if (data.PendingPreset == PresetType.None)
                return;
            ModerationActionTypeHeader.SetText(c, "...");
            UniTask.Create(async () =>
            {
                int v = Interlocked.Increment(ref data.ActionModeVersion);
                if (data.PendingPreset == PresetType.None || !PunishmentPresets.TryGetPreset(data.PendingPreset, out PunishmentPreset[] presets))
                    return;
                int nextLevel = await _moderationSql.GetNextPresetLevel(data.SelectedPlayer, data.PendingPreset, player.DisconnectToken).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(player.DisconnectToken);
                if (data.ActionModeVersion != v)
                    return;
                int index = nextLevel;
                if (index < 1)
                    index = 1;
                else if (index > presets.Length)
                    index = presets.Length;
                PunishmentPreset preset = presets[index - 1];
                data.PendingPresetValue = preset;
                string str = _valueFormatter.FormatEnum(preset.PrimaryModerationType, player.Locale.LanguageInfo);

                if (preset.PrimaryDuration.HasValue)
                {
                    str = (preset.PrimaryDuration.Value.Ticks < 0
                        ? "Permanent "
                        : (FormattingUtility.ToTimeString(preset.PrimaryDuration.Value) + " ")) + str;
                }

                if (preset.SecondaryModerationType is > ModerationEntryType.None)
                {
                    str += " + ";
                    if (preset.SecondaryDuration.HasValue)
                    {
                        str += preset.SecondaryDuration.Value.Ticks < 0
                            ? "Permanent "
                            : (FormattingUtility.ToTimeString(preset.SecondaryDuration.Value) + " ");
                    }

                    str += _valueFormatter.FormatEnum(preset.SecondaryModerationType!.Value, player.Locale.LanguageInfo);
                }

                ModerationActionTypeHeader.SetText(c, str);
                ModerationActionPresetHeader.SetText(c, _valueFormatter.FormatEnum(data.PendingPreset, player.Locale.LanguageInfo) + " | Level " + nextLevel);
                CreateInstances(data, player);

                UpdateArgumentTypes(player, editingExisting);


                ModerationActionMessage.SetText(player.Connection, preset.DefaultMessage ?? string.Empty);

                ModerationActionPresetHeaderRoot.SetVisibility(c, true);
            });
        }
        ModerationFormRoot.SetVisibility(c, true);
    }
    public static string GetPresetButtonText(PresetType type)
    {
        return type switch
        {
            PresetType.BypassingPunishment => "Bypassing Pnshmnt.",
            PresetType.InappropriateProfile => "Inappr. Profile",
            PresetType.DisruptiveBehavior => "Disrpt. Bhvr.",
            PresetType.TargetedHarassment => "Harassment",
            PresetType.IntentionalTeamkilling => "Int. TKing",
            PresetType.AssetWaste => "Asset Waste",
            _ => type.ToString()
        };
    }
    public static string GetModerationTypeButtonText(ModerationEntryType type)
    {
        return type switch
        {
            ModerationEntryType.VehicleTeamkill => "Veh. Tk.",
            ModerationEntryType.BattlEyeKick => "BtlEye. Kick",
            ModerationEntryType.GriefingReport => "Grief. Rep.",
            ModerationEntryType.ChatAbuseReport => "Chat Ab. Rep.",
            ModerationEntryType.CheatingReport => "Cheat. Rep.",
            ModerationEntryType.Commendation => "Commend.",
            ModerationEntryType.BugReportAccepted => "Bug Rep. Acc.",
            ModerationEntryType.PlayerReportAccepted => "Pl. Rep. Acc.",
            _ => type.ToString()
        };
    }
    private void UpdateArgumentTypes(WarfarePlayer player, bool editingExisting)
    {
        ModerationData data = GetOrAddModerationData(player);

        ModerationEntryType type = data.PendingType;
        ITransportConnection c = player.Connection;
        bool hasForgiveable = false, hasRemoveable = false;
        bool hasMute, hasAssetBan, isNote, hasPrimaryDuration, hasSecondaryDuration;
        if (!editingExisting)
        {
            if (data.PendingPresetValue == null)
            {
                if (type == ModerationEntryType.None)
                    goto hideAllElements;
                
                hasMute = type == ModerationEntryType.Mute;
                hasAssetBan = type == ModerationEntryType.AssetBan;
                hasPrimaryDuration = ModerationReflection.IsOfType<IDurationModerationEntry>(type);
                hasSecondaryDuration = false;
                isNote = type == ModerationEntryType.Note;
            }
            else
            {
                PresetType presetType = data.PendingPresetValue.Type;
                if (presetType == PresetType.None)
                    goto hideAllElements;

                hasMute = data.PendingPresetValue.PrimaryModerationType == ModerationEntryType.Mute || data.PendingPresetValue.SecondaryModerationType is ModerationEntryType.Mute;
                hasAssetBan = data.PendingPresetValue.PrimaryModerationType == ModerationEntryType.AssetBan || data.PendingPresetValue.SecondaryModerationType is ModerationEntryType.AssetBan;
                hasPrimaryDuration = ModerationReflection.IsOfType<IDurationModerationEntry>(data.PendingPresetValue.PrimaryModerationType);
                hasSecondaryDuration = data.PendingPresetValue.SecondaryModerationType.HasValue
                                       && ModerationReflection.IsOfType<IDurationModerationEntry>(data.PendingPresetValue.SecondaryModerationType.Value);
                isNote = false;
            }
        }
        else
        {
            hasMute = data.PrimaryEditingEntry is Mute || data.SecondaryEditingEntry is Mute;
            hasAssetBan = data.PrimaryEditingEntry is AssetBan || data.SecondaryEditingEntry is AssetBan;
            isNote = data is { PrimaryEditingEntry: Note, SecondaryEditingEntry: Note };
            hasRemoveable = data.PrimaryEditingEntry is { Removed: false } || data.SecondaryEditingEntry is { Removed: false };
            hasForgiveable = hasRemoveable && (data.PrimaryEditingEntry is IForgiveableModerationEntry forgiveable && forgiveable.IsApplied(true)
                          || data.SecondaryEditingEntry is IForgiveableModerationEntry forgiveable2 && forgiveable2.IsApplied(true));
            hasPrimaryDuration = data.PrimaryEditingEntry is IDurationModerationEntry;
            hasSecondaryDuration = data.SecondaryEditingEntry is IDurationModerationEntry;
        }

        GetLogger().LogDebug("hasMute: {0}, hasAssetBan: {1}, isNote: {2}, hasPrimaryDuration: {3}, hasSecondaryDuration: {4}.", hasMute, hasAssetBan, isNote, hasPrimaryDuration, hasSecondaryDuration);

        if (editingExisting)
            ModerationActionMessage.SetText(player, (data.PrimaryEditingEntry?.Message ?? data.SecondaryEditingEntry?.Message) ?? string.Empty);
        else
        {
            string msg = ModerationActionMessage.UpdateFromDataMainThread(player.UnturnedPlayer);
            if (data.PrimaryEditingEntry != null)
                data.PrimaryEditingEntry.Message = msg;
            if (data.SecondaryEditingEntry != null)
                data.SecondaryEditingEntry.Message = msg;
        }
        
        if (hasMute)
        {
            MuteTypeTracker.Show(c);
            Mute? mute = data.PrimaryEditingEntry as Mute ?? data.SecondaryEditingEntry as Mute;
            MuteTypeTracker.Set(player.UnturnedPlayer, mute?.Type ?? MuteType.Both);
        }
        else
        {
            MuteTypeTracker.Hide(c);
        }

        if (hasPrimaryDuration)
        {
            ModerationActionMiniInputBox1.Show(c);
            string primaryName;
            if (hasSecondaryDuration)
            {
                primaryName = _valueFormatter.FormatEnum(editingExisting
                    ? ModerationReflection.GetType(data.PrimaryEditingEntry!.GetType()) ?? ModerationEntryType.None
                    : data.PendingPresetValue!.PrimaryModerationType, player.Locale.LanguageInfo) + " ";
            }
            else primaryName = string.Empty;

            TimeSpan duration = editingExisting
                ? ((IDurationModerationEntry?)data.PrimaryEditingEntry)!.Duration
                : (data.PendingPresetValue?.PrimaryDuration ?? TimeSpan.FromHours(12d));

            ModerationActionMiniInputBox1.SetText(c, duration < TimeSpan.Zero ? "Permanent" : FormattingUtility.ToTimeString(duration));
            ModerationActionMiniInputBox1.SetPlaceholder(c, primaryName + "Duration");
        }
        else ModerationActionMiniInputBox1.Hide(c);

        if (hasSecondaryDuration)
        {
            ModerationActionMiniInputBox2.Show(c);
            string secondaryName;
            if (hasPrimaryDuration)
            {
                secondaryName = _valueFormatter.FormatEnum(editingExisting
                    ? ModerationReflection.GetType(data.SecondaryEditingEntry!.GetType())!.Value
                    : data.PendingPresetValue!.SecondaryModerationType!.Value, player.Locale.LanguageInfo) + " ";
            }
            else secondaryName = string.Empty;

            TimeSpan duration = editingExisting
                ? ((IDurationModerationEntry?)data.SecondaryEditingEntry)!.Duration
                : (data.PendingPresetValue!.SecondaryDuration ?? TimeSpan.FromHours(6d));

            ModerationActionMiniInputBox2.SetText(c, duration < TimeSpan.Zero ? "Permanent" : FormattingUtility.ToTimeString(duration));
            ModerationActionMiniInputBox2.SetPlaceholder(c, secondaryName + "Duration");
        }
        else ModerationActionMiniInputBox2.Hide(c);

        if (!isNote)
        {
            ModerationActionInputBox2.Show(c);
            double rep;
            if (editingExisting)
            {
                if (data is { PrimaryEditingEntry: not null, SecondaryEditingEntry: not null })
                {
                    rep = (data.PrimaryEditingEntry.Reputation + data.SecondaryEditingEntry.Reputation) / 2d;
                }
                else if (data.SecondaryEditingEntry != null)
                {
                    rep = data.SecondaryEditingEntry.Reputation;
                }
                else if (data.PrimaryEditingEntry != null)
                {
                    rep = data.PrimaryEditingEntry.Reputation;
                }
                else
                {
                    rep = 0d;
                }
            }
            else if (data.PendingPresetValue != null)
            {
                rep = data.PendingPresetValue.Reputation;
            }
            else
            {
                rep = GetDefaultRep(data.PendingType);
            }

            if (data.PrimaryEditingEntry != null)
            {
                data.PrimaryEditingEntry.Reputation = rep;
                data.PrimaryEditingEntry.PendingReputation = rep;
            }

            if (data.SecondaryEditingEntry != null)
            {
                data.SecondaryEditingEntry.Reputation = rep;
                data.SecondaryEditingEntry.PendingReputation = data.PrimaryEditingEntry != null ? 0d : rep;
            }
            
            ModerationActionInputBox2.SetText(c, rep == 0 ? string.Empty : rep.ToString(player.Locale.ParseFormat));
            ModerationActionInputBox2.SetPlaceholder(c, "Reputation");
        }
        else ModerationActionInputBox2.Hide(c);

        if (hasAssetBan)
        {
            ModerationActionInputBox3.Show(c);
            AssetBan? assetBan = data.PrimaryEditingEntry as AssetBan ?? data.SecondaryEditingEntry as AssetBan;
            if (assetBan != null)
                ModerationActionInputBox3.SetText(c, assetBan.GetCommaList(true));
            else
                ModerationActionInputBox3.SetText(c, string.Empty);
            ModerationActionInputBox3.SetPlaceholder(c, "Vehicles (comma separated, blank = all)");
        }
        else ModerationActionInputBox3.Hide(c);

        ModerationActionToggleButton2.Hide(c);

        int ct;
        ModerationActionControls[0].SetText(c, "Cancel");
        if (editingExisting)
        {
            ModerationActionControls[1].SetText(c, "Save");
            ct = 2;
            if (hasRemoveable)
            {
                ModerationActionControls[ct].SetText(c, "Remove");
                ++ct;
            }
            if (hasForgiveable)
            {
                ModerationActionControls[ct].SetText(c, "Forgive");
                ++ct;
            }

            if (ct > ModerationActionControls.Length)
                ct = ModerationActionControls.Length;
        }
        else
        {
            ModerationActionControls[1].SetText(c, "Add");
            ct = Math.Min(2, ModerationActionControls.Length);
        }

        int i2 = 0;
        for (; i2 < ct; ++i2)
            ModerationActionControls[i2].SetVisibility(c, true);

        for (; i2 < ModerationActionControls.Length; ++i2)
            ModerationActionControls[i2].SetVisibility(c, false);

        TimeUtility.InvokeAfterDelay(() => LogicModerationActionsUpdateScrollVisual.Show(player), 0.125f);

        return;

        hideAllElements:

        for (int i = 0; i < ModerationActionControls.Length; ++i)
            ModerationActionControls[i].SetVisibility(c, false);
        ModerationActionInputBox2.Hide(c);
        ModerationActionInputBox3.Hide(c);
        ModerationActionToggleButton1.Button.SetVisibility(c, false);
        ModerationActionToggleButton2.Button.SetVisibility(c, false);
        ModerationActionMiniInputBox1.Hide(c);
        ModerationActionMiniInputBox2.Hide(c);

        TimeUtility.InvokeAfterDelay(() => LogicModerationActionsUpdateScrollVisual.Show(player), 0.075f);
        TimeUtility.InvokeAfterDelay(() => LogicModerationActionsUpdateScrollVisual.Show(player), 0.25f);
    }
    private void SendActorsAndEvidence(WarfarePlayer player, int startIndActor = 0, int lenActor = -1, int startIndEvidence = 0, int lenEvidence = -1)
    {
        ModerationData data = GetOrAddModerationData(player);

        int v = Interlocked.Increment(ref data.ActionVersion);
        ITransportConnection c = player.Connection;

        if (lenActor < 0)
            lenActor = data.Actors.Count - startIndActor;
        if (lenEvidence < 0)
            lenEvidence = data.Evidence.Count - startIndEvidence;

        if (lenEvidence > 0)
        {
            int i = 0;
            int ct = Math.Min(lenEvidence, ModerationActionEvidence.Length - startIndEvidence);
            ModerationActionAddEvidenceButton.SetState(c, data.Evidence.Count < ModerationActionEvidence.Length);
            for (; i < ct; ++i)
            {
                Evidence evidence = data.Evidence[i + startIndEvidence];
                ModerationSelectedEvidence evidenceUi = ModerationActionEvidence[i + startIndEvidence];

                evidenceUi.LinkInput.SetText(c, evidence.URL ?? string.Empty);
                evidenceUi.Steam64Input.SetText(c, evidence.Actor.Id.ToString(CultureInfo.InvariantCulture));
                evidenceUi.TimestampInput.SetText(c, evidence.Timestamp.UtcDateTime.ToString(DateTimeFormatInput, player.Locale.ParseFormat));

                string name;
                if (evidence.URL is { Length: > 1 })
                {
                    int lastSlash = evidence.URL.LastIndexOf('/');
                    if (lastSlash == evidence.URL.Length - 1)
                        lastSlash = evidence.URL.LastIndexOf('/', lastSlash - 1);

                    name = lastSlash < 0 ? evidence.URL : evidence.URL.Substring(lastSlash + 1);
                }
                else name = evidence.URL ?? string.Empty;

                if (evidence.Image)
                {
                    evidenceUi.NoPreviewName.SetVisibility(c, false);
                    evidenceUi.PreviewRoot.SetVisibility(c, true);
                    evidenceUi.PreviewImage.SetImage(c, evidence.URL);
                    evidenceUi.PreviewName.SetVisibility(c, true);
                    evidenceUi.PreviewName.SetText(c, name);
                }
                else
                {
                    evidenceUi.NoPreviewName.SetVisibility(c, true);
                    evidenceUi.NoPreviewName.SetText(c, name);
                    evidenceUi.PreviewRoot.SetVisibility(c, false);
                    evidenceUi.PreviewName.SetVisibility(c, false);
                }

                evidenceUi.YouButton.SetVisibility(c, player.Equals(evidence.Actor.Id));
                evidenceUi.MessageInput.SetText(c, evidence.Message ?? string.Empty);
                if (i + startIndEvidence == 0)
                    evidenceUi.RemoveButton.SetVisibility(c, false);
            }

            if (startIndEvidence + lenEvidence == data.Evidence.Count)
            {
                for (; i < ModerationActionEvidence.Length - startIndEvidence; ++i)
                    ModerationActionEvidence[i + startIndEvidence].Root.SetVisibility(c, false);
            }

            UniTask.Create(async () =>
            {
                if (data.ActionVersion != v)
                    return;

                for (int j = 0; j < ct; ++j)
                {
                    IModerationActor actor = data.Evidence[j].Actor;
                    ValueTask<string> name = actor.GetDisplayName(_moderationSql, player.DisconnectToken);
                    UnturnedLabel nameLbl = ModerationActionEvidence[j].ActorName;
                    if (name.IsCompleted)
                    {
                        nameLbl.SetText(c, name.Result);
                    }
                    else
                    {
                        string nameText = await name.ConfigureAwait(false);

                        if (data.ActionVersion == v)
                            nameLbl.SetText(c, nameText);
                    }
                }
            });
        }
        else
        {
            ModerationActionAddEvidenceButton.SetState(c, true);
            for (int i = lenEvidence - 1; i >= 0; --i)
            {
                ModerationActionEvidence[i + startIndEvidence].Root.SetVisibility(c, false);
            }
        }

        if (lenActor > 0)
        {
            ModerationActionAddActorButton.SetState(c, false);
            UniTask.Create(async () =>
            {
                if (data.ActionVersion != v)
                    return;
                int i = 0;
                int ct = Math.Min(lenActor, data.Actors.Count - startIndActor);
                ulong[] steamIds = await _moderationSql.GetActorSteam64IDs(data.Actors.Skip(startIndActor).Take(lenActor).Select(x => x.Actor).ToArray(), player.DisconnectToken).ConfigureAwait(false);

                if (data.ActionVersion != v)
                    return;

                await _moderationSql.CacheAvatarsAsync(steamIds, AvatarSize.Medium, player.DisconnectToken);
                ModerationActionAddActorButton.SetState(c, data.Actors.Count < ModerationActionActors.Length);

                if (data.ActionVersion != v)
                    return;

                for (; i < ct; ++i)
                {
                    RelatedActor actor = data.Actors[i];
                    ModerationSelectedActor actorUi = ModerationActionActors[i];
                    actorUi.RoleInput.SetText(c, string.IsNullOrWhiteSpace(actor.Role) ? "No role" : actor.Role);
                    if (new CSteamID(actor.Actor.Id).GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
                        actorUi.Steam64Input.SetText(c, actor.Actor.Id.ToString(CultureInfo.InvariantCulture));
                    else actorUi.Steam64Input.SetText(c, string.Empty);
                    actorUi.AsAdminToggleState.SetVisibility(c, actor.Admin);
                    actorUi.Root.SetVisibility(c, true);
                }

                for (; i < ModerationActionActors.Length; ++i)
                    ModerationActionActors[i].Root.SetVisibility(c, false);

                for (i = 0; i < ct; ++i)
                {
                    RelatedActor actor = data.Actors[i];
                    ModerationSelectedActor actorUi = ModerationActionActors[i];
                    ValueTask<string> unTask = actor.Actor.GetDisplayName(_moderationSql, player.DisconnectToken);
                    ValueTask<string?> imgTask;

                    if (new CSteamID(actor.Actor.Id).GetEAccountType() == EAccountType.k_EAccountTypeIndividual && _moderationSql.TryGetAvatar(actor.Actor.Id, AvatarSize.Medium, out string avatar))
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
                        if (data.ActionVersion != v)
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
                        if (data.ActionVersion != v)
                            return;
                        actorUi.ProfilePicture.SetImage(c, url);
                    }
                }

            });
        }
        else
        {
            ModerationActionAddActorButton.SetState(c, true);
            for (int i = lenActor - 1; i >= 0; --i)
            {
                ModerationActionEvidence[i + startIndActor].Root.SetVisibility(c, false);
            }
        }
    }
    private static double GetDefaultRep(ModerationEntryType type)
    {
        return type switch
        {
            ModerationEntryType.Ban => -80,
            ModerationEntryType.Mute => -70,
            ModerationEntryType.Kick => -20,
            ModerationEntryType.Warning => -15,
            ModerationEntryType.BugReportAccepted => 80,
            ModerationEntryType.PlayerReportAccepted => 25,
            ModerationEntryType.Commendation => 80,
            ModerationEntryType.Teamkill => -40,
            ModerationEntryType.VehicleTeamkill => -50,
            _ => 0
        };
    }
    private void CreateInstances(ModerationData data, WarfarePlayer player)
    {
        if (data.PrimaryEditingEntry == null)
        {
            if (data.PendingType != ModerationEntryType.None)
            {
                if (ModerationReflection.GetType(data.PendingType) is not { } type)
                {
                    GetLogger().LogWarning("Unknown pending type: {0}.", data.PendingType);
                    return;
                }

                data.PrimaryEditingEntry = (ModerationEntry)Activator.CreateInstance(type);
                data.PrimaryEditingEntry.Player = data.SelectedPlayer;
                data.PrimaryEditingEntry.StartedTimestamp = DateTimeOffset.UtcNow;
                data.SecondaryEditingEntry = null;

                if (data.PrimaryEditingEntry is IDurationModerationEntry duration)
                    duration.Duration = TimeSpan.FromHours(12d);
            }
            else if (data.PendingPresetValue != null)
            {
                if (ModerationReflection.GetType(data.PendingPresetValue.PrimaryModerationType) is not { } primaryType)
                {
                    GetLogger().LogWarning("Unknown preset primary type: {0}.", data.PendingPresetValue.PrimaryModerationType);
                    return;
                }

                Type? secondaryType = data.PendingPresetValue.SecondaryModerationType.HasValue
                    ? ModerationReflection.GetType(data.PendingPresetValue.SecondaryModerationType.Value)
                    : null;

                data.PrimaryEditingEntry = (ModerationEntry)Activator.CreateInstance(primaryType);
                data.PrimaryEditingEntry.Player = data.SelectedPlayer;
                data.PrimaryEditingEntry.StartedTimestamp = DateTimeOffset.UtcNow;
                data.SecondaryEditingEntry = secondaryType == null ? null : (ModerationEntry)Activator.CreateInstance(secondaryType);
                if (data.SecondaryEditingEntry != null)
                {
                    data.SecondaryEditingEntry.Player = data.SelectedPlayer;
                    data.SecondaryEditingEntry.StartedTimestamp = DateTimeOffset.UtcNow;
                }

                if (data.PrimaryEditingEntry is Punishment p)
                {
                    p.PresetType = data.PendingPresetValue.Type;
                    p.PresetLevel = data.PendingPresetValue.Level;
                }
                if (data.SecondaryEditingEntry is Punishment p2)
                {
                    p2.PresetType = data.PendingPresetValue.Type;
                    p2.PresetLevel = data.PendingPresetValue.Level;
                }

                if (data.PendingPresetValue.PrimaryDuration.HasValue && data.PrimaryEditingEntry is IDurationModerationEntry duration)
                    duration.Duration = data.PendingPresetValue.PrimaryDuration.Value;

                if (data.PendingPresetValue.SecondaryDuration.HasValue && data.SecondaryEditingEntry is IDurationModerationEntry duration2)
                    duration2.Duration = data.PendingPresetValue.SecondaryDuration.Value;
            }
            else return;
        }

        if (string.IsNullOrEmpty(data.PrimaryEditingEntry.Message))
        {
            string? msg = ModerationActionMessage.GetOrAddData(player.UnturnedPlayer, string.Empty).Text;

            data.PrimaryEditingEntry.Message = msg;
            if (data.SecondaryEditingEntry != null)
                data.SecondaryEditingEntry.Message = msg;
        }
        else
        {
            ModerationActionMessage.SetText(player, data.PrimaryEditingEntry.Message);
        }

        Mute? mute = data.PrimaryEditingEntry as Mute ?? data.SecondaryEditingEntry as Mute;

        if (mute != null)
        {
            if ((mute.Type == MuteType.None || mute.Id == 0u) && MuteTypeTracker.TryGetSelection(player.UnturnedPlayer, out MuteType muteType))
                mute.Type = muteType;
            else
            {
                if (mute.Type == MuteType.None)
                    mute.Type = MuteType.Both;
                MuteTypeTracker.Set(player.UnturnedPlayer, mute.Type);
            }
        }

        AssetBan? assetBan = data.PrimaryEditingEntry as AssetBan ?? data.SecondaryEditingEntry as AssetBan;

        if (assetBan != null)
        {
            if (assetBan.Id != 0u)
                ModerationActionInputBox3.SetText(player, assetBan.GetCommaList(true));
            else if (ModerationActionInputBox3.TextBox.GetOrAddData(player.UnturnedPlayer).Text is { Length: > 0 } text)
            {
                GetLogger().LogDebug("Filled from save of text: \"{0}\".", text);
                assetBan.FillFromText(text);
            }
            else
                ModerationActionInputBox3.SetText(player, string.Empty);
        }
    }
    private void Save(ModerationData data, WarfarePlayer player)
    {
        CreateInstances(data, player);
        if (data.PrimaryEditingEntry == null)
            return;
        bool isNew = data.PrimaryEditingEntry.Id == 0u;

        data.Actors.RemoveAll(x => x.Actor == null);
        data.Evidence.RemoveAll(x => string.IsNullOrWhiteSpace(x.URL));

        data.PrimaryEditingEntry.Actors = data.Actors.ToArray();
        if (data.SecondaryEditingEntry != null)
            data.SecondaryEditingEntry.Actors = data.PrimaryEditingEntry.Actors;

        data.PrimaryEditingEntry.Evidence = data.Evidence.ToArray();
        if (data.SecondaryEditingEntry != null)
            data.SecondaryEditingEntry.Evidence = data.PrimaryEditingEntry.Evidence;

        if (data.PrimaryEditingEntry.IsAppealable)
        {
            if (!string.IsNullOrEmpty(_discordInviteCode))
            {
                if (string.IsNullOrWhiteSpace(data.PrimaryEditingEntry.Message))
                {
                    data.PrimaryEditingEntry.Message = "Appeal at 'discord.gg/" + _discordInviteCode + "'.";
                }
                else if (data.PrimaryEditingEntry.Message!.IndexOf(".gg/" + _discordInviteCode, StringComparison.InvariantCultureIgnoreCase) == -1 &&
                         data.PrimaryEditingEntry.Message!.IndexOf("unappealable", StringComparison.InvariantCultureIgnoreCase) == -1 &&
                         data.PrimaryEditingEntry.Message!.IndexOf("un-appealable", StringComparison.InvariantCultureIgnoreCase) == -1 &&
                         data.PrimaryEditingEntry.Message!.IndexOf("no appeal", StringComparison.InvariantCultureIgnoreCase) == -1 &&
                         data.PrimaryEditingEntry.Message!.IndexOf("noappeal", StringComparison.InvariantCultureIgnoreCase) == -1 &&
                         data.PrimaryEditingEntry.Message!.IndexOf("no-appeal", StringComparison.InvariantCultureIgnoreCase) == -1 &&
                         data.PrimaryEditingEntry.Message!.IndexOf("dnub", StringComparison.InvariantCultureIgnoreCase) == -1 && // Do Not UnBan
                         data.PrimaryEditingEntry.Message!.IndexOf("do not appeal", StringComparison.InvariantCultureIgnoreCase) == -1)
                {
                    if (!data.PrimaryEditingEntry.Message.EndsWith(". "))
                    {
                        if (data.PrimaryEditingEntry.Message.EndsWith('.'))
                            data.PrimaryEditingEntry.Message += " Appeal at 'discord.gg/" + _discordInviteCode + "'.";
                    }
                    else
                    {
                        data.PrimaryEditingEntry.Message += ". Appeal at 'discord.gg/" + _discordInviteCode + "'.";
                    }
                }
            }

            if (data.SecondaryEditingEntry != null)
                data.SecondaryEditingEntry.Message = data.PrimaryEditingEntry.Message;
        }

        // add editor
        if (!Array.Exists(data.PrimaryEditingEntry.Actors, x => player.Equals(x.Actor.Id)))
        {
            RelatedActor[] actors = data.PrimaryEditingEntry.Actors;
            Array.Resize(ref actors, (actors?.Length ?? 0) + 1);
            actors[^1] = new RelatedActor(RelatedActor.RoleEditor, true, Actors.GetActor(player.Steam64));
            data.PrimaryEditingEntry.Actors = actors;
        }
        if (data.SecondaryEditingEntry != null && !Array.Exists(data.SecondaryEditingEntry.Actors, x => player.Equals(x.Actor.Id)))
        {
            RelatedActor[] actors = data.SecondaryEditingEntry.Actors;
            Array.Resize(ref actors, (actors?.Length ?? 0) + 1);
            actors[^1] = new RelatedActor(RelatedActor.RoleEditor, true, Actors.GetActor(player.Steam64));
            data.SecondaryEditingEntry.Actors = actors;
        }

        // add to related entries
        if (data is { SecondaryEditingEntry.Id: not 0u } &&
            Array.IndexOf(data.PrimaryEditingEntry.RelatedEntryKeys, data.SecondaryEditingEntry.Id) < 0)
        {
            uint[] relatedEntries = data.PrimaryEditingEntry.RelatedEntryKeys;
            Array.Resize(ref relatedEntries, (relatedEntries?.Length ?? 0) + 1);
            relatedEntries[^1] = data.SecondaryEditingEntry.Id;
            data.PrimaryEditingEntry.RelatedEntryKeys = relatedEntries;
            data.PrimaryEditingEntry.RelatedEntries = null;
        }
        if (data is { PrimaryEditingEntry.Id: not 0u, SecondaryEditingEntry: not null } &&
            Array.IndexOf(data.SecondaryEditingEntry.RelatedEntryKeys, data.PrimaryEditingEntry.Id) < 0)
        {
            uint[] relatedEntries = data.SecondaryEditingEntry.RelatedEntryKeys;
            Array.Resize(ref relatedEntries, (relatedEntries?.Length ?? 0) + 1);
            relatedEntries[^1] = data.PrimaryEditingEntry.Id;
            data.SecondaryEditingEntry.RelatedEntryKeys = relatedEntries;
            data.SecondaryEditingEntry.RelatedEntries = null;
        }

        if (isNew)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (data.PrimaryEditingEntry.StartedTimestamp == default)
                data.PrimaryEditingEntry.StartedTimestamp = now;
            data.PrimaryEditingEntry.ResolvedTimestamp = now;
            if (data.SecondaryEditingEntry != null)
            {
                if (data.SecondaryEditingEntry.StartedTimestamp == default)
                    data.SecondaryEditingEntry.StartedTimestamp = now;
                data.SecondaryEditingEntry.ResolvedTimestamp = now;
            }
        }
        for (int i = 0; i < ModerationActionControls.Length; ++i)
            ModerationActionControls[i].Hide(player);

        UniTask.Create(async () =>
        {
            ModerationEntry? select = null;
            ModerationEntry? m1 = data.PrimaryEditingEntry;
            ModerationEntry? m2 = data.SecondaryEditingEntry;

            double pendingRep = 0;
            ulong repPlayer = 0;

            if (data.PrimaryEditingEntry != null && data.PrimaryEditingEntry.PendingReputation != 0)
            {
                repPlayer = data.PrimaryEditingEntry.Player;
                pendingRep = data.PrimaryEditingEntry.PendingReputation;
                data.PrimaryEditingEntry.PendingReputation = 0;
            }
            if (data.SecondaryEditingEntry != null && data.SecondaryEditingEntry.PendingReputation != 0)
            {
                if (data.PrimaryEditingEntry != null && data.SecondaryEditingEntry.Player != data.PrimaryEditingEntry.Player)
                {
                    await _pointsStore.AddToReputationAsync(new CSteamID(data.SecondaryEditingEntry.Player), data.SecondaryEditingEntry.PendingReputation);
                }
                else
                {
                    pendingRep += data.SecondaryEditingEntry.PendingReputation;
                    repPlayer = data.SecondaryEditingEntry.Player;
                }

                data.SecondaryEditingEntry.PendingReputation = 0;
            }

            if (pendingRep != 0)
            {
                await _pointsStore.AddToReputationAsync(new CSteamID(repPlayer), pendingRep);
            }

            if (m1 != null)
            {
                m1 = await SaveEntry(m1, GetLogger(), this, player, CancellationToken.None);
            }
            if (m2 != null)
            {
                m2 = await SaveEntry(m2, GetLogger(), this, player, CancellationToken.None);
            }

            // add to related entries
            if (m1 != null && m2 != null)
            {
                bool d1 = false, d2 = false;
                if (Array.IndexOf(m1.RelatedEntryKeys, m2.Id) < 0)
                {
                    uint[] relatedEntries = m1.RelatedEntryKeys;
                    Array.Resize(ref relatedEntries, (relatedEntries?.Length ?? 0) + 1);
                    relatedEntries[^1] = m2.Id;
                    m1.RelatedEntryKeys = relatedEntries;
                    m1.RelatedEntries = null;
                    d1 = true;

                }
                if (Array.IndexOf(m2.RelatedEntryKeys, m1.Id) < 0)
                {
                    uint[] relatedEntries = m2.RelatedEntryKeys;
                    Array.Resize(ref relatedEntries, (relatedEntries?.Length ?? 0) + 1);
                    relatedEntries[^1] = m1.Id;
                    m2.RelatedEntryKeys = relatedEntries;
                    m2.RelatedEntries = null;
                    d2 = true;

                    await _moderationSql.AddOrUpdate(m2, CancellationToken.None);
                }

                if (d1 || d2)
                {
                    string q = $"INSERT INTO `{DatabaseInterface.TableRelatedEntries}` " +
                               $"({MySqlSnippets.ColumnList(DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnRelatedEntry)}) VALUES ";
                    if (d1)
                    {
                        q += "(" + m1.Id.ToString(CultureInfo.InvariantCulture) + "," + m2.Id.ToString(CultureInfo.InvariantCulture) + ")";
                    }

                    if (d2)
                    {
                        if (d1) q += ",";
                        q += "(" + m2.Id.ToString(CultureInfo.InvariantCulture) + "," + m1.Id.ToString(CultureInfo.InvariantCulture) + ")";
                    }

                    q += ";";
                    await _moderationSql.Sql.NonQueryAsync(q, null, CancellationToken.None).ConfigureAwait(false);
                }
            }

            CancellationToken token = player.DisconnectToken;
            await UniTask.SwitchToMainThread(token);

            if (!player.IsOnline)
                return;

            EndEditInActionMenu(player);
            SelectEntry(player, select);
            await RefreshModerationHistory(player, token);

            static async UniTask<ModerationEntry> SaveEntry(ModerationEntry entry, ILogger logger, ModerationUI ui, WarfarePlayer player, CancellationToken token)
            {
                ModerationEntry? current = entry.Id != 0u ? await ui._moderationSql.ReadOne<ModerationEntry>(entry.Id, false, token: token) : null;
                if (current == null)
                {
                    entry.Id = 0u;
                    current = entry;
                    if (current is AssetBan b)
                        logger.LogDebug("Entries: '{0}'.", b.VehicleTypeFilter);
                    // todo: ActionLog.Add(ActionLogType.CreateModerationEntry, $"Entry Id {entry.Id}. {entry.GetType().Name}, \"{entry.Message ?? "No Message"}\".", player.Steam64);
                }
                else
                {
                    StringBuilder changes = new StringBuilder();
                    if (!string.Equals(entry.Message, current.Message, StringComparison.Ordinal))
                    {
                        current.Message = entry.Message;
                        Append(changes, "msg", entry.Message);
                    }

                    for (int i = 0; i < entry.Actors.Length; ++i)
                    {
                        int j = Array.IndexOf(current.Actors, entry.Actors[i]);
                        if (j == -1)
                            Append(changes, "+actor", entry.Actors[i].ToString());
                    }
                    for (int i = 0; i < current.Actors.Length; ++i)
                    {
                        int j = Array.IndexOf(entry.Actors, current.Actors[i]);
                        if (j == -1)
                            Append(changes, "-actor", current.Actors[i].ToString());
                    }

                    current.Actors = entry.Actors;

                    for (int i = 0; i < entry.Evidence.Length; ++i)
                    {
                        int j = Array.IndexOf(current.Evidence, entry.Evidence[i]);
                        if (j == -1)
                            Append(changes, "+evidence", entry.Evidence[i].ToString());
                    }
                    for (int i = 0; i < current.Evidence.Length; ++i)
                    {
                        int j = Array.IndexOf(entry.Evidence, current.Evidence[i]);
                        if (j == -1)
                            Append(changes, "-evidence", current.Evidence[i].ToString());
                    }

                    current.Evidence = entry.Evidence;

                    if (entry.Reputation != current.Reputation)
                    {
                        current.PendingReputation += entry.Reputation - current.Reputation;
                        current.Reputation = entry.Reputation;
                        Append(changes, "rep", entry.Reputation.ToString("0.#", CultureInfo.InvariantCulture));
                    }

                    if (entry is IDurationModerationEntry entryDuration && current is IDurationModerationEntry currentDuration && entryDuration.Duration != currentDuration.Duration)
                    {
                        currentDuration.Duration = entryDuration.Duration;
                        Append(changes, "duration", FormattingUtility.ToTimeString(entryDuration.Duration));
                    }

                    if (entry is AssetBan entryAssetBan && current is AssetBan currentAssetBan)
                    {
                        for (int i = 0; i < entryAssetBan.VehicleTypeFilter.Length; ++i)
                        {
                            int j = Array.IndexOf(currentAssetBan.VehicleTypeFilter, entryAssetBan.VehicleTypeFilter[i]);
                            if (j == -1)
                                Append(changes, "+filter", entryAssetBan.VehicleTypeFilter[i].ToString());
                        }
                        for (int i = 0; i < currentAssetBan.VehicleTypeFilter.Length; ++i)
                        {
                            int j = Array.IndexOf(entryAssetBan.VehicleTypeFilter, currentAssetBan.VehicleTypeFilter[i]);
                            if (j == -1)
                                Append(changes, "-filter", currentAssetBan.VehicleTypeFilter[i].ToString());
                        }

                        logger.LogDebug("Current entries: '{0}'.", currentAssetBan.VehicleTypeFilter);
                        logger.LogDebug("Entry entries:   '{0}'.", entryAssetBan.VehicleTypeFilter);
                        currentAssetBan.VehicleTypeFilter = entryAssetBan.VehicleTypeFilter;
                    }

                    if (entry is Mute entryMute && current is Mute currentMute && entryMute.Type != currentMute.Type)
                    {
                        currentMute.Type = entryMute.Type;
                        Append(changes, "muteType", entryMute.Type.ToString());
                    }

                    bool anyChanges = changes.Length > 0;
                    if (!anyChanges)
                    {
                        logger.LogConditional("No changes were made.");
                        return current;
                    }

                    // todo: ActionLog.Add(ActionLogType.EditModerationEntry, $"Entry Id {entry.Id}. {entry.GetType().Name}. Changes: \"{changes}\"", player.Steam64);
                }

                await ui._moderationSql.AddOrUpdate(current, token).ConfigureAwait(false);
                return current;

                static void Append(StringBuilder sb, string key, string? val)
                {
                    if (sb.Length != 0)
                        sb.Append(',');
                    sb.Append(key).Append('=').Append(val ?? "null");
                }
            }
        });
    }
    private void OnActionControlClicked(UnturnedButton button, Player player)
    {
        int control = Array.FindIndex(ModerationActionControls, x => x.Button == button);
        if (control == -1)
            return;

        WarfarePlayer ucp = _playerService.GetOnlinePlayer(player);

        ModerationData data = GetOrAddModerationData(ucp);

        switch (control)
        {
            case 0:
                OnClickedCancel(ucp);
                break;
            case 1:
                OnClickedAddOrSave(data, ucp);
                break;
            case 2:
                OnClickedRemove(data, ucp);
                break;
            case 3:
                OnClickedForgive(data, ucp);
                break;
        }
    }
    private void OnClickedCancel(WarfarePlayer player)
    {
        EndEditInActionMenu(player);
    }
    private void OnClickedAddOrSave(ModerationData data, WarfarePlayer player)
    {
        Save(data, player);
    }
    private void OnClickedRemove(ModerationData data, WarfarePlayer player)
    {
        if (data.PrimaryEditingEntry == null)
        {
            GetLogger().LogWarning("Tried to remove an in-progress entry.");
            return;
        }

        ModerationEntry? m1 = data.PrimaryEditingEntry;
        ModerationEntry? m2 = data.SecondaryEditingEntry;
        if (m1 is { Removed: true })
            m1 = null;
        if (m2 is { Removed: true })
            m2 = null;
        if (m1 == null && m2 == null)
        {
            GetLogger().LogWarning("Tried to remove unremoveable entries.");
            return;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        string? msg = data.PrimaryEditingEntry!.Message;
        IModerationActor actor = Actors.GetActor(player.Steam64);

        for (int i = 0; i < ModerationActionControls.Length; ++i)
            ModerationActionControls[i].Hide(player);

        UniTask.Create(async () =>
        {
            ModerationEntry? select = null;
            if (m1 is { Id: not 0u })
            {
                ModerationEntry? entry = await _moderationSql.ReadOne<ModerationEntry>(m1.Id, false, token: CancellationToken.None);
                if (entry != null)
                {
                    entry.RemovedMessage = string.Equals(entry.Message, msg, StringComparison.Ordinal) ? null : msg;
                    entry.RemovedTimestamp = now;
                    entry.RemovedBy = actor;
                    entry.Removed = true;
                    entry.PendingReputation -= entry.Reputation;

                    // todo: ActionLog.Add(ActionLogType.RemoveModerationEntry, $"Entry #{entry.Id} ({entry.GetType().Name}) - " + msg, player.Steam64);
                    await _moderationSql.AddOrUpdate(entry, CancellationToken.None).ConfigureAwait(false);
                    select = entry;
                }
            }
            if (m2 is { Id: not 0u })
            {
                ModerationEntry? entry = await _moderationSql.ReadOne<ModerationEntry>(m2.Id, false, token: CancellationToken.None);
                if (entry != null)
                {
                    entry.RemovedMessage = string.Equals(entry.Message, msg, StringComparison.Ordinal) ? null : msg;
                    entry.RemovedTimestamp = now;
                    entry.RemovedBy = actor;
                    entry.Removed = true;
                    entry.PendingReputation -= entry.Reputation;

                    // todo: ActionLog.Add(ActionLogType.RemoveModerationEntry, $"Entry #{entry.Id} ({entry.GetType().Name}) - " + msg, player.Steam64);
                    await _moderationSql.AddOrUpdate(entry, CancellationToken.None).ConfigureAwait(false);
                    select ??= entry;
                }
            }

            await UniTask.SwitchToMainThread(player.DisconnectToken);
            EndEditInActionMenu(player);
            SelectEntry(player, select);
            await RefreshModerationHistory(player, player.DisconnectToken);
        });
    }
    private void OnClickedForgive(ModerationData data, WarfarePlayer player)
    {
        IForgiveableModerationEntry? f1 = data.PrimaryEditingEntry as IForgiveableModerationEntry;
        IForgiveableModerationEntry? f2 = data.SecondaryEditingEntry as IForgiveableModerationEntry;
        if (f1 != null && !f1.IsApplied(true))
            f1 = null;
        if (f2 != null && !f2.IsApplied(true))
            f2 = null;
        if (f1 == null && f2 == null)
        {
            GetLogger().LogWarning("Tried to forgive unforgivable entries.");
            return;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        string? msg = data.PrimaryEditingEntry!.Message;
        IModerationActor actor = Actors.GetActor(player.Steam64);

        for (int i = 0; i < ModerationActionControls.Length; ++i)
            ModerationActionControls[i].Hide(player);

        UniTask.Create(async () =>
        {
            bool sel = false;
            if (f1 is { Id: not 0u })
            {
                IForgiveableModerationEntry? entry = await _moderationSql.ReadOne<IForgiveableModerationEntry>(f1.Id, false, token: CancellationToken.None);
                if (entry != null)
                {
                    entry.ForgiveMessage = string.Equals(entry.Message, msg, StringComparison.Ordinal) ? null : msg;
                    entry.ForgiveTimestamp = now;
                    entry.ForgivenBy = actor;
                    entry.Forgiven = true;

                    // todo: ActionLog.Add(ActionLogType.ForgiveModerationEntry, $"Entry #{entry.Id} ({entry.GetType().Name}) - " + msg, player.Steam64);
                    await _moderationSql.AddOrUpdate(entry, CancellationToken.None);
                    data.PrimaryEditingEntry = entry as ModerationEntry;
                    if (data.PendingPreset != PresetType.None && entry is Punishment p)
                        data.PendingPreset = p.PresetType;
                    else
                        data.PendingType = ModerationReflection.GetType(entry.GetType()) ?? ModerationEntryType.None;
                    sel = true;
                }
            }

            if (f2 is { Id: not 0u })
            {
                IForgiveableModerationEntry? entry = await _moderationSql.ReadOne<IForgiveableModerationEntry>(f2.Id, false, token: CancellationToken.None);
                if (entry != null)
                {
                    entry.ForgiveMessage = string.Equals(entry.Message, msg, StringComparison.Ordinal) ? null : msg;
                    entry.ForgiveTimestamp = now;
                    entry.ForgivenBy = actor;
                    entry.Forgiven = true;

                    // todo: ActionLog.Add(ActionLogType.ForgiveModerationEntry, $"Entry #{entry.Id} ({entry.GetType().Name}) - " + msg, player.Steam64);
                    await _moderationSql.AddOrUpdate(entry, CancellationToken.None);
                    data.SecondaryEditingEntry = entry as ModerationEntry;
                    if (!sel)
                    {
                        if (data.PendingPreset != PresetType.None && entry is Punishment p)
                            data.PendingPreset = p.PresetType;
                        else
                            data.PendingType = ModerationReflection.GetType(entry.GetType()) ?? ModerationEntryType.None;
                        sel = true;
                    }
                }
            }

            if (sel)
            {
                await UniTask.SwitchToMainThread(player.DisconnectToken);
                SelectEntry(player, data.PrimaryEditingEntry);
                LoadActionMenu(player, true);
                await RefreshModerationHistory(player, player.DisconnectToken);
            }
        });
    }
    private void OnClickedAddActor(UnturnedButton button, Player player)
    {
        WarfarePlayer ucp = _playerService.GetOnlinePlayer(player);
        ModerationData data = GetOrAddModerationData(ucp);
        int ct = data.Actors.Count;
        if (ct >= ModerationActionActors.Length)
        {
            GetLogger().LogWarning("Too many actors.");
            ModerationActionAddActorButton.Disable(player);
            return;
        }
        
        data.Actors.Add(new RelatedActor(string.Empty, false, ConsoleActor.Instance));
        ModerationSelectedActor actorUi = ModerationActionActors[ct];
        actorUi.AsAdminToggleState.SetVisibility(player, false);
        actorUi.Steam64Input.SetText(player, string.Empty);
        actorUi.ProfilePicture.SetImage(player, string.Empty);
        actorUi.YouButton.SetVisibility(player, true);
        actorUi.Name.SetText(player, string.Empty);
        actorUi.RoleInput.SetText(player, string.Empty);
        actorUi.Root.SetVisibility(player, true);

        ModerationActionAddEvidenceButton.SetVisibility(player, ct + 1 != ModerationActionEvidence.Length);

        TimeUtility.InvokeAfterDelay(() => LogicModerationActionsUpdateScrollVisual.Show(player), 0.125f);
    }
    private void OnClickedRemoveActor(UnturnedButton button, Player player)
    {
        WarfarePlayer ucp = _playerService.GetOnlinePlayer(player);
        ModerationData data = GetOrAddModerationData(ucp);
        int index = Array.FindIndex(ModerationActionActors, x => x.RemoveButton == button);
        if (index < 0)
            return;
        ModerationSelectedActor actorUi = ModerationActionActors[index];
        if (index >= data.Actors.Count)
        {
            actorUi.Root.Hide(player);
            GetLogger().LogWarning("Actor not supposed to be added.");
            return;
        }

        data.Actors.RemoveAt(index);

        if (data.Actors.Count < ModerationActionActors.Length)
            ModerationActionActors[data.Actors.Count].Root.Hide(player);

        SendActorsAndEvidence(ucp, index, lenEvidence: 0);

        TimeUtility.InvokeAfterDelay(() => LogicModerationActionsUpdateScrollVisual.Show(player), 0.125f);
    }
    private void OnClickedAddEvidence(UnturnedButton button, Player player)
    {
        WarfarePlayer ucp = _playerService.GetOnlinePlayer(player);
        ModerationData data = GetOrAddModerationData(ucp);
        int ct = data.Evidence.Count;
        if (ct >= ModerationActionEvidence.Length)
        {
            GetLogger().LogWarning("Too many evidence entries.");
            ModerationActionAddEvidenceButton.Disable(player);
            return;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        data.Evidence.Add(new Evidence(string.Empty, string.Empty, string.Empty, false, ConsoleActor.Instance, now));
        ModerationSelectedEvidence evidenceUi = ModerationActionEvidence[ct];
        evidenceUi.ActorName.SetText(player, player.channel.owner.playerID.playerName);
        evidenceUi.Steam64Input.SetText(player, player.channel.owner.playerID.steamID.m_SteamID.ToString("D17", CultureInfo.InvariantCulture));
        evidenceUi.LinkInput.SetText(player, string.Empty);
        evidenceUi.MessageInput.SetText(player, string.Empty);
        evidenceUi.NoPreviewName.SetVisibility(player, true);
        evidenceUi.NoPreviewName.SetText(player, string.Empty);
        evidenceUi.PreviewName.SetVisibility(player, false);
        evidenceUi.PreviewName.SetText(player, string.Empty);
        evidenceUi.PreviewRoot.SetVisibility(player, false);
        evidenceUi.TimestampInput.SetText(player, now.ToString(DateTimeFormatInput, ucp.Locale.ParseFormat));
        evidenceUi.Root.SetVisibility(player, true);

        ModerationActionAddEvidenceButton.SetVisibility(player, ct + 1 != ModerationActionEvidence.Length);

        TimeUtility.InvokeAfterDelay(() => LogicModerationActionsUpdateScrollVisual.Show(player), 0.125f);
    }
    private void OnClickedRemoveEvidence(UnturnedButton button, Player player)
    {
        WarfarePlayer ucp = _playerService.GetOnlinePlayer(player);
        ModerationData data = GetOrAddModerationData(ucp);
        int index = Array.FindIndex(ModerationActionEvidence, x => x.RemoveButton == button);
        if (index < 0)
            return;
        ModerationSelectedEvidence evidenceUi = ModerationActionEvidence[index];
        if (index >= data.Evidence.Count)
        {
            evidenceUi.Root.Hide(player);
            GetLogger().LogWarning("Evidence not supposed to be added.");
            return;
        }

        data.Evidence.RemoveAt(index);

        if (data.Evidence.Count < ModerationActionEvidence.Length)
            ModerationActionEvidence[data.Evidence.Count].Root.Hide(player);

        SendActorsAndEvidence(ucp, startIndEvidence: index, lenActor: 0);

        TimeUtility.InvokeAfterDelay(() => LogicModerationActionsUpdateScrollVisual.Show(player), 0.125f);
    }
    private void OnClickedModerateButton(Player player, ModerationEntryType type)
    {
        WarfarePlayer ucp = _playerService.GetOnlinePlayer(player);
        Type? sysType = ModerationReflection.GetType(type);
        if (sysType == null || !typeof(ModerationEntry).IsAssignableFrom(sysType))
        {
            GetLogger().LogWarning("Unknown moderation type: {0}.", type);
            return;
        }

        GetLogger().LogDebug("Pressed action: {0}.", type);

        ModerationData data = GetOrAddModerationData(ucp);
        bool isDeselecting = new CSteamID(data.SelectedPlayer).GetEAccountType() != EAccountType.k_EAccountTypeIndividual;

        if (data.PendingPreset != PresetType.None)
        {
            if ((int)data.PendingPreset - 1 < Presets.Length)
                Presets[(int)data.PendingPreset - 1].Enable(ucp.Connection);

            data.PendingPreset = PresetType.None;
            data.PendingPresetValue = null;
        }

        if (data.PendingType != ModerationEntryType.None && data.PendingType != type)
        {
            LabeledStateButton? btn = GetModerationButton(data.PendingType);
            btn?.Enable(ucp.Connection);
        }

        if (!isDeselecting)
        {
            data.PendingType = type;

            LabeledStateButton? btn = GetModerationButton(type);
            btn?.Disable(ucp.Connection);
            data.PrimaryEditingEntry = null;
            data.SecondaryEditingEntry = null;
            LoadActionMenu(ucp, false);
        }
        else
        {
            ModerationFormRoot.SetVisibility(ucp.Connection, false);
        }
    }
    private void OnClickedPreset(UnturnedButton button, Player player)
    {
        int index = Array.FindIndex(Presets, x => x.Button == button);
        if (index == -1 || PunishmentPresets.Presets.Count <= index)
        {
            GetLogger().LogWarning("Unknown preset type: {0}.", button.Name);
            return;
        }

        WarfarePlayer ucp = _playerService.GetOnlinePlayer(player);
        PresetType preset = (PresetType)(index + 1);
        if (!PunishmentPresets.TryGetPreset(preset, out _))
        {
            GetLogger().LogWarning("Preset not found: {0}.", preset);
            return;
        }

        GetLogger().LogDebug("Pressed preset: {0}.", preset);

        ModerationData data = GetOrAddModerationData(ucp);
        if (data.PendingPreset != PresetType.None && data.PendingPreset != preset)
        {
            if ((int)data.PendingPreset - 1 < Presets.Length)
                Presets[(int)data.PendingPreset - 1].Enable(ucp.Connection);
        }

        bool isDeselecting = new CSteamID(data.SelectedPlayer).GetEAccountType() != EAccountType.k_EAccountTypeIndividual;

        if (!isDeselecting)
        {
            data.PendingPreset = preset;
            data.PendingPresetValue = null;
            Presets[index].Disable(ucp.Connection);

            if (data.PendingType != ModerationEntryType.None)
            {
                LabeledStateButton? btn = GetModerationButton(data.PendingType);
                btn?.Enable(ucp.Connection);

                data.PendingType = ModerationEntryType.None;
            }

            data.PrimaryEditingEntry = null;
            data.SecondaryEditingEntry = null;

            LoadActionMenu(ucp, false);
        }
        else
        {
            ModerationFormRoot.SetVisibility(ucp.Connection, false);
        }
    }
    private void OnMessageUpdated(UnturnedTextBox textbox, Player player, string text)
    {
        WarfarePlayer ucp = _playerService.GetOnlinePlayer(player);
        ModerationData data = GetOrAddModerationData(ucp);

        if (data.PrimaryEditingEntry != null)
            data.PrimaryEditingEntry.Message = text;
        if (data.SecondaryEditingEntry != null)
            data.SecondaryEditingEntry.Message = text;

        GetLogger().LogDebug("Message updated: {0}.", text);
    }
    private void OnMuteTypeUpdated(UnturnedEnumButton<MuteType> button, Player player, MuteType value)
    {
        WarfarePlayer ucp = _playerService.GetOnlinePlayer(player);
        ModerationData data = GetOrAddModerationData(ucp);

        if (value is > MuteType.Both or < MuteType.Voice)
            return;

        if (data.PrimaryEditingEntry is Mute mute)
            mute.Type = value;
        if (data.SecondaryEditingEntry is Mute mute2)
            mute2.Type = value;

        GetLogger().LogDebug("Mute type updated: {0}.", value);
    }
    private void OnVehicleListUpdated(UnturnedTextBox textbox, Player player, string text)
    {
        WarfarePlayer ucp = _playerService.GetOnlinePlayer(player);
        ModerationData data = GetOrAddModerationData(ucp);

        AssetBan? ban = data.PrimaryEditingEntry as AssetBan ?? data.SecondaryEditingEntry as AssetBan;
        if (ban == null)
            return;

        ban.FillFromText(text);
        string commaList = ban.GetCommaList(true);
        textbox.SetText(ucp.Connection, commaList);

        GetLogger().LogDebug("Vehicle filter updated: {0} ({1}).", commaList, ban.VehicleTypeFilter);
    }
    private void OnDurationUpdated(UnturnedTextBox textbox, Player player, string text)
    {
        WarfarePlayer ucp = _playerService.GetOnlinePlayer(player);
        ModerationData data = GetOrAddModerationData(ucp);

        TimeSpan duration = FormattingUtility.ParseTimespan(text);
        bool primary = textbox == ModerationActionMiniInputBox1.TextBox;
        string timeString = duration.Ticks < 0L ? "Permanent" : FormattingUtility.ToTimeString(duration);
        textbox.SetText(ucp.Connection, timeString);
        if (primary && data.PrimaryEditingEntry is IDurationModerationEntry durEntry)
            durEntry.Duration = duration;
        else if (!primary && data.SecondaryEditingEntry is IDurationModerationEntry durEntry2)
            durEntry2.Duration = duration;

        GetLogger().LogDebug("Duration updated: {0} (primary: {1}).", timeString, primary);
    }
    private void OnReputationUpdated(UnturnedTextBox textbox, Player player, string text)
    {
        WarfarePlayer ucp = _playerService.GetOnlinePlayer(player);
        ModerationData data = GetOrAddModerationData(ucp);

        if (string.IsNullOrEmpty(text)
            || text.Equals("0", StringComparison.InvariantCultureIgnoreCase)
            || !double.TryParse(text, NumberStyles.Number, ucp.Locale.ParseFormat, out double rep))
        {
            textbox.SetText(ucp.Connection, string.Empty);
            return;
        }

        rep = Math.Round(rep, 1, MidpointRounding.AwayFromZero);

        textbox.SetText(ucp.Connection, rep == 0 ? string.Empty : rep.ToString("0.#", ucp.Locale.ParseFormat));

        if (data.PrimaryEditingEntry != null)
            data.PrimaryEditingEntry.Reputation = rep;
        if (data.SecondaryEditingEntry != null)
            data.SecondaryEditingEntry.Reputation = rep;

        GetLogger().LogDebug("Reputation updated: {0}.", rep.ToString("0.#", CultureInfo.InvariantCulture));
    }

    private bool GetEvidenceDetails(Player player, Predicate<ModerationSelectedEvidence> selector, out int index, out WarfarePlayer ucp, out ModerationData data)
    {
        ucp = _playerService.GetOnlinePlayer(player);
        index = Array.FindIndex(ModerationActionEvidence, selector);
        if (index == -1 || ModerationActionEvidence.Length <= index)
        {
            GetLogger().LogWarning("Unknown evidence.");
            data = null!;
            return false;
        }

        if (ucp is not null)
        {
            data = GetOrAddModerationData(ucp);
            if (data.Evidence.Count > index)
            {
                return true;
            }

            GetLogger().LogWarning("Evidence out of bounds.");
            return false;
        }

        data = null!;
        return false;
    }
    private bool GetActorDetails(Player player, Predicate<ModerationSelectedActor> selector, out int index, out WarfarePlayer ucp, out ModerationData data)
    {
        ucp = _playerService.GetOnlinePlayer(player);
        index = Array.FindIndex(ModerationActionActors, selector);
        if (index == -1 || ModerationActionActors.Length <= index)
        {
            GetLogger().LogWarning("Unknown actor.");
            data = null!;
            return false;
        }
        if (ucp is not null)
        {
            data = GetOrAddModerationData(ucp);
            if (data.Actors.Count > index)
            {
                return true;
            }

            GetLogger().LogWarning("Actor out of bounds.");
            return false;

        }

        data = null!;
        return false;
    }
    private void OnClickedActorAdminToggle(UnturnedButton button, Player player)
    {
        if (!GetActorDetails(player, x => x.AsAdminToggleButton == button, out int index, out _, out ModerationData data))
            return;

        RelatedActor actor = data.Actors[index];
        data.Actors[index] = new RelatedActor(actor.Role, !actor.Admin, actor.Actor);
        ModerationActionActors[index].AsAdminToggleState.SetVisibility(player, actor.Admin);
    }
    private void OnClickedActorYouButton(UnturnedButton button, Player player)
    {
        if (!GetActorDetails(player, x => x.YouButton == button, out int index, out WarfarePlayer ucPlayer, out ModerationData data))
            return;

        RelatedActor actor = data.Actors[index];
        data.Actors[index] = new RelatedActor(actor.Role, !actor.Admin, Actors.GetActor(ucPlayer.Steam64));
        ModerationActionActors[index].Steam64Input.SetText(player, ucPlayer.Steam64.m_SteamID.ToString("D17", CultureInfo.InvariantCulture));
        ModerationActionActors[index].Name.SetText(player, ucPlayer.Names.GetDisplayNameOrPlayerName());
        ModerationActionActors[index].ProfilePicture.SetImage(player, ucPlayer.SteamSummary.AvatarUrlMedium);
    }
    private void OnTypedActorRole(UnturnedTextBox textbox, Player player, string text)
    {
        if (!GetActorDetails(player, x => x.RoleInput == textbox, out int index, out _, out ModerationData data))
            return;

        RelatedActor actor = data.Actors[index];
        data.Actors[index] = new RelatedActor(text, !actor.Admin, actor.Actor);
    }
    private void OnTypedActorSteam64(UnturnedTextBox textbox, Player player, string text)
    {
        if (!GetActorDetails(player, x => x.Steam64Input == textbox, out int index, out WarfarePlayer ucPlayer, out ModerationData data))
            return;

        RelatedActor actor = data.Actors[index];
        if (!SteamIdHelper.TryParseSteamId(text, out CSteamID steamId))
        {
            textbox.SetText(player, actor.Actor is { Id: not 0 } ? actor.Actor.Id.ToString("D17", CultureInfo.InvariantCulture) : string.Empty);
            return;
        }

        IModerationActor moderationActor = Actors.GetActor(steamId.m_SteamID);
        data.Actors[index] = new RelatedActor(actor.Role, !actor.Admin, moderationActor);
        ModerationActionActors[index].Steam64Input.SetText(player, steamId.m_SteamID.ToString("D17", CultureInfo.InvariantCulture));
        if (_moderationSql.TryGetUsernames(moderationActor, out PlayerNames un))
            ModerationActionActors[index].Name.SetText(player, un.GetDisplayNameOrPlayerName());
        else
        {
            UniTask.Create(async () =>
            {
                string? username = await moderationActor.GetDisplayName(_moderationSql, ucPlayer.DisconnectToken);
                ModerationActionActors[index].Name.SetText(player, username ?? steamId.m_SteamID.ToString("D17", CultureInfo.InvariantCulture));
            });
        }
        if (_moderationSql.TryGetAvatar(moderationActor, AvatarSize.Medium, out string avatar))
            ModerationActionActors[index].ProfilePicture.SetImage(player, avatar);
        else
        {
            UniTask.Create(async () =>
            {
                string? pfp = await moderationActor.GetProfilePictureURL(_moderationSql, AvatarSize.Medium, ucPlayer.DisconnectToken);
                ModerationActionActors[index].ProfilePicture.SetImage(player, pfp ?? string.Empty);
            });
        }
    }
    private void OnTypedEvidenceLink(UnturnedTextBox textbox, Player player, string text)
    {
        // remove webp format specifier since unity doesn't support it
        text = text.Replace("format=webp&", string.Empty).Replace("?format=webp", string.Empty).Replace("&format=webp", string.Empty);

        if (!GetEvidenceDetails(player, x => x.LinkInput == textbox, out int index, out _, out ModerationData data))
            return;

        Evidence evidence = data.Evidence[index];
        data.Evidence[index] = evidence = new Evidence(text, evidence.URL, null, false, evidence.Actor, evidence.Timestamp);
        UniTask.Create(async () =>
        {
            UnityWebRequest req = new UnityWebRequest(evidence.URL, "HEAD", null, null);
            GetLogger().LogConditional("HEAD -> {0}.", evidence.URL);
            ModerationSelectedEvidence ui = ModerationActionEvidence[index];
            try
            {
                await req.SendWebRequest();
                GetLogger().LogConditional("  Done");
                if (!data.Evidence[index].URL.Equals(evidence.URL, StringComparison.Ordinal))
                    return;

                string name;
                if (Uri.TryCreate(evidence.URL, UriKind.Absolute, out Uri uri) && uri.Segments.Length > 0)
                {
                    name = uri.Segments[^1];
                }
                else
                {
                    name = evidence.URL;
                }

                if (evidence.URL.Length > 1)
                {
                    int lastSlash = evidence.URL.LastIndexOf('/');
                    if (lastSlash == evidence.URL.Length - 1)
                        lastSlash = evidence.URL.LastIndexOf('/', lastSlash - 1);

                    name = lastSlash < 0 ? evidence.URL : evidence.URL.Substring(lastSlash + 1);
                }

                ui.LinkInput.SetText(player, req.uri.ToString());
                GetLogger().LogConditional("  Responded: {0}", req.redirectLimit);
                if (req.GetResponseHeader("Content-Type") is { } contentType && contentType.StartsWith("image/"))
                {
                    GetLogger().LogConditional("  Image");
                    data.Evidence[index] = new Evidence(text, evidence.URL, data.Evidence[index].SavedLocation, true, evidence.Actor, evidence.Timestamp);
                    ui.PreviewName.SetVisibility(player, true);
                    ui.NoPreviewName.SetVisibility(player, false);
                    ui.PreviewName.SetText(player, name);
                    ui.PreviewRoot.SetVisibility(player, true);
                    ui.PreviewImage.SetImage(player, evidence.URL);
                }
                else
                {
                    GetLogger().LogConditional("  Not image");
                    ui.NoPreviewName.SetVisibility(player, true);
                    ui.PreviewName.SetVisibility(player, false);
                    ui.NoPreviewName.SetText(player, name);
                    ui.PreviewRoot.SetVisibility(player, false);
                }
            }
            catch (UnityWebRequestException ex)
            {
                if (!data.Evidence[index].URL.Equals(evidence.URL, StringComparison.Ordinal))
                    return;
                ui.NoPreviewName.SetVisibility(player, true);
                ui.PreviewName.SetVisibility(player, false);
                ui.NoPreviewName.SetText(player, "File not found (" + ex.ResponseCode + ")");
                ui.PreviewRoot.SetVisibility(player, false);
            }
        });
    }
    private void OnTypedEvidenceMessage(UnturnedTextBox textbox, Player player, string text)
    {
        if (!GetEvidenceDetails(player, x => x.MessageInput == textbox, out int index, out _, out ModerationData data))
            return;

        Evidence evidence = data.Evidence[index];
        data.Evidence[index] = new Evidence(evidence.URL, text, evidence.SavedLocation, evidence.Image, evidence.Actor, evidence.Timestamp);
    }
    private void OnTypedEvidenceTimestamp(UnturnedTextBox textbox, Player player, string text)
    {
        if (!GetEvidenceDetails(player, x => x.TimestampInput == textbox, out int index, out WarfarePlayer ucPlayer, out ModerationData data))
            return;

        Evidence evidence = data.Evidence[index];
        if (!DateTimeOffset.TryParseExact(text, DateTimeFormatInput, ucPlayer.Locale.ParseFormat, DateTimeStyles.AssumeUniversal, out DateTimeOffset dateTimeOffset) &&
            !DateTimeOffset.TryParse(text, ucPlayer.Locale.ParseFormat, DateTimeStyles.AssumeUniversal, out dateTimeOffset))
        {
            textbox.SetText(player, evidence.Timestamp.ToString(DateTimeFormatInput, ucPlayer.Locale.ParseFormat));
            return;
        }
        
        data.Evidence[index] = new Evidence(evidence.URL, evidence.Message, evidence.SavedLocation, evidence.Image, evidence.Actor, dateTimeOffset);
        ModerationActionEvidence[index].TimestampInput.SetText(player, dateTimeOffset.ToString(DateTimeFormatInput, ucPlayer.Locale.ParseFormat));
    }
    private void OnTypedEvidenceSteam64(UnturnedTextBox textbox, Player player, string text)
    {
        if (!GetEvidenceDetails(player, x => x.Steam64Input == textbox, out int index, out WarfarePlayer ucPlayer, out ModerationData data))
            return;

        Evidence evidence = data.Evidence[index];
        if (!SteamIdHelper.TryParseSteamId(text, out CSteamID steamId))
        {
            textbox.SetText(player, evidence.Actor is { Id: not 0 } ? evidence.Actor.Id.ToString("D17", CultureInfo.InvariantCulture) : string.Empty);
            return;
        }

        IModerationActor moderationActor = Actors.GetActor(steamId.m_SteamID);
        data.Evidence[index] = new Evidence(evidence.URL, evidence.Message, evidence.SavedLocation, evidence.Image, moderationActor, evidence.Timestamp);
        ModerationActionEvidence[index].Steam64Input.SetText(player, steamId.m_SteamID.ToString("D17", CultureInfo.InvariantCulture));
        if (_moderationSql.TryGetUsernames(moderationActor, out PlayerNames un))
            ModerationActionEvidence[index].ActorName.SetText(player, un.GetDisplayNameOrPlayerName());
        else
        {
            UniTask.Create(async () =>
            {
                string? username = await moderationActor.GetDisplayName(_moderationSql, ucPlayer.DisconnectToken);
                ModerationActionEvidence[index].ActorName.SetText(player, username ?? steamId.m_SteamID.ToString("D17", CultureInfo.InvariantCulture));
            });
        }
    }
    private void OnClickedEvidenceNowButton(UnturnedButton button, Player player)
    {
        if (!GetEvidenceDetails(player, x => x.NowButton == button, out int index, out WarfarePlayer ucPlayer, out ModerationData data))
            return;

        Evidence evidence = data.Evidence[index];
        DateTimeOffset now = DateTimeOffset.UtcNow;
        data.Evidence[index] = new Evidence(evidence.URL, evidence.Message, evidence.SavedLocation, evidence.Image, evidence.Actor, now);
        ModerationActionEvidence[index].TimestampInput.SetText(player, now.ToString(DateTimeFormatInput, ucPlayer.Locale.ParseFormat));
    }
    private void OnClickedEvidenceYouButton(UnturnedButton button, Player player)
    {
        if (!GetEvidenceDetails(player, x => x.YouButton == button, out int index, out WarfarePlayer ucPlayer, out ModerationData data))
            return;

        Evidence evidence = data.Evidence[index];
        data.Evidence[index] = new Evidence(evidence.URL, evidence.Message, evidence.SavedLocation, evidence.Image, Actors.GetActor(ucPlayer.Steam64), evidence.Timestamp);
        ModerationActionEvidence[index].Steam64Input.SetText(player, ucPlayer.Steam64.m_SteamID.ToString("D17", CultureInfo.InvariantCulture));
        ModerationActionEvidence[index].ActorName.SetText(player, ucPlayer.Names.GetDisplayNameOrPlayerName());
    }
}
