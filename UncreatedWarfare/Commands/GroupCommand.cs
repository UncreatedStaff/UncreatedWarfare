using System.Globalization;
using System.Linq;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("group")]
public class GroupCommand : IExecutableCommand
{
    private const string Syntax = "/group [join <team>]";
    private const string Help = "View or manage your current group/team.";

    private readonly GroupCommandTranslations _translations;
    private readonly ITeamManager<Team> _teamManager;
    private readonly IFactionDataStore _factionDataStore;
    private readonly ILogger<GroupCommand> _logger;

    private static readonly PermissionLeaf PermissionJoin = new PermissionLeaf("commands.group.join", unturned: false, warfare: true);

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    public GroupCommand(TranslationInjection<GroupCommandTranslations> translations, ITeamManager<Team> teamManager, IFactionDataStore factionDataStore, ILogger<GroupCommand> logger)
    {
        _teamManager = teamManager;
        _factionDataStore = factionDataStore;
        _logger = logger;
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

            Team? newTeam = null;
            if (!Context.TryGet(1, out ulong groupId))
            {
                string groupInput = Context.Get(1)!;

                newTeam = _teamManager.FindTeam(groupInput);
                if (newTeam == null)
                {
                    FactionInfo? faction = _factionDataStore.FindFaction(groupInput);

                    if (faction == null)
                    {
                        throw Context.Reply(_translations.GroupNotFound, groupInput);
                    }

                    newTeam = _teamManager.AllTeams.FirstOrDefault(x => x.Faction.PrimaryKey == faction.PrimaryKey);
                    if (newTeam == null)
                    {
                        throw Context.Reply(_translations.GroupNotFound, groupInput);
                    }
                }

                groupId = newTeam.GroupId.m_SteamID;
            }
            else
            {
                newTeam = _teamManager.GetTeam(new CSteamID(groupId));
            }

            if (Context.Player.UnturnedPlayer.quests.groupID.m_SteamID == groupId)
            {
                throw Context.Reply(_translations.AlreadyInGroup);
            }

            GroupInfo groupInfo = GroupManager.getGroupInfo(new CSteamID(groupId));

            if (groupInfo == null)
            {
                if (newTeam != null && newTeam.IsValid)
                    _logger.LogError("Group info not found for group ID: {0}, team: {1}.", groupId, newTeam.Faction.Name);

                throw Context.Reply(_translations.GroupNotFound, groupId.ToString(Data.LocalLocale));
            }

            if (!Context.Player.UnturnedPlayer.quests.ServerAssignToGroup(groupInfo.groupID, EPlayerGroupRank.MEMBER, true))
            {
                throw Context.Reply(_translations.GroupNotFound, groupId.ToString(Data.LocalLocale));
            }

            if (newTeam != null && newTeam.IsValid)
            {
                Context.Reply(_translations.JoinedGroup, newTeam.GroupId.m_SteamID, newTeam.Faction.Name, newTeam.Faction.Color);
                _logger.LogInformation("{0} ({1}) joined group \"{2}\": {3} (ID {4}).", Context.Player.Names.GetDisplayNameOrPlayerName(), Context.CallerId, newTeam.Faction, newTeam, groupInfo.groupID);
                Context.LogAction(ActionLogType.ChangeGroupWithCommand, "GROUP: " + newTeam.Faction.Name.ToUpper());
            }
            else
            {
                Context.Reply(_translations.JoinedGroupNoName, groupInfo.groupID.m_SteamID);
                _logger.LogInformation("{0} ({1}) joined group ID {2}.", Context.Player.Names.GetDisplayNameOrPlayerName(), Context.CallerId, groupInfo.groupID);
                Context.LogAction(ActionLogType.ChangeGroupWithCommand, "GROUP: " + groupInfo.groupID.m_SteamID.ToString("D17", CultureInfo.InvariantCulture));
            }
        }
        else if (Context.ArgumentCount == 0)
        {
            GroupInfo info = GroupManager.getGroupInfo(Context.Player.UnturnedPlayer.quests.groupID);
            if (info == null)
                throw Context.Reply(_translations.NotInGroup);
            Team team = _teamManager.GetTeam(info.groupID);
            Context.Reply(_translations.CurrentGroup, info.groupID.m_SteamID, info.name, team.Faction.Color);
        }
        else throw Context.SendCorrectUsage(Syntax + " - " + Help);
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