using SDG.Unturned;
using Steamworks;
using System;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class GroupCommand : Command
{
    private const string Syntax = "/group [join <team>]";
    private const string Help = "View or manage your current group/team.";

    public GroupCommand() : base("group", EAdminType.MEMBER) { }

    public override void Execute(CommandInteraction ctx)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertRanByPlayer();

        ctx.AssertGamemode(out ITeams gm);

        ctx.AssertHelpCheck(0, Syntax + " - " + Help);

        if (ctx.MatchParameter(0, "join"))
        {
            ctx.AssertHelpCheck(1, "/group join <team>");

            ctx.AssertPermissions(EAdminType.MODERATOR);

            if (!ctx.TryGet(1, out ulong groupId))
                throw ctx.Reply(T.GroupNotFound, ctx.Get(1)!);

            if (groupId is > 0 and < 4)
                groupId = TeamManager.GetGroupID(groupId);

            if (ctx.Caller.Player.quests.groupID.m_SteamID == groupId)
                throw ctx.Reply(T.AlreadyInGroup);

            GroupInfo groupInfo = GroupManager.getGroupInfo(new CSteamID(groupId));

            if (groupInfo == null)
            {
                ctx.Reply(T.GroupNotFound, groupId.ToString(Data.LocalLocale));
                return;
            }
            if (ctx.Caller.Player.quests.ServerAssignToGroup(groupInfo.groupID, EPlayerGroupRank.MEMBER, true))
            {
                GroupManager.save();
                ulong team = ctx.Caller.GetTeam();
                if (gm.TeamSelector != null)
                    gm.TeamSelector.ForceUpdate();
                if (team == 0) team = ctx.Caller.Player.quests.groupID.m_SteamID;
                if (team is > 0 and < 4)
                {
                    ctx.Reply(T.JoinedGroup, team, TeamManager.TranslateName(team, ctx.Caller, true), TeamManager.GetTeamColor(team));
                    L.Log($"{ctx.Caller.Name.PlayerName} ({ctx.CallerID}) joined group \"{TeamManager.TranslateName(team, 0)}\": {team} (ID {groupInfo.groupID}).", ConsoleColor.Cyan);
                    ctx.LogAction(ActionLogType.ChangeGroupWithCommand, "GROUP: " + TeamManager.TranslateName(team, 0).ToUpper());
                }
                else
                {
                    ctx.Reply(T.GroupNotFound, groupId.ToString(Data.LocalLocale));
                }
            }
            else
            {
                ctx.Reply(T.GroupNotFound, groupId.ToString(Data.LocalLocale));
            }
        }
        else if (ctx.ArgumentCount == 0)
        {
            GroupInfo info = GroupManager.getGroupInfo(ctx.Caller.Player.quests.groupID);
            if (info == null)
                throw ctx.Reply(T.NotInGroup);
            ctx.Reply(T.CurrentGroup, ctx.CallerID, info.name, TeamManager.GetTeamColor(info.groupID.m_SteamID.GetTeam()));
        }
        else throw ctx.SendCorrectUsage(Syntax + " - " + Help);
    }
}
