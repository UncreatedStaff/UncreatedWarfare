using System;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("group")]
[MetadataFile(nameof(GetHelpMetadata))]
public class GroupCommand : IExecutableCommand
{
    private const string Syntax = "/group [join <team>]";
    private const string Help = "View or manage your current group/team.";

    private readonly GroupCommandTranslations _translations;
    private readonly ITeamManager<Team> _teamManager;
    private readonly IFactionDataStore _factionDataStore;

    private static readonly PermissionLeaf PermissionJoin = new PermissionLeaf("commands.group.join", unturned: false, warfare: true);

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    public GroupCommand(TranslationInjection<GroupCommandTranslations> translations, ITeamManager<Team> teamManager, IFactionDataStore factionDataStore)
    {
        _teamManager = teamManager;
        _factionDataStore = factionDataStore;
        _translations = translations.Value;
    }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "View your current group or join a different one.",
            Parameters =
            [
                new CommandParameter("join", typeof(int), "1", "2", "3")
                {
                    Permission = PermissionJoin,
                    Description = "Join a group with a team number.",
                    IsOptional = true
                }
            ]
        };
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (Context.MatchParameter(0, "join"))
        {
            Context.AssertArgs(2, "/group join <team>");

            await Context.AssertPermissions(PermissionJoin, token);
            await UniTask.SwitchToMainThread(token);

            if (!Context.TryGet(1, out ulong groupId))
            {
                string groupInput = Context.Get(1)!;

                Team? newTeam = _teamManager.FindTeam(groupInput);
                if (newTeam == null)
                {
                    FactionInfo? faction = _factionDataStore.FindFaction(groupInput);

                    if (faction == null)
                        throw Context.Reply(_translations.GroupNotFound, groupInput);

                    _teamManager.FindTeam(faction.Name);
                    if (newTeam == null)
                        throw Context.Reply(_translations.GroupNotFound, groupInput);
                }

                groupId = newTeam.GroupId.m_SteamID;
            }

            // if (groupId is > 0 and < 4)
            // {
            //     groupId = TeamManager.GetGroupID(groupId);
            // }

            if (Context.Player.UnturnedPlayer.quests.groupID.m_SteamID == groupId)
            {
                throw Context.Reply(_translations.AlreadyInGroup);
            }

            GroupInfo groupInfo = GroupManager.getGroupInfo(new CSteamID(groupId));

            if (groupInfo == null)
            {
                throw Context.Reply(_translations.GroupNotFound, groupId.ToString(Data.LocalLocale));
            }

            if (!Context.Player.UnturnedPlayer.quests.ServerAssignToGroup(groupInfo.groupID, EPlayerGroupRank.MEMBER, true))
            {
                throw Context.Reply(_translations.GroupNotFound, groupId.ToString(Data.LocalLocale));
            }

            GroupManager.save();

            Team team = Context.Player.Team;
            // todo update team selector

            if (team.IsValid)
            {
                Context.Reply(_translations.JoinedGroup, team.GroupId.m_SteamID, team.Faction.Name, team.Faction.Color);
                L.Log($"{Context.Player.Names.PlayerName} ({Context.CallerId.m_SteamID}) joined group \"{team.Faction.Name}\": {team} (ID {groupInfo.groupID}).", ConsoleColor.Cyan);
                Context.LogAction(ActionLogType.ChangeGroupWithCommand, "GROUP: " + team.Faction.Name.ToUpper());
            }
            else
            {
                Context.Reply(_translations.GroupNotFound, groupId.ToString(Data.LocalLocale));
            }
        }
        else if (Context.ArgumentCount == 0)
        {
            GroupInfo info = GroupManager.getGroupInfo(Context.Player.UnturnedPlayer.quests.groupID);
            if (info == null)
                throw Context.Reply(_translations.NotInGroup);
            Context.Reply(_translations.CurrentGroup, Context.CallerId.m_SteamID, info.name, Color.white);
        }
        else throw Context.SendCorrectUsage(Syntax + " - " + Help);
    }
}

public class GroupCommandTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Group Command";

    [TranslationData("Output from /group, tells the player their current group.", "Group ID", "Group Name", "Team Color (if applicable)")]
    public readonly Translation<ulong, string, Color> CurrentGroup = new Translation<ulong, string, Color>("<#e6e3d5>Group <#{2}>{0}</color>: <#{2}>{1}</color>");

    [TranslationData("Output from /group join <id>.", "Group ID", "Group Name", "Team Color (if applicable)", IsPriorityTranslation = false)]
    public readonly Translation<ulong, string, Color> JoinedGroup = new Translation<ulong, string, Color>("<#e6e3d5>You have joined group <#{2}>{0}</color>: <#{2}>{1}</color>.");

    [TranslationData("Output from /group when the player is not in a group.", IsPriorityTranslation = false)]
    public readonly Translation NotInGroup = new Translation("<#ff8c69>You aren't in a group.");

    [TranslationData("Output from /group join <id> when the player is already in that group.", IsPriorityTranslation = false)]
    public readonly Translation AlreadyInGroup = new Translation("<#ff8c69>You are already in that group.");

    [TranslationData("Output from /group join <id> when the group is not found.", "Input", IsPriorityTranslation = false)]
    public readonly Translation<string> GroupNotFound = new Translation<string>("<#ff8c69>Could not find group <#4785ff>{0}</color>.");
}