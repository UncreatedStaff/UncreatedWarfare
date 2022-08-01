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
    private const string SYNTAX = "/group [join <team>]";
    private const string HELP = "View or manage your current group/team.";

    public GroupCommand() : base("group", EAdminType.MEMBER) { }

    public override void Execute(CommandInteraction ctx)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertRanByPlayer();

        ctx.AssertGamemode(out ITeams gm);

        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        if (ctx.MatchParameter(0, "join"))
        {
            ctx.AssertHelpCheck(1, "/group join <team>");

            ctx.AssertPermissions(EAdminType.MODERATOR);

            if (!ctx.TryGet(1, out ulong groupId))
                throw ctx.Reply("joined_group_not_found", ctx.Get(1)!);

            if (groupId > 0 && groupId < 4)
                groupId = TeamManager.GetGroupID(groupId);

            if (ctx.Caller!.Player.quests.groupID.m_SteamID == groupId)
                throw ctx.Reply("joined_already_in_group");

            GroupInfo groupInfo = GroupManager.getGroupInfo(new CSteamID(groupId));

            if (groupInfo == null)
            {
                ctx.Reply("joined_group_not_found", groupId.ToString(Data.Locale));
                return;
            }
            ulong oldgroup = ctx.Caller!.Player.quests.groupID.m_SteamID;
            if (ctx.Caller.Player.quests.ServerAssignToGroup(groupInfo.groupID, EPlayerGroupRank.MEMBER, true))
            {
                GroupManager.save();
                EventDispatcher.InvokeOnGroupChanged(ctx.Caller, oldgroup, groupInfo.groupID.m_SteamID);
                ulong team = ctx.Caller.GetTeam();
                if (gm.TeamSelector != null)
                    gm.TeamSelector.ForceUpdate();
                if (team == 0) team = ctx.Caller.Player.quests.groupID.m_SteamID;
                if (team > 0 && team < 4)
                {
                    ctx.Reply("joined_group", TeamManager.TranslateName(team, ctx.Caller, true),
                        groupInfo.groupID.m_SteamID.ToString(Data.Locale));
                    L.Log(Localization.Translate("joined_group_console", 0, out _, ctx.Caller.Player.channel.owner.playerID.playerName,
                        ctx.Caller.Player.channel.owner.playerID.steamID.m_SteamID.ToString(Data.Locale), TeamManager.TranslateName(team, 0),
                        groupInfo.groupID.m_SteamID.ToString(Data.Locale)), ConsoleColor.Cyan);
                    ctx.LogAction(EActionLogType.CHANGE_GROUP_WITH_COMMAND, "GROUP: " + TeamManager.TranslateName(team, 0).ToUpper());
                }
                else
                {
                    ctx.Reply("joined_group_not_found", groupId.ToString(Data.Locale));
                }
            }
            else
            {
                ctx.Reply("joined_group_not_found", groupId.ToString(Data.Locale));
            }
        }
        else if (ctx.ArgumentCount == 0)
        {
            GroupInfo info = GroupManager.getGroupInfo(ctx.Caller!.Player.quests.groupID);
            if (info == null)
                throw ctx.Reply("not_in_group");
            else
                ctx.Reply("current_group", ctx.CallerID.ToString(Data.Locale), info.name);
        }
        else throw ctx.SendCorrectUsage(SYNTAX + " - " + HELP);
    }
}
