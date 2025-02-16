using System.Globalization;
using System.Linq;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("join"), SubCommandOf(typeof(GroupCommand))]
internal sealed class GroupJoinCommand : IExecutableCommand
{
    private readonly GroupCommandTranslations _translations;
    private readonly ITeamManager<Team> _teamManager;
    private readonly IFactionDataStore _factionDataStore;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public GroupJoinCommand(TranslationInjection<GroupCommandTranslations> translations, ITeamManager<Team> teamManager, IFactionDataStore factionDataStore)
    {
        _teamManager = teamManager;
        _factionDataStore = factionDataStore;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        Context.AssertArgs(1);
        
        Team? newTeam;
        if (!Context.TryGet(0, out ulong groupId))
        {
            string groupInput = Context.Get(0)!;

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

        GroupInfo? groupInfo = GroupManager.getGroupInfo(new CSteamID(groupId));

        if (groupInfo == null)
        {
            if (newTeam != null && newTeam.IsValid)
                Context.Logger.LogError("Group info not found for group ID: {0}, team: {1}.", groupId, newTeam.Faction.Name);

            throw Context.Reply(_translations.GroupNotFound, groupId.ToString(Context.Culture));
        }

        if (newTeam.IsValid)
        {
            await _teamManager.JoinTeamAsync(Context.Player, newTeam, token);
            await UniTask.SwitchToMainThread(token);

            if (Context.Player.Team != newTeam)
            {
                throw Context.Reply(_translations.GroupNotFound, groupId.ToString(Context.Culture));
            }
        }
        else
        {
            if (!Context.Player.UnturnedPlayer.quests.ServerAssignToGroup(groupInfo.groupID, EPlayerGroupRank.MEMBER, true))
            {
                throw Context.Reply(_translations.GroupNotFound, groupId.ToString(Context.Culture));
            }
        }

        if (newTeam != null && newTeam.IsValid)
        {
            Context.Reply(_translations.JoinedGroup, newTeam.GroupId.m_SteamID, newTeam.Faction.Name, newTeam.Faction.Color);
            Context.Logger.LogInformation("{0} ({1}) joined group \"{2}\": {3} (ID {4}).", Context.Player.Names.GetDisplayNameOrPlayerName(), Context.CallerId, newTeam.Faction, newTeam, groupInfo.groupID);
            Context.LogAction(ActionLogType.ChangeGroupWithCommand, "GROUP: " + newTeam.Faction.Name.ToUpper());
        }
        else
        {
            Context.Reply(_translations.JoinedGroupNoName, groupInfo.groupID.m_SteamID);
            Context.Logger.LogInformation("{0} ({1}) joined group ID {2}.", Context.Player.Names.GetDisplayNameOrPlayerName(), Context.CallerId, groupInfo.groupID);
            Context.LogAction(ActionLogType.ChangeGroupWithCommand, "GROUP: " + groupInfo.groupID.m_SteamID.ToString("D17", CultureInfo.InvariantCulture));
        }
    }
}