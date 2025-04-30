using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("group"), MetadataFile]
internal sealed class GroupCommand : IExecutableCommand
{
    private readonly GroupCommandTranslations _translations;
    private readonly ITeamManager<Team> _teamManager;
    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public GroupCommand(TranslationInjection<GroupCommandTranslations> translations, ITeamManager<Team> teamManager)
    {
        _teamManager = teamManager;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (Context.ArgumentCount > 0)
            throw Context.SendHelp();

        GroupInfo info = GroupManager.getGroupInfo(Context.Player.UnturnedPlayer.quests.groupID);
        if (info == null)
            throw Context.Reply(_translations.NotInGroup);
        Team team = _teamManager.GetTeam(info.groupID);
        Context.Reply(_translations.CurrentGroup, info.groupID.m_SteamID, info.name, team.Faction.Color);
        return UniTask.CompletedTask;
    }
}

public class GroupCommandTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Commands/Group";

    [TranslationData("Output from /group, tells the player their current group.", "Group ID", "Group Name", "Team Color (if applicable)")]
    public readonly Translation<ulong, string, Color> CurrentGroup = new Translation<ulong, string, Color>("<#e6e3d5>You are a member of group #<#{2}>{0}</color>: <#{2}>{1}</color>.");

    [TranslationData("Output from /group join <id>.", "Group ID", "Group Name", "Team Color (if applicable)", IsPriorityTranslation = false)]
    public readonly Translation<ulong, string, Color> JoinedGroup = new Translation<ulong, string, Color>("<#e6e3d5>You have joined group #<#{2}>{0}</color>: <#{2}>{1}</color>.");

    [TranslationData("Output from /group join <id>.", "Group ID", IsPriorityTranslation = false)]
    public readonly Translation<ulong> JoinedGroupNoName = new Translation<ulong>("<#e6e3d5>You have joined group #<#dddddd>{0}</color>.");

    [TranslationData("Output from /group when the player is not in a group.", IsPriorityTranslation = false)]
    public readonly Translation NotInGroup = new Translation("<#ff8c69>You aren't in a group.");

    [TranslationData("Output from /group join <id> when the player is already in that group.", IsPriorityTranslation = false)]
    public readonly Translation AlreadyInGroup = new Translation("<#ff8c69>You are already in that group.");

    [TranslationData("Output from /group join <id> when the group is not found.", "Input", IsPriorityTranslation = false)]
    public readonly Translation<string> GroupNotFound = new Translation<string>("<#ff8c69>Could not find group <#4785ff>{0}</color>.");
}