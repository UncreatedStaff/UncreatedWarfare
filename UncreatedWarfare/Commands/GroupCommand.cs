using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Commands;

internal class GroupCommand : IRocketCommand
{
    private readonly List<string> _permissions = new List<string>(1) { "uc.group" }; //.join, .create, .current
    private readonly List<string> _aliases = new List<string>(0);
    public AllowedCaller AllowedCaller => AllowedCaller.Player;
    public string Name => "group";
    public string Help => "Join a group";
    public string Syntax => "/group - or - /group join <team>";
    public List<string> Aliases => _aliases;
    public List<string> Permissions => _permissions;
    public void Execute(IRocketPlayer caller, string[] command)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCCommandContext ctx = new UCCommandContext(caller, command);
        if (!ctx.IsConsoleReply()) return;
        if (!Data.Is(out ITeams gm))
        {
            ctx.Reply("command_e_gamemode");
            return;
        }
        if (ctx.MatchParameter(0, "join"))
        {
            if (ctx.HasPermissionOrReply("uc.group.join"))
            {
                if (!ctx.TryGet(1, out ulong groupId))
                {
                    ctx.Reply("joined_group_not_found", command[1]);
                    return;
                }

                if (groupId > 0 && groupId < 4)
                    groupId = TeamManager.GetGroupID(groupId);

                if (ctx.Caller!.Player.quests.groupID.m_SteamID == groupId)
                {
                    ctx.Reply("joined_already_in_group");
                    return;
                }
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
                    if (gm.JoinManager != null)
                        gm.JoinManager.UpdatePlayer(ctx.Caller);
                    if (team == 0) team = ctx.Caller.Player.quests.groupID.m_SteamID;
                    if (team > 0 && team < 4)
                    {
                        ctx.Reply("joined_group", TeamManager.TranslateName(team, ctx.Caller, true),
                            groupInfo.groupID.m_SteamID.ToString(Data.Locale));
                        L.Log(Translation.Translate("joined_group_console", 0, out _, ctx.Caller.Player.channel.owner.playerID.playerName,
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
        }
        else if (ctx.ArgumentCount == 0)
        {
            if (ctx.HasPermissionOrReply("uc.group.current"))
            {
                GroupInfo info = GroupManager.getGroupInfo(ctx.Caller!.Player.quests.groupID);
                if (info == null)
                {
                    ctx.Reply("not_in_group");
                }
                else
                {
                    ctx.Reply("current_group", ctx.CallerID.ToString(Data.Locale), info.name);
                }
            }
        }
        else ctx.SendCorrectUsage("/group - or - /group join <team>");
    }
}
