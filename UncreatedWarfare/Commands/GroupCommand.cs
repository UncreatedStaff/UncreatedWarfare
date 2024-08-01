using System;
using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Commands;

[Command("group")]
[HelpMetadata(nameof(GetHelpMetadata))]
public class GroupCommand : IExecutableCommand
{
    private const string Syntax = "/group [join <team>]";
    private const string Help = "View or manage your current group/team.";

    private static readonly PermissionLeaf PermissionJoin = new PermissionLeaf("commands.group.join", unturned: false, warfare: true);

    /// <inheritdoc />
    public CommandContext Context { get; set; }

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

        Context.AssertGamemode(out ITeams gm);

        Context.AssertHelpCheck(0, Syntax + " - " + Help);

        if (Context.MatchParameter(0, "join"))
        {
            Context.AssertHelpCheck(1, "/group join <team>");

            Context.AssertArgs(2, "/group join <team>");

            await Context.AssertPermissions(PermissionJoin, token);
            await UniTask.SwitchToMainThread(token);

            if (!Context.TryGet(1, out ulong groupId))
            {
                string groupInput = Context.Get(1)!;
                FactionInfo? faction = TeamManager.FindFactionInfo(groupInput);
                
                if (faction == null)
                    throw Context.Reply(T.GroupNotFound, groupInput);

                if (faction.FactionId.Equals(TeamManager.Team1Faction.FactionId, StringComparison.Ordinal))
                    groupId = TeamManager.Team1ID;
                else if (faction.FactionId.Equals(TeamManager.Team2Faction.FactionId, StringComparison.Ordinal))
                    groupId = TeamManager.Team2ID;
                else if (faction.FactionId.Equals(TeamManager.AdminFaction.FactionId, StringComparison.Ordinal))
                    groupId = TeamManager.AdminID;
                else
                    throw Context.Reply(T.GroupNotFound, groupInput);
            }

            if (groupId is > 0 and < 4)
            {
                groupId = TeamManager.GetGroupID(groupId);
            }

            if (Context.Player.UnturnedPlayer.quests.groupID.m_SteamID == groupId)
            {
                throw Context.Reply(T.AlreadyInGroup);
            }

            GroupInfo groupInfo = GroupManager.getGroupInfo(new CSteamID(groupId));

            if (groupInfo == null)
            {
                throw Context.Reply(T.GroupNotFound, groupId.ToString(Data.LocalLocale));
            }

            if (!Context.Player.UnturnedPlayer.quests.ServerAssignToGroup(groupInfo.groupID, EPlayerGroupRank.MEMBER, true))
            {
                throw Context.Reply(T.GroupNotFound, groupId.ToString(Data.LocalLocale));
            }

            GroupManager.save();

            ulong team = Context.Player.GetTeam();
            if (gm.TeamSelector != null)
            {
                gm.TeamSelector.ForceUpdate();
            }

            if (team == 0)
            {
                team = Context.Player.UnturnedPlayer.quests.groupID.m_SteamID;
            }

            if (team is > 0 and < 4)
            {
                Context.Reply(T.JoinedGroup, team, TeamManager.TranslateName(team, Context.Player, true),
                    TeamManager.GetTeamColor(team));
                L.Log($"{Context.Player.Name.PlayerName} ({Context.CallerId.m_SteamID}) joined group \"{TeamManager.TranslateName(team)}\": {team} (ID {groupInfo.groupID}).", ConsoleColor.Cyan);
                Context.LogAction(ActionLogType.ChangeGroupWithCommand, "GROUP: " + TeamManager.TranslateName(team).ToUpper());
            }
            else
            {
                Context.Reply(T.GroupNotFound, groupId.ToString(Data.LocalLocale));
            }
        }
        else if (Context.ArgumentCount == 0)
        {
            GroupInfo info = GroupManager.getGroupInfo(Context.Player.UnturnedPlayer.quests.groupID);
            if (info == null)
                throw Context.Reply(T.NotInGroup);
            Context.Reply(T.CurrentGroup, Context.CallerId.m_SteamID, info.name, TeamManager.GetTeamColor(info.groupID.m_SteamID.GetTeam()));
        }
        else throw Context.SendCorrectUsage(Syntax + " - " + Help);
    }
}