using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Tweaks;

namespace Uncreated.Warfare.Commands;

[SynchronizedCommand, Command("duty", "onduty", "offduty", "d")]
[MetadataFile(nameof(GetHelpMetadata))]
public class DutyCommand : IExecutableCommand
{
    private readonly UserPermissionStore _permissions;
    private readonly WarfareModule _warfare;
    private readonly SignInstancer _signs;
    private readonly DutyCommandTranslations _translations;
    private readonly ITranslationService _translationService;
    private readonly ChatService _chatService;

    private const string Help = "Swap your duty status between on and off. For admins and trial admins.";

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    internal DutyCommand(
        UserPermissionStore permissions,
        WarfareModule warfare,
        SignInstancer signs,
        TranslationInjection<DutyCommandTranslations> translations,
        ITranslationService translationService,
        ChatService chatService)
    {
        _translations = translations.Value;
        _permissions = permissions;
        _warfare = warfare;
        _signs = signs;
        _translationService = translationService;
        _chatService = chatService;
    }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = Help
        };
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        IReadOnlyList<PermissionGroup> permGroups = await _permissions.GetPermissionGroupsAsync(Context.CallerId, forceRedownload: true, token).ConfigureAwait(false);

        IConfigurationSection permSection = _warfare.Configuration.GetSection("permissions");

        string staffOffDuty = permSection["staff_off_duty"] ?? throw Context.SendUnknownError();
        string trialOffDuty = permSection["trial_off_duty"] ?? throw Context.SendUnknownError();
        string adminOffDuty = permSection["admin_off_duty"] ?? throw Context.SendUnknownError();
        string staffOnDuty  = permSection["staff_on_duty" ] ?? throw Context.SendUnknownError();
        string trialOnDuty  = permSection["trial_on_duty" ] ?? throw Context.SendUnknownError();
        string adminOnDuty  = permSection["admin_on_duty" ] ?? throw Context.SendUnknownError();

        bool wasOnDuty = false, isAdmin = false, isTrial = false, isStaff = false;
        if (permGroups.Any(x => x.Id.Equals(adminOnDuty, StringComparison.Ordinal)))
        {
            wasOnDuty = true;
            isAdmin = true;
        }
        else if (permGroups.Any(x => x.Id.Equals(adminOffDuty, StringComparison.Ordinal)))
        {
            isAdmin = true;
        }
        else if (permGroups.Any(x => x.Id.Equals(trialOnDuty, StringComparison.Ordinal)))
        {
            wasOnDuty = true;
            isTrial = true;
        }
        else if (permGroups.Any(x => x.Id.Equals(trialOffDuty, StringComparison.Ordinal)))
        {
            isTrial = true;
        }
        else if (permGroups.Any(x => x.Id.Equals(staffOnDuty, StringComparison.Ordinal)))
        {
            wasOnDuty = true;
            isStaff = true;
        }
        else if (permGroups.Any(x => x.Id.Equals(staffOffDuty, StringComparison.Ordinal)))
        {
            isStaff = true;
        }

        if (!isStaff && !isTrial && !isAdmin)
        {
            throw Context.SendNoPermission();
        }

        if (wasOnDuty)
        {
            await _permissions.RemovePermissionGroupsAsync(Context.CallerId, [ adminOnDuty, trialOnDuty, staffOnDuty ], CancellationToken.None).ConfigureAwait(false);

            if (isAdmin)
            {
                await _permissions.AddPermissionGroupsAsync(Context.CallerId, [ adminOffDuty, trialOffDuty, staffOffDuty ], CancellationToken.None).ConfigureAwait(false);
            }
            else if (isTrial)
            {
                await _permissions.AddPermissionGroupsAsync(Context.CallerId, [ trialOffDuty, staffOffDuty ], CancellationToken.None).ConfigureAwait(false);
            }
            else
            {
                await _permissions.AddPermissionGroupsAsync(Context.CallerId, [ staffOffDuty ], CancellationToken.None).ConfigureAwait(false);
            }
        }
        else
        {
            await _permissions.RemovePermissionGroupsAsync(Context.CallerId, [ adminOffDuty, trialOffDuty, staffOffDuty ], CancellationToken.None).ConfigureAwait(false);

            if (isAdmin)
            {
                await _permissions.AddPermissionGroupsAsync(Context.CallerId, [ adminOnDuty, trialOnDuty, staffOnDuty ], CancellationToken.None).ConfigureAwait(false);
            }
            else if (isTrial)
            {
                await _permissions.AddPermissionGroupsAsync(Context.CallerId, [ trialOnDuty, staffOnDuty ], CancellationToken.None).ConfigureAwait(false);
            }
            else
            {
                await _permissions.AddPermissionGroupsAsync(Context.CallerId, [ staffOnDuty ], CancellationToken.None).ConfigureAwait(false);
            }
        }

        await UniTask.SwitchToMainThread(CancellationToken.None);

        if (wasOnDuty)
        {
            ClearAdminPermissions(Context.Player);

            Context.Reply(_translations.DutyOffFeedback);
            _chatService.Broadcast(_translationService.SetOf.AllPlayersExcept(Context.CallerId.m_SteamID), _translations.DutyOffBroadcast, Context.Player);

            L.Log($"{Context.Player.Names.PlayerName} ({Context.CallerId.m_SteamID.ToString(CultureInfo.InvariantCulture)}) went off duty (admin: {isAdmin}, trial admin: {isTrial}, staff: {isStaff}).", ConsoleColor.Cyan);
            ActionLog.Add(ActionLogType.DutyChanged, "OFF DUTY", Context.CallerId.m_SteamID);

            // PlayerManager.NetCalls.SendDutyChanged.NetInvoke(Context.CallerId.m_SteamID, false);
        }
        else
        {
            Context.Reply(_translations.DutyOnFeedback);
            _chatService.Broadcast(_translationService.SetOf.AllPlayersExcept(Context.CallerId.m_SteamID), _translations.DutyOnBroadcast, Context.Player);

            L.Log($"{Context.Player.Names.PlayerName} ({Context.CallerId.m_SteamID.ToString(CultureInfo.InvariantCulture)}) went on duty (admin: {isAdmin}, trial admin: {isTrial}, staff: {isStaff}).", ConsoleColor.Cyan);
            ActionLog.Add(ActionLogType.DutyChanged, "ON DUTY", Context.CallerId.m_SteamID);

            // PlayerManager.NetCalls.SendDutyChanged.NetInvoke(Context.CallerId.m_SteamID, true);

            GiveAdminPermissions(Context.Player, isAdmin);
        }
    }
    private void ClearAdminPermissions(WarfarePlayer player)
    {
        if (player.UnturnedPlayer != null)
        {
            if (player.UnturnedPlayer.look != null)
            {
                player.UnturnedPlayer.look.sendFreecamAllowed(false);
                player.UnturnedPlayer.look.sendWorkzoneAllowed(false);
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
    }
    private void GiveAdminPermissions(WarfarePlayer player, bool isAdmin)
    {
        if (player.UnturnedPlayer.look != null)
        {
            player.UnturnedPlayer.look.sendFreecamAllowed(isAdmin);
            player.UnturnedPlayer.look.sendWorkzoneAllowed(isAdmin);
        }

        _signs.UpdateSigns<KitSignInstanceProvider>(player);
    }
}

public class DutyCommandTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Commands/Duty";

    [TranslationData("Sent to a player when they go on duty.", IsPriorityTranslation = false)]
    public readonly Translation DutyOnFeedback = new Translation("<#c6d4b8>You are now <#95ff4a>on duty</color>.");

    [TranslationData("Sent to a player when they go off duty.", IsPriorityTranslation = false)]
    public readonly Translation DutyOffFeedback = new Translation("<#c6d4b8>You are now <#ff8c4a>off duty</color>.");

    [TranslationData("Sent to all players when a player goes on duty (gains permissions).")]
    public readonly Translation<IPlayer> DutyOnBroadcast = new Translation<IPlayer>("<#c6d4b8><#d9e882>{0}</color> is now <#95ff4a>on duty</color>.");

    [TranslationData("Sent to all players when a player goes off duty (loses permissions).")]
    public readonly Translation<IPlayer> DutyOffBroadcast = new Translation<IPlayer>("<#c6d4b8><#d9e882>{0}</color> is now <#ff8c4a>off duty</color>.");
}