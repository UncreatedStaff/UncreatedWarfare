using Cysharp.Threading.Tasks;
using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Presets;
using Uncreated.SQL;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Moderation.Punishments;
using Uncreated.Warfare.Moderation.Punishments.Presets;
using Uncreated.Warfare.Moderation.Records;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Moderation;
internal partial class ModerationUI
{
    private void OnMessageUpdated(UnturnedTextBox textbox, Player player, string text)
    {
        if (UCPlayer.FromPlayer(player) is not { } ucPlayer)
            return;
        ModerationData data = GetOrAddModerationData(ucPlayer);

        if (data.PrimaryEditingEntry != null)
            data.PrimaryEditingEntry.Message = text;
        if (data.SecondaryEditingEntry != null)
            data.SecondaryEditingEntry.Message = text;

        L.LogDebug($"Message updated: {text}.");
    }
    private void OnMuteTypeUpdated(UnturnedEnumButtonTracker<MuteType> button, Player player, MuteType value)
    {
        if (UCPlayer.FromPlayer(player) is not { } ucPlayer)
            return;
        ModerationData data = GetOrAddModerationData(ucPlayer);

        if (value is > MuteType.Both or < MuteType.Voice)
            return;

        if (data.PrimaryEditingEntry is Mute mute)
            mute.Type = value;
        if (data.SecondaryEditingEntry is Mute mute2)
            mute2.Type = value;

        L.LogDebug($"Mute type updated: {value}.");
    }
    private void OnVehicleListUpdated(UnturnedTextBox textbox, Player player, string text)
    {
        if (UCPlayer.FromPlayer(player) is not { } ucPlayer)
            return;
        ModerationData data = GetOrAddModerationData(ucPlayer);
        
        AssetBan? ban = data.PrimaryEditingEntry as AssetBan ?? data.SecondaryEditingEntry as AssetBan;
        if (ban == null)
            return;

        ban.FillFromText(text);
        string commaList = ban.GetCommaList(true);
        textbox.SetText(ucPlayer.Connection, commaList);

        L.LogDebug($"Vehicle filter updated: {commaList}.");
    }
    private void OnDurationUpdated(UnturnedTextBox textbox, Player player, string text)
    {
        if (UCPlayer.FromPlayer(player) is not { } ucPlayer)
            return;
        ModerationData data = GetOrAddModerationData(ucPlayer);

        TimeSpan duration = Util.ParseTimespan(text);

        string timeString = Util.ToTimeString((int)Math.Round(duration.TotalSeconds));
        textbox.SetText(ucPlayer.Connection, timeString);
        if (data.PrimaryEditingEntry is IDurationModerationEntry durEntry)
            durEntry.Duration = duration;
        if (data.SecondaryEditingEntry is IDurationModerationEntry durEntry2)
            durEntry2.Duration = duration;

        L.LogDebug($"Duration updated: {timeString}.");
    }
    private void OnReputationUpdated(UnturnedTextBox textbox, Player player, string text)
    {
        if (UCPlayer.FromPlayer(player) is not { } ucPlayer)
            return;
        ModerationData data = GetOrAddModerationData(ucPlayer);

        if (string.IsNullOrEmpty(text)
            || text.Equals("0", StringComparison.InvariantCultureIgnoreCase)
            || !double.TryParse(text, NumberStyles.Number, ucPlayer.Locale.ParseFormat, out double rep))
        {
            textbox.SetText(ucPlayer.Connection, "0");
            return;
        }

        rep = Math.Round(rep, 1, MidpointRounding.AwayFromZero);

        textbox.SetText(ucPlayer.Connection, rep.ToString("0.#", ucPlayer.Locale.ParseFormat));

        if (data.PrimaryEditingEntry != null)
            data.PrimaryEditingEntry.Reputation = rep;
        if (data.SecondaryEditingEntry != null)
            data.SecondaryEditingEntry.Reputation = rep;

        L.LogDebug($"Reputation updated: {rep.ToString("0.#", CultureInfo.InvariantCulture)}.");
    }
    private void EditInActionMenu(UCPlayer player, bool editingExisting)
    {
        ModerationData data = GetOrAddModerationData(player);

        ITransportConnection c = player.Connection;
        if (!Util.IsValidSteam64Id(data.SelectedPlayer) || !editingExisting && data is { PendingPreset: PresetType.None, PendingType: ModerationEntryType.None })
        {
            ModerationFormRoot.SetVisibility(c, false);
            return;
        }

        if (editingExisting)
        {
            if (data.PrimaryEditingEntry == null)
            {
                L.LogWarning("Missing moderation entry.");
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

            if (data.PendingType == ModerationEntryType.None && data.PendingPreset == PresetType.None)
            {
                L.LogWarning("Invalid moderation types.");
                return;
            }
            SendActorsAndEvidence(player);
        }
        else
        {
            for (int i = 1; i < data.ActionsActorCount; ++i)
                ModerationActionActors[i].Root.SetVisibility(c, false);

            for (int i = 1; i < data.ActionsEvidenceCount; ++i)
                ModerationActionEvidence[i].Root.SetVisibility(c, false);

            data.ActionsActorCount = 1;
            data.ActionsEvidenceCount = 1;
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
            mainActor.Name.SetText(c, player.Name.PlayerName);
            mainActor.YouButton.SetVisibility(c, false);
            mainActor.Steam64Input.SetText(c, player.Steam64.ToString(CultureInfo.InvariantCulture));
            mainActor.RoleInput.SetText(c, RelatedActor.RolePrimaryAdmin);
            mainActor.AsAdminToggleState.SetVisibility(c, false);
            mainActor.AsAdminToggleButton.SetVisibility(c, false);

            ModerationSelectedEvidence mainEvidence = ModerationActionEvidence[0];
            mainEvidence.Root.SetVisibility(c, true);
            mainEvidence.PreviewImage.SetVisibility(c, false);
            mainEvidence.PreviewName.SetVisibility(c, false);
            mainEvidence.NoPreviewName.SetText(c, string.Empty);
            mainEvidence.ActorName.SetText(c, player.Name.PlayerName);
            mainEvidence.TimestampInput.SetText(c, evidence.Timestamp.UtcDateTime.ToString(DateTimeFormatInput));
            mainEvidence.MessageInput.SetText(c, string.Empty);
            mainEvidence.LinkInput.SetText(c, string.Empty);
            mainEvidence.Steam64Input.SetText(c, player.Steam64.ToString(CultureInfo.InvariantCulture));
            mainEvidence.YouButton.SetVisibility(c, false);

            if (player.CachedSteamProfile != null)
            {
                mainActor.ProfilePicture.SetImage(c, player.CachedSteamProfile.AvatarUrlMedium ?? string.Empty);
            }
            else
            {
                UniTask.Create(async () =>
                {
                    string? url = await player.GetProfilePictureURL(AvatarSize.Medium, player.DisconnectToken);
                    mainActor.ProfilePicture.SetImage(c, url ?? string.Empty);
                });
            }

        }

        if (data.PendingType != ModerationEntryType.None)
        {
            ModerationActionPresetHeaderRoot.SetVisibility(c, false);
            ModerationActionTypeHeader.SetText(c, Localization.TranslateEnum(data.PendingType));
            UpdateArgumentTypes(player, editingExisting);
            CreateInstances(data, player);
            Interlocked.Increment(ref data.ActionModeVersion);
        }
        else
        {
            if (data.PendingPreset == PresetType.None)
                return;
            ModerationActionTypeHeader.SetText(c, "...");
            UCWarfare.RunTask(async token =>
            {
                int v = Interlocked.Increment(ref data.ActionModeVersion);
                if (data.PendingPreset == PresetType.None || !PunishmentPresets.TryGetPreset(data.PendingPreset, out PunishmentPreset[] presets))
                    return;
                int nextLevel = await Data.ModerationSql.GetNextLevel(data.SelectedPlayer, data.PendingPreset, token).ConfigureAwait(false);
                await UCWarfare.ToUpdate(token);
                if (data.ActionModeVersion != v)
                    return;
                int index = nextLevel;
                if (index < 1)
                    index = 1;
                else if (index > presets.Length)
                    index = presets.Length;
                PunishmentPreset preset = presets[index - 1];
                data.PendingPresetValue = preset;
                string str = Localization.TranslateEnum(preset.PrimaryModerationType);

                if (preset.PrimaryDuration.HasValue)
                {
                    str = (preset.PrimaryDuration.Value.Ticks < 0
                        ? "Permanent "
                        : (Util.ToTimeString((int)Math.Round(preset.PrimaryDuration.Value.TotalSeconds)) + " ")) + str;
                }

                if (preset.SecondaryModerationType is > ModerationEntryType.None)
                {
                    str += " + ";
                    if (preset.SecondaryDuration.HasValue)
                    {
                        str += preset.SecondaryDuration.Value.Ticks < 0
                            ? "Permanent "
                            : (Util.ToTimeString((int)Math.Round(preset.SecondaryDuration.Value.TotalSeconds)) + " ");
                    }

                    str += Localization.TranslateEnum(preset.SecondaryModerationType!.Value);
                }

                ModerationActionTypeHeader.SetText(c, str);
                ModerationActionPresetHeader.SetText(c, Localization.TranslateEnum(data.PendingPreset) + " | Level " + nextLevel);
                CreateInstances(data, player);

                UpdateArgumentTypes(player, editingExisting);

                if (!string.IsNullOrEmpty(preset.DefaultMessage))
                    ModerationActionMessage.SetText(player.Connection, preset.DefaultMessage!);

                ModerationActionPresetHeaderRoot.SetVisibility(c, true);
            }, player.DisconnectToken, ctx: $"Update preset level for {player.Steam64} for player {data.SelectedPlayer}.");
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
    private void UpdateArgumentTypes(UCPlayer player, bool editingExisting)
    {
        ModerationData data = GetOrAddModerationData(player);

        ModerationEntryType type = data.PendingType;
        ITransportConnection c = player.Connection;
        bool hasForgiveable = false;
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
                PresetType presetType = data.PendingPresetValue.PresetType;
                if (presetType == PresetType.None)
                    goto hideAllElements;

                hasMute = data.PendingPresetValue.PrimaryModerationType == ModerationEntryType.Mute || data.PendingPresetValue.SecondaryModerationType is ModerationEntryType.Mute;
                hasAssetBan = data.PendingPresetValue.PrimaryModerationType == ModerationEntryType.AssetBan || data.PendingPresetValue.SecondaryModerationType is ModerationEntryType.AssetBan;
                hasPrimaryDuration = ModerationReflection.IsOfType<IDurationModerationEntry>(data.PendingPresetValue.PrimaryModerationType);
                hasSecondaryDuration = data.PendingPresetValue.SecondaryModerationType.HasValue
                                       && ModerationReflection.IsOfType<IDurationModerationEntry>(
                                           data.PendingPresetValue.SecondaryModerationType.Value);
                isNote = false;
            }
        }
        else
        {
            hasMute = data.PrimaryEditingEntry is Mute || data.SecondaryEditingEntry is Mute;
            hasAssetBan = data.PrimaryEditingEntry is AssetBan || data.SecondaryEditingEntry is AssetBan;
            isNote = data.PrimaryEditingEntry is Note || data.SecondaryEditingEntry is Note;
            hasForgiveable = data.PrimaryEditingEntry is IForgiveableModerationEntry forgiveable && !forgiveable.IsApplied(true)
                          || data.SecondaryEditingEntry is IForgiveableModerationEntry forgiveable2 && !forgiveable2.IsApplied(true);
            hasPrimaryDuration = data.PrimaryEditingEntry is IDurationModerationEntry;
            hasSecondaryDuration = data.SecondaryEditingEntry is IDurationModerationEntry;
        }

        if (editingExisting)
            ModerationActionMessage.SetText(player, (data.PrimaryEditingEntry?.Message ?? data.SecondaryEditingEntry?.Message) ?? string.Empty);
        else
        {
            string msg = ModerationActionMessage.UpdateFromDataMainThread(player.Player);
            if (data.PrimaryEditingEntry != null)
                data.PrimaryEditingEntry.Message = msg;
            if (data.SecondaryEditingEntry != null)
                data.SecondaryEditingEntry.Message = msg;
        }

        // bool tgl1 = false, msg1 = false; (add back if needed)
        if (hasMute)
        {
            MuteTypeTracker.Show(c);
            Mute? mute = data.PrimaryEditingEntry as Mute ?? data.SecondaryEditingEntry as Mute;
            if (mute != null)
                MuteTypeTracker.Set(player.Player, mute.Type);
            else
                MuteTypeTracker.Update(player.Player);
            // tgl1 = true;
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
                primaryName = Localization.TranslateEnum(editingExisting
                    ? ModerationReflection.GetType(data.PrimaryEditingEntry!.GetType())
                    : data.PendingPresetValue!.PrimaryModerationType, player.Locale.LanguageInfo) + " ";
            }
            else primaryName = string.Empty;

            ModerationActionMiniInputBox1.SetText(c, Util.ToTimeString((int)Math.Round((editingExisting
                ? ((IDurationModerationEntry?)data.PrimaryEditingEntry)!.Duration
                : (data.PendingPresetValue!.PrimaryDuration ?? TimeSpan.Zero)).TotalSeconds)));
            ModerationActionMiniInputBox1.SetPlaceholder(c, primaryName + "Duration");
        }
        else ModerationActionMiniInputBox1.Hide(c);

        if (hasSecondaryDuration)
        {
            ModerationActionMiniInputBox2.Show(c);
            string secondaryName;
            if (hasPrimaryDuration)
            {
                secondaryName = Localization.TranslateEnum(editingExisting
                    ? ModerationReflection.GetType(data.SecondaryEditingEntry!.GetType())
                    : data.PendingPresetValue!.SecondaryModerationType!.Value, player.Locale.LanguageInfo) + " ";
            }
            else secondaryName = string.Empty;

            ModerationActionMiniInputBox2.SetText(c, Util.ToTimeString((int)Math.Round((editingExisting
                ? ((IDurationModerationEntry?)data.SecondaryEditingEntry)!.Duration
                : (data.PendingPresetValue!.SecondaryDuration ?? TimeSpan.Zero)).TotalSeconds)));
            ModerationActionMiniInputBox2.SetPlaceholder(c, secondaryName + "Duration");
        }
        else ModerationActionMiniInputBox2.Hide(c);

        if (!isNote)
        {
            ModerationActionInputBox2.Show(c);
            double rep;
            if (editingExisting)
            {
                if (data.PrimaryEditingEntry != null && data.SecondaryEditingEntry != null)
                    rep = (data.PrimaryEditingEntry.Reputation + data.SecondaryEditingEntry.Reputation) / 2d;
                else if (data.SecondaryEditingEntry != null)
                    rep = data.SecondaryEditingEntry.Reputation;
                else if (data.PrimaryEditingEntry != null)
                    rep = data.PrimaryEditingEntry.Reputation;
                else rep = 0d;
            }
            else if (data.PendingPresetValue != null)
                rep = data.PendingPresetValue.Reputation;
            else
                rep = GetDefaultRep(data.PendingType);

            ModerationActionInputBox2.SetText(c, rep == 0 ? string.Empty : rep.ToString(player.Locale.ParseFormat));
            ModerationActionInputBox2.SetPlaceholder(c, "Reputation");
        }
        else ModerationActionInputBox2.Hide(c);

        ModerationActionToggleButton2.Hide(c);
        if (hasAssetBan)
        {
            ModerationActionInputBox2.Show(c);
            AssetBan? assetBan = data.PrimaryEditingEntry as AssetBan ?? data.SecondaryEditingEntry as AssetBan;
            if (assetBan != null)
                ModerationActionInputBox2.SetText(c, assetBan.GetCommaList(true));
            else
                ModerationActionInputBox2.SetText(c, string.Empty);
            ModerationActionInputBox2.SetPlaceholder(c, "Vehicles (comma separated, blank = all)");
            // msg1 = true;
        }
        else ModerationActionInputBox2.Hide(c);

        ModerationActionInputBox3.Hide(c);
        ModerationActionMiniInputBox2.Hide(c);

        int ct;
        ModerationActionControls[0].Text.SetText(c, "Cancel");
        if (editingExisting)
        {
            ModerationActionControls[1].Text.SetText(c, "Save");
            ModerationActionControls[2].Text.SetText(c, "Remove");
            ct = 3;
            if (hasForgiveable)
            {
                ModerationActionControls[3].Text.SetText(c, "Forgive");
                ct = 4;
            }

            if (ct > ModerationActionControls.Length)
                ct = ModerationActionControls.Length;
        }
        else
        {
            ModerationActionControls[1].Text.SetText(c, "Add");
            ct = Math.Min(2, ModerationActionControls.Length);
        }

        int i2 = 0;
        for (; i2 < ct; ++i2)
            ModerationActionControls[i2].Root.SetVisibility(c, true);

        for (; i2 < ModerationActionControls.Length; ++i2)
            ModerationActionControls[i2].Root.SetVisibility(c, false);

        return;

        hideAllElements:

        for (int i = 0; i < ModerationActionControls.Length; ++i)
            ModerationActionControls[i].Root.SetVisibility(c, false);
        ModerationActionInputBox2.Hide(c);
        ModerationActionInputBox3.Hide(c);
        ModerationActionToggleButton1.Button.SetVisibility(c, false);
        ModerationActionToggleButton2.Button.SetVisibility(c, false);
        ModerationActionMiniInputBox1.Hide(c);
        ModerationActionMiniInputBox2.Hide(c);
    }

    private static double GetDefaultRep(ModerationEntryType type)
    {
        return type switch
        {
            ModerationEntryType.Ban => -80,
            ModerationEntryType.Mute => -70,
            ModerationEntryType.Kick => -20,
            ModerationEntryType.Warning => -15,
            ModerationEntryType.BugReportAccepted => 150,
            ModerationEntryType.PlayerReportAccepted => 80,
            ModerationEntryType.Commendation => 100,
            ModerationEntryType.Teamkill => -15,
            ModerationEntryType.VehicleTeamkill => -25,
            _ => 0
        };
    }
    private void OnActionControlClicked(UnturnedButton button, Player player)
    {
        int control = Array.FindIndex(ModerationActionControls, x => x.Root == button);
        if (control == -1 || UCPlayer.FromPlayer(player) is not { } ucPlayer)
            return;

        ModerationData data = GetOrAddModerationData(ucPlayer);

        switch (control)
        {
            case 0:
                OnClickedCancel(data, ucPlayer);
                break;
            case 1:
                OnClickedAddOrSave(data, ucPlayer);
                break;
            case 2:
                OnClickedRemove(data, ucPlayer);
                break;
            case 3:
                OnClickedForgive(data, ucPlayer);
                break;
        }
    }
    private void OnClickedCancel(ModerationData data, UCPlayer player)
    {
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

        data.PrimaryEditingEntry = null;
        data.SecondaryEditingEntry = null;

        ModerationFormRoot.SetVisibility(player.Connection, false);
        for (int i = 0; i < ModerationActionControls.Length; ++i)
            ModerationActionControls[i].Root.SetVisibility(player, false);
    }
    private void CreateInstances(ModerationData data, UCPlayer player)
    {
        if (data.PrimaryEditingEntry == null)
        {
            if (data.PendingType != ModerationEntryType.None)
            {
                if (ModerationReflection.GetType(data.PendingType) is not { } type)
                {
                    L.LogWarning($"Unknown pending type: {data.PendingType}.");
                    return;
                }

                data.PrimaryEditingEntry = (ModerationEntry)Activator.CreateInstance(type);
                data.SecondaryEditingEntry = null;

                if (data.PrimaryEditingEntry is IDurationModerationEntry duration)
                    duration.Duration = TimeSpan.FromHours(12d);
            }
            else if (data.PendingPresetValue != null)
            {
                if (ModerationReflection.GetType(data.PendingPresetValue.PrimaryModerationType) is not { } primaryType)
                {
                    L.LogWarning($"Unknown preset primary type: {data.PendingPresetValue.PrimaryModerationType}.");
                    return;
                }

                Type? secondaryType = data.PendingPresetValue.SecondaryModerationType.HasValue
                    ? ModerationReflection.GetType(data.PendingPresetValue.SecondaryModerationType.Value)
                    : null;

                data.PrimaryEditingEntry = (ModerationEntry)Activator.CreateInstance(primaryType);
                data.SecondaryEditingEntry = secondaryType == null ? null : (ModerationEntry)Activator.CreateInstance(secondaryType);

                if (data.PendingPresetValue.PrimaryDuration.HasValue && data.PrimaryEditingEntry is IDurationModerationEntry duration)
                    duration.Duration = data.PendingPresetValue.PrimaryDuration.Value;

                if (data.PendingPresetValue.SecondaryDuration.HasValue && data.SecondaryEditingEntry is IDurationModerationEntry duration2)
                    duration2.Duration = data.PendingPresetValue.SecondaryDuration.Value;
            }
            else return;
        }

        string? msg = ModerationActionMessage.GetOrAddData(player.Player, string.Empty).Text;

        data.PrimaryEditingEntry.Message = msg;
        if (data.SecondaryEditingEntry != null)
            data.SecondaryEditingEntry.Message = msg;

        Mute? mute = data.PrimaryEditingEntry as Mute ?? data.SecondaryEditingEntry as Mute;

        if (mute != null && MuteTypeTracker.TryGetSelection(player.Player, out MuteType muteType))
            mute.Type = muteType;

        AssetBan? assetBan = data.PrimaryEditingEntry as AssetBan ?? data.SecondaryEditingEntry as AssetBan;

        if (assetBan != null && ModerationActionMiniInputBox1.TextBox.GetOrAddData(player.Player).Text is { Length: > 0 } text)
        {
            assetBan.FillFromText(text);
        }
    }
    private void OnClickedAddOrSave(ModerationData data, UCPlayer player)
    {
        CreateInstances(data, player);
        if (data.PrimaryEditingEntry == null)
            return;
        bool isNew = !data.PrimaryEditingEntry.Id.IsValid;

        // add editor
        if (!Array.Exists(data.PrimaryEditingEntry.Actors, x => x.Actor.Id == player.Steam64))
        {
            RelatedActor[] actors = data.PrimaryEditingEntry.Actors;
            Util.AddToArray(ref actors!, new RelatedActor(RelatedActor.RoleEditor, true, Actors.GetActor(player.Steam64)));
            data.PrimaryEditingEntry.Actors = actors;
        }
        if (data.SecondaryEditingEntry != null && !Array.Exists(data.SecondaryEditingEntry.Actors, x => x.Actor.Id == player.Steam64))
        {
            RelatedActor[] actors = data.SecondaryEditingEntry.Actors;
            Util.AddToArray(ref actors!, new RelatedActor(RelatedActor.RoleEditor, true, Actors.GetActor(player.Steam64)));
            data.SecondaryEditingEntry.Actors = actors;
        }

        // add to related entries
        if (data is { SecondaryEditingEntry.Id.IsValid: true } &&
            !Array.Exists(data.PrimaryEditingEntry.RelatedEntryKeys, x => x.Key == data.SecondaryEditingEntry.Id.Key))
        {
            PrimaryKey[] relatedEntries = data.PrimaryEditingEntry.RelatedEntryKeys;
            Util.AddToArray(ref relatedEntries!, data.SecondaryEditingEntry.Id);
            data.PrimaryEditingEntry.RelatedEntryKeys = relatedEntries;
            data.PrimaryEditingEntry.RelatedEntries = null;
        }
        if (data is { PrimaryEditingEntry.Id.IsValid: true, SecondaryEditingEntry: not null } &&
            !Array.Exists(data.SecondaryEditingEntry.RelatedEntryKeys, x => x.Key == data.PrimaryEditingEntry.Id.Key))
        {
            PrimaryKey[] relatedEntries = data.SecondaryEditingEntry.RelatedEntryKeys;
            Util.AddToArray(ref relatedEntries!, data.PrimaryEditingEntry.Id);
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

        Save(data, player);
    }
    private void Save(ModerationData data, UCPlayer player)
    {

    }
    private void OnClickedRemove(ModerationData data, UCPlayer player)
    {
        if (data.PrimaryEditingEntry == null)
        {
            L.LogWarning("Tried to remove an in-progress entry.");
            return;
        }


    }
    private void OnClickedForgive(ModerationData data, UCPlayer player)
    {
        IForgiveableModerationEntry? f1 = data.PrimaryEditingEntry as IForgiveableModerationEntry;
        IForgiveableModerationEntry? f2 = data.SecondaryEditingEntry as IForgiveableModerationEntry;


    }
    private void OnClickedAddActor(UnturnedButton button, Player player)
    {
        UCPlayer? ucPlayer = UCPlayer.FromPlayer(player);
        if (ucPlayer == null)
            return;
        ModerationData data = GetOrAddModerationData(ucPlayer);
        int ct = data.Actors.Count;

    }
    private void OnClickedRemoveActor(UnturnedButton button, Player player)
    {
        UCPlayer? ucPlayer = UCPlayer.FromPlayer(player);
        if (ucPlayer == null)
            return;
        ModerationData data = GetOrAddModerationData(ucPlayer);
        int index = Array.FindIndex(ModerationActionActors, x => x.RemoveButton == button);
        if (index < 0 || index >= data.Actors.Count)
            return;

        // todo
    }
    private void OnClickedAddEidence(UnturnedButton button, Player player)
    {
        UCPlayer? ucPlayer = UCPlayer.FromPlayer(player);
        if (ucPlayer == null)
            return;
    }
    private void OnClickedRemoveEvidence(UnturnedButton button, Player player)
    {
        UCPlayer? ucPlayer = UCPlayer.FromPlayer(player);
        if (ucPlayer == null)
            return;
        ModerationData data = GetOrAddModerationData(ucPlayer);
        int index = Array.FindIndex(ModerationActionEvidence, x => x.RemoveButton == button);
        if (index < 0 || index >= data.Evidence.Count)
            return;

        // todo
    }
    private void SendActorsAndEvidence(UCPlayer player, int startIndActor = 0, int lenActor = -1, int startIndEvidence = 0, int lenEvidence = -1)
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
                    evidenceUi.PreviewImage.SetVisibility(c, true);
                    evidenceUi.PreviewImage.SetImage(c, evidence.URL);
                    evidenceUi.PreviewName.SetVisibility(c, true);
                    evidenceUi.PreviewName.SetText(c, name);
                }
                else
                {
                    evidenceUi.NoPreviewName.SetVisibility(c, true);
                    evidenceUi.NoPreviewName.SetText(c, name);
                    evidenceUi.PreviewImage.SetVisibility(c, false);
                    evidenceUi.PreviewName.SetVisibility(c, false);
                }

                evidenceUi.YouButton.SetVisibility(c, evidence.Actor.Id == player.Steam64);
                evidenceUi.MessageInput.SetText(c, evidence.Message ?? string.Empty);

                if (i + startIndEvidence >= data.InfoActorCount)
                    evidenceUi.Root.SetVisibility(c, true);
            }

            if (startIndEvidence + lenEvidence == data.Evidence.Count)
            {
                for (; i < data.ActionsEvidenceCount; ++i)
                    ModerationActionEvidence[i + startIndEvidence].Root.SetVisibility(c, false);

                data.ActionsEvidenceCount = ct + startIndEvidence;
            }


            UCWarfare.RunTask(async token =>
            {
                if (data.InfoVersion != v)
                    return;
                for (int j = 0; j < ct; ++j)
                {
                    IModerationActor actor = data.Evidence[i].Actor;
                    ValueTask<string> name = actor.GetDisplayName(Data.ModerationSql, token);
                    UnturnedLabel nameLbl = ModerationActionEvidence[i].ActorName;
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
            }, player.DisconnectToken, ctx: $"Update evidence action info for actions for {data.Player}.");
        }
        else
        {
            for (int i = data.InfoEvidenceCount - 1; i >= 0; --i)
            {
                ModerationActionEvidence[i].Root.SetVisibility(c, false);
            }
        }

        if (lenActor > 0)
        {
            UCWarfare.RunTask(async token =>
            {
                if (data.ActionVersion != v)
                    return;
                int i = 0;
                int ct = Math.Min(lenActor, data.Actors.Count - startIndActor);
                ulong[] steamIds = await Data.ModerationSql.GetSteam64IDs(data.Actors.Skip(startIndActor).Take(lenActor).Select(x => x.Actor).ToArray(), token).ConfigureAwait(false);

                if (data.ActionVersion != v)
                    return;

                await Data.ModerationSql.CacheAvatars(steamIds, token);

                if (data.ActionVersion != v)
                    return;

                for (; i < ct; ++i)
                {
                    RelatedActor actor = data.Actors[i];
                    ModerationSelectedActor actorUi = ModerationActionActors[i];
                    actorUi.RoleInput.SetText(c, string.IsNullOrWhiteSpace(actor.Role) ? "No role" : actor.Role);
                    if (Util.IsValidSteam64Id(actor.Actor.Id))
                        actorUi.Steam64Input.SetText(c, actor.Actor.Id.ToString(CultureInfo.InvariantCulture));
                    else actorUi.Steam64Input.SetText(c, string.Empty);
                    if (i >= data.ActionsActorCount)
                        actorUi.Root.SetVisibility(c, true);
                }

                for (; i < data.ActionsActorCount; ++i)
                    ModerationActionActors[i].Root.SetVisibility(c, false);

                data.ActionsActorCount = ct;

                for (i = 0; i < ct; ++i)
                {
                    RelatedActor actor = data.Actors[i];
                    ModerationSelectedActor actorUi = ModerationActionActors[i];
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

            }, player.DisconnectToken, ctx: $"Update actor action info for actions for {data.Player}.");
        }
        else
        {
            for (int i = data.ActionsActorCount - 1; i >= 0; --i)
            {
                ModerationActionEvidence[i].Root.SetVisibility(c, false);
            }
        }
    }
    private void OnClickedModerateButton(Player player, ModerationEntryType type)
    {
        UCPlayer? ucPlayer = UCPlayer.FromPlayer(player);
        if (ucPlayer == null)
            return;
        Type? sysType = ModerationReflection.GetType(type);
        if (sysType == null || !typeof(ModerationEntry).IsAssignableFrom(sysType))
        {
            L.LogWarning($"Unknown moderation type: {type}.");
            return;
        }

        L.LogDebug($"Pressed action: {type}.");

        ModerationData data = GetOrAddModerationData(ucPlayer);
        bool isDeselecting = !Util.IsValidSteam64Id(data.SelectedPlayer);

        if (data.PendingPreset != PresetType.None)
        {
            if ((int)data.PendingPreset - 1 < Presets.Length)
                Presets[(int)data.PendingPreset - 1].Enable(ucPlayer.Connection);

            data.PendingPreset = PresetType.None;
        }

        if (data.PendingType != ModerationEntryType.None && data.PendingType != type)
        {
            LabeledStateButton? btn = GetModerationButton(data.PendingType);
            btn?.Enable(ucPlayer.Connection);
        }

        if (!isDeselecting)
        {
            data.PendingType = type;

            LabeledStateButton? btn = GetModerationButton(type);
            btn?.Disable(ucPlayer.Connection);

            EditInActionMenu(ucPlayer, false);
        }
        else
        {
            ModerationFormRoot.SetVisibility(ucPlayer.Connection, false);
        }
    }
    private void OnClickedPreset(UnturnedButton button, Player player)
    {
        int index = Array.FindIndex(Presets, x => x.Button == button);
        if (index == -1 || PunishmentPresets.Presets.Count <= index)
        {
            L.LogWarning($"Unknown preset type: {button.Name}.");
            return;
        }

        UCPlayer? ucPlayer = UCPlayer.FromPlayer(player);
        PresetType preset = (PresetType)(index + 1);
        if (ucPlayer == null || !PunishmentPresets.TryGetPreset(preset, out _))
        {
            L.LogWarning($"Preset not found: {preset}.");
            return;
        }

        L.LogDebug($"Pressed preset: {preset}.");

        ModerationData data = GetOrAddModerationData(ucPlayer);
        if (data.PendingPreset != PresetType.None && data.PendingPreset != preset)
        {
            if ((int)data.PendingPreset - 1 < Presets.Length)
                Presets[(int)data.PendingPreset - 1].Enable(ucPlayer.Connection);
        }

        bool isDeselecting = !Util.IsValidSteam64Id(data.SelectedPlayer);

        if (!isDeselecting)
        {
            data.PendingPreset = preset;
            Presets[index].Disable(ucPlayer.Connection);

            if (data.PendingType != ModerationEntryType.None)
            {
                LabeledStateButton? btn = GetModerationButton(data.PendingType);
                btn?.Enable(ucPlayer.Connection);

                data.PendingType = ModerationEntryType.None;
            }

            EditInActionMenu(ucPlayer, false);
        }
        else
        {
            ModerationFormRoot.SetVisibility(ucPlayer.Connection, false);
        }
    }
}
