using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[SynchronizedCommand, Command("duty", "onduty", "offduty", "d"), MetadataFile]
internal sealed class DutyCommand : IExecutableCommand
{
    private readonly UserPermissionStore _permissions;
    private readonly WarfareModule _warfare;
    private readonly DutyService _dutyService;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public DutyCommand(
        UserPermissionStore permissions,
        WarfareModule warfare,
        DutyService dutyService)
    {
        _permissions = permissions;
        _warfare = warfare;
        _dutyService = dutyService;
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
        string owner        = permSection["owner"         ] ?? throw Context.SendUnknownError();

        bool wasOnDuty = false;
        DutyLevel level = DutyLevel.Member;
        if (permGroups.Any(x => x.Id.Equals(owner, StringComparison.Ordinal)))
        {
            wasOnDuty = true;
            level = DutyLevel.Owner;
        }
        else if (Context.CallerId.m_SteamID is 76561198267927009ul or 76561198857595123ul)
        {
            level = DutyLevel.Owner;
        }
        else if (permGroups.Any(x => x.Id.Equals(adminOnDuty, StringComparison.Ordinal)))
        {
            wasOnDuty = true;
            level = DutyLevel.Admin;
        }
        else if (permGroups.Any(x => x.Id.Equals(adminOffDuty, StringComparison.Ordinal)))
        {
            level = DutyLevel.Admin;
        }
        else if (permGroups.Any(x => x.Id.Equals(trialOnDuty, StringComparison.Ordinal)))
        {
            wasOnDuty = true;
            level = DutyLevel.TrialAdmin;
        }
        else if (permGroups.Any(x => x.Id.Equals(trialOffDuty, StringComparison.Ordinal)))
        {
            level = DutyLevel.TrialAdmin;
        }
        else if (permGroups.Any(x => x.Id.Equals(staffOnDuty, StringComparison.Ordinal)))
        {
            wasOnDuty = true;
            level = DutyLevel.Staff;
        }
        else if (permGroups.Any(x => x.Id.Equals(staffOffDuty, StringComparison.Ordinal)))
        {
            level = DutyLevel.Staff;
        }

        if (level == DutyLevel.Member)
        {
            throw Context.SendNoPermission();
        }

        if (wasOnDuty)
        {
            await _permissions.RemovePermissionGroupsAsync(Context.CallerId, [ adminOnDuty, trialOnDuty, staffOnDuty, owner ], CancellationToken.None).ConfigureAwait(false);

            if (level is DutyLevel.Admin or DutyLevel.Owner)
            {
                await _permissions.AddPermissionGroupsAsync(Context.CallerId, [ adminOffDuty, trialOffDuty, staffOffDuty ], CancellationToken.None).ConfigureAwait(false);
            }
            else if (level == DutyLevel.TrialAdmin)
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

            if (level == DutyLevel.Owner)
            {
                await _permissions.AddPermissionGroupsAsync(Context.CallerId, [ adminOnDuty, trialOnDuty, staffOnDuty, owner ], CancellationToken.None).ConfigureAwait(false);
            }
            else if (level == DutyLevel.Admin)
            {
                await _permissions.AddPermissionGroupsAsync(Context.CallerId, [ adminOnDuty, trialOnDuty, staffOnDuty ], CancellationToken.None).ConfigureAwait(false);
            }
            else if (level == DutyLevel.TrialAdmin)
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
            await _dutyService.ApplyOffDuty(Context.Player, level, CancellationToken.None);
        }
        else
        {
            await _dutyService.ApplyOnDuty(Context.Player, level, CancellationToken.None);
        }

        Context.Defer();
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
    public readonly Translation<IPlayer> DutyOnBroadcast = new Translation<IPlayer>("<#c6d4b8><#d9e882>{0}</color> is now <#95ff4a>on duty</color>.", arg0Fmt: WarfarePlayer.FormatDisplayOrPlayerName);

    [TranslationData("Sent to all players when a player goes off duty (loses permissions).")]
    public readonly Translation<IPlayer> DutyOffBroadcast = new Translation<IPlayer>("<#c6d4b8><#d9e882>{0}</color> is now <#ff8c4a>off duty</color>.", arg0Fmt: WarfarePlayer.FormatDisplayOrPlayerName);
}