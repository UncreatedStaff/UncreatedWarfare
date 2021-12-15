using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Commands
{
    internal class GroupCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "group";

        public string Help => "Join a group";

        public string Syntax => "/group <join [id]|create [name]|rename|delete> [name]";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string> { "uc.group" }; //.join, .create, .current

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = caller as UnturnedPlayer;
            if (!Data.Is(out ITeams gm))
            {
                player.Message("command_e_gamemode");
                return;
            }
            if (command.Length == 0)
            {
                if (player.HasPermission("uc.group.current"))
                {
                    GroupInfo info = GroupManager.getGroupInfo(player.Player.quests.groupID);
                    if (info == null)
                    {
                        player.Player.SendChat("not_in_group");
                    }
                    else
                    {
                        player.Player.SendChat("current_group", player.Player.quests.groupID.m_SteamID.ToString(Data.Locale), info.name);
                    }
                }
                else
                    player.Player.SendChat("no_permissions");
            }
            else if (command.Length == 2)
            {
                if (command[0].ToLower() == "create")
                {
                    if (player.HasPermission("uc.group.create"))
                    {
                        if (!player.Player.quests.hasPermissionToCreateGroup)
                        {
                            player.Player.SendChat("cant_create_group");
                            return;
                        }
                        player.Player.quests.ReceiveCreateGroupRequest();
                        player.Player.quests.ReceiveRenameGroupRequest(command[1]);
                        GroupManager.save();
                        player.Player.SendChat("created_group",
                            command[1], player.Player.quests.groupID.m_SteamID.ToString(Data.Locale));
                        F.Log(F.Translate("created_group_console", 0, out _, player.Player.channel.owner.playerID.playerName,
                            player.Player.channel.owner.playerID.steamID.m_SteamID.ToString(Data.Locale),
                            player.Player.quests.groupID.m_SteamID.ToString(Data.Locale), command[1]), ConsoleColor.Cyan);
                    }
                    else
                        player.Player.SendChat("no_permissions");
                }
                else if (command[0].ToLower() == "join")
                {
                    if (player.HasPermission("uc.group.join"))
                    {
                        if (!ulong.TryParse(command[1], System.Globalization.NumberStyles.Any, Data.Locale, out ulong ID))
                        {
                            player.SendChat("joined_group_not_found", command[1]);
                            return;
                        }
                        if (player.Player.quests.groupID.m_SteamID == ID)
                        {
                            player.SendChat("joined_already_in_group");
                            return;
                        }
                        GroupInfo groupInfo = GroupManager.getGroupInfo(new CSteamID(ID));
                        if (groupInfo == null)
                        {
                            player.SendChat("joined_group_not_found", ID.ToString(Data.Locale));
                            return;
                        }
                        ulong oldgroup = player.Player.quests.groupID.m_SteamID;
                        if (player.Player.quests.ServerAssignToGroup(groupInfo.groupID, EPlayerGroupRank.MEMBER, true))
                        {
                            GroupManager.save();
                            EventFunctions.OnGroupChangedInvoke(player.Player.channel.owner, oldgroup, groupInfo.groupID.m_SteamID);
                            ulong team = player.GetTeam();
                            if (gm.JoinManager != null)
                                gm.JoinManager.UpdatePlayer(player.Player);
                            if (team == 0) team = player.Player.quests.groupID.m_SteamID;
                            if (team == 1)
                            {
                                player.SendChat("joined_group", TeamManager.TranslateName(TeamManager.Team1ID, player, true),
                                    groupInfo.groupID.m_SteamID.ToString(Data.Locale));
                                F.Log(F.Translate("joined_group_console", 0, out _, player.Player.channel.owner.playerID.playerName,
                                    player.Player.channel.owner.playerID.steamID.m_SteamID.ToString(Data.Locale), TeamManager.TranslateName(TeamManager.Team1ID, 0),
                                    groupInfo.groupID.m_SteamID.ToString(Data.Locale)), ConsoleColor.Cyan);
                            }
                            else if (team == 2)
                            {
                                player.SendChat("joined_group", TeamManager.TranslateName(TeamManager.Team2ID, player, true),
                                    groupInfo.groupID.m_SteamID.ToString(Data.Locale));
                                F.Log(F.Translate("joined_group_console", 0, out _, player.Player.channel.owner.playerID.playerName,
                                    player.Player.channel.owner.playerID.steamID.m_SteamID.ToString(Data.Locale), TeamManager.TranslateName(TeamManager.Team2ID, 0),
                                    groupInfo.groupID.m_SteamID.ToString(Data.Locale)), ConsoleColor.Cyan);
                            }
                            else if (team == 3)
                            {
                                player.SendChat("joined_group", TeamManager.TranslateName(TeamManager.AdminID, player, true),
                                    groupInfo.groupID.m_SteamID.ToString(Data.Locale));
                                F.Log(F.Translate("joined_group_console", 0, out _, player.Player.channel.owner.playerID.playerName,
                                    player.Player.channel.owner.playerID.steamID.m_SteamID.ToString(Data.Locale), TeamManager.TranslateName(TeamManager.AdminID, 0),
                                    groupInfo.groupID.m_SteamID.ToString(Data.Locale)), ConsoleColor.Cyan);
                            }
                            else
                            {
                                player.SendChat("joined_group_not_found", ID.ToString(Data.Locale));
                            }
                        }
                        else
                        {
                            player.SendChat("joined_group_not_found", ID.ToString(Data.Locale));
                        }
                    }
                    else
                        player.Player.SendChat("no_permissions");
                }
            }
            else
            {
                player.SendChat("group_usage");
            }
        }
    }
}
