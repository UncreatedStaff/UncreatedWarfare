using Cysharp.Threading.Tasks;
using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Globalization;
using System.Threading;
using Uncreated.Framework;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Presets;
using Uncreated.SQL;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Moderation.Punishments;
using Uncreated.Warfare.Moderation.Punishments.Presets;

namespace Uncreated.Warfare.Moderation;
internal partial class ModerationUI
{
    private void EditInActionMenu(UCPlayer player, bool editingExisting)
    {
        ModerationData data = GetOrAddModerationData(player);

        ITransportConnection c = player.Connection;
        if (!Util.IsValidSteam64Id(data.SelectedPlayer) || data is { PendingPreset: PresetType.None, PendingType: ModerationEntryType.None })
        {
            ModerationFormRoot.SetVisibility(c, false);
            return;
        }

        if (!editingExisting)
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

            ModerationSelectedActor mainActor = ModerationActionActors[0];
            mainActor.Root.SetVisibility(c, true);
            mainActor.Name.SetText(c, player.Name.PlayerName);
            mainActor.YouButton.SetVisibility(c, false);
            mainActor.Steam64Input.SetText(c, player.Steam64.ToString(CultureInfo.InvariantCulture));
            mainActor.RoleInput.SetText(c, RelatedActor.RolePrimaryAdmin);
            mainActor.AsAdminToggleState.SetVisibility(c, true);
            mainActor.AsAdminToggleButton.SetVisibility(c, false);
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
        else
        {
            if (data.PendingType != ModerationEntryType.None)
            {
                ModerationActionPresetHeaderRoot.SetVisibility(c, false);
                ModerationActionTypeHeader.SetText(c, Localization.TranslateEnum(data.PendingType));
                UpdateArgumentTypes(player, editingExisting);
            }
            else
            {
                ModerationActionTypeHeader.SetText(c, "...");
            }

            if (data.PendingPreset != PresetType.None)
            {
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
                    UpdateArgumentTypes(player, editingExisting);

                    if (!string.IsNullOrEmpty(preset.DefaultMessage))
                        ModerationActionMessage.SetText(player.Connection, preset.DefaultMessage!);

                    CreateInstances(data, player);

                    ModerationActionPresetHeaderRoot.SetVisibility(c, true);
                    FillFields(player);
                }, player.DisconnectToken, ctx: $"Update preset level for {player.Steam64} for player {data.SelectedPlayer}.");
            }
            else
            {
                ModerationActionMessage.TextBox.UpdateFromDataMainThread(player.Player);
                CreateInstances(data, player);
                Interlocked.Increment(ref data.ActionModeVersion);
            }
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
        bool hasMute;
        bool hasAssetBan;
        if (!editingExisting)
        {
            if (data.PendingPresetValue == null)
            {
                if (type == ModerationEntryType.None)
                    goto hideAllElements;
                
                hasMute = type == ModerationEntryType.Mute;
                hasAssetBan = type == ModerationEntryType.AssetBan;
            }
            else
            {
                PresetType presetType = data.PendingPresetValue.PresetType;
                if (presetType == PresetType.None)
                    goto hideAllElements;

                hasMute = data.PendingPresetValue.PrimaryModerationType == ModerationEntryType.Mute || data.PendingPresetValue.SecondaryModerationType is ModerationEntryType.Mute;
                hasAssetBan = data.PendingPresetValue.PrimaryModerationType == ModerationEntryType.AssetBan || data.PendingPresetValue.SecondaryModerationType is ModerationEntryType.AssetBan;
            }
        }
        else
        {
            hasMute = data.PrimaryEditingEntry is Mute || data.SecondaryEditingEntry is Mute;
            hasAssetBan = data.PrimaryEditingEntry is AssetBan || data.SecondaryEditingEntry is AssetBan;
            IForgiveableModerationEntry? forgiveable = data.PrimaryEditingEntry as IForgiveableModerationEntry ?? data.SecondaryEditingEntry as IForgiveableModerationEntry;
            if (forgiveable != null)
                hasForgiveable = forgiveable.IsApplied(true);
        }

        // bool tgl1 = false, msg1 = false; (add back if needed)
        if (hasMute)
        {
            MuteTypeTracker.Show(c);
            Mute? mute = data.PrimaryEditingEntry as Mute ?? data.SecondaryEditingEntry as Mute;
            if (mute != null)
                MuteTypeTracker.Set(player, mute.Type);
            else
                MuteTypeTracker.Update(player);
            // tgl1 = true;
        }
        else
        {
            ModerationActionToggleButton1.Hide(c);
        }

        ModerationActionToggleButton2.Hide(c);
        if (hasAssetBan)
        {
            ModerationActionInputBox2.Show(c);
            ModerationActionInputBox2.SetPlaceholder(c, "Vehicles (comma separated, blank = all)");
            // msg1 = true;
        }
        else ModerationActionInputBox2.Hide(c);

        ModerationActionInputBox3.Hide(c);
        ModerationActionMiniInputBox1.Hide(c);
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
    private void FillFields(UCPlayer player)
    {
        ModerationData data = GetOrAddModerationData(player);

        
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
            LabeledStateButton btn = GetModerationButton(data.PendingType);
            if (btn.Button != null)
                btn.Enable(player.Connection);

            data.PendingType = ModerationEntryType.None;
        }

        ModerationFormRoot.SetVisibility(player.Connection, false);
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

        string? msg = ModerationActionMessage.TextBox.GetOrAddData(player, string.Empty).Text;

        data.PrimaryEditingEntry.Message = msg;
        if (data.SecondaryEditingEntry != null)
            data.SecondaryEditingEntry.Message = msg;

        Mute? mute = data.PrimaryEditingEntry as Mute ?? data.SecondaryEditingEntry as Mute;

        if (mute != null && MuteTypeTracker.TryGetSelection(player, out MuteType muteType))
            mute.Type = muteType;

        AssetBan? assetBan = data.PrimaryEditingEntry as AssetBan ?? data.SecondaryEditingEntry as AssetBan;

        if (assetBan != null && ModerationActionMiniInputBox1.TextBox.GetOrAddData(player).Text is { Length: > 0 } text)
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
            data.PrimaryEditingEntry.StartedTimestamp = now;
            if (data.SecondaryEditingEntry != null)
                data.SecondaryEditingEntry.StartedTimestamp = now;
        }
    }
    private void OnClickedRemove(ModerationData data, UCPlayer player)
    {

    }
    private void OnClickedForgive(ModerationData data, UCPlayer player)
    {

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
            LabeledStateButton btn = GetModerationButton(data.PendingType);
            if (btn.Button != null)
                btn.Enable(ucPlayer.Connection);
        }

        if (!isDeselecting)
        {
            data.PendingType = type;

            LabeledStateButton btn = GetModerationButton(type);
            if (btn.Button != null)
                btn.Disable(ucPlayer.Connection);

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
                LabeledStateButton btn = GetModerationButton(data.PendingType);
                if (btn.Button != null)
                    btn.Enable(ucPlayer.Connection);

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
