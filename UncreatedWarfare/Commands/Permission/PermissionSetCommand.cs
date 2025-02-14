using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("set"), SubCommandOf(typeof(PermissionCommand)), SynchronizedCommand]
internal sealed class PermissionSetCommand : IExecutableCommand
{
    private readonly UserPermissionStore _permissionStore;
    private readonly IConfiguration _systemConfig;
    private readonly PermissionsTranslations _translations;
    private readonly IPlayerService _playerService;
    private readonly IUserDataService _userDataService;
    private readonly DutyService _dutyService;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public PermissionSetCommand(
        UserPermissionStore permissionStore,
        IConfiguration systemConfig,
        TranslationInjection<PermissionsTranslations> translations,
        IPlayerService playerService,
        IUserDataService userDataService,
        DutyService dutyService)
    {
        _permissionStore = permissionStore;
        _systemConfig = systemConfig;
        _playerService = playerService;
        _userDataService = userDataService;
        _dutyService = dutyService;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        // p set <player> <member | staff | trial | admin | owner> [display name]
        Context.AssertArgs(2);

        IPlayer player;

        await _playerService.TakePlayerConnectionLock(token);
        string? displayName;
        try
        {
            (CSteamID? steam64, WarfarePlayer? onlinePlayer) = await Context.TryGetPlayer(0, searchType: PlayerNameType.PlayerName);
            if (!steam64.HasValue)
            {
                throw Context.SendPlayerNotFound();
            }

            player = onlinePlayer ?? await _playerService.CreateOfflinePlayerAsync(steam64.Value, token);

            IConfigurationSection permSection = _systemConfig.GetSection("permissions");

            string staffOffDuty = permSection["staff_off_duty"] ?? throw Context.SendUnknownError();
            string trialOffDuty = permSection["trial_off_duty"] ?? throw Context.SendUnknownError();
            string adminOffDuty = permSection["admin_off_duty"] ?? throw Context.SendUnknownError();
            string staffOnDuty  = permSection["staff_on_duty"]  ?? throw Context.SendUnknownError();
            string trialOnDuty  = permSection["trial_on_duty"]  ?? throw Context.SendUnknownError();
            string adminOnDuty  = permSection["admin_on_duty"]  ?? throw Context.SendUnknownError();
            string owner        = permSection["owner"]          ?? throw Context.SendUnknownError();

            PermissionGroup grpStaffOffDuty = _permissionStore.PermissionGroups.FirstOrDefault(x => x.Id.Equals(staffOffDuty)) ?? throw Context.SendUnknownError();
            PermissionGroup grpTrialOffDuty = _permissionStore.PermissionGroups.FirstOrDefault(x => x.Id.Equals(trialOffDuty)) ?? throw Context.SendUnknownError();
            PermissionGroup grpAdminOffDuty = _permissionStore.PermissionGroups.FirstOrDefault(x => x.Id.Equals(adminOffDuty)) ?? throw Context.SendUnknownError();
            PermissionGroup grpStaffOnDuty  = _permissionStore.PermissionGroups.FirstOrDefault(x => x.Id.Equals(staffOnDuty))  ?? throw Context.SendUnknownError();
            PermissionGroup grpTrialOnDuty  = _permissionStore.PermissionGroups.FirstOrDefault(x => x.Id.Equals(trialOnDuty))  ?? throw Context.SendUnknownError();
            PermissionGroup grpAdminOnDuty  = _permissionStore.PermissionGroups.FirstOrDefault(x => x.Id.Equals(adminOnDuty))  ?? throw Context.SendUnknownError();
            PermissionGroup grpOwner        = _permissionStore.PermissionGroups.FirstOrDefault(x => x.Id.Equals(owner))        ?? throw Context.SendUnknownError();

            if (!Context.TryGet(2, out displayName) || (displayName = displayName.Trim()).Length <= 1)
            {
                displayName = null;
            }

            DutyLevel permissionLevel;
            IEnumerable<string> groupsToAdd;
            IEnumerable<string> groupsToRemove;
            if (Context.MatchParameter(1, "-"))
            {
                if (displayName != null)
                    goto setDisplayName;

                throw Context.Reply(_translations.DisplayNameNotSet);
            }
            if (Context.MatchParameter(1, "member", "player"))
            {
                permissionLevel = DutyLevel.Member;
                groupsToAdd = Array.Empty<string>();
                groupsToRemove = [ grpStaffOffDuty.Id, grpStaffOnDuty.Id, grpTrialOffDuty.Id, grpTrialOnDuty.Id, grpAdminOffDuty.Id, grpAdminOnDuty.Id, grpOwner.Id ];
            }
            else if (Context.MatchParameter(1, "staff"))
            {
                permissionLevel = DutyLevel.Staff;
                groupsToAdd = [ grpStaffOffDuty.Id ];
                groupsToRemove = [ grpStaffOnDuty.Id, grpTrialOffDuty.Id, grpTrialOnDuty.Id, grpAdminOffDuty.Id, grpAdminOnDuty.Id, grpOwner.Id ];
            }
            else if (Context.MatchParameter(1, "trial", "trialadmin", "trial_admin"))
            {
                permissionLevel = DutyLevel.TrialAdmin;
                groupsToAdd = [ grpStaffOffDuty.Id, grpTrialOffDuty.Id ];
                groupsToRemove = [ grpStaffOnDuty.Id, grpTrialOnDuty.Id, grpAdminOffDuty.Id, grpAdminOnDuty.Id, grpOwner.Id ];
            }
            else if (Context.MatchParameter(1, "admin"))
            {
                permissionLevel = DutyLevel.Admin;
                groupsToAdd = [ grpStaffOffDuty.Id, grpTrialOffDuty.Id, grpAdminOffDuty.Id ];
                groupsToRemove = [ grpStaffOnDuty.Id, grpTrialOnDuty.Id, grpAdminOnDuty.Id, grpOwner.Id ];
            }
            else if (Context.MatchParameter(1, "owner"))
            {
                if (Context.CallerId.m_SteamID is not 76561198267927009ul and not 76561198857595123ul || steam64.Value.m_SteamID is not 76561198267927009ul and not 76561198857595123ul)
                    throw Context.SendNoPermission();
                permissionLevel = DutyLevel.Owner;
                groupsToAdd = [ grpStaffOffDuty.Id, grpTrialOffDuty.Id, grpAdminOffDuty.Id, grpOwner.Id ];
                groupsToRemove = [ grpStaffOnDuty.Id, grpTrialOnDuty.Id, grpAdminOnDuty.Id ];
            }
            else
            {
                // no special permissions
                PermissionGroup? matchingGroup = _permissionStore.PermissionGroups.FirstOrDefault(x => Context.MatchParameter(1, x.Id));
                bool remove = Context.MatchParameter(2, "remove");
                if (matchingGroup == null)
                {
                    if (!PermissionBranch.TryParse(Context.Get(1)!, out PermissionBranch branch))
                        throw Context.Reply(_translations.GroupOrPermNotFound, Context.Get(1)!);

                    if (remove)
                    {
                        if (!await _permissionStore.RemovePermissionAsync(steam64.Value, branch, token))
                            throw Context.Reply(_translations.DoesntHavePermission, player, branch);

                        Context.Reply(_translations.RemovedPermission, player, branch);
                    }
                    else
                    {
                        if (!await _permissionStore.AddPermissionAsync(steam64.Value, branch, token))
                            throw Context.Reply(_translations.AlreadyHavePermission, player, branch);

                        Context.Reply(_translations.AddedPermission, player, branch);
                    }
                    return;
                }

                if (matchingGroup == grpStaffOffDuty
                    || matchingGroup == grpStaffOnDuty
                    || matchingGroup == grpTrialOffDuty
                    || matchingGroup == grpTrialOnDuty
                    || matchingGroup == grpAdminOffDuty
                    || matchingGroup == grpAdminOnDuty
                    || matchingGroup == grpOwner)
                {
                    throw Context.Reply(_translations.GroupOrPermNotFound, Context.Get(1)!);
                }

                if (remove)
                {
                    if (!await _permissionStore.RemovePermissionGroupAsync(steam64.Value, matchingGroup.Id, token))
                        throw Context.Reply(_translations.DoesntHavePermissionGroup, player, matchingGroup);

                    Context.Reply(_translations.RemovedPermissionGroup, player, matchingGroup);
                }
                else
                {
                    if (!await _permissionStore.AddPermissionGroupAsync(steam64.Value, matchingGroup.Id, token))
                        throw Context.Reply(_translations.AlreadyHavePermissionGroup, player, matchingGroup);

                    Context.Reply(_translations.AddedPermissionGroup, player, matchingGroup);
                }
                return;
            }

            IReadOnlyList<PermissionGroup> groups = await _permissionStore.GetPermissionGroupsAsync(steam64.Value, forceRedownload: true, token).ConfigureAwait(false);
            
            DutyLevel oldPermissionLevel = DutyLevel.Member;
            bool wasOnDuty = false;
            if (groups.Count != 0)
            {
                wasOnDuty = groups.Any(x => x == grpStaffOnDuty || x == grpTrialOnDuty || x == grpAdminOnDuty || x == grpOwner);
                if (steam64.Value.m_SteamID is 76561198267927009ul or 76561198857595123ul)
                {
                    oldPermissionLevel = DutyLevel.Owner;
                }
                else if (groups.Any(x => x == grpAdminOffDuty || x == grpAdminOnDuty))
                {
                    oldPermissionLevel = DutyLevel.Admin;
                }
                else if (groups.Any(x => x == grpTrialOffDuty || x == grpTrialOnDuty))
                {
                    oldPermissionLevel = DutyLevel.TrialAdmin;
                }
                else if (groups.Any(x => x == grpStaffOffDuty || x == grpStaffOnDuty))
                {
                    oldPermissionLevel = DutyLevel.Staff;
                }
            }

            if (onlinePlayer is { IsOnline: true } && wasOnDuty)
            {
                await _dutyService.ApplyOffDuty(onlinePlayer, oldPermissionLevel, token);
            }

            int changed = await _permissionStore.RemovePermissionGroupsAsync(steam64.Value, groupsToRemove, token);
            changed +=  await _permissionStore.AddPermissionGroupsAsync(steam64.Value, groupsToAdd, CancellationToken.None);

            if (changed == 0)
                Context.Reply(_translations.AlreadyHavePermissionGroupSpecial, player, permissionLevel);
            else
                Context.Reply(_translations.AddedPermissionGroupSpecial, player, permissionLevel);
        }
        finally
        {
            _playerService.ReleasePlayerConnectionLock();
        }

        if (displayName == null)
        {
            return;
        }

        setDisplayName:
        if (displayName.Equals("null", StringComparison.OrdinalIgnoreCase))
            displayName = null;

        string? oldDisplayName = null;
        await _userDataService.AddOrUpdateAsync(player.Steam64.m_SteamID, (data, dbContext) =>
        {
            oldDisplayName = data.DisplayName;
            data.DisplayName = displayName;
            dbContext.Update(data);
        }, token);

        if (oldDisplayName == null && displayName == null)
            return;

        if (oldDisplayName != null)
        {
            if (displayName == null)
                Context.Reply(_translations.RemovedDisplayName, player, oldDisplayName);
            else
                Context.Reply(_translations.UpdatedDisplayName, player, oldDisplayName, displayName);
        }
        else
            Context.Reply(_translations.AddedDisplayName, player, displayName!);
    }
}