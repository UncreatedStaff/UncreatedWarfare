using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Exceptions;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Exceptions;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Tweaks;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Players.Permissions;

[GenerateRpcSource]
public partial class DutyService : IAsyncEventListener<PlayerLeft>
{
    private readonly SignInstancer _signs;
    private readonly ChatService _chatService;
    private readonly ILogger<DutyService> _logger;
    private readonly IPlayerService _playerService;
    private readonly DutyUI _dutyUi;
    private readonly ZoneStore _zoneStore;
    private readonly UserPermissionStore _permissionStore;
    private readonly DutyCommandTranslations _translations;

    public static readonly PermissionLeaf PermissionFreecam = new PermissionLeaf("unturned::features.freecam");
    public static readonly PermissionLeaf PermissionWorkzone = new PermissionLeaf("unturned::features.workzone");

    public string StaffOffDuty { get; }
    public string TrialOffDuty { get; }
    public string AdminOffDuty { get; }
    public string StaffOnDuty { get; }
    public string TrialOnDuty { get; }
    public string AdminOnDuty { get; }
    public string Owner { get; }

    public DutyService(
        SignInstancer signs,
        TranslationInjection<DutyCommandTranslations> translations,
        ChatService chatService,
        ILogger<DutyService> logger,
        UserPermissionStore permissionStore,
        IConfiguration configuration,
        IPlayerService playerService,
        DutyUI dutyUi,
        ZoneStore zoneStore)
    {
        _signs = signs;
        _chatService = chatService;
        _logger = logger;
        _permissionStore = permissionStore;
        _playerService = playerService;
        _dutyUi = dutyUi;
        _zoneStore = zoneStore;
        _translations = translations.Value;

        IConfigurationSection permSection = configuration.GetSection("permissions");
        StaffOffDuty = permSection["staff_off_duty"] ?? throw new GameConfigurationException("Missing duty permissions.");
        TrialOffDuty = permSection["trial_off_duty"] ?? throw new GameConfigurationException("Missing duty permissions.");
        AdminOffDuty = permSection["admin_off_duty"] ?? throw new GameConfigurationException("Missing duty permissions.");
        StaffOnDuty  = permSection["staff_on_duty"]  ?? throw new GameConfigurationException("Missing duty permissions.");
        TrialOnDuty  = permSection["trial_on_duty"]  ?? throw new GameConfigurationException("Missing duty permissions.");
        AdminOnDuty  = permSection["admin_on_duty"]  ?? throw new GameConfigurationException("Missing duty permissions.");
        Owner        = permSection["owner"]          ?? throw new GameConfigurationException("Missing duty permissions.");
    }

    [RpcSend]
    protected partial RpcTask SendDutyChanged(ulong steam64, DutyLevel level, bool isOnDuty);

    /// <summary>
    /// Switch the player's duty state to whatever it isn't currently.
    /// </summary>
    /// <returns><see langword="true"/> if the player's duty state was toggled, otherwise <see langword="false"/> because the player is not staff.</returns>
    [RpcReceive]
    public async Task<bool> ToggleDutyStateAsync(CSteamID player, CancellationToken token = default)
    {
        (DutyLevel level, bool wasOnDuty) = await CheckDutyStateAsync(player, false, token).ConfigureAwait(false);
        if (level == DutyLevel.Member)
            return false;

        await SetDutyStateIntl(player, level, !wasOnDuty, token).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Sets the player's duty state to whatever it isn't currently.
    /// </summary>
    /// <returns><see langword="true"/> if the player's duty state was set, otherwise <see langword="false"/> because they are already in the requested duty state or are not staff.</returns>
    [RpcReceive]
    public async Task<bool> SetDutyStateAsync(CSteamID steam64, bool isOnDuty, CancellationToken token = default)
    {
        (DutyLevel level, bool wasOnDuty) = await CheckDutyStateAsync(steam64, false, token).ConfigureAwait(false);
        if (level == DutyLevel.Member || wasOnDuty == isOnDuty)
            return false;

        await SetDutyStateIntl(steam64, level, !wasOnDuty, token).ConfigureAwait(false);
        return true;
    }

    private async Task SetDutyStateIntl(CSteamID steam64, DutyLevel level, bool isOnDuty, CancellationToken token = default)
    {
        if (!isOnDuty)
        {
            if (level is DutyLevel.Admin or DutyLevel.Owner)
            {
                await _permissionStore.AddPermissionGroupsAsync(steam64, [ AdminOffDuty, TrialOffDuty, StaffOffDuty ], token).ConfigureAwait(false);
            }
            else if (level == DutyLevel.TrialAdmin)
            {
                await _permissionStore.AddPermissionGroupsAsync(steam64, [ TrialOffDuty, StaffOffDuty ], CancellationToken.None).ConfigureAwait(false);
            }
            else
            {
                await _permissionStore.AddPermissionGroupsAsync(steam64, [ StaffOffDuty ], CancellationToken.None).ConfigureAwait(false);
            }

            await _permissionStore.RemovePermissionGroupsAsync(steam64, [ AdminOnDuty, TrialOnDuty, StaffOnDuty, Owner ], CancellationToken.None).ConfigureAwait(false);
        }
        else
        {
            if (level == DutyLevel.Owner)
            {
                await _permissionStore.AddPermissionGroupsAsync(steam64, [ AdminOnDuty, TrialOnDuty, StaffOnDuty, Owner ], token).ConfigureAwait(false);
            }
            else if (level == DutyLevel.Admin)
            {
                await _permissionStore.AddPermissionGroupsAsync(steam64, [ AdminOnDuty, TrialOnDuty, StaffOnDuty ], CancellationToken.None).ConfigureAwait(false);
            }
            else if (level == DutyLevel.TrialAdmin)
            {
                await _permissionStore.AddPermissionGroupsAsync(steam64, [ TrialOnDuty, StaffOnDuty ], CancellationToken.None).ConfigureAwait(false);
            }
            else
            {
                await _permissionStore.AddPermissionGroupsAsync(steam64, [ StaffOnDuty ], CancellationToken.None).ConfigureAwait(false);
            }

            await _permissionStore.RemovePermissionGroupsAsync(steam64, [ AdminOffDuty, TrialOffDuty, StaffOffDuty ], CancellationToken.None).ConfigureAwait(false);
        }

        await UniTask.SwitchToMainThread(CancellationToken.None);

        if (_playerService.GetOnlinePlayerOrNull(steam64) is { } player)
        {
            if (!isOnDuty)
            {
                await ApplyOffDuty(player, level, CancellationToken.None);
            }
            else
            {
                await ApplyOnDuty(player, level, CancellationToken.None);
            }
            return;
        }

        // todo: ActionLog.Add(ActionLogType.DutyChanged, isOnDuty ? "ON DUTY (offline)" : "OFF DUTY (offline)", steam64.m_SteamID);
        try
        {
            await SendDutyChanged(steam64.m_SteamID, level, isOnDuty);
        }
        catch (RpcNoConnectionsException) { }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to trigger SendDutyChanged.");
        }
    }

    public async Task<(DutyLevel Level, bool IsOnDuty)> CheckDutyStateAsync(CSteamID player, bool validatePermissions, CancellationToken token = default)
    {
        IReadOnlyList<PermissionGroup> permGroups = await _permissionStore.GetPermissionGroupsAsync(player, forceRedownload: true, token).ConfigureAwait(false);

        bool isOnDuty = false;
        DutyLevel level = DutyLevel.Member;
        if (permGroups.Any(x => x.Id.Equals(Owner, StringComparison.Ordinal)))
        {
            isOnDuty = true;
            level = DutyLevel.Owner;
        }
        else if (player.m_SteamID is 76561198267927009ul or 76561198857595123ul)
        {
            level = DutyLevel.Owner;
        }
        else if (permGroups.Any(x => x.Id.Equals(AdminOnDuty, StringComparison.Ordinal)))
        {
            isOnDuty = true;
            level = DutyLevel.Admin;
        }
        else if (permGroups.Any(x => x.Id.Equals(AdminOffDuty, StringComparison.Ordinal)))
        {
            level = DutyLevel.Admin;
        }
        else if (permGroups.Any(x => x.Id.Equals(TrialOnDuty, StringComparison.Ordinal)))
        {
            isOnDuty = true;
            level = DutyLevel.TrialAdmin;
        }
        else if (permGroups.Any(x => x.Id.Equals(TrialOffDuty, StringComparison.Ordinal)))
        {
            level = DutyLevel.TrialAdmin;
        }
        else if (permGroups.Any(x => x.Id.Equals(StaffOnDuty, StringComparison.Ordinal)))
        {
            isOnDuty = true;
            level = DutyLevel.Staff;
        }
        else if (permGroups.Any(x => x.Id.Equals(StaffOffDuty, StringComparison.Ordinal)))
        {
            level = DutyLevel.Staff;
        }

        if (validatePermissions && _playerService.GetOnlinePlayerOrNullThreadSafe(player) is { } onlinePlayer)
        {
            if (isOnDuty)
                await ApplyOnDuty(onlinePlayer, level, token);
            else
                await ApplyOffDuty(onlinePlayer, level, token);
        }

        return (level, isOnDuty);
    }

    internal async UniTask ApplyOnDuty(WarfarePlayer player, DutyLevel level, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        if (player.UnturnedPlayer.look != null)
        {
            bool freecam = await _permissionStore.HasPermissionAsync(player, PermissionFreecam, token);
            bool workzone = await _permissionStore.HasPermissionAsync(player, PermissionWorkzone, token);
            await UniTask.SwitchToMainThread(token);

            player.UnturnedPlayer.look.sendFreecamAllowed(freecam);
            player.UnturnedPlayer.look.sendWorkzoneAllowed(workzone);
        }

        if (player.UnturnedPlayer.interact != null)
        {
            player.UnturnedPlayer.interact.sendSalvageTimeOverride(1f);
        }

        if (_zoneStore.IsInMainBase(player))
        {
            ItemJar? heldItem = player.GetHeldItem(out _);
            if (heldItem != null && heldItem.GetAsset() is ItemGunAsset gun && (EFiremode)player.UnturnedPlayer.equipment.state[11] == EFiremode.SAFETY)
            {
                player.UnturnedPlayer.equipment.state[11] = (byte)ItemUtility.GetDefaultFireMode(gun);
                player.UnturnedPlayer.equipment.sendUpdateState();
            }
        }

        if (!player.IsOnDuty)
        {
            _chatService.Send(player, _translations.DutyOnFeedback);
            _chatService.Broadcast<IPlayer>(_translations.TranslationService.SetOf.AllPlayersExcept(player), _translations.DutyOnBroadcast, player);

            _logger.LogInformation("{0} ({1}) went on duty (owner: {2}, admin: {3}, trial admin: {4}, staff: {5}).",
                player.Names.GetDisplayNameOrPlayerName(),
                player.Steam64,
                level == DutyLevel.Owner,
                level == DutyLevel.Admin,
                level == DutyLevel.TrialAdmin,
                level == DutyLevel.Staff
            );

            // todo: ActionLog.Add(ActionLogType.DutyChanged, "ON DUTY", player.Steam64);
        }

        player.UpdateDutyState(true, level);

        _dutyUi.SendToPlayer(player);

        try
        {
            await SendDutyChanged(player.Steam64.m_SteamID, level, true);
        }
        catch (RpcNoConnectionsException) { }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to trigger SendDutyChanged.");
        }

        await UniTask.SwitchToMainThread(token);
        _signs.UpdateSigns<KitSignInstanceProvider>(player);
    }

    internal async UniTask ApplyOffDuty(WarfarePlayer player, DutyLevel level, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        if (player.UnturnedPlayer != null)
        {
            if (player.UnturnedPlayer.look != null)
            {
                bool freecam = await _permissionStore.HasPermissionAsync(player, PermissionFreecam, token);
                bool workzone = await _permissionStore.HasPermissionAsync(player, PermissionWorkzone, token);
                await UniTask.SwitchToMainThread(token);

                player.UnturnedPlayer.look.sendFreecamAllowed(freecam);
                player.UnturnedPlayer.look.sendWorkzoneAllowed(workzone);
            }

            if (player.UnturnedPlayer.movement != null)
            {
                if (player.UnturnedPlayer.movement.pluginSpeedMultiplier != 1f)
                    player.UnturnedPlayer.movement.sendPluginSpeedMultiplier(1f);
                if (player.UnturnedPlayer.movement.pluginJumpMultiplier != 1f)
                    player.UnturnedPlayer.movement.sendPluginJumpMultiplier(1f);
            }

            if (player.UnturnedPlayer.interact != null)
            {
                player.UnturnedPlayer.interact.sendSalvageTimeOverride(-1f);
            }

            if (player.UnturnedPlayer.quests != null)
            {
                if (player.UnturnedPlayer.quests.isMarkerPlaced)
                {
                    if (!player.IsSquadLeader() || player.UnturnedPlayer.quests.markerTextOverride == null)
                        player.UnturnedPlayer.quests.replicateSetMarker(false, Vector3.zero);
                }
            }

            if (_zoneStore.IsInMainBase(player))
            {
                ItemJar? heldItem = player.GetHeldItem(out _);
                ItemAsset? heldAsset = heldItem?.GetAsset();
                if (heldAsset is ItemGunAsset && (EFiremode)player.UnturnedPlayer.equipment.state[11] != EFiremode.SAFETY)
                {
                    player.UnturnedPlayer.equipment.state[11] = (byte)EFiremode.SAFETY;
                    player.UnturnedPlayer.equipment.sendUpdateState();
                }
                else if (heldAsset is ItemThrowableAsset)
                {
                    player.UnturnedPlayer.equipment.ServerEquip(byte.MaxValue, 0, 0);
                }
            }
        }

        player.Component<PlayerJumpComponent>().IsActive = false;
        player.Component<VanishPlayerComponent>().IsActive = false;
        player.Component<GodPlayerComponent>().SetAdminActive(false);

        _signs.UpdateSigns<KitSignInstanceProvider>(player);

        if (player.IsOnDuty)
        {
            _chatService.Send(player, _translations.DutyOffFeedback);
            _chatService.Broadcast<IPlayer>(_translations.TranslationService.SetOf.AllPlayersExcept(player), _translations.DutyOffBroadcast, player);

            _logger.LogInformation("{0} ({1}) went off duty (owner: {2}, admin: {3}, trial admin: {4}, staff: {5}).",
                player.Names.GetDisplayNameOrPlayerName(),
                player.Steam64,
                level == DutyLevel.Owner,
                level == DutyLevel.Admin,
                level == DutyLevel.TrialAdmin,
                level == DutyLevel.Staff
            );

            // todo: ActionLog.Add(ActionLogType.DutyChanged, "OFF DUTY", player.Steam64);
        }

        player.UpdateDutyState(false, level);

        _dutyUi.ClearFromPlayer(player.Connection);

        try
        {
            await SendDutyChanged(player.Steam64.m_SteamID, level, false);
        }
        catch (RpcNoConnectionsException) { }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to trigger SendDutyChanged.");
        }
    }

    async UniTask IAsyncEventListener<PlayerLeft>.HandleEventAsync(PlayerLeft e, IServiceProvider serviceProvider, CancellationToken token)
    {
        if (await SetDutyStateAsync(e.Steam64, false, CancellationToken.None).ConfigureAwait(false))
        {
            _chatService.Broadcast<IPlayer>(_translations.TranslationService.SetOf.AllPlayers(), _translations.DutyOffBroadcast, e.Player);
            _logger.LogInformation("{0} ({1}) went off duty from disconnecting.", e.Player.Names.GetDisplayNameOrPlayerName(), e.Player.Steam64);
        }
    }
}

public enum DutyLevel : byte
{
    Member,
    Staff,
    TrialAdmin,
    Admin,
    Owner
}