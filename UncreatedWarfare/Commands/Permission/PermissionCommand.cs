using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("permission", "p"), MetadataFile]
internal sealed class PermissionCommand : IExecutableCommand
{
    private readonly UserPermissionStore _permissionStore;
    private readonly PermissionsTranslations _translations;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public PermissionCommand(UserPermissionStore permissionStore, TranslationInjection<PermissionsTranslations> translations)
    {
        _permissionStore = permissionStore;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        Context.AssertArgsExact(0);

        IReadOnlyList<PermissionGroup> groups = await _permissionStore.GetPermissionGroupsAsync(Context.CallerId, forceRedownload: true, token: token);
        IReadOnlyList<PermissionBranch> perms = await _permissionStore.GetPermissionsAsync(Context.CallerId, forceRedownload: false, token: token);

        StringBuilder? sb = null;

        if (groups.Count == 1)
        {
            Context.Reply(_translations.SinglePermGroup, groups[0]);
        }
        else if (groups.Count > 0)
        {
            sb = new StringBuilder();
            sb.Append(_translations.MultiplePermGroupsHeader.Translate(Context.Player));
            for (int i = 0; i < groups.Count; i++)
            {
                PermissionGroup group = groups[i];
                if (i != 0)
                    sb.Append(", ");

                sb.Append(group.Translate(_translations.TranslationService, Context.Caller, canUseIMGUI: true));
            }

            Context.ReplyString(sb.ToString());
        }

        if (perms.Count == 1)
        {
            Context.Reply(_translations.SinglePermission, perms[0]);
        }
        else if (perms.Count > 0)
        {
            if (sb == null)
                sb = new StringBuilder();
            else
                sb.Clear();
            if (Context.IMGUI)
            {
                sb.Append(_translations.MultiplePermsHeader.Translate(Context.Player));
                for (int i = 0; i < perms.Count; i++)
                {
                    PermissionBranch branch = perms[i];
                    if (i != 0)
                        sb.Append(", ");

                    sb.Append(branch.Translate(_translations.TranslationService, Context.Caller, canUseIMGUI: true));
                }

                Context.ReplyString(sb.ToString());
            }
            else
            {
                sb.AppendLine(_translations.MultiplePermsHeader.Translate(Context.Player));
                for (int i = 0; i < perms.Count; i++)
                {
                    PermissionBranch branch = perms[i];
                    if (i != 0)
                    {
                        if (i % 2 == 1)
                            sb.Append("<pos=50%>");
                        else
                            sb.AppendLine();
                    }

                    sb.Append(branch.Translate(_translations.TranslationService, Context.Caller));
                }
            }

            Context.ReplyString(sb.ToString());
        }

        if (!Context.Responded)
        {
            Context.Reply(_translations.NoPermissionGroups);
        }
    }
}

public sealed class PermissionsTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Commands/Permissions";

    public readonly Translation<PermissionGroup> SinglePermGroup = new Translation<PermissionGroup>("<#ff99cc>You are in the {0} group.");
    
    public readonly Translation<PermissionBranch> SinglePermission = new Translation<PermissionBranch>("<#ff99cc>You individually have the {0} permission.");
    
    public readonly Translation MultiplePermGroupsHeader = new Translation("<#ff99cc>You are in the following groups: ");
    
    public readonly Translation MultiplePermsHeader = new Translation("<#ff99cc>You have the following individual permissions: ");

    public readonly Translation NoPermissionGroups = new Translation("<#a89791>You are not in any permission groups.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<string> GroupOrPermNotFound = new Translation<string>("<#a89791>Unable to find an individual permission or permission group by the name {0}.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<IPlayer, PermissionBranch> AlreadyHavePermission = new Translation<IPlayer, PermissionBranch>("<#a89791>{0} already has the permission {1}.", arg0Fmt: WarfarePlayer.FormatColoredPlayerName);

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<IPlayer, PermissionGroup> AlreadyHavePermissionGroup = new Translation<IPlayer, PermissionGroup>("<#a89791>{0} already has the permission group {1}.", arg0Fmt: WarfarePlayer.FormatColoredPlayerName);

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<IPlayer, PermissionBranch> DoesntHavePermission = new Translation<IPlayer, PermissionBranch>("<#a89791>{0} doesn't have the permission {1}.", arg0Fmt: WarfarePlayer.FormatColoredPlayerName);

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<IPlayer, PermissionGroup> DoesntHavePermissionGroup = new Translation<IPlayer, PermissionGroup>("<#a89791>{0} doesn't have the permission group {1}.", arg0Fmt: WarfarePlayer.FormatColoredPlayerName);

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<IPlayer, PermissionBranch> AddedPermission = new Translation<IPlayer, PermissionBranch>("<#ff99cc>Added permission {1} to {0}.", arg0Fmt: WarfarePlayer.FormatColoredPlayerName);

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<IPlayer, PermissionGroup> AddedPermissionGroup = new Translation<IPlayer, PermissionGroup>("<#ff99cc>Added permission group {1} to {0}.", arg0Fmt: WarfarePlayer.FormatColoredPlayerName);

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<IPlayer, PermissionBranch> RemovedPermission = new Translation<IPlayer, PermissionBranch>("<#ff99cc>Removed permission {1} from {0}.", arg0Fmt: WarfarePlayer.FormatColoredPlayerName);

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<IPlayer, PermissionGroup> RemovedPermissionGroup = new Translation<IPlayer, PermissionGroup>("<#ff99cc>Removed permission group {1} from {0}.", arg0Fmt: WarfarePlayer.FormatColoredPlayerName);

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<IPlayer, DutyLevel> AddedPermissionGroupSpecial = new Translation<IPlayer, DutyLevel>("<#ff99cc>Added {0} to special permission group <#cedcde>{1}</color>.", arg0Fmt: WarfarePlayer.FormatColoredPlayerName);

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<IPlayer, DutyLevel> AlreadyHavePermissionGroupSpecial = new Translation<IPlayer, DutyLevel>("<#a89791>{0} was already in special permission group <#cedcde>{1}</color>.", arg0Fmt: WarfarePlayer.FormatColoredPlayerName);

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<IPlayer, string, string> UpdatedDisplayName = new Translation<IPlayer, string, string>("<#ff99cc>Updated {0}'s display name: <#fff>\"{1}\"</color> -> <#fff>\"{2}\"</color>.", arg0Fmt: WarfarePlayer.FormatColoredPlayerName);

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<IPlayer, string> AddedDisplayName = new Translation<IPlayer, string>("<#ff99cc>Added a display name for {0}: <#fff>\"{1}\"</color>.", arg0Fmt: WarfarePlayer.FormatColoredPlayerName);

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<IPlayer, string> RemovedDisplayName = new Translation<IPlayer, string>("<#ff99cc>Removed {0}'s display name: <#fff>\"{1}\"</color>.", arg0Fmt: WarfarePlayer.FormatColoredPlayerName);

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation DisplayNameNotSet = new Translation("<#a89791>Nick name must be provided to skip permission set.");
}