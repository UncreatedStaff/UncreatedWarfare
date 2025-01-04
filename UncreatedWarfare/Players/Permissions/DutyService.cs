using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Exceptions;
using System;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Tweaks;

namespace Uncreated.Warfare.Players.Permissions;

public class DutyService
{
    private readonly SignInstancer _signs;
    private readonly ChatService _chatService;
    private readonly ILogger<DutyService> _logger;
    private readonly UserPermissionStore _permissionStore;
    private readonly DutyCommandTranslations _translations;

    public static readonly PermissionLeaf PermissionFreecam = new PermissionLeaf("unturned::features.freecam");
    public static readonly PermissionLeaf PermissionWorkzone = new PermissionLeaf("unturned::features.workzone");

    public DutyService(
        SignInstancer signs,
        TranslationInjection<DutyCommandTranslations> translations,
        ChatService chatService,
        ILogger<DutyService> logger,
        UserPermissionStore permissionStore)
    {
        _signs = signs;
        _chatService = chatService;
        _logger = logger;
        _permissionStore = permissionStore;
        _translations = translations.Value;
    }

    [RpcSend]
    public virtual void SendDutyChanged(ulong steam64, DutyLevel level, bool isOnDuty) { }

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

        _chatService.Send(player, _translations.DutyOnFeedback);
        _chatService.Broadcast(_translations.TranslationService.SetOf.AllPlayersExcept(player), _translations.DutyOnBroadcast, player);

        _logger.LogInformation("{0} ({1}) went on duty (owner: {2}, admin: {3}, trial admin: {4}, staff: {5}).",
            player.Names.GetDisplayNameOrPlayerName(),
            player.Steam64,
            level == DutyLevel.Owner,
            level == DutyLevel.Admin,
            level == DutyLevel.TrialAdmin,
            level == DutyLevel.Staff
        );

        ActionLog.Add(ActionLogType.DutyChanged, "ON DUTY", player.Steam64);

        try
        {
            SendDutyChanged(player.Steam64.m_SteamID, level, true);
        }
        catch (RpcNoConnectionsException) { }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to trigger SendDutyChanged.");
        }

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
        }

        player.Component<PlayerJumpComponent>().IsActive = false;
        player.Component<VanishPlayerComponent>().IsActive = false;
        player.Component<GodPlayerComponent>().IsActive = false;

        _signs.UpdateSigns<KitSignInstanceProvider>(player);

        _chatService.Send(player, _translations.DutyOffFeedback);
        _chatService.Broadcast(_translations.TranslationService.SetOf.AllPlayersExcept(player), _translations.DutyOffBroadcast, player);

        _logger.LogInformation("{0} ({1}) went off duty (owner: {2}, admin: {3}, trial admin: {4}, staff: {5}).",
            player.Names.GetDisplayNameOrPlayerName(),
            player.Steam64,
            level == DutyLevel.Owner,
            level == DutyLevel.Admin,
            level == DutyLevel.TrialAdmin,
            level == DutyLevel.Staff
        );

        ActionLog.Add(ActionLogType.DutyChanged, "OFF DUTY", player.Steam64);

        try
        {
            SendDutyChanged(player.Steam64.m_SteamID, level, false);
        }
        catch (RpcNoConnectionsException) { }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to trigger SendDutyChanged.");
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