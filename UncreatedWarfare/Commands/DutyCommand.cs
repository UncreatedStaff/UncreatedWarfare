using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Players.Management.Legacy;

namespace Uncreated.Warfare.Commands;

[SynchronizedCommand, Command("duty", "onduty", "offduty", "d")]
[HelpMetadata(nameof(GetHelpMetadata))]
public class DutyCommand : IExecutableCommand
{
    private readonly UserPermissionStore _permissions;
    private readonly WarfareModule _warfare;

    private const string Syntax = "/duty";
    private const string Help = "Swap your duty status between on and off. For admins and trial admins.";

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    internal DutyCommand(UserPermissionStore permissions, WarfareModule warfare)
    {
        _permissions = permissions;
        _warfare = warfare;
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

        Context.AssertHelpCheck(0, Syntax + " - " + Help);

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

            Context.Reply(T.DutyOffFeedback);
            Chat.Broadcast(LanguageSet.AllBut(Context.CallerId.m_SteamID), T.DutyOffBroadcast, Context.Player);

            L.Log($"{Context.Player.Name.PlayerName} ({Context.CallerId.m_SteamID.ToString(Data.AdminLocale)}) went off duty (admin: {isAdmin}, trial admin: {isTrial}, staff: {isStaff}).", ConsoleColor.Cyan);
            ActionLog.Add(ActionLogType.DutyChanged, "OFF DUTY", Context.CallerId.m_SteamID);

            PlayerManager.NetCalls.SendDutyChanged.NetInvoke(Context.CallerId.m_SteamID, false);
        }
        else
        {
            Context.Reply(T.DutyOnFeedback);
            Chat.Broadcast(LanguageSet.AllBut(Context.CallerId.m_SteamID), T.DutyOnBroadcast, Context.Player);

            L.Log($"{Context.Player.Name.PlayerName} ({Context.CallerId.m_SteamID.ToString(Data.AdminLocale)}) went on duty (admin: {isAdmin}, trial admin: {isTrial}, staff: {isStaff}).", ConsoleColor.Cyan);
            ActionLog.Add(ActionLogType.DutyChanged, "ON DUTY", Context.CallerId.m_SteamID);

            PlayerManager.NetCalls.SendDutyChanged.NetInvoke(Context.CallerId.m_SteamID, true);

            GiveAdminPermissions(Context.Player, isAdmin);
        }
    }
    private static void ClearAdminPermissions(UCPlayer player)
    {
        if (player.Player != null)
        {
            if (player.Player.look != null)
            {
                player.Player.look.sendFreecamAllowed(false);
                player.Player.look.sendWorkzoneAllowed(false);
            }

            if (player.Player.movement != null)
            {
                if (player.Player.movement.pluginSpeedMultiplier != 1f)
                    player.Player.movement.sendPluginSpeedMultiplier(1f);
                if (player.Player.movement.pluginJumpMultiplier != 1f)
                    player.Player.movement.sendPluginJumpMultiplier(1f);
            }
        }

        player.JumpOnPunch = false;
        player.VanishMode = false;
        player.GodMode = false;

        Signs.UpdateKitSigns(player, null);
        Signs.UpdateLoadoutSigns(player);
    }
    private static void GiveAdminPermissions(UCPlayer player, bool isAdmin)
    {
        if (player.Player.look != null)
        {
            player.Player.look.sendFreecamAllowed(isAdmin);
            player.Player.look.sendWorkzoneAllowed(isAdmin);
        }

        Signs.UpdateKitSigns(player, null);
        Signs.UpdateLoadoutSigns(player);
    }
    public static void SetVanishMode(Player player, bool vanished)
    {
    }
}